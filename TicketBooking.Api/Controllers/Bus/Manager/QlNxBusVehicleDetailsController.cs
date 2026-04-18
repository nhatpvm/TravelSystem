// FILE #287: TicketBooking.Api/Controllers/QlNxBusVehicleDetailsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Fleet;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlnx/fleet/bus-vehicle-details")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX}")]
public sealed class QlNxBusVehicleDetailsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlNxBusVehicleDetailsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] bool includeDeleted = false,
        [FromQuery] Guid? vehicleId = null,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        IQueryable<BusVehicleDetail> query = _db.BusVehicleDetails;

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == _tenantContext.TenantId);

        if (vehicleId.HasValue && vehicleId.Value != Guid.Empty)
            query = query.Where(x => x.VehicleId == vehicleId.Value);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.VehicleId,
                x.BusType,
                x.AmenitiesJson,
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

        IQueryable<BusVehicleDetail> query = _db.BusVehicleDetails;
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (item is null)
            return NotFound(new { message = "BusVehicleDetail not found in this tenant." });

        return Ok(item);
    }

    public sealed class UpsertBusVehicleDetailRequest
    {
        public Guid VehicleId { get; set; }
        public string? BusType { get; set; }
        public string? AmenitiesJson { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] UpsertBusVehicleDetailRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        if (req is null)
            return BadRequest(new { message = "Request body is required." });

        if (req.VehicleId == Guid.Empty)
            return BadRequest(new { message = "VehicleId is required." });

        var vehicle = await _db.Vehicles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == req.VehicleId &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted, ct);

        if (vehicle is null)
            return BadRequest(new { message = "VehicleId is invalid for this tenant." });

        if (vehicle.VehicleType != VehicleType.Bus && vehicle.VehicleType != VehicleType.TourBus)
            return BadRequest(new { message = "Vehicle must be type Bus or TourBus." });

        var exists = await _db.BusVehicleDetails.IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == _tenantContext.TenantId &&
                x.VehicleId == req.VehicleId &&
                !x.IsDeleted, ct);

        if (exists)
            return Conflict(new { message = "BusVehicleDetail already exists for this vehicle." });

        var entity = new BusVehicleDetail
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            VehicleId = req.VehicleId,
            BusType = req.BusType?.Trim(),
            AmenitiesJson = req.AmenitiesJson,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.BusVehicleDetails.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = entity.Id, version = "1.0" }, entity);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpsertBusVehicleDetailRequest req,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        if (req is null)
            return BadRequest(new { message = "Request body is required." });

        if (req.VehicleId == Guid.Empty)
            return BadRequest(new { message = "VehicleId is required." });

        var entity = await _db.BusVehicleDetails.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "BusVehicleDetail not found in this tenant." });

        var vehicle = await _db.Vehicles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == req.VehicleId &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted, ct);

        if (vehicle is null)
            return BadRequest(new { message = "VehicleId is invalid for this tenant." });

        if (vehicle.VehicleType != VehicleType.Bus && vehicle.VehicleType != VehicleType.TourBus)
            return BadRequest(new { message = "Vehicle must be type Bus or TourBus." });

        var exists = await _db.BusVehicleDetails.IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == _tenantContext.TenantId &&
                x.VehicleId == req.VehicleId &&
                x.Id != id &&
                !x.IsDeleted, ct);

        if (exists)
            return Conflict(new { message = "Another BusVehicleDetail already exists for that vehicle." });

        entity.VehicleId = req.VehicleId;
        entity.BusType = req.BusType?.Trim();
        entity.AmenitiesJson = req.AmenitiesJson;
        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(
        Guid id,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.BusVehicleDetails
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "BusVehicleDetail not found." });

        if (entity.TenantId != _tenantContext.TenantId)
            return NotFound(new { message = "BusVehicleDetail not found in this tenant." });

        _db.BusVehicleDetails.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(
        Guid id,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        var entity = await _db.BusVehicleDetails.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == _tenantContext.TenantId, ct);

        if (entity is null)
            return NotFound(new { message = "BusVehicleDetail not found." });

        var vehicle = await _db.Vehicles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == entity.VehicleId &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted, ct);

        if (vehicle is null)
            return BadRequest(new { message = "Linked VehicleId is invalid for this tenant." });

        if (vehicle.VehicleType != VehicleType.Bus && vehicle.VehicleType != VehicleType.TourBus)
            return BadRequest(new { message = "Linked vehicle must be type Bus or TourBus." });

        var duplicate = await _db.BusVehicleDetails.IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == _tenantContext.TenantId &&
                x.VehicleId == entity.VehicleId &&
                x.Id != entity.Id &&
                !x.IsDeleted, ct);

        if (duplicate)
            return Conflict(new { message = "Cannot restore because another active BusVehicleDetail already exists for this vehicle." });

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
}

