//FILE: TicketBooking.Api/Controllers/Admin/FlightsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Flight;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;
using FlightEntity = TicketBooking.Domain.Flight.Flight;

namespace TicketBooking.Api.Controllers.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/flight/flights")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class FlightsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public FlightsAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public sealed class FlightUpsertRequest
    {
        public Guid AirlineId { get; set; }
        public Guid AircraftId { get; set; }
        public Guid FromAirportId { get; set; }
        public Guid ToAirportId { get; set; }
        public string FlightNumber { get; set; } = string.Empty;
        public DateTimeOffset DepartureAt { get; set; }
        public DateTimeOffset ArrivalAt { get; set; }
        public FlightStatus Status { get; set; } = FlightStatus.Published;
        public bool IsActive { get; set; } = true;
        public string? Notes { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] Guid? airlineId,
        [FromQuery] Guid? aircraftId,
        [FromQuery] Guid? fromAirportId,
        [FromQuery] Guid? toAirportId,
        [FromQuery] DateOnly? date,
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

        IQueryable<FlightEntity> query = _db.FlightFlights.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (airlineId.HasValue && airlineId.Value != Guid.Empty)
            query = query.Where(x => x.AirlineId == airlineId.Value);

        if (aircraftId.HasValue && aircraftId.Value != Guid.Empty)
            query = query.Where(x => x.AircraftId == aircraftId.Value);

        if (fromAirportId.HasValue && fromAirportId.Value != Guid.Empty)
            query = query.Where(x => x.FromAirportId == fromAirportId.Value);

        if (toAirportId.HasValue && toAirportId.Value != Guid.Empty)
            query = query.Where(x => x.ToAirportId == toAirportId.Value);

        if (date.HasValue)
        {
            var start = new DateTimeOffset(
                date.Value.Year,
                date.Value.Month,
                date.Value.Day,
                0, 0, 0,
                TimeSpan.FromHours(7));

            var end = start.AddDays(1);
            query = query.Where(x => x.DepartureAt >= start && x.DepartureAt < end);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var uq = q.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.FlightNumber.ToUpper().Contains(uq) ||
                (x.Notes != null && x.Notes.ToUpper().Contains(uq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.DepartureAt)
            .ThenBy(x => x.FlightNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.AircraftId,
                x.FromAirportId,
                x.ToAirportId,
                x.FlightNumber,
                x.DepartureAt,
                x.ArrivalAt,
                Status = x.Status.ToString(),
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

        IQueryable<FlightEntity> query = _db.FlightFlights.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.AircraftId,
                x.FromAirportId,
                x.ToAirportId,
                x.FlightNumber,
                x.DepartureAt,
                x.ArrivalAt,
                Status = x.Status.ToString(),
                x.IsActive,
                x.IsDeleted,
                x.Notes,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return NotFound(new { message = "Flight not found." });

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] FlightUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var validation = await ValidateUpsertAsync(tenantId, req, idToExclude: null, ct);
        if (validation is not null)
            return validation;

        var flightNumber = NormalizeRequired(req.FlightNumber, 20)!;

        var entity = new FlightEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AirlineId = req.AirlineId,
            AircraftId = req.AircraftId,
            FromAirportId = req.FromAirportId,
            ToAirportId = req.ToAirportId,
            FlightNumber = flightNumber,
            DepartureAt = req.DepartureAt,
            ArrivalAt = req.ArrivalAt,
            Status = req.Status,
            IsActive = req.IsActive,
            Notes = TrimOrNull(req.Notes, 2000),
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.FlightFlights.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new { entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] FlightUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightFlights.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Flight not found." });

        var validation = await ValidateUpsertAsync(tenantId, req, id, ct);
        if (validation is not null)
            return validation;

        entity.AirlineId = req.AirlineId;
        entity.AircraftId = req.AircraftId;
        entity.FromAirportId = req.FromAirportId;
        entity.ToAirportId = req.ToAirportId;
        entity.FlightNumber = NormalizeRequired(req.FlightNumber, 20)!;
        entity.DepartureAt = req.DepartureAt;
        entity.ArrivalAt = req.ArrivalAt;
        entity.Status = req.Status;
        entity.IsActive = req.IsActive;
        entity.Notes = TrimOrNull(req.Notes, 2000);
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

        var entity = await _db.FlightFlights.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Flight not found." });

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

        var entity = await _db.FlightFlights.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Flight not found." });

        if (entity.IsDeleted)
        {
            var exists = await _db.FlightFlights.IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.TenantId == tenantId &&
                    x.FlightNumber == entity.FlightNumber &&
                    x.DepartureAt == entity.DepartureAt &&
                    x.Id != id &&
                    !x.IsDeleted, ct);

            if (exists)
                return Conflict(new { message = "Cannot restore: another flight with same FlightNumber and DepartureAt already exists." });

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTimeOffset.Now;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    private async Task<IActionResult?> ValidateUpsertAsync(
        Guid tenantId,
        FlightUpsertRequest req,
        Guid? idToExclude,
        CancellationToken ct)
    {
        if (req.AirlineId == Guid.Empty)
            return BadRequest(new { message = "AirlineId is required." });

        if (req.AircraftId == Guid.Empty)
            return BadRequest(new { message = "AircraftId is required." });

        if (req.FromAirportId == Guid.Empty)
            return BadRequest(new { message = "FromAirportId is required." });

        if (req.ToAirportId == Guid.Empty)
            return BadRequest(new { message = "ToAirportId is required." });

        if (req.FromAirportId == req.ToAirportId)
            return BadRequest(new { message = "FromAirportId and ToAirportId must be different." });

        var flightNumber = NormalizeRequired(req.FlightNumber, 20);
        if (flightNumber is null)
            return BadRequest(new { message = "FlightNumber is required." });

        if (req.ArrivalAt <= req.DepartureAt)
            return BadRequest(new { message = "ArrivalAt must be after DepartureAt." });

        var airlineExists = await _db.FlightAirlines.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.AirlineId && !x.IsDeleted, ct);

        if (!airlineExists)
            return BadRequest(new { message = "AirlineId not found." });

        var aircraft = await _db.FlightAircrafts.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.Id == req.AircraftId && !x.IsDeleted)
            .Select(x => new { x.Id, x.AirlineId })
            .FirstOrDefaultAsync(ct);

        if (aircraft is null)
            return BadRequest(new { message = "AircraftId not found." });

        if (aircraft.AirlineId != req.AirlineId)
            return BadRequest(new { message = "Aircraft does not belong to the specified AirlineId." });

        var fromAirportExists = await _db.FlightAirports.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.FromAirportId && !x.IsDeleted, ct);

        if (!fromAirportExists)
            return BadRequest(new { message = "FromAirportId not found." });

        var toAirportExists = await _db.FlightAirports.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.ToAirportId && !x.IsDeleted, ct);

        if (!toAirportExists)
            return BadRequest(new { message = "ToAirportId not found." });

        var exists = await _db.FlightFlights.IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.FlightNumber == flightNumber &&
                x.DepartureAt == req.DepartureAt &&
                (!idToExclude.HasValue || x.Id != idToExclude.Value), ct);

        if (exists)
            return Conflict(new { message = "A flight with the same FlightNumber and DepartureAt already exists." });

        return null;
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
