//FILE: TicketBooking.Api/Controllers/Admin/FlightAircraftModelsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Flight;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/flight/aircraft-models")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class FlightAircraftModelsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public FlightAircraftModelsAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public sealed class AircraftModelUpsertRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int? TypicalSeatCapacity { get; set; }
        public string? MetadataJson { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);

        IQueryable<AircraftModel> query = _db.FlightAircraftModels.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var uq = q.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.Code.ToUpper().Contains(uq) ||
                x.Manufacturer.ToUpper().Contains(uq) ||
                x.Model.ToUpper().Contains(uq));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.Manufacturer)
            .ThenBy(x => x.Model)
            .ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Manufacturer,
                x.Model,
                x.TypicalSeatCapacity,
                x.MetadataJson,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new
        {
            page,
            pageSize,
            total,
            items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        IQueryable<AircraftModel> query = _db.FlightAircraftModels.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Manufacturer,
                x.Model,
                x.TypicalSeatCapacity,
                x.MetadataJson,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return NotFound(new { message = "Aircraft model not found." });

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] AircraftModelUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var code = NormalizeRequired(req.Code, 50);
        var manufacturer = NormalizeRequired(req.Manufacturer, 100);
        var model = NormalizeRequired(req.Model, 100);

        if (code is null)
            return BadRequest(new { message = "Code is required." });

        if (manufacturer is null)
            return BadRequest(new { message = "Manufacturer is required." });

        if (model is null)
            return BadRequest(new { message = "Model is required." });

        if (req.TypicalSeatCapacity.HasValue && req.TypicalSeatCapacity.Value < 0)
            return BadRequest(new { message = "TypicalSeatCapacity must be >= 0." });

        var codeExists = await _db.FlightAircraftModels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (codeExists)
            return Conflict(new { message = $"Aircraft model code '{code}' already exists." });

        var entity = new AircraftModel
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Manufacturer = manufacturer,
            Model = model,
            TypicalSeatCapacity = req.TypicalSeatCapacity,
            MetadataJson = NormalizeJson(req.MetadataJson),
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.FlightAircraftModels.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new { entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] AircraftModelUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightAircraftModels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Aircraft model not found." });

        var code = NormalizeRequired(req.Code, 50);
        var manufacturer = NormalizeRequired(req.Manufacturer, 100);
        var model = NormalizeRequired(req.Model, 100);

        if (code is null)
            return BadRequest(new { message = "Code is required." });

        if (manufacturer is null)
            return BadRequest(new { message = "Manufacturer is required." });

        if (model is null)
            return BadRequest(new { message = "Model is required." });

        if (req.TypicalSeatCapacity.HasValue && req.TypicalSeatCapacity.Value < 0)
            return BadRequest(new { message = "TypicalSeatCapacity must be >= 0." });

        var codeExists = await _db.FlightAircraftModels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code && x.Id != id, ct);

        if (codeExists)
            return Conflict(new { message = $"Aircraft model code '{code}' already exists." });

        entity.Code = code;
        entity.Manufacturer = manufacturer;
        entity.Model = model;
        entity.TypicalSeatCapacity = req.TypicalSeatCapacity;
        entity.MetadataJson = NormalizeJson(req.MetadataJson);
        entity.IsActive = req.IsActive;
        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightAircraftModels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Aircraft model not found." });

        if (!entity.IsDeleted)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTimeOffset.Now;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightAircraftModels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Aircraft model not found." });

        if (entity.IsDeleted)
        {
            var codeExists = await _db.FlightAircraftModels.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Code == entity.Code && x.Id != id && !x.IsDeleted, ct);

            if (codeExists)
                return Conflict(new { message = $"Cannot restore: aircraft model code '{entity.Code}' already exists." });

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTimeOffset.Now;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    private static string? NormalizeRequired(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? NormalizeJson(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        return input.Trim();
    }
}
