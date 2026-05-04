// FILE #060: TicketBooking.Api/Controllers/VehicleModelsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Fleet;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/fleet/vehicle-models")]
[Route("api/v{version:apiVersion}/tenant/fleet/vehicle-models")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX},{RoleNames.QLVT},{RoleNames.QLVMM},{RoleNames.QLTour}")]
public sealed class VehicleModelsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public VehicleModelsAdminController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] VehicleType? vehicleType = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        var allowedTypes = GetAllowedVehicleTypes();
        if (!User.IsInRole(RoleNames.Admin) && !_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác quản lý mẫu phương tiện." });

        if (!User.IsInRole(RoleNames.Admin) && vehicleType.HasValue && !allowedTypes.Contains(vehicleType.Value))
            return BadRequest(new { message = "Loại phương tiện không thuộc phạm vi tenant này." });

        IQueryable<VehicleModel> query = _db.VehicleModels;

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenantContext.HasTenant)
            query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        if (!User.IsInRole(RoleNames.Admin))
            query = query.Where(x => allowedTypes.Contains(x.VehicleType));
        else if (vehicleType.HasValue)
            query = query.Where(x => x.VehicleType == vehicleType.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Manufacturer.Contains(keyword) ||
                x.ModelName.Contains(keyword));
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.VehicleType,
                x.Manufacturer,
                x.ModelName,
                x.ModelYear,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        IQueryable<VehicleModel> query = _db.VehicleModels;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenantContext.HasTenant)
            query = query.Where(x => x.TenantId == _tenantContext.TenantId);
        if (!User.IsInRole(RoleNames.Admin))
            query = query.Where(x => GetAllowedVehicleTypes().Contains(x.VehicleType));

        var item = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(new { message = "Không tìm thấy mẫu phương tiện." });

        return Ok(item);
    }

    public sealed class UpsertVehicleModelRequest
    {
        public VehicleType VehicleType { get; set; }
        public string Manufacturer { get; set; } = "";
        public string ModelName { get; set; } = "";
        public int? ModelYear { get; set; }
        public string? MetadataJson { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertVehicleModelRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        Validate(req);
        if (!IsVehicleTypeAllowed(req.VehicleType))
            return BadRequest(new { message = "Loại phương tiện không thuộc phạm vi tenant này." });

        // Optional uniqueness: manufacturer+model+year per tenant+type
        var exists = await _db.VehicleModels.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.VehicleType == req.VehicleType &&
            x.Manufacturer == req.Manufacturer.Trim() &&
            x.ModelName == req.ModelName.Trim() &&
            x.ModelYear == req.ModelYear, ct);

        if (exists) return Conflict(new { message = "Mẫu phương tiện đã tồn tại trong tenant này." });

        var entity = new VehicleModel
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            VehicleType = req.VehicleType,
            Manufacturer = req.Manufacturer.Trim(),
            ModelName = req.ModelName.Trim(),
            ModelYear = req.ModelYear,
            MetadataJson = req.MetadataJson,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.VehicleModels.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertVehicleModelRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        Validate(req);
        if (!IsVehicleTypeAllowed(req.VehicleType))
            return BadRequest(new { message = "Loại phương tiện không thuộc phạm vi tenant này." });

        var entity = await _db.VehicleModels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Không tìm thấy mẫu phương tiện trong tenant này." });

        var exists = await _db.VehicleModels.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.VehicleType == req.VehicleType &&
            x.Manufacturer == req.Manufacturer.Trim() &&
            x.ModelName == req.ModelName.Trim() &&
            x.ModelYear == req.ModelYear &&
            x.Id != id, ct);

        if (exists) return Conflict(new { message = "Mẫu phương tiện đã tồn tại trong tenant này." });

        entity.VehicleType = req.VehicleType;
        entity.Manufacturer = req.Manufacturer.Trim();
        entity.ModelName = req.ModelName.Trim();
        entity.ModelYear = req.ModelYear;
        entity.MetadataJson = req.MetadataJson;
        entity.IsActive = req.IsActive;

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        var entity = await _db.VehicleModels
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);
        if (entity is null) return NotFound(new { message = "Không tìm thấy mẫu phương tiện trong tenant này." });
        if (!IsVehicleTypeAllowed(entity.VehicleType))
            return NotFound(new { message = "Không tìm thấy mẫu phương tiện trong phạm vi tenant này." });

        _db.VehicleModels.Remove(entity); // interceptor => soft delete
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        var entity = await _db.VehicleModels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Không tìm thấy mẫu phương tiện." });
        if (!IsVehicleTypeAllowed(entity.VehicleType))
            return NotFound(new { message = "Không tìm thấy mẫu phương tiện trong phạm vi tenant này." });

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    private static void Validate(UpsertVehicleModelRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (string.IsNullOrWhiteSpace(req.Manufacturer)) throw new InvalidOperationException("Hãng sản xuất là bắt buộc.");
        if (string.IsNullOrWhiteSpace(req.ModelName)) throw new InvalidOperationException("Tên mẫu là bắt buộc.");

        if (req.Manufacturer.Length > 100) throw new InvalidOperationException("Hãng sản xuất tối đa 100 ký tự.");
        if (req.ModelName.Length > 120) throw new InvalidOperationException("Tên mẫu tối đa 120 ký tự.");
        if (req.ModelYear.HasValue && (req.ModelYear.Value < 1900 || req.ModelYear.Value > DateTime.UtcNow.Year + 2))
            throw new InvalidOperationException("Năm sản xuất không hợp lệ.");
    }

    private bool IsVehicleTypeAllowed(VehicleType vehicleType)
        => User.IsInRole(RoleNames.Admin) || GetAllowedVehicleTypes().Contains(vehicleType);

    private VehicleType[] GetAllowedVehicleTypes()
    {
        if (User.IsInRole(RoleNames.Admin))
            return Enum.GetValues<VehicleType>();

        var values = new List<VehicleType>();
        if (User.IsInRole(RoleNames.QLNX))
            values.AddRange(new[] { VehicleType.Bus, VehicleType.TourBus });
        if (User.IsInRole(RoleNames.QLVT))
            values.Add(VehicleType.Train);
        if (User.IsInRole(RoleNames.QLVMM))
            values.Add(VehicleType.Airplane);
        if (User.IsInRole(RoleNames.QLTour))
            values.Add(VehicleType.TourBus);

        return values.Distinct().ToArray();
    }
}
