using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlvt/train/fare-rules")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLVT}")]
public sealed class QlVtTrainFareRulesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlVtTrainFareRulesController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public sealed class UpsertFareRuleRequest
    {
        public Guid? RouteId { get; set; }
        public Guid? TripId { get; set; }
        public Guid FareClassId { get; set; }
        public int FromStopIndex { get; set; }
        public int ToStopIndex { get; set; }
        public string CurrencyCode { get; set; } = "VND";
        public decimal BaseFare { get; set; }
        public decimal? TaxesFees { get; set; }
        public decimal? TotalPrice { get; set; }
        public DateTimeOffset? EffectiveFrom { get; set; }
        public DateTimeOffset? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? routeId = null,
        [FromQuery] Guid? tripId = null,
        [FromQuery] Guid? fareClassId = null,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        EnsureTenantScope();
        var tenantId = _tenantContext.TenantId!.Value;

        IQueryable<TrainFareRule> query = _db.TrainFareRules;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);
        if (routeId.HasValue)
            query = query.Where(x => x.RouteId == routeId.Value);
        if (tripId.HasValue)
            query = query.Where(x => x.TripId == tripId.Value);
        if (fareClassId.HasValue)
            query = query.Where(x => x.FareClassId == fareClassId.Value);

        var fareClasses = await _db.TrainFareClasses.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId)
            .Select(x => new { x.Id, x.Code, x.Name, x.SeatType, x.DefaultModifier, x.IsActive, x.IsDeleted })
            .ToDictionaryAsync(x => x.Id, x => x, ct);

        var routeIds = await query
            .Where(x => x.RouteId.HasValue)
            .Select(x => x.RouteId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var tripIds = await query
            .Where(x => x.TripId.HasValue)
            .Select(x => x.TripId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var routes = routeIds.Count == 0
            ? new Dictionary<Guid, object>()
            : await _db.TrainRoutes.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && routeIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.Name,
                    x.FromStopPointId,
                    x.ToStopPointId,
                    x.IsActive,
                    x.IsDeleted
                })
                .ToDictionaryAsync(x => x.Id, x => (object)x, ct);

        var trips = tripIds.Count == 0
            ? new Dictionary<Guid, object>()
            : await _db.TrainTrips.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && tripIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    x.RouteId,
                    x.TrainNumber,
                    x.Code,
                    x.Name,
                    x.DepartureAt,
                    x.ArrivalAt,
                    x.Status,
                    x.IsActive,
                    x.IsDeleted
                })
                .ToDictionaryAsync(x => x.Id, x => (object)x, ct);

        var items = await query
            .OrderBy(x => x.TripId == null ? 0 : 1)
            .ThenBy(x => x.RouteId)
            .ThenBy(x => x.TripId)
            .ThenBy(x => x.FromStopIndex)
            .ThenBy(x => x.ToStopIndex)
            .ThenBy(x => x.FareClassId)
            .Select(x => new
            {
                x.Id,
                x.RouteId,
                x.TripId,
                x.FareClassId,
                x.FromStopIndex,
                x.ToStopIndex,
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice,
                x.EffectiveFrom,
                x.EffectiveTo,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new
        {
            items = items.Select(x => new
            {
                x.Id,
                x.RouteId,
                x.TripId,
                x.FareClassId,
                x.FromStopIndex,
                x.ToStopIndex,
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice,
                x.EffectiveFrom,
                x.EffectiveTo,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt,
                fareClass = fareClasses.GetValueOrDefault(x.FareClassId),
                route = x.RouteId.HasValue ? routes.GetValueOrDefault(x.RouteId.Value) : null,
                trip = x.TripId.HasValue ? trips.GetValueOrDefault(x.TripId.Value) : null
            })
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        EnsureTenantScope();

        IQueryable<TrainFareRule> query = _db.TrainFareRules;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (item is null)
            return NotFound(new { message = "Fare rule not found in this tenant." });

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertFareRuleRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        Validate(req);
        var tenantId = _tenantContext.TenantId!.Value;

        var scopeError = await ValidateScopeAsync(tenantId, req, ct);
        if (scopeError is not null)
            return BadRequest(new { message = scopeError });

        if (await HasDuplicateRuleAsync(tenantId, null, req, ct))
            return Conflict(new { message = "A fare rule already exists for this scope, segment and fare class." });

        var entity = new TrainFareRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RouteId = req.RouteId,
            TripId = req.TripId,
            FareClassId = req.FareClassId,
            FromStopIndex = req.FromStopIndex,
            ToStopIndex = req.ToStopIndex,
            CurrencyCode = NormalizeCurrency(req.CurrencyCode),
            BaseFare = req.BaseFare,
            TaxesFees = req.TaxesFees,
            TotalPrice = req.TotalPrice ?? (req.BaseFare + (req.TaxesFees ?? 0m)),
            EffectiveFrom = req.EffectiveFrom,
            EffectiveTo = req.EffectiveTo,
            IsActive = req.IsActive,
            CreatedAt = DateTimeOffset.Now
        };

        _db.TrainFareRules.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertFareRuleRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        Validate(req);
        var tenantId = _tenantContext.TenantId!.Value;

        var entity = await _db.TrainFareRules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Fare rule not found in this tenant." });

        var scopeError = await ValidateScopeAsync(tenantId, req, ct);
        if (scopeError is not null)
            return BadRequest(new { message = scopeError });

        if (await HasDuplicateRuleAsync(tenantId, id, req, ct))
            return Conflict(new { message = "Another fare rule already exists for this scope, segment and fare class." });

        entity.RouteId = req.RouteId;
        entity.TripId = req.TripId;
        entity.FareClassId = req.FareClassId;
        entity.FromStopIndex = req.FromStopIndex;
        entity.ToStopIndex = req.ToStopIndex;
        entity.CurrencyCode = NormalizeCurrency(req.CurrencyCode);
        entity.BaseFare = req.BaseFare;
        entity.TaxesFees = req.TaxesFees;
        entity.TotalPrice = req.TotalPrice ?? (req.BaseFare + (req.TaxesFees ?? 0m));
        entity.EffectiveFrom = req.EffectiveFrom;
        entity.EffectiveTo = req.EffectiveTo;
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

        var entity = await _db.TrainFareRules
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Fare rule not found." });

        _db.TrainFareRules.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.TrainFareRules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Fare rule not found." });

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<string?> ValidateScopeAsync(Guid tenantId, UpsertFareRuleRequest req, CancellationToken ct)
    {
        var fareClassExists = await _db.TrainFareClasses.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == req.FareClassId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (!fareClassExists)
            return "FareClassId is invalid for this tenant.";

        Guid routeId;
        if (req.TripId.HasValue)
        {
            var trip = await _db.TrainTrips.IgnoreQueryFilters()
                .Select(x => new { x.Id, x.TenantId, x.RouteId, x.IsDeleted })
                .FirstOrDefaultAsync(x => x.Id == req.TripId.Value && x.TenantId == tenantId && !x.IsDeleted, ct);

            if (trip is null)
                return "TripId is invalid for this tenant.";

            if (req.RouteId.HasValue && req.RouteId.Value != trip.RouteId)
                return "RouteId must match the selected trip route.";

            routeId = trip.RouteId;

            var validSegment = await _db.TrainTripStopTimes.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TripId == trip.Id && !x.IsDeleted)
                .Select(x => x.StopIndex)
                .ToListAsync(ct);

            if (!validSegment.Contains(req.FromStopIndex) || !validSegment.Contains(req.ToStopIndex))
                return "Stop index is outside the selected trip schedule.";
        }
        else
        {
            if (!req.RouteId.HasValue)
                return "RouteId or TripId is required.";

            routeId = req.RouteId.Value;
            var routeExists = await _db.TrainRoutes.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == routeId && x.TenantId == tenantId && !x.IsDeleted, ct);

            if (!routeExists)
                return "RouteId is invalid for this tenant.";
        }

        var routeStopIndexes = await _db.TrainRouteStops.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.RouteId == routeId && !x.IsDeleted)
            .Select(x => x.StopIndex)
            .ToListAsync(ct);

        if (routeStopIndexes.Count > 0 &&
            (!routeStopIndexes.Contains(req.FromStopIndex) || !routeStopIndexes.Contains(req.ToStopIndex)))
            return "Stop index is outside the selected route.";

        return null;
    }

    private async Task<bool> HasDuplicateRuleAsync(
        Guid tenantId,
        Guid? currentId,
        UpsertFareRuleRequest req,
        CancellationToken ct)
    {
        return await _db.TrainFareRules.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == tenantId &&
            (!currentId.HasValue || x.Id != currentId.Value) &&
            x.RouteId == req.RouteId &&
            x.TripId == req.TripId &&
            x.FareClassId == req.FareClassId &&
            x.FromStopIndex == req.FromStopIndex &&
            x.ToStopIndex == req.ToStopIndex &&
            !x.IsDeleted, ct);
    }

    private void EnsureTenantScope()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required.");
    }

    private static void Validate(UpsertFareRuleRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.FromStopIndex < 0) throw new InvalidOperationException("FromStopIndex must be greater than or equal to 0.");
        if (req.ToStopIndex <= req.FromStopIndex) throw new InvalidOperationException("ToStopIndex must be greater than FromStopIndex.");
        if (req.BaseFare < 0) throw new InvalidOperationException("BaseFare must be greater than or equal to 0.");
        if (req.TaxesFees.HasValue && req.TaxesFees.Value < 0) throw new InvalidOperationException("TaxesFees must be greater than or equal to 0.");
        if (req.TotalPrice.HasValue && req.TotalPrice.Value < 0) throw new InvalidOperationException("TotalPrice must be greater than or equal to 0.");
        if (req.EffectiveFrom.HasValue && req.EffectiveTo.HasValue && req.EffectiveFrom > req.EffectiveTo)
            throw new InvalidOperationException("EffectiveFrom must be before EffectiveTo.");
        _ = NormalizeCurrency(req.CurrencyCode);
    }

    private static string NormalizeCurrency(string value)
    {
        var currency = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (currency.Length != 3)
            throw new InvalidOperationException("CurrencyCode must contain exactly 3 characters.");
        return currency;
    }
}
