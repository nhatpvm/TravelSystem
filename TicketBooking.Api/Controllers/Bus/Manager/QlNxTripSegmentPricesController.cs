// FILE #284: TicketBooking.Api/Controllers/QlNxTripSegmentPricesController.cs
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
[Route("api/v{version:apiVersion}/qlnx/bus/trip-segment-prices")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX}")]
public sealed class QlNxTripSegmentPricesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlNxTripSegmentPricesController(AppDbContext db, ITenantContext tenantContext)
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

        IQueryable<TripSegmentPrice> query = _db.BusTripSegmentPrices.Where(x => x.TripId == tripId);
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        var items = await query
            .OrderBy(x => x.FromStopIndex)
            .ThenBy(x => x.ToStopIndex)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.TripId,
                x.FromTripStopTimeId,
                x.ToTripStopTimeId,
                x.FromStopIndex,
                x.ToStopIndex,
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice,
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

        IQueryable<TripSegmentPrice> query = _db.BusTripSegmentPrices;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.TripId,
                x.FromTripStopTimeId,
                x.ToTripStopTimeId,
                x.FromStopIndex,
                x.ToStopIndex,
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (item is null)
            return NotFound(new { message = "TripSegmentPrice not found in this tenant." });

        var stopRefs = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted &&
                (x.Id == item.FromTripStopTimeId || x.Id == item.ToTripStopTimeId))
            .OrderBy(x => x.StopIndex)
            .Select(x => new
            {
                x.Id,
                x.TripId,
                x.StopPointId,
                x.StopIndex,
                x.ArriveAt,
                x.DepartAt,
                x.MinutesFromStart,
                x.IsActive
            })
            .ToListAsync(ct);

        return Ok(new
        {
            item,
            stopRefs
        });
    }

    public sealed class UpsertTripSegmentPriceRequest
    {
        public Guid TripId { get; set; }
        public Guid FromTripStopTimeId { get; set; }
        public Guid ToTripStopTimeId { get; set; }
        public string CurrencyCode { get; set; } = "VND";
        public decimal BaseFare { get; set; }
        public decimal? TaxesFees { get; set; }
        public decimal? TotalPrice { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] UpsertTripSegmentPriceRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();
        ValidateUpsert(req);

        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == req.TripId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (trip is null)
            return BadRequest(new { message = "TripId is invalid for this tenant." });

        if (await HasActiveSeatOccupancyAsync(req.TripId, ct))
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });

        var stopRef = await ValidateAndResolveSegmentAsync(
            req.TripId,
            req.FromTripStopTimeId,
            req.ToTripStopTimeId,
            ct);

        var exists = await _db.BusTripSegmentPrices.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.TripId == req.TripId &&
            x.FromStopIndex == stopRef.FromStopIndex &&
            x.ToStopIndex == stopRef.ToStopIndex, ct);

        if (exists)
            return Conflict(new { message = "TripSegmentPrice already exists for this trip segment." });

        var entity = new TripSegmentPrice
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            TripId = req.TripId,
            FromTripStopTimeId = req.FromTripStopTimeId,
            ToTripStopTimeId = req.ToTripStopTimeId,
            FromStopIndex = stopRef.FromStopIndex,
            ToStopIndex = stopRef.ToStopIndex,
            CurrencyCode = req.CurrencyCode.Trim().ToUpperInvariant(),
            BaseFare = req.BaseFare,
            TaxesFees = req.TaxesFees,
            TotalPrice = req.TotalPrice ?? (req.BaseFare + (req.TaxesFees ?? 0m)),
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.BusTripSegmentPrices.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpsertTripSegmentPriceRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();
        ValidateUpsert(req);

        var entity = await _db.BusTripSegmentPrices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "TripSegmentPrice not found in this tenant." });

        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == req.TripId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (trip is null)
            return BadRequest(new { message = "TripId is invalid for this tenant." });

        if (await HasActiveSeatOccupancyAsync(req.TripId, ct))
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });

        var stopRef = await ValidateAndResolveSegmentAsync(
            req.TripId,
            req.FromTripStopTimeId,
            req.ToTripStopTimeId,
            ct);

        var exists = await _db.BusTripSegmentPrices.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.TripId == req.TripId &&
            x.FromStopIndex == stopRef.FromStopIndex &&
            x.ToStopIndex == stopRef.ToStopIndex &&
            x.Id != id, ct);

        if (exists)
            return Conflict(new { message = "Another TripSegmentPrice already exists for this trip segment." });

        entity.TripId = req.TripId;
        entity.FromTripStopTimeId = req.FromTripStopTimeId;
        entity.ToTripStopTimeId = req.ToTripStopTimeId;
        entity.FromStopIndex = stopRef.FromStopIndex;
        entity.ToStopIndex = stopRef.ToStopIndex;
        entity.CurrencyCode = req.CurrencyCode.Trim().ToUpperInvariant();
        entity.BaseFare = req.BaseFare;
        entity.TaxesFees = req.TaxesFees;
        entity.TotalPrice = req.TotalPrice ?? (req.BaseFare + (req.TaxesFees ?? 0m));
        entity.IsActive = req.IsActive;
        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    public sealed class ReplaceTripSegmentPricesRequest
    {
        public Guid TripId { get; set; }
        public string CurrencyCode { get; set; } = "VND";
        public List<Item> Items { get; set; } = new();

        public sealed class Item
        {
            public Guid FromTripStopTimeId { get; set; }
            public Guid ToTripStopTimeId { get; set; }
            public decimal BaseFare { get; set; }
            public decimal? TaxesFees { get; set; }
            public decimal? TotalPrice { get; set; }
            public bool IsActive { get; set; } = true;
        }
    }

    [HttpPut("replace")]
    public async Task<IActionResult> Replace(
    [FromBody] ReplaceTripSegmentPricesRequest req,
    CancellationToken ct = default)
    {
        EnsureTenantScope();
        ValidateReplace(req);

        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == req.TripId && x.TenantId == _tenantContext.TenantId, ct);

        if (trip is null)
            return NotFound(new { message = "Trip not found in this tenant." });

        if (await HasActiveSeatOccupancyAsync(req.TripId, ct))
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });

        var resolved = new List<ResolvedSegmentItem>();

        foreach (var item in req.Items)
        {
            var stopRef = await ValidateAndResolveSegmentAsync(
                req.TripId,
                item.FromTripStopTimeId,
                item.ToTripStopTimeId,
                ct);

            resolved.Add(new ResolvedSegmentItem
            {
                FromTripStopTimeId = item.FromTripStopTimeId,
                ToTripStopTimeId = item.ToTripStopTimeId,
                FromStopIndex = stopRef.FromStopIndex,
                ToStopIndex = stopRef.ToStopIndex,
                BaseFare = item.BaseFare,
                TaxesFees = item.TaxesFees,
                TotalPrice = item.TotalPrice ?? (item.BaseFare + (item.TaxesFees ?? 0m)),
                IsActive = item.IsActive
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
            .Where(x => x.TripId == req.TripId && x.TenantId == _tenantContext.TenantId)
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

        foreach (var x in resolved)
        {
            var key = (x.FromStopIndex, x.ToStopIndex);
            touched.Add(key);

            if (existingByKey.TryGetValue(key, out var row))
            {
                var changed = false;

                if (row.FromTripStopTimeId != x.FromTripStopTimeId) { row.FromTripStopTimeId = x.FromTripStopTimeId; changed = true; }
                if (row.ToTripStopTimeId != x.ToTripStopTimeId) { row.ToTripStopTimeId = x.ToTripStopTimeId; changed = true; }
                if (!string.Equals(row.CurrencyCode, currency, StringComparison.OrdinalIgnoreCase)) { row.CurrencyCode = currency; changed = true; }
                if (row.BaseFare != x.BaseFare) { row.BaseFare = x.BaseFare; changed = true; }
                if (row.TaxesFees != x.TaxesFees) { row.TaxesFees = x.TaxesFees; changed = true; }
                if (row.TotalPrice != x.TotalPrice) { row.TotalPrice = x.TotalPrice; changed = true; }
                if (row.IsActive != x.IsActive) { row.IsActive = x.IsActive; changed = true; }
                if (row.IsDeleted) { row.IsDeleted = false; changed = true; }

                if (changed)
                    row.UpdatedAt = now;
            }
            else
            {
                _db.BusTripSegmentPrices.Add(new TripSegmentPrice
                {
                    Id = Guid.NewGuid(),
                    TenantId = _tenantContext.TenantId!.Value,
                    TripId = req.TripId,
                    FromTripStopTimeId = x.FromTripStopTimeId,
                    ToTripStopTimeId = x.ToTripStopTimeId,
                    FromStopIndex = x.FromStopIndex,
                    ToStopIndex = x.ToStopIndex,
                    CurrencyCode = currency,
                    BaseFare = x.BaseFare,
                    TaxesFees = x.TaxesFees,
                    TotalPrice = x.TotalPrice,
                    IsActive = x.IsActive,
                    IsDeleted = false,
                    CreatedAt = now
                });
            }
        }

        foreach (var row in existingByKey.Values)
        {
            var key = (row.FromStopIndex, row.ToStopIndex);
            if (!touched.Contains(key) && !row.IsDeleted)
            {
                row.IsDeleted = true;
                row.UpdatedAt = now;
            }
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, count = resolved.Count });
    }

    public sealed class GenerateAllPairsRequest
    {
        public Guid TripId { get; set; }
        public string CurrencyCode { get; set; } = "VND";
        public decimal DefaultBaseFare { get; set; }
        public decimal? DefaultTaxesFees { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpPost("generate-all-pairs")]
    public async Task<IActionResult> GenerateAllPairs(
        [FromBody] GenerateAllPairsRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        if (req.TripId == Guid.Empty)
            return BadRequest(new { message = "TripId is required." });

        if (string.IsNullOrWhiteSpace(req.CurrencyCode) || req.CurrencyCode.Trim().Length != 3)
            return BadRequest(new { message = "CurrencyCode must be 3 chars (e.g. VND)." });

        if (req.DefaultBaseFare < 0)
            return BadRequest(new { message = "DefaultBaseFare must be >= 0." });

        if (req.DefaultTaxesFees.HasValue && req.DefaultTaxesFees.Value < 0)
            return BadRequest(new { message = "DefaultTaxesFees must be >= 0." });

        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == req.TripId && x.TenantId == _tenantContext.TenantId, ct);

        if (trip is null)
            return NotFound(new { message = "Trip not found in this tenant." });

        if (await HasActiveSeatOccupancyAsync(req.TripId, ct))
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });

        var stopTimes = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .Where(x => x.TripId == req.TripId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted)
            .OrderBy(x => x.StopIndex)
            .ToListAsync(ct);

        if (stopTimes.Count < 2)
            return BadRequest(new { message = "TripStopTimes must contain at least 2 rows before generating segment prices." });

        var items = new List<ResolvedSegmentItem>();

        for (var i = 0; i < stopTimes.Count - 1; i++)
        {
            for (var j = i + 1; j < stopTimes.Count; j++)
            {
                items.Add(new ResolvedSegmentItem
                {
                    FromTripStopTimeId = stopTimes[i].Id,
                    ToTripStopTimeId = stopTimes[j].Id,
                    FromStopIndex = stopTimes[i].StopIndex,
                    ToStopIndex = stopTimes[j].StopIndex,
                    BaseFare = req.DefaultBaseFare,
                    TaxesFees = req.DefaultTaxesFees,
                    TotalPrice = req.DefaultBaseFare + (req.DefaultTaxesFees ?? 0m),
                    IsActive = req.IsActive
                });
            }
        }

        var replaceReq = new ReplaceTripSegmentPricesRequest
        {
            TripId = req.TripId,
            CurrencyCode = req.CurrencyCode.Trim().ToUpperInvariant(),
            Items = items.Select(x => new ReplaceTripSegmentPricesRequest.Item
            {
                FromTripStopTimeId = x.FromTripStopTimeId,
                ToTripStopTimeId = x.ToTripStopTimeId,
                BaseFare = x.BaseFare,
                TaxesFees = x.TaxesFees,
                TotalPrice = x.TotalPrice,
                IsActive = x.IsActive
            }).ToList()
        };

        return await Replace(replaceReq, ct);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.BusTripSegmentPrices
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "TripSegmentPrice not found." });

        if (entity.TenantId != _tenantContext.TenantId)
            return NotFound(new { message = "TripSegmentPrice not found in this tenant." });

        if (await HasActiveSeatOccupancyAsync(entity.TripId, ct))
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });

        _db.BusTripSegmentPrices.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.BusTripSegmentPrices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "TripSegmentPrice not found." });

        if (await HasActiveSeatOccupancyAsync(entity.TripId, ct))
            return Conflict(new { message = BusSeatOccupancySupport.TripMutationBlockedMessage });

        var duplicate = await _db.BusTripSegmentPrices.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.TripId == entity.TripId &&
            x.FromStopIndex == entity.FromStopIndex &&
            x.ToStopIndex == entity.ToStopIndex &&
            x.Id != entity.Id &&
            !x.IsDeleted, ct);

        if (duplicate)
            return Conflict(new { message = "Cannot restore because another active row already exists for this trip segment." });

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

    private static void ValidateUpsert(UpsertTripSegmentPriceRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.TripId == Guid.Empty) throw new InvalidOperationException("TripId is required.");
        if (req.FromTripStopTimeId == Guid.Empty) throw new InvalidOperationException("FromTripStopTimeId is required.");
        if (req.ToTripStopTimeId == Guid.Empty) throw new InvalidOperationException("ToTripStopTimeId is required.");
        if (string.IsNullOrWhiteSpace(req.CurrencyCode) || req.CurrencyCode.Trim().Length != 3)
            throw new InvalidOperationException("CurrencyCode must be 3 chars (e.g. VND).");
        if (req.BaseFare < 0) throw new InvalidOperationException("BaseFare must be >= 0.");
        if (req.TaxesFees.HasValue && req.TaxesFees.Value < 0) throw new InvalidOperationException("TaxesFees must be >= 0.");
        if (req.TotalPrice.HasValue && req.TotalPrice.Value < 0) throw new InvalidOperationException("TotalPrice must be >= 0.");
    }

    private static void ValidateReplace(ReplaceTripSegmentPricesRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.TripId == Guid.Empty) throw new InvalidOperationException("TripId is required.");
        if (string.IsNullOrWhiteSpace(req.CurrencyCode) || req.CurrencyCode.Trim().Length != 3)
            throw new InvalidOperationException("CurrencyCode must be 3 chars (e.g. VND).");
        if (req.Items is null || req.Items.Count == 0)
            throw new InvalidOperationException("Items is required.");

        foreach (var item in req.Items)
        {
            if (item.FromTripStopTimeId == Guid.Empty)
                throw new InvalidOperationException("FromTripStopTimeId is required.");
            if (item.ToTripStopTimeId == Guid.Empty)
                throw new InvalidOperationException("ToTripStopTimeId is required.");
            if (item.BaseFare < 0)
                throw new InvalidOperationException("BaseFare must be >= 0.");
            if (item.TaxesFees.HasValue && item.TaxesFees.Value < 0)
                throw new InvalidOperationException("TaxesFees must be >= 0.");
            if (item.TotalPrice.HasValue && item.TotalPrice.Value < 0)
                throw new InvalidOperationException("TotalPrice must be >= 0.");
        }
    }

    private async Task<ResolvedStopRef> ValidateAndResolveSegmentAsync(
        Guid tripId,
        Guid fromTripStopTimeId,
        Guid toTripStopTimeId,
        CancellationToken ct)
    {
        var refs = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .Where(x =>
                x.TripId == tripId &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted &&
                (x.Id == fromTripStopTimeId || x.Id == toTripStopTimeId))
            .Select(x => new
            {
                x.Id,
                x.StopIndex
            })
            .ToListAsync(ct);

        var from = refs.FirstOrDefault(x => x.Id == fromTripStopTimeId);
        var to = refs.FirstOrDefault(x => x.Id == toTripStopTimeId);

        if (from is null || to is null)
            throw new InvalidOperationException("From/To TripStopTimeId is invalid for this trip.");

        if (from.StopIndex >= to.StopIndex)
            throw new InvalidOperationException("FromStopIndex must be < ToStopIndex.");

        return new ResolvedStopRef
        {
            FromStopIndex = from.StopIndex,
            ToStopIndex = to.StopIndex
        };
    }

    private sealed class ResolvedStopRef
    {
        public int FromStopIndex { get; set; }
        public int ToStopIndex { get; set; }
    }

    private sealed class ResolvedSegmentItem
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

    private Task<bool> HasActiveSeatOccupancyAsync(Guid tripId, CancellationToken ct)
        => BusSeatOccupancySupport.HasActiveSeatOccupancyAsync(
            _db,
            _tenantContext.TenantId!.Value,
            tripId,
            DateTimeOffset.Now,
            ct);
}
