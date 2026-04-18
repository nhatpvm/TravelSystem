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
[Route("api/v{version:apiVersion}/qlnx/bus/stop-points")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX}")]
public sealed class QlNxStopPointsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlNxStopPointsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] StopPointType? type = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        IQueryable<StopPoint> query = _db.BusStopPoints;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                (x.AddressLine != null && x.AddressLine.Contains(keyword)));
        }

        var items = await query
            .OrderBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAt)
            .Take(300)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.LocationId,
                x.Type,
                x.Name,
                x.AddressLine,
                x.Latitude,
                x.Longitude,
                x.Notes,
                x.IsActive,
                x.IsDeleted,
                x.SortOrder,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        var locationIds = items.Select(x => x.LocationId).Distinct().ToList();
        var locations = locationIds.Count == 0
            ? new Dictionary<Guid, object>()
            : await _db.Locations.IgnoreQueryFilters()
                .Where(x => locationIds.Contains(x.Id) && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Code
                })
                .ToDictionaryAsync(x => x.Id, x => (object)new { x.Id, x.Name, x.Code }, ct);

        return Ok(new
        {
            items = items.Select(x => new
            {
                x.Id,
                x.TenantId,
                x.LocationId,
                x.Type,
                x.Name,
                x.AddressLine,
                x.Latitude,
                x.Longitude,
                x.Notes,
                x.IsActive,
                x.IsDeleted,
                x.SortOrder,
                x.CreatedAt,
                x.UpdatedAt,
                location = locations.GetValueOrDefault(x.LocationId)
            })
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        EnsureTenantScope();

        IQueryable<StopPoint> query = _db.BusStopPoints;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);
        if (item is null)
            return NotFound(new { message = "StopPoint not found in this tenant." });

        var location = await _db.Locations.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == item.LocationId && x.TenantId == _tenantContext.TenantId, ct);

        return Ok(new { stopPoint = item, location });
    }

    public sealed class UpsertStopPointRequest
    {
        public Guid LocationId { get; set; }
        public StopPointType Type { get; set; } = StopPointType.Terminal;
        public string Name { get; set; } = "";
        public string? AddressLine { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Notes { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertStopPointRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        Validate(req);

        var locationOk = await _db.Locations.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == req.LocationId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (!locationOk)
            return BadRequest(new { message = "LocationId is invalid for this tenant." });

        var entity = new StopPoint
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            LocationId = req.LocationId,
            Type = req.Type,
            Name = req.Name.Trim(),
            AddressLine = req.AddressLine?.Trim(),
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            Notes = req.Notes?.Trim(),
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.BusStopPoints.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertStopPointRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        Validate(req);

        var entity = await _db.BusStopPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "StopPoint not found in this tenant." });

        var locationOk = await _db.Locations.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == req.LocationId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (!locationOk)
            return BadRequest(new { message = "LocationId is invalid for this tenant." });

        entity.LocationId = req.LocationId;
        entity.Type = req.Type;
        entity.Name = req.Name.Trim();
        entity.AddressLine = req.AddressLine?.Trim();
        entity.Latitude = req.Latitude;
        entity.Longitude = req.Longitude;
        entity.Notes = req.Notes?.Trim();
        entity.SortOrder = req.SortOrder;
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

        var entity = await _db.BusStopPoints.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);
        if (entity is null)
            return NotFound(new { message = "StopPoint not found in this tenant." });

        _db.BusStopPoints.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.BusStopPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "StopPoint not found." });

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

    private static void Validate(UpsertStopPointRequest req)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        if (req.LocationId == Guid.Empty)
            throw new InvalidOperationException("LocationId is required.");

        if (string.IsNullOrWhiteSpace(req.Name))
            throw new InvalidOperationException("Name is required.");

        if (req.Name.Length > 200)
            throw new InvalidOperationException("Name max length is 200.");

        if (req.AddressLine is not null && req.AddressLine.Length > 300)
            throw new InvalidOperationException("AddressLine max length is 300.");
    }
}
