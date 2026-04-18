// FILE #063: TicketBooking.Api/Controllers/VehiclesAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Catalog;
using TicketBooking.Domain.Fleet;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/fleet/vehicles")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class VehiclesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public VehiclesAdminController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] VehicleType? vehicleType = null,
        [FromQuery] Guid? providerId = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        IQueryable<Vehicle> query = _db.Vehicles;

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenantContext.HasTenant)
            query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        if (vehicleType.HasValue)
            query = query.Where(x => x.VehicleType == vehicleType.Value);

        if (providerId.HasValue && providerId.Value != Guid.Empty)
            query = query.Where(x => x.ProviderId == providerId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword) ||
                                     (x.PlateNumber != null && x.PlateNumber.Contains(keyword)) ||
                                     (x.RegistrationNumber != null && x.RegistrationNumber.Contains(keyword)));
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.VehicleType,
                x.ProviderId,
                x.VehicleModelId,
                x.SeatMapId,
                x.Code,
                x.Name,
                ProviderName = _db.Providers.IgnoreQueryFilters()
                    .Where(p => p.Id == x.ProviderId)
                    .Select(p => p.Name)
                    .FirstOrDefault(),
                VehicleModelName = _db.VehicleModels.IgnoreQueryFilters()
                    .Where(m => m.Id == x.VehicleModelId)
                    .Select(m => m.Manufacturer + " " + m.ModelName)
                    .FirstOrDefault(),
                SeatMapName = _db.SeatMaps.IgnoreQueryFilters()
                    .Where(m => m.Id == x.SeatMapId)
                    .Select(m => m.Name)
                    .FirstOrDefault(),
                SeatMapSeatCount = _db.Seats.IgnoreQueryFilters()
                    .Count(s => s.SeatMapId == x.SeatMapId && !s.IsDeleted),
                x.SeatCapacity,
                x.PlateNumber,
                x.RegistrationNumber,
                x.Status,
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
        IQueryable<Vehicle> query = _db.Vehicles;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenantContext.HasTenant)
            query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        var item = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(new { message = "Không tìm thấy phương tiện." });

        var busDetail = await _db.BusVehicleDetails.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.VehicleId == id && !x.IsDeleted, ct);

        var provider = await _db.Providers.IgnoreQueryFilters()
            .Where(x => x.Id == item.ProviderId)
            .Select(x => new { x.Id, x.Name, x.Type })
            .FirstOrDefaultAsync(ct);

        var vehicleModel = item.VehicleModelId.HasValue
            ? await _db.VehicleModels.IgnoreQueryFilters()
                .Where(x => x.Id == item.VehicleModelId.Value)
                .Select(x => new { x.Id, x.VehicleType, x.Manufacturer, x.ModelName, x.ModelYear })
                .FirstOrDefaultAsync(ct)
            : null;

        var seatMap = item.SeatMapId.HasValue
            ? await _db.SeatMaps.IgnoreQueryFilters()
                .Where(x => x.Id == item.SeatMapId.Value)
                .Select(x => new { x.Id, x.VehicleType, x.Name, x.Code, x.TotalRows, x.TotalColumns, x.DeckCount })
                .FirstOrDefaultAsync(ct)
            : null;

        return Ok(new { vehicle = item, provider, vehicleModel, seatMap, busDetail });
    }

    public sealed class UpsertVehicleRequest
    {
        public VehicleType VehicleType { get; set; }
        public Guid ProviderId { get; set; }
        public Guid? VehicleModelId { get; set; }
        public Guid? SeatMapId { get; set; }

        public string Code { get; set; } = "";
        public string Name { get; set; } = "";

        public string? PlateNumber { get; set; }
        public string? RegistrationNumber { get; set; }

        public int SeatCapacity { get; set; }

        public DateTimeOffset? InServiceFrom { get; set; }
        public DateTimeOffset? InServiceTo { get; set; }
        public string? Status { get; set; }

        public bool IsActive { get; set; } = true;
        public string? MetadataJson { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertVehicleRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        Validate(req);
        var normalized = await ValidateVehicleRelationsAsync(req, ct);

        // uniqueness code per tenant+type
        var exists = await _db.Vehicles.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.VehicleType == req.VehicleType &&
            x.Code == normalized.Code, ct);

        if (exists) return Conflict(new { message = "Mã phương tiện đã tồn tại trong tenant này cho loại phương tiện đã chọn." });

        var entity = new Vehicle
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,

            VehicleType = req.VehicleType,
            ProviderId = normalized.ProviderId,
            VehicleModelId = normalized.VehicleModelId,
            SeatMapId = normalized.SeatMapId,

            Code = normalized.Code,
            Name = normalized.Name,

            PlateNumber = normalized.PlateNumber,
            RegistrationNumber = normalized.RegistrationNumber,

            SeatCapacity = req.SeatCapacity,

            InServiceFrom = req.InServiceFrom,
            InServiceTo = req.InServiceTo,
            Status = normalized.Status,

            IsActive = req.IsActive,
            MetadataJson = req.MetadataJson,

            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.Vehicles.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertVehicleRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        Validate(req);
        var normalized = await ValidateVehicleRelationsAsync(req, ct);

        var entity = await _db.Vehicles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Không tìm thấy phương tiện trong tenant này." });

        var exists = await _db.Vehicles.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.VehicleType == req.VehicleType &&
            x.Code == normalized.Code &&
            x.Id != id, ct);

        if (exists) return Conflict(new { message = "Mã phương tiện đã tồn tại trong tenant này cho loại phương tiện đã chọn." });

        entity.VehicleType = req.VehicleType;
        entity.ProviderId = normalized.ProviderId;
        entity.VehicleModelId = normalized.VehicleModelId;
        entity.SeatMapId = normalized.SeatMapId;

        entity.Code = normalized.Code;
        entity.Name = normalized.Name;

        entity.PlateNumber = normalized.PlateNumber;
        entity.RegistrationNumber = normalized.RegistrationNumber;

        entity.SeatCapacity = req.SeatCapacity;

        entity.InServiceFrom = req.InServiceFrom;
        entity.InServiceTo = req.InServiceTo;
        entity.Status = normalized.Status;

        entity.IsActive = req.IsActive;
        entity.MetadataJson = req.MetadataJson;

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

        var entity = await _db.Vehicles
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);
        if (entity is null) return NotFound(new { message = "Không tìm thấy phương tiện trong tenant này." });

        _db.Vehicles.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Cần gửi X-TenantId cho thao tác ghi của admin." });

        var entity = await _db.Vehicles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Không tìm thấy phương tiện." });

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    private static void Validate(UpsertVehicleRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.ProviderId == Guid.Empty) throw new InvalidOperationException("Đối tác là bắt buộc.");
        if (string.IsNullOrWhiteSpace(req.Code)) throw new InvalidOperationException("Mã là bắt buộc.");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new InvalidOperationException("Tên là bắt buộc.");
        if (req.Code.Length > 50) throw new InvalidOperationException("Mã tối đa 50 ký tự.");
        if (req.Name.Length > 200) throw new InvalidOperationException("Tên tối đa 200 ký tự.");
        if (req.SeatCapacity <= 0 || req.SeatCapacity > 500) throw new InvalidOperationException("Sức chứa ghế phải trong khoảng 1 đến 500.");
        if (req.PlateNumber is not null && req.PlateNumber.Length > 50) throw new InvalidOperationException("Biển số tối đa 50 ký tự.");
        if (req.RegistrationNumber is not null && req.RegistrationNumber.Length > 50) throw new InvalidOperationException("Số đăng kiểm tối đa 50 ký tự.");
        if (req.Status is not null && req.Status.Length > 50) throw new InvalidOperationException("Trạng thái tối đa 50 ký tự.");
    }

    private async Task<NormalizedVehicleRelations> ValidateVehicleRelationsAsync(UpsertVehicleRequest req, CancellationToken ct)
    {
        var providerId = req.ProviderId;
        var vehicleModelId = req.VehicleModelId.HasValue && req.VehicleModelId.Value != Guid.Empty ? req.VehicleModelId : null;
        var seatMapId = req.SeatMapId.HasValue && req.SeatMapId.Value != Guid.Empty ? req.SeatMapId : null;
        var code = req.Code.Trim();
        var name = req.Name.Trim();
        var plateNumber = req.PlateNumber?.Trim();
        var registrationNumber = req.RegistrationNumber?.Trim();
        var status = req.Status?.Trim();

        var provider = await _db.Providers.IgnoreQueryFilters()
            .Where(x => x.Id == providerId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted)
            .Select(x => new { x.Type })
            .FirstOrDefaultAsync(ct);

        if (provider is null)
            throw new InvalidOperationException("Đối tác không hợp lệ trong tenant này.");

        if (!IsProviderCompatible(provider.Type, req.VehicleType))
            throw new InvalidOperationException("Loại đối tác không tương thích với loại phương tiện.");

        if (vehicleModelId.HasValue)
        {
            var vehicleModel = await _db.VehicleModels.IgnoreQueryFilters()
                .Where(x => x.Id == vehicleModelId.Value && x.TenantId == _tenantContext.TenantId && !x.IsDeleted)
                .Select(x => new { x.VehicleType })
                .FirstOrDefaultAsync(ct);

            if (vehicleModel is null)
                throw new InvalidOperationException("Mẫu phương tiện không hợp lệ trong tenant này.");

            if (vehicleModel.VehicleType != req.VehicleType)
                throw new InvalidOperationException("Loại của mẫu phương tiện phải khớp với loại phương tiện.");
        }

        if (seatMapId.HasValue)
        {
            var seatMap = await _db.SeatMaps.IgnoreQueryFilters()
                .Where(x => x.Id == seatMapId.Value && x.TenantId == _tenantContext.TenantId && !x.IsDeleted)
                .Select(x => new { x.VehicleType })
                .FirstOrDefaultAsync(ct);

            if (seatMap is null)
                throw new InvalidOperationException("Sơ đồ ghế không hợp lệ trong tenant này.");

            if (seatMap.VehicleType != req.VehicleType)
                throw new InvalidOperationException("Loại của sơ đồ ghế phải khớp với loại phương tiện.");

            var seatCount = await _db.Seats.IgnoreQueryFilters()
                .CountAsync(x => x.SeatMapId == seatMapId.Value && !x.IsDeleted, ct);

            if (seatCount > 0 && req.SeatCapacity < seatCount)
                throw new InvalidOperationException("Sức chứa ghế không được nhỏ hơn số ghế hiện có của sơ đồ ghế.");
        }

        return new NormalizedVehicleRelations
        {
            ProviderId = providerId,
            VehicleModelId = vehicleModelId,
            SeatMapId = seatMapId,
            Code = code,
            Name = name,
            PlateNumber = plateNumber,
            RegistrationNumber = registrationNumber,
            Status = status
        };
    }

    private static bool IsProviderCompatible(ProviderType providerType, VehicleType vehicleType)
        => vehicleType switch
        {
            VehicleType.Bus => providerType == ProviderType.Bus,
            VehicleType.Train => providerType == ProviderType.Train,
            VehicleType.Airplane => providerType == ProviderType.Flight,
            VehicleType.TourBus => providerType == ProviderType.Bus || providerType == ProviderType.Tour,
            _ => false
        };

    private sealed class NormalizedVehicleRelations
    {
        public Guid ProviderId { get; init; }
        public Guid? VehicleModelId { get; init; }
        public Guid? SeatMapId { get; init; }
        public string Code { get; init; } = "";
        public string Name { get; init; } = "";
        public string? PlateNumber { get; init; }
        public string? RegistrationNumber { get; init; }
        public string? Status { get; init; }
    }
}
