// FILE #297: TicketBooking.Api/Controllers/QlVtTrainTripStopTimesController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Train;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlvt/train/trip-stop-times")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLVT}")]
public sealed class QlVtTrainTripStopTimesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlVtTrainTripStopTimesController(AppDbContext db, ITenantContext tenantContext)
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

        IQueryable<TrainTrip> tripQuery = _db.TrainTrips;
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
                x.TrainNumber,
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
            return NotFound(new { message = "TrainTrip not found in this tenant." });

        IQueryable<TrainTripStopTime> query = _db.TrainTripStopTimes.Where(x => x.TripId == tripId);
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

        return Ok(new
        {
            trip,
            items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(
        Guid id,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        IQueryable<TrainTripStopTime> query = _db.TrainTripStopTimes;
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
            return NotFound(new { message = "TrainTripStopTime not found in this tenant." });

        return Ok(item);
    }

    public sealed class UpsertTrainTripStopTimeRequest
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
        [FromBody] UpsertTrainTripStopTimeRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();
        ValidateUpsert(req);

        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (trip is null)
            return BadRequest(new { message = "TripId is invalid for this tenant." });

        if (await HasActiveSeatOccupancyAsync(tripId, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

        await ValidateStopPointExistsAsync(req.StopPointId, ct);
        await EnsureStopIndexAvailableAsync(tripId, req.StopIndex, excludeId: null, ct);

        var entity = new TrainTripStopTime
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

        _db.TrainTripStopTimes.Add(entity);
        await _db.SaveChangesAsync(ct);

        await RefreshTripBoundaryTimesAsync(tripId, ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpsertTrainTripStopTimeRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();
        ValidateUpsert(req);

        var entity = await _db.TrainTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "TrainTripStopTime not found in this tenant." });

        if (await HasActiveSeatOccupancyAsync(entity.TripId, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

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

    public sealed class ReplaceTrainTripStopTimesRequest
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
        [FromBody] ReplaceTrainTripStopTimesRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && x.TenantId == _tenantContext.TenantId, ct);

        if (trip is null)
            return NotFound(new { message = "TrainTrip not found in this tenant." });

        if (await HasActiveSeatOccupancyAsync(tripId, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

        ValidateReplace(req);

        var ordered = req.Items.OrderBy(x => x.StopIndex).ToList();

        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].StopIndex != i)
                return BadRequest(new { message = "StopIndex must be continuous from 0..n-1." });
        }

        await ValidateStopPointsExistAsync(ordered.Select(x => x.StopPointId).Distinct().ToList(), ct);

        var existing = await _db.TrainTripStopTimes.IgnoreQueryFilters()
            .Where(x => x.TripId == tripId && x.TenantId == _tenantContext.TenantId)
            .OrderBy(x => x.StopIndex)
            .ToListAsync(ct);

        var now = DateTimeOffset.Now;
        var existingByIndex = new Dictionary<int, TrainTripStopTime>();

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
                _db.TrainTripStopTimes.Add(new TrainTripStopTime
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
            _db.TrainTripStopTimes.Remove(old);

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

        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && x.TenantId == _tenantContext.TenantId, ct);

        if (trip is null)
            return NotFound(new { message = "TrainTrip not found in this tenant." });

        if (await HasActiveSeatOccupancyAsync(tripId, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

        var routeStops = await _db.TrainRouteStops.IgnoreQueryFilters()
            .Where(x => x.RouteId == trip.RouteId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted)
            .OrderBy(x => x.StopIndex)
            .ToListAsync(ct);

        if (routeStops.Count < 2)
            return BadRequest(new { message = "TrainRouteStops must have at least 2 items before generating stop times." });

        var items = routeStops.Select(rs =>
        {
            DateTimeOffset? time = null;
            if (req.UseRouteStopMinutes && rs.MinutesFromStart.HasValue)
                time = req.DepartureAt.AddMinutes(rs.MinutesFromStart.Value);

            return new ReplaceTrainTripStopTimesRequest.Item
            {
                StopPointId = rs.StopPointId,
                StopIndex = rs.StopIndex,
                MinutesFromStart = rs.MinutesFromStart,
                ArriveAt = time,
                DepartAt = time,
                IsActive = rs.IsActive
            };
        }).ToList();

        items[0].ArriveAt = items[0].ArriveAt ?? req.DepartureAt;
        items[0].DepartAt = req.DepartureAt;

        var last = items[^1];
        if (!last.ArriveAt.HasValue && !last.DepartAt.HasValue)
            last.ArriveAt = req.DepartureAt;
        last.DepartAt = null;

        var replaceReq = new ReplaceTrainTripStopTimesRequest { Items = items };
        var result = await Replace(tripId, replaceReq, ct);

        if (result is not OkObjectResult)
            return result;

        var tripEntity = await _db.TrainTrips.IgnoreQueryFilters()
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

        var entity = await _db.TrainTripStopTimes
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "TrainTripStopTime not found." });

        if (entity.TenantId != _tenantContext.TenantId)
            return NotFound(new { message = "TrainTripStopTime not found in this tenant." });

        var tripId = entity.TripId;

        if (await HasActiveSeatOccupancyAsync(tripId, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

        _db.TrainTripStopTimes.Remove(entity);
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

        var entity = await _db.TrainTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "TrainTripStopTime not found." });

        if (await HasActiveSeatOccupancyAsync(entity.TripId, ct))
            return Conflict(new { message = TrainSeatOccupancySupport.TripMutationBlockedMessage });

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

    private static void ValidateUpsert(UpsertTrainTripStopTimeRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.StopPointId == Guid.Empty) throw new InvalidOperationException("StopPointId is required.");
        if (req.StopIndex < 0) throw new InvalidOperationException("StopIndex must be >= 0.");
        if (req.ArriveAt.HasValue && req.DepartAt.HasValue && req.ArriveAt.Value > req.DepartAt.Value)
            throw new InvalidOperationException("ArriveAt must be <= DepartAt.");
    }

    private static void ValidateReplace(ReplaceTrainTripStopTimesRequest req)
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
        var exists = await _db.TrainStopPoints.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == stopPointId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (!exists)
            throw new InvalidOperationException("StopPointId is invalid for this tenant.");
    }

    private async Task ValidateStopPointsExistAsync(List<Guid> stopPointIds, CancellationToken ct)
    {
        var count = await _db.TrainStopPoints.IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == _tenantContext.TenantId && stopPointIds.Contains(x.Id) && !x.IsDeleted, ct);

        if (count != stopPointIds.Count)
            throw new InvalidOperationException("One or more StopPointId does not exist in this tenant.");
    }

    private async Task EnsureStopIndexAvailableAsync(Guid tripId, int stopIndex, Guid? excludeId, CancellationToken ct)
    {
        var query = _db.TrainTripStopTimes.IgnoreQueryFilters()
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
        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && x.TenantId == _tenantContext.TenantId, ct);

        if (trip is null)
            return;

        var stopTimes = await _db.TrainTripStopTimes.IgnoreQueryFilters()
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
        => TrainSeatOccupancySupport.HasActiveSeatOccupancyAsync(
            _db,
            _tenantContext.TenantId!.Value,
            tripId,
            DateTimeOffset.Now,
            ct);
}
