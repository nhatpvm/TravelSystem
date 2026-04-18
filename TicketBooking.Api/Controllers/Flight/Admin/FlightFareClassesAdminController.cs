//FILE: TicketBooking.Api/Controllers/Admin/FlightFareClassesAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Flight;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/flight/fare-classes")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class FlightFareClassesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public FlightFareClassesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public sealed class FareClassUpsertRequest
    {
        public Guid AirlineId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public CabinClass CabinClass { get; set; } = CabinClass.Economy;
        public bool IsRefundable { get; set; }
        public bool IsChangeable { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] Guid? airlineId,
        [FromQuery] CabinClass? cabinClass,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);

        IQueryable<FareClass> query = _db.FlightFareClasses.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (airlineId.HasValue && airlineId.Value != Guid.Empty)
            query = query.Where(x => x.AirlineId == airlineId.Value);

        if (cabinClass.HasValue)
            query = query.Where(x => x.CabinClass == cabinClass.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var uq = q.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.Code.ToUpper().Contains(uq) ||
                x.Name.ToUpper().Contains(uq));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.AirlineId)
            .ThenBy(x => x.CabinClass)
            .ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.Code,
                x.Name,
                CabinClass = x.CabinClass.ToString(),
                x.IsRefundable,
                x.IsChangeable,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new
        {
            page,
            pageSize,
            total,
            items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        IQueryable<FareClass> query = _db.FlightFareClasses.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.Code,
                x.Name,
                CabinClass = x.CabinClass.ToString(),
                x.IsRefundable,
                x.IsChangeable,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return NotFound(new { message = "Fare class not found." });

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] FareClassUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var validationError = await ValidateUpsertAsync(tenantId, req, idToExclude: null, ct);
        if (validationError is not null)
            return validationError;

        var entity = new FareClass
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AirlineId = req.AirlineId,
            Code = NormalizeRequired(req.Code, 10)!,
            Name = NormalizeRequired(req.Name, 200)!,
            CabinClass = req.CabinClass,
            IsRefundable = req.IsRefundable,
            IsChangeable = req.IsChangeable,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.FlightFareClasses.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new { entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] FareClassUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightFareClasses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Fare class not found." });

        var validationError = await ValidateUpsertAsync(tenantId, req, id, ct);
        if (validationError is not null)
            return validationError;

        entity.AirlineId = req.AirlineId;
        entity.Code = NormalizeRequired(req.Code, 10)!;
        entity.Name = NormalizeRequired(req.Name, 200)!;
        entity.CabinClass = req.CabinClass;
        entity.IsRefundable = req.IsRefundable;
        entity.IsChangeable = req.IsChangeable;
        entity.IsActive = req.IsActive;
        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightFareClasses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Fare class not found." });

        if (!entity.IsDeleted)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTimeOffset.Now;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(
        [FromRoute] Guid id,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightFareClasses.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Fare class not found." });

        if (entity.IsDeleted)
        {
            var exists = await _db.FlightFareClasses.IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.TenantId == tenantId &&
                    x.AirlineId == entity.AirlineId &&
                    x.Code == entity.Code &&
                    x.Id != id &&
                    !x.IsDeleted, ct);

            if (exists)
                return Conflict(new { message = $"Cannot restore: fare class '{entity.Code}' already exists for this airline." });

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTimeOffset.Now;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    private async Task<IActionResult?> ValidateUpsertAsync(
        Guid tenantId,
        FareClassUpsertRequest req,
        Guid? idToExclude,
        CancellationToken ct)
    {
        if (req.AirlineId == Guid.Empty)
            return BadRequest(new { message = "AirlineId is required." });

        var code = NormalizeRequired(req.Code, 10);
        var name = NormalizeRequired(req.Name, 200);

        if (code is null)
            return BadRequest(new { message = "Code is required." });

        if (name is null)
            return BadRequest(new { message = "Name is required." });

        var airlineExists = await _db.FlightAirlines.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.AirlineId && !x.IsDeleted, ct);

        if (!airlineExists)
            return BadRequest(new { message = "AirlineId not found." });

        var exists = await _db.FlightFareClasses.IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.AirlineId == req.AirlineId &&
                x.Code == code &&
                (!idToExclude.HasValue || x.Id != idToExclude.Value), ct);

        if (exists)
            return Conflict(new { message = $"Fare class '{code}' already exists for this airline." });

        return null;
    }

    private static string? NormalizeRequired(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
