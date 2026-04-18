// FILE #071: TicketBooking.Api/Controllers/BusTripStopPointsAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/bus")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class BusTripStopPointsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public BusTripStopPointsAdminController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    // =========================
    // Pickup points
    // =========================

    [HttpGet("trip-stop-times/{tripStopTimeId:guid}/pickup-points")]
    public async Task<IActionResult> ListPickupPoints(Guid tripStopTimeId, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        IQueryable<TripStopPickupPoint> query = _db.BusTripStopPickupPoints.Where(x => x.TripStopTimeId == tripStopTimeId);

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var items = await query
            .OrderBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.TripStopTimeId,
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

        return Ok(new { items });
    }

    public sealed class UpsertPickupPointRequest
    {
        public string Name { get; set; } = "";
        public string? AddressLine { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsDefault { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    [HttpPost("trip-stop-times/{tripStopTimeId:guid}/pickup-points")]
    public async Task<IActionResult> CreatePickupPoint(Guid tripStopTimeId, [FromBody] UpsertPickupPointRequest req, CancellationToken ct = default)
    {
        RequireTenantWrite();
        ValidatePickup(req);

        var tst = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripStopTimeId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (tst is null) return BadRequest(new { message = "TripStopTimeId is invalid for this tenant." });

        // If set default, unset other defaults for this stop time
        if (req.IsDefault)
        {
            var others = await _db.BusTripStopPickupPoints.IgnoreQueryFilters()
                .Where(x => x.TripStopTimeId == tripStopTimeId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted && x.IsDefault)
                .ToListAsync(ct);

            foreach (var o in others)
            {
                o.IsDefault = false;
                o.UpdatedAt = DateTimeOffset.Now;
            }
        }

        var entity = new TripStopPickupPoint
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            TripStopTimeId = tripStopTimeId,
            Name = req.Name.Trim(),
            AddressLine = req.AddressLine?.Trim(),
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            IsDefault = req.IsDefault,
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.BusTripStopPickupPoints.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(entity);
    }

    [HttpPut("trip-stop-pickup-points/{id:guid}")]
    public async Task<IActionResult> UpdatePickupPoint(Guid id, [FromBody] UpsertPickupPointRequest req, CancellationToken ct = default)
    {
        RequireTenantWrite();
        ValidatePickup(req);

        var entity = await _db.BusTripStopPickupPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Pickup point not found in this tenant." });

        if (req.IsDefault && !entity.IsDefault)
        {
            var others = await _db.BusTripStopPickupPoints.IgnoreQueryFilters()
                .Where(x => x.TripStopTimeId == entity.TripStopTimeId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted && x.IsDefault)
                .ToListAsync(ct);

            foreach (var o in others)
            {
                o.IsDefault = false;
                o.UpdatedAt = DateTimeOffset.Now;
            }
        }

        entity.Name = req.Name.Trim();
        entity.AddressLine = req.AddressLine?.Trim();
        entity.Latitude = req.Latitude;
        entity.Longitude = req.Longitude;
        entity.IsDefault = req.IsDefault;
        entity.SortOrder = req.SortOrder;
        entity.IsActive = req.IsActive;

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("trip-stop-pickup-points/{id:guid}")]
    public async Task<IActionResult> DeletePickupPoint(Guid id, CancellationToken ct = default)
    {
        RequireTenantWrite();

        var entity = await _db.BusTripStopPickupPoints.FirstOrDefaultAsync(
        x => x.Id == id && x.TenantId == _tenantContext.TenantId,
        ct);

        if (entity is null)
            return NotFound(new { message = "Pickup point not found in this tenant." });

        _db.BusTripStopPickupPoints.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });

    }

    // =========================
    // Dropoff points
    // =========================

    [HttpGet("trip-stop-times/{tripStopTimeId:guid}/dropoff-points")]
    public async Task<IActionResult> ListDropoffPoints(Guid tripStopTimeId, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        IQueryable<TripStopDropoffPoint> query = _db.BusTripStopDropoffPoints.Where(x => x.TripStopTimeId == tripStopTimeId);

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var items = await query
            .OrderBy(x => x.SortOrder)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.TripStopTimeId,
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

        return Ok(new { items });
    }

    public sealed class UpsertDropoffPointRequest
    {
        public string Name { get; set; } = "";
        public string? AddressLine { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public bool IsDefault { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    [HttpPost("trip-stop-times/{tripStopTimeId:guid}/dropoff-points")]
    public async Task<IActionResult> CreateDropoffPoint(Guid tripStopTimeId, [FromBody] UpsertDropoffPointRequest req, CancellationToken ct = default)
    {
        RequireTenantWrite();
        ValidateDropoff(req);

        var tst = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripStopTimeId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted, ct);

        if (tst is null) return BadRequest(new { message = "TripStopTimeId is invalid for this tenant." });

        if (req.IsDefault)
        {
            var others = await _db.BusTripStopDropoffPoints.IgnoreQueryFilters()
                .Where(x => x.TripStopTimeId == tripStopTimeId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted && x.IsDefault)
                .ToListAsync(ct);

            foreach (var o in others)
            {
                o.IsDefault = false;
                o.UpdatedAt = DateTimeOffset.Now;
            }
        }

        var entity = new TripStopDropoffPoint
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            TripStopTimeId = tripStopTimeId,
            Name = req.Name.Trim(),
            AddressLine = req.AddressLine?.Trim(),
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            IsDefault = req.IsDefault,
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.BusTripStopDropoffPoints.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(entity);
    }

    [HttpPut("trip-stop-dropoff-points/{id:guid}")]
    public async Task<IActionResult> UpdateDropoffPoint(Guid id, [FromBody] UpsertDropoffPointRequest req, CancellationToken ct = default)
    {
        RequireTenantWrite();
        ValidateDropoff(req);

        var entity = await _db.BusTripStopDropoffPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null) return NotFound(new { message = "Dropoff point not found in this tenant." });

        if (req.IsDefault && !entity.IsDefault)
        {
            var others = await _db.BusTripStopDropoffPoints.IgnoreQueryFilters()
                .Where(x => x.TripStopTimeId == entity.TripStopTimeId && x.TenantId == _tenantContext.TenantId && !x.IsDeleted && x.IsDefault)
                .ToListAsync(ct);

            foreach (var o in others)
            {
                o.IsDefault = false;
                o.UpdatedAt = DateTimeOffset.Now;
            }
        }

        entity.Name = req.Name.Trim();
        entity.AddressLine = req.AddressLine?.Trim();
        entity.Latitude = req.Latitude;
        entity.Longitude = req.Longitude;
        entity.IsDefault = req.IsDefault;
        entity.SortOrder = req.SortOrder;
        entity.IsActive = req.IsActive;

        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("trip-stop-dropoff-points/{id:guid}")]
    public async Task<IActionResult> DeleteDropoffPoint(Guid id, CancellationToken ct = default)
    {
        RequireTenantWrite();

        var entity = await _db.BusTripStopDropoffPoints.FirstOrDefaultAsync(
            x => x.Id == id && x.TenantId == _tenantContext.TenantId,
            ct);

        if (entity is null)
            return NotFound(new { message = "Dropoff point not found in this tenant." });

        _db.BusTripStopDropoffPoints.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });

    }

    // =========================
    // Helpers
    // =========================

    private void RequireTenantWrite()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required for admin write requests.");
    }

    private static void ValidatePickup(UpsertPickupPointRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (string.IsNullOrWhiteSpace(req.Name)) throw new InvalidOperationException("Name is required.");
        if (req.Name.Length > 200) throw new InvalidOperationException("Name max length is 200.");
        if (req.AddressLine is not null && req.AddressLine.Length > 300) throw new InvalidOperationException("AddressLine max length is 300.");
    }

    private static void ValidateDropoff(UpsertDropoffPointRequest req)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (string.IsNullOrWhiteSpace(req.Name)) throw new InvalidOperationException("Name is required.");
        if (req.Name.Length > 200) throw new InvalidOperationException("Name max length is 200.");
        if (req.AddressLine is not null && req.AddressLine.Length > 300) throw new InvalidOperationException("AddressLine max length is 300.");
    }
}