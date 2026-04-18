// FILE #070: TicketBooking.Api/Controllers/BusTripsAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/bus/trips")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class BusTripsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public BusTripsAdminController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] Guid? providerId = null,
        [FromQuery] Guid? routeId = null,
        [FromQuery] DateTimeOffset? fromDepartureAt = null,
        [FromQuery] DateTimeOffset? toDepartureAt = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        IQueryable<Trip> query = _db.BusTrips;

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (providerId.HasValue && providerId.Value != Guid.Empty)
            query = query.Where(x => x.ProviderId == providerId.Value);

        if (routeId.HasValue && routeId.Value != Guid.Empty)
            query = query.Where(x => x.RouteId == routeId.Value);

        if (fromDepartureAt.HasValue)
            query = query.Where(x => x.DepartureAt >= fromDepartureAt.Value);

        if (toDepartureAt.HasValue)
            query = query.Where(x => x.DepartureAt <= toDepartureAt.Value);

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
        IQueryable<Trip> query = _db.BusTrips;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var trip = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (trip is null) return NotFound(new { message = "Trip not found." });

        var stopTimes = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .Where(x => x.TripId == id && !x.IsDeleted)
            .OrderBy(x => x.StopIndex)
            .Select(x => new
            {
                x.Id,
                x.StopIndex,
                x.StopPointId,
                x.ArriveAt,
                x.DepartAt,
                x.MinutesFromStart,
                x.IsActive
            })
            .ToListAsync(ct);

        var segmentCount = await _db.BusTripSegmentPrices.IgnoreQueryFilters()
            .CountAsync(x => x.TripId == id && !x.IsDeleted, ct);

        var holdCount = await _db.BusTripSeatHolds.IgnoreQueryFilters()
            .CountAsync(x => x.TripId == id && !x.IsDeleted, ct);

        var activeSeatOccupancyCount = await BusSeatOccupancySupport.CountActiveSeatOccupancyAsync(
            _db,
            trip.TenantId,
            id,
            DateTimeOffset.Now,
            ct);

        return Ok(new { trip, stopTimes, segmentCount, holdCount, activeSeatOccupancyCount });
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
    public async Task<IActionResult> Create([FromBody] UpsertTripRequest req, CancellationToken ct = default)
    {
        RequireTenantWrite();
        Validate(req);

        await ValidateTripRefsAsync(req.ProviderId, req.RouteId, req.VehicleId, ct);

        var exists = await _db.BusTrips.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.Code == req.Code.Trim(), ct);

        if (exists) return Conflict(new { message = "Trip Code already exists in this tenant." });

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
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertTripRequest req, CancellationToken ct = default)
    {
        RequireTenantWrite();
        Validate(req);

        var entity = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Trip not found in this tenant." });

        await ValidateTripRefsAsync(req.ProviderId, req.RouteId, req.VehicleId, ct);

        var exists = await _db.BusTrips.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.Code == req.Code.Trim() &&
            x.Id != id, ct);

        if (exists) return Conflict(new { message = "Trip Code already exists in this tenant." });

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
        RequireTenantWrite();

        var entity = await _db.BusTrips.FirstOrDefaultAsync(
         x => x.Id == id && x.TenantId == _tenantContext.TenantId,
         ct);

        if (entity is null)
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
        RequireTenantWrite();

        var entity = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Trip not found." });

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    // -------------------- STOP TIMES --------------------

    public sealed class ReplaceStopTimesRequest
    {
        public List<StopTimeItem> Items { get; set; } = new();

        public sealed class StopTimeItem
        {
            public Guid StopPointId { get; set; }
            public int StopIndex { get; set; }
            public DateTimeOffset? ArriveAt { get; set; }
            public DateTimeOffset? DepartAt { get; set; }
            public int? MinutesFromStart { get; set; }
            public bool IsActive { get; set; } = true;
        }
    }

    [HttpPut("{id:guid}/stop-times/replace")]
    public async Task<IActionResult> ReplaceStopTimes(Guid id, [FromBody] ReplaceStopTimesRequest req, CancellationToken ct = default)
    {
        RequireTenantWrite();

        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);
        if (trip is null) return NotFound(new { message = "Trip not found in this tenant." });

        if (await BusSeatOccupancySupport.HasActiveSeatOccupancyAsync(
                _db,
                _tenantContext.TenantId!.Value,
                id,
                DateTimeOffset.Now,
                ct))
        {
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });
        }

        if (req.Items is null || req.Items.Count < 2)
            return BadRequest(new { message = "Stop times must contain at least 2 items." });

        var ordered = req.Items.OrderBy(x => x.StopIndex).ToList();
        for (var i = 0; i < ordered.Count; i++)
        {
            if (ordered[i].StopIndex != i)
                return BadRequest(new { message = "StopIndex must be continuous from 0..n-1." });
            if (ordered[i].StopPointId == Guid.Empty)
                return BadRequest(new { message = "StopPointId is required." });
        }

        var stopIds = ordered.Select(x => x.StopPointId).Distinct().ToList();
        var count = await _db.BusStopPoints.IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == _tenantContext.TenantId && stopIds.Contains(x.Id) && !x.IsDeleted, ct);
        if (count != stopIds.Count)
            return BadRequest(new { message = "One or more StopPointId does not exist in this tenant." });

        var now = DateTimeOffset.Now;

        var existing = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .Where(x => x.TripId == id && x.TenantId == _tenantContext.TenantId)
            .ToListAsync(ct);

        var existingByIndex = new Dictionary<int, TripStopTime>();

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

        foreach (var s in ordered)
        {
            keepIndices.Add(s.StopIndex);

            if (existingByIndex.TryGetValue(s.StopIndex, out var row))
            {
                var changed = false;

                if (row.StopPointId != s.StopPointId) { row.StopPointId = s.StopPointId; changed = true; }
                if (row.ArriveAt != s.ArriveAt) { row.ArriveAt = s.ArriveAt; changed = true; }
                if (row.DepartAt != s.DepartAt) { row.DepartAt = s.DepartAt; changed = true; }
                if (row.MinutesFromStart != s.MinutesFromStart) { row.MinutesFromStart = s.MinutesFromStart; changed = true; }
                if (row.IsActive != s.IsActive) { row.IsActive = s.IsActive; changed = true; }
                if (row.IsDeleted) { row.IsDeleted = false; changed = true; }

                if (changed)
                    row.UpdatedAt = now;
            }
            else
            {
                _db.BusTripStopTimes.Add(new TripStopTime
                {
                    Id = Guid.NewGuid(),
                    TenantId = _tenantContext.TenantId!.Value,
                    TripId = id,
                    StopPointId = s.StopPointId,
                    StopIndex = s.StopIndex,
                    ArriveAt = s.ArriveAt,
                    DepartAt = s.DepartAt,
                    MinutesFromStart = s.MinutesFromStart,
                    IsActive = s.IsActive,
                    IsDeleted = false,
                    CreatedAt = now
                });
            }
        }

        foreach (var row in existingByIndex.Values)
        {
            if (!keepIndices.Contains(row.StopIndex) && !row.IsDeleted)
            {
                row.IsDeleted = true;
                row.UpdatedAt = now;
            }
        }

        await _db.SaveChangesAsync(ct);

        var activeRows = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .Where(x => x.TripId == id && x.TenantId == _tenantContext.TenantId && !x.IsDeleted)
            .OrderBy(x => x.StopIndex)
            .ToListAsync(ct);

        if (activeRows.Count > 0)
        {
            var first = activeRows.First();
            var last = activeRows.Last();

            trip.DepartureAt = first.DepartAt ?? first.ArriveAt ?? trip.DepartureAt;
            trip.ArrivalAt = last.ArriveAt ?? last.DepartAt ?? trip.ArrivalAt;
            trip.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true, count = ordered.Count });
    }


    public sealed class GenerateStopTimesFromRouteRequest
    {
        public DateTimeOffset DepartureAt { get; set; }
        public bool UseRouteStopMinutes { get; set; } = true; // if RouteStops.MinutesFromStart exists
    }

    /// <summary>
    /// Generate TripStopTimes from RouteStops:
    /// - If MinutesFromStart exists and UseRouteStopMinutes=true -> Arrive/Depart = DepartureAt + MinutesFromStart
    /// - Otherwise: only sets StopIndex + StopPointId (Arrive/Depart null)
    /// Also updates Trip.DepartureAt/ArrivalAt.
    /// </summary>
    [HttpPost("{id:guid}/stop-times/generate-from-route")]
    public async Task<IActionResult> GenerateStopTimesFromRoute(Guid id, [FromBody] GenerateStopTimesFromRouteRequest req, CancellationToken ct = default)
    {
        RequireTenantWrite();

        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);
        if (trip is null) return NotFound(new { message = "Trip not found in this tenant." });

        if (await BusSeatOccupancySupport.HasActiveSeatOccupancyAsync(
                _db,
                _tenantContext.TenantId!.Value,
                id,
                DateTimeOffset.Now,
                ct))
        {
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });
        }

        var routeStops = await _db.BusRouteStops.IgnoreQueryFilters()
            .Where(x => x.TenantId == _tenantContext.TenantId && x.RouteId == trip.RouteId && !x.IsDeleted)
            .OrderBy(x => x.StopIndex)
            .ToListAsync(ct);

        if (routeStops.Count < 2)
            return BadRequest(new { message = "RouteStops must have at least 2 items before generating stop times." });

        var items = routeStops.Select(rs =>
        {
            DateTimeOffset? time = null;
            if (req.UseRouteStopMinutes && rs.MinutesFromStart.HasValue)
                time = req.DepartureAt.AddMinutes(rs.MinutesFromStart.Value);

            return new ReplaceStopTimesRequest.StopTimeItem
            {
                StopPointId = rs.StopPointId,
                StopIndex = rs.StopIndex,
                MinutesFromStart = rs.MinutesFromStart,
                ArriveAt = time,
                DepartAt = time,
                IsActive = rs.IsActive
            };
        }).ToList();

        items[0].DepartAt = req.DepartureAt;
        if (items[0].ArriveAt is null) items[0].ArriveAt = req.DepartureAt;

        var last = items[^1];
        if (!last.ArriveAt.HasValue && !last.DepartAt.HasValue)
            last.ArriveAt = req.DepartureAt;
        last.DepartAt = null;

        var replaceReq = new ReplaceStopTimesRequest
        {
            Items = items
        };

        var result = await ReplaceStopTimes(id, replaceReq, ct);
        if (result is not OkObjectResult)
            return result;

        var updatedTrip = await _db.BusTrips.IgnoreQueryFilters()
            .Where(x => x.Id == id && x.TenantId == _tenantContext.TenantId)
            .Select(x => new { x.DepartureAt, x.ArrivalAt })
            .FirstOrDefaultAsync(ct);

        return Ok(new
        {
            ok = true,
            count = items.Count,
            departureAt = updatedTrip?.DepartureAt,
            arrivalAt = updatedTrip?.ArrivalAt
        });
    }


    // -------------------- SEGMENT PRICES --------------------

    public sealed class ReplaceSegmentPricesRequest
    {
        public string CurrencyCode { get; set; } = "VND";
        public List<SegmentItem> Items { get; set; } = new();

        public sealed class SegmentItem
        {
            public int FromStopIndex { get; set; }
            public int ToStopIndex { get; set; }
            public decimal BaseFare { get; set; }
            public decimal? TaxesFees { get; set; }
            public decimal? TotalPrice { get; set; } // if null -> BaseFare + TaxesFees
            public bool IsActive { get; set; } = true;
        }
    }

    /// <summary>
    /// Replace all TripSegmentPrices for a trip.
    /// Requires TripStopTimes already exist (to resolve FromTripStopTimeId/ToTripStopTimeId).
    /// </summary>
    [HttpPut("{id:guid}/segment-prices/replace")]
    public async Task<IActionResult> ReplaceSegmentPrices(Guid id, [FromBody] ReplaceSegmentPricesRequest req, CancellationToken ct = default)
    {
        RequireTenantWrite();

        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);
        if (trip is null) return NotFound(new { message = "Trip not found in this tenant." });

        if (await BusSeatOccupancySupport.HasActiveSeatOccupancyAsync(
                _db,
                _tenantContext.TenantId!.Value,
                id,
                DateTimeOffset.Now,
                ct))
        {
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });
        }

        if (string.IsNullOrWhiteSpace(req.CurrencyCode) || req.CurrencyCode.Trim().Length != 3)
            return BadRequest(new { message = "CurrencyCode must be 3 chars (e.g. VND)." });

        if (req.Items is null || req.Items.Count == 0)
            return BadRequest(new { message = "Items is required." });

        var stopTimes = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .Where(x => x.TripId == id && x.TenantId == _tenantContext.TenantId && !x.IsDeleted)
            .OrderBy(x => x.StopIndex)
            .ToListAsync(ct);

        if (stopTimes.Count < 2)
            return BadRequest(new { message = "TripStopTimes must exist before setting segment prices." });

        var byIndex = stopTimes.ToDictionary(x => x.StopIndex);

        var resolved = new List<ResolvedSegmentPriceRow>();

        foreach (var s in req.Items)
        {
            if (s.FromStopIndex < 0 || s.ToStopIndex < 0 || s.FromStopIndex >= s.ToStopIndex)
                return BadRequest(new { message = "Invalid segment indices (FromStopIndex must be < ToStopIndex)." });

            if (!byIndex.ContainsKey(s.FromStopIndex) || !byIndex.ContainsKey(s.ToStopIndex))
                return BadRequest(new { message = "Segment indices not found in TripStopTimes." });

            if (s.BaseFare < 0)
                return BadRequest(new { message = "BaseFare must be >= 0." });

            var from = byIndex[s.FromStopIndex];
            var to = byIndex[s.ToStopIndex];
            var total = s.TotalPrice ?? (s.BaseFare + (s.TaxesFees ?? 0m));

            resolved.Add(new ResolvedSegmentPriceRow
            {
                FromTripStopTimeId = from.Id,
                ToTripStopTimeId = to.Id,
                FromStopIndex = s.FromStopIndex,
                ToStopIndex = s.ToStopIndex,
                BaseFare = s.BaseFare,
                TaxesFees = s.TaxesFees,
                TotalPrice = total,
                IsActive = s.IsActive
            });
        }

        var duplicatePairs = resolved
            .GroupBy(x => new { x.FromStopIndex, x.ToStopIndex })
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.FromStopIndex}->{g.Key.ToStopIndex}")
            .ToList();

        if (duplicatePairs.Count > 0)
            return BadRequest(new { message = $"Duplicate segment pairs in request: {string.Join(", ", duplicatePairs)}." });

        var now = DateTimeOffset.Now;
        var currency = req.CurrencyCode.Trim().ToUpperInvariant();

        var existing = await _db.BusTripSegmentPrices.IgnoreQueryFilters()
            .Where(x => x.TripId == id && x.TenantId == _tenantContext.TenantId)
            .ToListAsync(ct);

        var existingByKey = new Dictionary<(int FromStopIndex, int ToStopIndex), TripSegmentPrice>();

        foreach (var g in existing.GroupBy(x => new { x.FromStopIndex, x.ToStopIndex }))
        {
            var keep = g.OrderByDescending(x => x.CreatedAt).First();
            existingByKey[(keep.FromStopIndex, keep.ToStopIndex)] = keep;

            foreach (var extra in g.Where(x => x.Id != keep.Id))
            {
                if (!extra.IsDeleted)
                {
                    extra.IsDeleted = true;
                    extra.UpdatedAt = now;
                }
            }
        }

        var touched = new HashSet<(int FromStopIndex, int ToStopIndex)>();

        foreach (var row in resolved)
        {
            var key = (row.FromStopIndex, row.ToStopIndex);
            touched.Add(key);

            if (existingByKey.TryGetValue(key, out var old))
            {
                var changed = false;

                if (old.FromTripStopTimeId != row.FromTripStopTimeId) { old.FromTripStopTimeId = row.FromTripStopTimeId; changed = true; }
                if (old.ToTripStopTimeId != row.ToTripStopTimeId) { old.ToTripStopTimeId = row.ToTripStopTimeId; changed = true; }
                if (!string.Equals(old.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)) { old.CurrencyCode = currency; changed = true; }
                if (old.BaseFare != row.BaseFare) { old.BaseFare = row.BaseFare; changed = true; }
                if (old.TaxesFees != row.TaxesFees) { old.TaxesFees = row.TaxesFees; changed = true; }
                if (old.TotalPrice != row.TotalPrice) { old.TotalPrice = row.TotalPrice; changed = true; }
                if (old.IsActive != row.IsActive) { old.IsActive = row.IsActive; changed = true; }
                if (old.IsDeleted) { old.IsDeleted = false; changed = true; }

                if (changed)
                    old.UpdatedAt = now;
            }
            else
            {
                _db.BusTripSegmentPrices.Add(new TripSegmentPrice
                {
                    Id = Guid.NewGuid(),
                    TenantId = _tenantContext.TenantId!.Value,
                    TripId = id,
                    FromTripStopTimeId = row.FromTripStopTimeId,
                    ToTripStopTimeId = row.ToTripStopTimeId,
                    FromStopIndex = row.FromStopIndex,
                    ToStopIndex = row.ToStopIndex,
                    CurrencyCode = currency,
                    BaseFare = row.BaseFare,
                    TaxesFees = row.TaxesFees,
                    TotalPrice = row.TotalPrice,
                    IsActive = row.IsActive,
                    IsDeleted = false,
                    CreatedAt = now
                });
            }
        }

        foreach (var old in existingByKey.Values)
        {
            var key = (old.FromStopIndex, old.ToStopIndex);
            if (!touched.Contains(key) && !old.IsDeleted)
            {
                old.IsDeleted = true;
                old.UpdatedAt = now;
            }
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, count = resolved.Count });
    }


    // -------------------- Helpers --------------------

    private void RequireTenantWrite()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required for admin write requests.");
    }
    private sealed class ResolvedSegmentPriceRow
    {
        public Guid FromTripStopTimeId { get; set; }
        public Guid ToTripStopTimeId { get; set; }
        public int FromStopIndex { get; set; }
        public int ToStopIndex { get; set; }
        public decimal BaseFare { get; set; }
        public decimal? TaxesFees { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsActive { get; set; }
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
