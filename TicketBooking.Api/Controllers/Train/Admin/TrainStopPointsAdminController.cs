// FILE #080: TicketBooking.Api/Controllers/TrainStopPointsAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/train/stop-points")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class TrainStopPointsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public TrainStopPointsAdminController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] TrainStopPointType? type = null,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        IQueryable<TrainStopPoint> query = _db.TrainStopPoints;

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x => x.Name.Contains(keyword) || (x.AddressLine != null && x.AddressLine.Contains(keyword)));
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
                x.IsActive,
                x.IsDeleted,
                x.SortOrder
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        IQueryable<TrainStopPoint> query = _db.TrainStopPoints;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound(new { message = "TrainStopPoint not found." });

        var loc = await _db.Locations.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == item.LocationId, ct);

        return Ok(new { stopPoint = item, location = loc });
    }

    public sealed class UpsertStopPointRequest
    {
        public Guid LocationId { get; set; }
        public TrainStopPointType Type { get; set; } = TrainStopPointType.Station;
        public string Name { get; set; } = "";
        public string? AddressLine { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Notes { get; set; }
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UpsertStopPointRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "X-TenantId is required for admin write requests." });

        Validate(req);

        var locOk = await _db.Locations.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == req.LocationId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (!locOk) return BadRequest(new { message = "LocationId is invalid for this tenant." });

        var entity = new TrainStopPoint
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            LocationId = req.LocationId,
            Type = req.Type,
            Name = req.Name.Trim(),
            AddressLine = req.AddressLine?.Trim(),
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            Notes = req.Notes,
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.TrainStopPoints.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertStopPointRequest req, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "X-TenantId is required for admin write requests." });

        Validate(req);

        var entity = await _db.TrainStopPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "TrainStopPoint not found in this tenant." });

        var locOk = await _db.Locations.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == req.LocationId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (!locOk) return BadRequest(new { message = "LocationId is invalid for this tenant." });

        entity.LocationId = req.LocationId;
        entity.Type = req.Type;
        entity.Name = req.Name.Trim();
        entity.AddressLine = req.AddressLine?.Trim();
        entity.Latitude = req.Latitude;
        entity.Longitude = req.Longitude;
        entity.Notes = req.Notes;
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
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "X-TenantId is required for admin write requests." });

        var entity = await _db.TrainStopPoints.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound(new { message = "TrainStopPoint not found." });

        _db.TrainStopPoints.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "X-TenantId is required for admin write requests." });

        var entity = await _db.TrainStopPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "TrainStopPoint not found." });

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    private static void Validate(UpsertStopPointRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.LocationId == Guid.Empty) throw new InvalidOperationException("LocationId is required.");
        if (string.IsNullOrWhiteSpace(req.Name)) throw new InvalidOperationException("Name is required.");
        if (req.Name.Length > 200) throw new InvalidOperationException("Name max length is 200.");
        if (req.AddressLine is not null && req.AddressLine.Length > 300) throw new InvalidOperationException("AddressLine max length is 300.");
    }
}