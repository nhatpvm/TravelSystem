// FILE #281: TicketBooking.Api/Controllers/QlNxTripsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Bus;
using TicketBooking.Domain.Bus;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;
using TicketBooking.Domain.Fleet;


namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlnx/bus/trips")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX}")]
public sealed class QlNxTripsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlNxTripsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] Guid? providerId = null,
        [FromQuery] Guid? routeId = null,
        [FromQuery] Guid? vehicleId = null,
        [FromQuery] DateTimeOffset? fromDepartureAt = null,
        [FromQuery] DateTimeOffset? toDepartureAt = null,
        [FromQuery] TripStatus? status = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        IQueryable<Trip> query = _db.BusTrips;

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        if (providerId.HasValue && providerId.Value != Guid.Empty)
            query = query.Where(x => x.ProviderId == providerId.Value);

        if (routeId.HasValue && routeId.Value != Guid.Empty)
            query = query.Where(x => x.RouteId == routeId.Value);

        if (vehicleId.HasValue && vehicleId.Value != Guid.Empty)
            query = query.Where(x => x.VehicleId == vehicleId.Value);

        if (fromDepartureAt.HasValue)
            query = query.Where(x => x.DepartureAt >= fromDepartureAt.Value);

        if (toDepartureAt.HasValue)
            query = query.Where(x => x.DepartureAt <= toDepartureAt.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword));
        }

        var items = await query
            .OrderByDescending(x => x.DepartureAt)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.ProviderId,
                x.RouteId,
                x.VehicleId,
                x.Code,
                x.Name,
                x.Status,
                x.DepartureAt,
                x.ArrivalAt,
                x.FareRulesJson,
                x.BaggagePolicyJson,
                x.BoardingPolicyJson,
                x.Notes,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(
        Guid id,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        IQueryable<Trip> query = _db.BusTrips;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var trip = await query
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (trip is null)
            return NotFound(new { message = "Trip not found in this tenant." });

        var stopTimes = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .Where(x => x.TripId == id && x.TenantId == _tenantContext.TenantId && (!x.IsDeleted || includeDeleted))
            .OrderBy(x => x.StopIndex)
            .Select(x => new
            {
                x.Id,
                x.StopPointId,
                x.StopIndex,
                x.ArriveAt,
                x.DepartAt,
                x.MinutesFromStart,
                x.IsActive,
                x.IsDeleted
            })
            .ToListAsync(ct);

        var segmentPrices = await _db.BusTripSegmentPrices.IgnoreQueryFilters()
            .Where(x => x.TripId == id && x.TenantId == _tenantContext.TenantId && (!x.IsDeleted || includeDeleted))
            .OrderBy(x => x.FromStopIndex)
            .ThenBy(x => x.ToStopIndex)
            .Select(x => new
            {
                x.Id,
                x.FromTripStopTimeId,
                x.ToTripStopTimeId,
                x.FromStopIndex,
                x.ToStopIndex,
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice,
                x.IsActive,
                x.IsDeleted
            })
            .ToListAsync(ct);

        var activeHoldCount = await _db.BusTripSeatHolds.IgnoreQueryFilters()
            .CountAsync(x =>
                x.TripId == id &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted &&
                x.Status == SeatHoldStatus.Held &&
                x.HoldExpiresAt > DateTimeOffset.Now, ct);

        var activeSeatOccupancyCount = await BusSeatOccupancySupport.CountActiveSeatOccupancyAsync(
            _db,
            _tenantContext.TenantId!.Value,
            id,
            DateTimeOffset.Now,
            ct);

        return Ok(new
        {
            trip,
            stopTimes,
            segmentPrices,
            activeHoldCount,
            activeSeatOccupancyCount
        });
    }

    public sealed class UpsertTripRequest
    {
        public Guid ProviderId { get; set; }
        public Guid RouteId { get; set; }
        public Guid VehicleId { get; set; }

        public string Code { get; set; } = "";
        public string Name { get; set; } = "";

        public TripStatus Status { get; set; } = TripStatus.Published;

        public DateTimeOffset DepartureAt { get; set; }
        public DateTimeOffset ArrivalAt { get; set; }

        public string? FareRulesJson { get; set; }
        public string? BaggagePolicyJson { get; set; }
        public string? BoardingPolicyJson { get; set; }
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] UpsertTripRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();
        Validate(req);

        await ValidateTripRefsAsync(req.ProviderId, req.RouteId, req.VehicleId, ct);

        var exists = await _db.BusTrips.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.Code == req.Code.Trim(), ct);

        if (exists)
            return Conflict(new { message = "Trip Code already exists in this tenant." });

        var entity = new Trip
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            ProviderId = req.ProviderId,
            RouteId = req.RouteId,
            VehicleId = req.VehicleId,
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            Status = req.Status,
            DepartureAt = req.DepartureAt,
            ArrivalAt = req.ArrivalAt,
            FareRulesJson = req.FareRulesJson,
            BaggagePolicyJson = req.BaggagePolicyJson,
            BoardingPolicyJson = req.BoardingPolicyJson,
            Notes = req.Notes,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.BusTrips.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpsertTripRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();
        Validate(req);

        var entity = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Trip not found in this tenant." });

        await ValidateTripRefsAsync(req.ProviderId, req.RouteId, req.VehicleId, ct);

        var exists = await _db.BusTrips.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.Code == req.Code.Trim() &&
            x.Id != id, ct);

        if (exists)
            return Conflict(new { message = "Trip Code already exists in this tenant." });

        var modifiesTripShape =
            entity.ProviderId != req.ProviderId ||
            entity.RouteId != req.RouteId ||
            entity.VehicleId != req.VehicleId ||
            entity.DepartureAt != req.DepartureAt ||
            entity.ArrivalAt != req.ArrivalAt;

        if (modifiesTripShape &&
            await BusSeatOccupancySupport.HasActiveSeatOccupancyAsync(
                _db,
                _tenantContext.TenantId!.Value,
                id,
                DateTimeOffset.Now,
                ct))
        {
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });
        }

        entity.ProviderId = req.ProviderId;
        entity.RouteId = req.RouteId;
        entity.VehicleId = req.VehicleId;
        entity.Code = req.Code.Trim();
        entity.Name = req.Name.Trim();
        entity.Status = req.Status;
        entity.DepartureAt = req.DepartureAt;
        entity.ArrivalAt = req.ArrivalAt;
        entity.FareRulesJson = req.FareRulesJson;
        entity.BaggagePolicyJson = req.BaggagePolicyJson;
        entity.BoardingPolicyJson = req.BoardingPolicyJson;
        entity.Notes = req.Notes;
        entity.IsActive = req.IsActive;
        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.BusTrips
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Trip not found." });

        if (entity.TenantId != _tenantContext.TenantId)
            return NotFound(new { message = "Trip not found in this tenant." });

        if (await BusSeatOccupancySupport.HasActiveSeatOccupancyAsync(
                _db,
                _tenantContext.TenantId!.Value,
                id,
                DateTimeOffset.Now,
                ct))
        {
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });
        }

        _db.BusTrips.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Trip not found." });

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    private void EnsureTenantScope()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required.");
    }

    private static void Validate(UpsertTripRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.ProviderId == Guid.Empty) throw new InvalidOperationException("ProviderId is required.");
        if (req.RouteId == Guid.Empty) throw new InvalidOperationException("RouteId is required.");
        if (req.VehicleId == Guid.Empty) throw new InvalidOperationException("VehicleId is required.");
        if (string.IsNullOrWhiteSpace(req.Code)) throw new InvalidOperationException("Code is required.");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new InvalidOperationException("Name is required.");
        if (req.Code.Length > 50) throw new InvalidOperationException("Code max length is 50.");
        if (req.Name.Length > 200) throw new InvalidOperationException("Name max length is 200.");
        if (req.ArrivalAt < req.DepartureAt) throw new InvalidOperationException("ArrivalAt must be >= DepartureAt.");
    }

    private async Task ValidateTripRefsAsync(Guid providerId, Guid routeId, Guid vehicleId, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId!.Value;

        var providerOk = await _db.Providers.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == providerId && x.TenantId == tenantId && !x.IsDeleted, ct);
        if (!providerOk) throw new InvalidOperationException("ProviderId is invalid for this tenant.");

        var routeOk = await _db.BusRoutes.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == routeId && x.TenantId == tenantId && !x.IsDeleted, ct);
        if (!routeOk) throw new InvalidOperationException("RouteId is invalid for this tenant.");

        var vehicle = await _db.Vehicles.IgnoreQueryFilters()
     .FirstOrDefaultAsync(x => x.Id == vehicleId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (vehicle is null)
            throw new InvalidOperationException("VehicleId is invalid for this tenant.");

        if (vehicle.VehicleType != VehicleType.Bus && vehicle.VehicleType != VehicleType.TourBus)
            throw new InvalidOperationException("VehicleId must be a Bus/TourBus vehicle.");

    }
}
