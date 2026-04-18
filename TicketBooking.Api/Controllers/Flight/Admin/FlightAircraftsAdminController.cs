//FILE: TicketBooking.Api/Controllers/Admin/FlightAircraftsAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/flight/aircrafts")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class FlightAircraftsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public FlightAircraftsAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public sealed class AircraftUpsertRequest
    {
        public Guid AircraftModelId { get; set; }
        public Guid AirlineId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Registration { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] Guid? aircraftModelId,
        [FromQuery] Guid? airlineId,
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

        IQueryable<Aircraft> query = _db.FlightAircrafts.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (aircraftModelId.HasValue && aircraftModelId.Value != Guid.Empty)
            query = query.Where(x => x.AircraftModelId == aircraftModelId.Value);

        if (airlineId.HasValue && airlineId.Value != Guid.Empty)
            query = query.Where(x => x.AirlineId == airlineId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var uq = q.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.Code.ToUpper().Contains(uq) ||
                (x.Registration != null && x.Registration.ToUpper().Contains(uq)) ||
                (x.Name != null && x.Name.ToUpper().Contains(uq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Registration)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.AircraftModelId,
                x.AirlineId,
                x.Code,
                x.Registration,
                x.Name,
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

        IQueryable<Aircraft> query = _db.FlightAircrafts.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.AircraftModelId,
                x.AirlineId,
                x.Code,
                x.Registration,
                x.Name,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return NotFound(new { message = "Aircraft not found." });

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] AircraftUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        if (req.AircraftModelId == Guid.Empty)
            return BadRequest(new { message = "AircraftModelId is required." });

        if (req.AirlineId == Guid.Empty)
            return BadRequest(new { message = "AirlineId is required." });

        var code = NormalizeRequired(req.Code, 50);
        if (code is null)
            return BadRequest(new { message = "Code is required." });

        var registration = TrimOrNull(req.Registration, 30);
        var name = TrimOrNull(req.Name, 200);

        var modelExists = await _db.FlightAircraftModels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.AircraftModelId && !x.IsDeleted, ct);

        if (!modelExists)
            return BadRequest(new { message = "AircraftModelId not found." });

        var airlineExists = await _db.FlightAirlines.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.AirlineId && !x.IsDeleted, ct);

        if (!airlineExists)
            return BadRequest(new { message = "AirlineId not found." });

        var codeExists = await _db.FlightAircrafts.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (codeExists)
            return Conflict(new { message = $"Aircraft code '{code}' already exists." });

        if (!string.IsNullOrWhiteSpace(registration))
        {
            var registrationExists = await _db.FlightAircrafts.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Registration == registration, ct);

            if (registrationExists)
                return Conflict(new { message = $"Registration '{registration}' already exists." });
        }

        var entity = new Aircraft
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AircraftModelId = req.AircraftModelId,
            AirlineId = req.AirlineId,
            Code = code,
            Registration = registration,
            Name = name,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.FlightAircrafts.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new { entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] AircraftUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightAircrafts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Aircraft not found." });

        if (req.AircraftModelId == Guid.Empty)
            return BadRequest(new { message = "AircraftModelId is required." });

        if (req.AirlineId == Guid.Empty)
            return BadRequest(new { message = "AirlineId is required." });

        var code = NormalizeRequired(req.Code, 50);
        if (code is null)
            return BadRequest(new { message = "Code is required." });

        var registration = TrimOrNull(req.Registration, 30);
        var name = TrimOrNull(req.Name, 200);

        var modelExists = await _db.FlightAircraftModels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.AircraftModelId && !x.IsDeleted, ct);

        if (!modelExists)
            return BadRequest(new { message = "AircraftModelId not found." });

        var airlineExists = await _db.FlightAirlines.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.AirlineId && !x.IsDeleted, ct);

        if (!airlineExists)
            return BadRequest(new { message = "AirlineId not found." });

        var codeExists = await _db.FlightAircrafts.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code && x.Id != id, ct);

        if (codeExists)
            return Conflict(new { message = $"Aircraft code '{code}' already exists." });

        if (!string.IsNullOrWhiteSpace(registration))
        {
            var registrationExists = await _db.FlightAircrafts.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Registration == registration && x.Id != id, ct);

            if (registrationExists)
                return Conflict(new { message = $"Registration '{registration}' already exists." });
        }

        entity.AircraftModelId = req.AircraftModelId;
        entity.AirlineId = req.AirlineId;
        entity.Code = code;
        entity.Registration = registration;
        entity.Name = name;
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

        var entity = await _db.FlightAircrafts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Aircraft not found." });

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

        var entity = await _db.FlightAircrafts.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Aircraft not found." });

        if (entity.IsDeleted)
        {
            var codeExists = await _db.FlightAircrafts.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Code == entity.Code && x.Id != id && !x.IsDeleted, ct);

            if (codeExists)
                return Conflict(new { message = $"Cannot restore: aircraft code '{entity.Code}' already exists." });

            if (!string.IsNullOrWhiteSpace(entity.Registration))
            {
                var registrationExists = await _db.FlightAircrafts.IgnoreQueryFilters()
                    .AnyAsync(x => x.TenantId == tenantId && x.Registration == entity.Registration && x.Id != id && !x.IsDeleted, ct);

                if (registrationExists)
                    return Conflict(new { message = $"Cannot restore: registration '{entity.Registration}' already exists." });
            }

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

    private static string? TrimOrNull(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
