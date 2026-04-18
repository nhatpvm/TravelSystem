//FILE: TicketBooking.Api/Controllers/Admin/FlightFareRulesAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/flight/fare-rules")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class FlightFareRulesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public FlightFareRulesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public sealed class FareRuleUpsertRequest
    {
        public Guid FareClassId { get; set; }
        public bool IsActive { get; set; } = true;
        public string RulesJson { get; set; } = "{}";
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? airlineId,
        [FromQuery] Guid? fareClassId,
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

        IQueryable<FareRule> query = _db.FlightFareRules.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (fareClassId.HasValue && fareClassId.Value != Guid.Empty)
        {
            query = query.Where(x => x.FareClassId == fareClassId.Value);
        }
        else if (airlineId.HasValue && airlineId.Value != Guid.Empty)
        {
            var fareClassIds = _db.FlightFareClasses.AsNoTracking()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.AirlineId == airlineId.Value)
                .Select(x => x.Id);

            query = query.Where(x => fareClassIds.Contains(x.FareClassId));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.FareClassId,
                x.IsActive,
                x.RulesJson,
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

        IQueryable<FareRule> query = _db.FlightFareRules.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.FareClassId,
                x.IsActive,
                x.RulesJson,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return NotFound(new { message = "Fare rule not found." });

        return Ok(item);
    }

    /// <summary>
    /// 1 FareClass = 1 FareRule document.
    /// If a deleted row already exists for the FareClass, revive it instead of creating a new row.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] FareRuleUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        if (req.FareClassId == Guid.Empty)
            return BadRequest(new { message = "FareClassId is required." });

        var fareClassExists = await _db.FlightFareClasses.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.FareClassId && !x.IsDeleted, ct);

        if (!fareClassExists)
            return BadRequest(new { message = "FareClassId not found." });

        var existing = await _db.FlightFareRules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.FareClassId == req.FareClassId, ct);

        if (existing is not null && !existing.IsDeleted)
            return Conflict(new { message = "Fare rule already exists for this FareClass. Use PUT to update." });

        var now = DateTimeOffset.Now;
        var rulesJson = NormalizeJson(req.RulesJson) ?? "{}";

        if (existing is not null)
        {
            existing.IsDeleted = false;
            existing.IsActive = req.IsActive;
            existing.RulesJson = rulesJson;
            existing.UpdatedAt = now;

            await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                ok = true,
                id = existing.Id,
                revived = true
            });
        }

        var entity = new FareRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FareClassId = req.FareClassId,
            IsActive = req.IsActive,
            RulesJson = rulesJson,
            IsDeleted = false,
            CreatedAt = now
        };

        _db.FlightFareRules.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new { entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] FareRuleUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightFareRules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Fare rule not found." });

        if (req.FareClassId == Guid.Empty)
            return BadRequest(new { message = "FareClassId is required." });

        var fareClassExists = await _db.FlightFareClasses.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.FareClassId && !x.IsDeleted, ct);

        if (!fareClassExists)
            return BadRequest(new { message = "FareClassId not found." });

        var otherExists = await _db.FlightFareRules.IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.FareClassId == req.FareClassId &&
                x.Id != id &&
                !x.IsDeleted, ct);

        if (otherExists)
            return Conflict(new { message = "Another fare rule already exists for this FareClass." });

        entity.FareClassId = req.FareClassId;
        entity.IsActive = req.IsActive;
        entity.RulesJson = NormalizeJson(req.RulesJson) ?? "{}";
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

        var entity = await _db.FlightFareRules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Fare rule not found." });

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

        var entity = await _db.FlightFareRules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Fare rule not found." });

        if (entity.IsDeleted)
        {
            var otherExists = await _db.FlightFareRules.IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.TenantId == tenantId &&
                    x.FareClassId == entity.FareClassId &&
                    x.Id != id &&
                    !x.IsDeleted, ct);

            if (otherExists)
                return Conflict(new { message = "Cannot restore: another fare rule already exists for this FareClass." });

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTimeOffset.Now;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    private static string? NormalizeJson(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        return input.Trim();
    }
}
