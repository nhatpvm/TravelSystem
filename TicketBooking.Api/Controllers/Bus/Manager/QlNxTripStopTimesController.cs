// FILE #283: TicketBooking.Api/Controllers/QlNxTripStopTimesController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Bus;
using TicketBooking.Domain.Bus;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlnx/bus/trip-stop-times")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX}")]
public sealed class QlNxTripStopTimesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlNxTripStopTimesController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet("trips/{tripId:guid}")]
    public async Task<IActionResult> ListByTrip(
        Guid tripId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        IQueryable<Trip> tripQuery = _db.BusTrips;
        if (includeDeleted)
            tripQuery = tripQuery.IgnoreQueryFilters();

        var trip = await tripQuery
            .AsNoTracking()
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
                x.IsActive,
                x.IsDeleted
            })
            .FirstOrDefaultAsync(x => x.Id == tripId && x.TenantId == _tenantContext.TenantId, ct);

        if (trip is null)
            return NotFound(new { message = "Trip not found in this tenant." });

        IQueryable<TripStopTime> query = _db.BusTripStopTimes.Where(x => x.TripId == tripId);
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        var items = await query
            .OrderBy(x => x.StopIndex)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.TripId,
                x.StopPointId,
                x.StopIndex,
                x.ArriveAt,
                x.DepartAt,
                x.MinutesFromStart,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        var ids = items.Select(x => x.Id).ToList();

        var pickupCounts = ids.Count == 0
            ? new Dictionary<Guid, int>()
            : await _db.BusTripStopPickupPoints.IgnoreQueryFilters()
                .Where(x => ids.Contains(x.TripStopTimeId) && x.TenantId == _tenantContext.TenantId && !x.IsDeleted)
                .GroupBy(x => x.TripStopTimeId)
                .Select(g => new { TripStopTimeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TripStopTimeId, x => x.Count, ct);

        var dropoffCounts = ids.Count == 0
            ? new Dictionary<Guid, int>()
            : await _db.BusTripStopDropoffPoints.IgnoreQueryFilters()
                .Where(x => ids.Contains(x.TripStopTimeId) && x.TenantId == _tenantContext.TenantId && !x.IsDeleted)
                .GroupBy(x => x.TripStopTimeId)
                .Select(g => new { TripStopTimeId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TripStopTimeId, x => x.Count, ct);

        var result = items.Select(x => new
        {
            x.Id,
            x.TenantId,
            x.TripId,
            x.StopPointId,
            x.StopIndex,
            x.ArriveAt,
            x.DepartAt,
            x.MinutesFromStart,
            x.IsActive,
            x.IsDeleted,
            x.CreatedAt,
            x.UpdatedAt,
            PickupPointCount = pickupCounts.TryGetValue(x.Id, out var p) ? p : 0,
            DropoffPointCount = dropoffCounts.TryGetValue(x.Id, out var d) ? d : 0
        });

        return Ok(new
        {
            trip,
            items = result
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(
        Guid id,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        IQueryable<TripStopTime> query = _db.BusTripStopTimes;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.TripId,
                x.StopPointId,
                x.StopIndex,
                x.ArriveAt,
                x.DepartAt,
                x.MinutesFromStart,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (item is null)
            return NotFound(new { message = "TripStopTime not found in this tenant." });

        var pickupPoints = await _db.BusTripStopPickupPoints.IgnoreQueryFilters()
            .Where(x => x.TripStopTimeId == id && x.TenantId == _tenantContext.TenantId && (!x.IsDeleted || includeDeleted))
            .OrderBy(x => x.SortOrder)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.AddressLine,
                x.Latitude,
                x.Longitude,
                x.IsDefault,
                x.SortOrder,
                x.IsActive,
                x.IsDeleted
            })
            .ToListAsync(ct);

        var dropoffPoints = await _db.BusTripStopDropoffPoints.IgnoreQueryFilters()
            .Where(x => x.TripStopTimeId == id && x.TenantId == _tenantContext.TenantId && (!x.IsDeleted || includeDeleted))
            .OrderBy(x => x.SortOrder)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.AddressLine,
                x.Latitude,
                x.Longitude,
                x.IsDefault,
                x.SortOrder,
                x.IsActive,
                x.IsDeleted
            })
            .ToListAsync(ct);

        return Ok(new
        {
            item,
            pickupPoints,
            dropoffPoints
        });
    }

    public sealed class UpsertTripStopTimeRequest
    {
        public Guid StopPointId { get; set; }
        public int StopIndex { get; set; }
        public DateTimeOffset? ArriveAt { get; set; }
        public DateTimeOffset? DepartAt { get; set; }
        public int? MinutesFromStart { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpPost("trips/{tripId:guid}")]
    public async Task<IActionResult> Create(
        Guid tripId,
        [FromBody] UpsertTripStopTimeRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();
        ValidateUpsert(req);

        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (trip is null)
            return BadRequest(new { message = "TripId is invalid for this tenant." });

        if (await HasActiveSeatOccupancyAsync(tripId, ct))
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });

        await ValidateStopPointExistsAsync(req.StopPointId, ct);
        await EnsureStopIndexAvailableAsync(tripId, req.StopIndex, excludeId: null, ct);

        var entity = new TripStopTime
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            TripId = tripId,
            StopPointId = req.StopPointId,
            StopIndex = req.StopIndex,
            ArriveAt = req.ArriveAt,
            DepartAt = req.DepartAt,
            MinutesFromStart = req.MinutesFromStart,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.BusTripStopTimes.Add(entity);
        await _db.SaveChangesAsync(ct);

        await RefreshTripBoundaryTimesAsync(tripId, ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpsertTripStopTimeRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();
        ValidateUpsert(req);

        var entity = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "TripStopTime not found in this tenant." });

        if (await HasActiveSeatOccupancyAsync(entity.TripId, ct))
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });

        await ValidateStopPointExistsAsync(req.StopPointId, ct);
        await EnsureStopIndexAvailableAsync(entity.TripId, req.StopIndex, id, ct);

        entity.StopPointId = req.StopPointId;
        entity.StopIndex = req.StopIndex;
        entity.ArriveAt = req.ArriveAt;
        entity.DepartAt = req.DepartAt;
        entity.MinutesFromStart = req.MinutesFromStart;
        entity.IsActive = req.IsActive;
        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        await RefreshTripBoundaryTimesAsync(entity.TripId, ct);

        return Ok(entity);
    }

    public sealed class ReplaceTripStopTimesRequest
    {
        public List<Item> Items { get; set; } = new();

        public sealed class Item
        {
            public Guid StopPointId { get; set; }
            public int StopIndex { get; set; }
            public DateTimeOffset? ArriveAt { get; set; }
            public DateTimeOffset? DepartAt { get; set; }
            public int? MinutesFromStart { get; set; }
            public bool IsActive { get; set; } = true;
        }
    }

    [HttpPut("trips/{tripId:guid}/replace")]
    public async Task<IActionResult> Replace(
        Guid tripId,
        [FromBody] ReplaceTripStopTimesRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && x.TenantId == _tenantContext.TenantId, ct);

        if (trip is null)
            return NotFound(new { message = "Trip not found in this tenant." });

        if (await HasActiveSeatOccupancyAsync(tripId, ct))
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });

        ValidateReplace(req);

        var ordered = req.Items.OrderBy(x => x.StopIndex).ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].StopIndex != i)
                return BadRequest(new { message = "StopIndex must be continuous from 0..n-1." });
        }

        await ValidateStopPointsExistAsync(ordered.Select(x => x.StopPointId).Distinct().ToList(), ct);

        var existing = await _db.BusTripStopTimes.IgnoreQueryFilters()
       .Where(x => x.TripId == tripId && x.TenantId == _tenantContext.TenantId)
       .ToListAsync(ct);

        var existingByIndex = new Dictionary<int, TripStopTime>();
        var now = DateTimeOffset.Now;

        foreach (var g in existing.GroupBy(x => x.StopIndex))
        {
            var keep = g.OrderByDescending(x => x.CreatedAt).First();
            existingByIndex[g.Key] = keep;

            foreach (var extra in g.Where(x => x.Id != keep.Id))
            {
                if (!extra.IsDeleted)
                {
                    extra.IsDeleted = true;
                    extra.UpdatedAt = now;
                }
            }
        }

        var keepIndices = new HashSet<int>();


        foreach (var item in ordered)
        {
            keepIndices.Add(item.StopIndex);

            if (existingByIndex.TryGetValue(item.StopIndex, out var row))
            {
                row.StopPointId = item.StopPointId;
                row.ArriveAt = item.ArriveAt;
                row.DepartAt = item.DepartAt;
                row.MinutesFromStart = item.MinutesFromStart;
                row.IsActive = item.IsActive;
                row.IsDeleted = false;
                row.UpdatedAt = now;
            }
            else
            {
                _db.BusTripStopTimes.Add(new TripStopTime
                {
                    Id = Guid.NewGuid(),
                    TenantId = _tenantContext.TenantId!.Value,
                    TripId = tripId,
                    StopPointId = item.StopPointId,
                    StopIndex = item.StopIndex,
                    ArriveAt = item.ArriveAt,
                    DepartAt = item.DepartAt,
                    MinutesFromStart = item.MinutesFromStart,
                    IsActive = item.IsActive,
                    IsDeleted = false,
                    CreatedAt = now
                });
            }
        }

        foreach (var old in existing.Where(x => !keepIndices.Contains(x.StopIndex)))
            _db.BusTripStopTimes.Remove(old);

        await _db.SaveChangesAsync(ct);
        await RefreshTripBoundaryTimesAsync(tripId, ct);

        return Ok(new { ok = true, count = ordered.Count });
    }

    public sealed class GenerateStopTimesFromRouteRequest
    {
        public DateTimeOffset DepartureAt { get; set; }
        public bool UseRouteStopMinutes { get; set; } = true;
    }

    [HttpPost("trips/{tripId:guid}/generate-from-route")]
    public async Task<IActionResult> GenerateFromRoute(
        Guid tripId,
        [FromBody] GenerateStopTimesFromRouteRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && x.TenantId == _tenantContext.TenantId, ct);

        if (trip is null)
            return NotFound(new { message = "Trip not found in this tenant." });

        if (await HasActiveSeatOccupancyAsync(tripId, ct))
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });

        var route = await _db.BusRoutes.IgnoreQueryFilters()
            .Where(x => x.Id == trip.RouteId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted)
            .Select(x => new { x.EstimatedMinutes })
            .FirstOrDefaultAsync(ct);

        if (route is null)
            return BadRequest(new { message = "Route is invalid for this trip." });

        var routeStops = await _db.BusRouteStops.IgnoreQueryFilters()
            .Where(x => x.RouteId == trip.RouteId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted)
            .OrderBy(x => x.StopIndex)
            .ToListAsync(ct);

        if (routeStops.Count < 2)
            return BadRequest(new { message = "RouteStops must have at least 2 items before generating stop times." });

        var departureAt = req.DepartureAt == default ? trip.DepartureAt : req.DepartureAt;
        var fallbackDuration = route.EstimatedMinutes > 0
            ? route.EstimatedMinutes
            : Math.Max(0, (int)Math.Round((trip.ArrivalAt - trip.DepartureAt).TotalMinutes));

        var items = routeStops.Select(rs =>
        {
            DateTimeOffset? time = null;
            if (req.UseRouteStopMinutes && rs.MinutesFromStart.HasValue)
                time = departureAt.AddMinutes(rs.MinutesFromStart.Value);

            return new ReplaceTripStopTimesRequest.Item
            {
                StopPointId = rs.StopPointId,
                StopIndex = rs.StopIndex,
                MinutesFromStart = rs.MinutesFromStart,
                ArriveAt = time,
                DepartAt = time,
                IsActive = rs.IsActive
            };
        }).ToList();

        items[0].ArriveAt = items[0].ArriveAt ?? departureAt;
        items[0].DepartAt = departureAt;

        var last = items[^1];
        if (!last.ArriveAt.HasValue && !last.DepartAt.HasValue)
            last.ArriveAt = departureAt.AddMinutes(fallbackDuration);
        last.DepartAt = null;

        var replaceReq = new ReplaceTripStopTimesRequest { Items = items };
        var result = await Replace(tripId, replaceReq, ct);

        if (result is not OkObjectResult)
            return result;

        var tripEntity = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && x.TenantId == _tenantContext.TenantId, ct);

        return Ok(new
        {
            ok = true,
            departureAt = tripEntity?.DepartureAt,
            arrivalAt = tripEntity?.ArrivalAt,
            count = items.Count
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(
        Guid id,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.BusTripStopTimes
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "TripStopTime not found." });

        if (entity.TenantId != _tenantContext.TenantId)
            return NotFound(new { message = "TripStopTime not found in this tenant." });

        var tripId = entity.TripId;

        if (await HasActiveSeatOccupancyAsync(tripId, ct))
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });

        _db.BusTripStopTimes.Remove(entity);
        await _db.SaveChangesAsync(ct);

        await RefreshTripBoundaryTimesAsync(tripId, ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(
        Guid id,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "TripStopTime not found." });

        if (await HasActiveSeatOccupancyAsync(entity.TripId, ct))
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });

        await EnsureStopIndexAvailableAsync(entity.TripId, entity.StopIndex, entity.Id, ct);

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        await RefreshTripBoundaryTimesAsync(entity.TripId, ct);

        return Ok(new { ok = true });
    }

    private void EnsureTenantScope()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required.");
    }

    private static void ValidateUpsert(UpsertTripStopTimeRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.StopPointId == Guid.Empty) throw new InvalidOperationException("StopPointId is required.");
        if (req.StopIndex < 0) throw new InvalidOperationException("StopIndex must be >= 0.");
        if (req.ArriveAt.HasValue && req.DepartAt.HasValue && req.ArriveAt.Value > req.DepartAt.Value)
            throw new InvalidOperationException("ArriveAt must be <= DepartAt.");
    }

    private static void ValidateReplace(ReplaceTripStopTimesRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.Items is null || req.Items.Count < 2)
            throw new InvalidOperationException("Items must contain at least 2 rows.");

        foreach (var item in req.Items)
        {
            if (item.StopPointId == Guid.Empty)
                throw new InvalidOperationException("StopPointId is required.");
            if (item.StopIndex < 0)
                throw new InvalidOperationException("StopIndex must be >= 0.");
            if (item.ArriveAt.HasValue && item.DepartAt.HasValue && item.ArriveAt.Value > item.DepartAt.Value)
                throw new InvalidOperationException("ArriveAt must be <= DepartAt.");
        }

        var ordered = req.Items.OrderBy(x => x.StopIndex).ToList();
        DateTimeOffset? previous = null;

        foreach (var item in ordered)
        {
            var current = item.DepartAt ?? item.ArriveAt;
            if (previous.HasValue && current.HasValue && current.Value < previous.Value)
                throw new InvalidOperationException("Stop times must be in chronological order.");
            previous = current ?? previous;
        }
    }

    private async Task ValidateStopPointExistsAsync(Guid stopPointId, CancellationToken ct)
    {
        var exists = await _db.BusStopPoints.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == stopPointId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (!exists)
            throw new InvalidOperationException("StopPointId is invalid for this tenant.");
    }

    private async Task ValidateStopPointsExistAsync(List<Guid> stopPointIds, CancellationToken ct)
    {
        var count = await _db.BusStopPoints.IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == _tenantContext.TenantId && stopPointIds.Contains(x.Id) && !x.IsDeleted, ct);

        if (count != stopPointIds.Count)
            throw new InvalidOperationException("One or more StopPointId does not exist in this tenant.");
    }

    private async Task EnsureStopIndexAvailableAsync(Guid tripId, int stopIndex, Guid? excludeId, CancellationToken ct)
    {
        var query = _db.BusTripStopTimes.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == _tenantContext.TenantId &&
                x.TripId == tripId &&
                x.StopIndex == stopIndex);

        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        var exists = await query.AnyAsync(ct);
        if (exists)
            throw new InvalidOperationException("StopIndex already exists for this trip.");
    }

    private async Task RefreshTripBoundaryTimesAsync(Guid tripId, CancellationToken ct)
    {
        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && x.TenantId == _tenantContext.TenantId, ct);

        if (trip is null)
            return;

        var stopTimes = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .Where(x => x.TripId == tripId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted)
            .OrderBy(x => x.StopIndex)
            .ToListAsync(ct);

        if (stopTimes.Count == 0)
            return;

        var first = stopTimes.First();
        var last = stopTimes.Last();

        trip.DepartureAt = first.DepartAt ?? first.ArriveAt ?? trip.DepartureAt;
        trip.ArrivalAt = last.ArriveAt ?? last.DepartAt ?? trip.ArrivalAt;
        trip.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
    }

    private Task<bool> HasActiveSeatOccupancyAsync(Guid tripId, CancellationToken ct)
        => BusSeatOccupancySupport.HasActiveSeatOccupancyAsync(
            _db,
            _tenantContext.TenantId!.Value,
            tripId,
            DateTimeOffset.Now,
            ct);
}
