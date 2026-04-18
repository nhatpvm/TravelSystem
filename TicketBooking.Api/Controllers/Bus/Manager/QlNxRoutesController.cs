// FILE #280: TicketBooking.Api/Controllers/QlNxRoutesController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Bus;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlnx/bus/routes")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX}")]
public sealed class QlNxRoutesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlNxRoutesController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] Guid? providerId = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        IQueryable<BusRoute> query = _db.BusRoutes;

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        if (providerId.HasValue && providerId.Value != Guid.Empty)
            query = query.Where(x => x.ProviderId == providerId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x => x.Code.Contains(keyword) || x.Name.Contains(keyword));
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.ProviderId,
                x.Code,
                x.Name,
                x.FromStopPointId,
                x.ToStopPointId,
                x.EstimatedMinutes,
                x.DistanceKm,
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

        IQueryable<BusRoute> query = _db.BusRoutes;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var route = await query
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (route is null)
            return NotFound(new { message = "Route not found in this tenant." });

        var stops = await _db.BusRouteStops.IgnoreQueryFilters()
            .Where(x => x.RouteId == id && x.TenantId == _tenantContext.TenantId && (!x.IsDeleted || includeDeleted))
            .OrderBy(x => x.StopIndex)
            .Select(x => new
            {
                x.Id,
                x.StopIndex,
                x.StopPointId,
                x.DistanceFromStartKm,
                x.MinutesFromStart,
                x.IsActive,
                x.IsDeleted
            })
            .ToListAsync(ct);

        return Ok(new { route, stops });
    }

    public sealed class UpsertRouteRequest
    {
        public Guid ProviderId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public Guid FromStopPointId { get; set; }
        public Guid ToStopPointId { get; set; }
        public int EstimatedMinutes { get; set; }
        public int DistanceKm { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] UpsertRouteRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();
        Validate(req);

        var providerOk = await _db.Providers.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == req.ProviderId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);
        if (!providerOk)
            return BadRequest(new { message = "ProviderId is invalid for this tenant." });

        await ValidateStopsExistAsync(req.FromStopPointId, req.ToStopPointId, ct);

        var exists = await _db.BusRoutes.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.Code == req.Code.Trim(), ct);

        if (exists)
            return Conflict(new { message = "Route Code already exists in this tenant." });

        var entity = new BusRoute
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            ProviderId = req.ProviderId,
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            FromStopPointId = req.FromStopPointId,
            ToStopPointId = req.ToStopPointId,
            EstimatedMinutes = req.EstimatedMinutes,
            DistanceKm = req.DistanceKm,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.BusRoutes.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpsertRouteRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();
        Validate(req);

        var entity = await _db.BusRoutes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Route not found in this tenant." });

        var providerOk = await _db.Providers.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == req.ProviderId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);
        if (!providerOk)
            return BadRequest(new { message = "ProviderId is invalid for this tenant." });

        await ValidateStopsExistAsync(req.FromStopPointId, req.ToStopPointId, ct);

        var exists = await _db.BusRoutes.IgnoreQueryFilters().AnyAsync(x =>
            x.TenantId == _tenantContext.TenantId &&
            x.Code == req.Code.Trim() &&
            x.Id != id, ct);

        if (exists)
            return Conflict(new { message = "Route Code already exists in this tenant." });

        entity.ProviderId = req.ProviderId;
        entity.Code = req.Code.Trim();
        entity.Name = req.Name.Trim();
        entity.FromStopPointId = req.FromStopPointId;
        entity.ToStopPointId = req.ToStopPointId;
        entity.EstimatedMinutes = req.EstimatedMinutes;
        entity.DistanceKm = req.DistanceKm;
        entity.IsActive = req.IsActive;
        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    public sealed class ReplaceRouteStopsRequest
    {
        public List<RouteStopItem> Stops { get; set; } = new();

        public sealed class RouteStopItem
        {
            public Guid StopPointId { get; set; }
            public int StopIndex { get; set; }
            public int? DistanceFromStartKm { get; set; }
            public int? MinutesFromStart { get; set; }
            public bool IsActive { get; set; } = true;
        }
    }

    [HttpPut("{id:guid}/stops")]
    public async Task<IActionResult> ReplaceStops(
     Guid id,
     [FromBody] ReplaceRouteStopsRequest req,
     CancellationToken ct = default)
    {
        EnsureTenantScope();

        var route = await _db.BusRoutes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (route is null)
            return NotFound(new { message = "Route not found in this tenant." });

        if (req.Stops is null || req.Stops.Count < 2)
            return BadRequest(new { message = "Stops must contain at least 2 items." });

        var ordered = req.Stops.OrderBy(x => x.StopIndex).ToList();

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

        var existing = await _db.BusRouteStops.IgnoreQueryFilters()
            .Where(x => x.RouteId == id && x.TenantId == _tenantContext.TenantId)
            .ToListAsync(ct);

        var existingByIndex = new Dictionary<int, RouteStop>();

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
                if (row.DistanceFromStartKm != s.DistanceFromStartKm) { row.DistanceFromStartKm = s.DistanceFromStartKm; changed = true; }
                if (row.MinutesFromStart != s.MinutesFromStart) { row.MinutesFromStart = s.MinutesFromStart; changed = true; }
                if (row.IsActive != s.IsActive) { row.IsActive = s.IsActive; changed = true; }
                if (row.IsDeleted) { row.IsDeleted = false; changed = true; }

                if (changed)
                    row.UpdatedAt = now;
            }
            else
            {
                _db.BusRouteStops.Add(new RouteStop
                {
                    Id = Guid.NewGuid(),
                    TenantId = _tenantContext.TenantId!.Value,
                    RouteId = id,
                    StopPointId = s.StopPointId,
                    StopIndex = s.StopIndex,
                    DistanceFromStartKm = s.DistanceFromStartKm,
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

        route.FromStopPointId = ordered.First().StopPointId;
        route.ToStopPointId = ordered.Last().StopPointId;
        route.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, count = ordered.Count });
    }


    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.BusRoutes
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Route not found." });

        if (entity.TenantId != _tenantContext.TenantId)
            return NotFound(new { message = "Route not found in this tenant." });

        _db.BusRoutes.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.BusRoutes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Route not found." });

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

    private async Task ValidateStopsExistAsync(Guid fromStopId, Guid toStopId, CancellationToken ct)
    {
        if (fromStopId == Guid.Empty || toStopId == Guid.Empty)
            throw new InvalidOperationException("FromStopPointId and ToStopPointId are required.");
        if (fromStopId == toStopId)
            throw new InvalidOperationException("FromStopPointId must be different from ToStopPointId.");

        var ids = new[] { fromStopId, toStopId };

        var count = await _db.BusStopPoints.IgnoreQueryFilters()
            .CountAsync(x => x.TenantId == _tenantContext.TenantId && ids.Contains(x.Id) && !x.IsDeleted, ct);

        if (count != 2)
            throw new InvalidOperationException("From/To StopPointId is invalid for this tenant.");
    }

    private static void Validate(UpsertRouteRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.ProviderId == Guid.Empty) throw new InvalidOperationException("ProviderId is required.");
        if (string.IsNullOrWhiteSpace(req.Code)) throw new InvalidOperationException("Code is required.");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new InvalidOperationException("Name is required.");
        if (req.Code.Length > 50) throw new InvalidOperationException("Code max length is 50.");
        if (req.Name.Length > 200) throw new InvalidOperationException("Name max length is 200.");
    }
}
