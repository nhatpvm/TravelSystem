//FILE: TicketBooking.Api/Controllers/Admin/FlightAncillaryDefinitionsAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/flight/ancillaries")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class FlightAncillaryDefinitionsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public FlightAncillaryDefinitionsAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public sealed class AncillaryUpsertRequest
    {
        public Guid AirlineId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AncillaryType Type { get; set; } = AncillaryType.Other;
        public string CurrencyCode { get; set; } = "VND";
        public decimal Price { get; set; }
        public string? RulesJson { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] Guid? airlineId,
        [FromQuery] AncillaryType? type,
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

        IQueryable<AncillaryDefinition> query = _db.FlightAncillaryDefinitions.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (airlineId.HasValue && airlineId.Value != Guid.Empty)
            query = query.Where(x => x.AirlineId == airlineId.Value);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

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
            .ThenBy(x => x.Type)
            .ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.Code,
                x.Name,
                Type = x.Type.ToString(),
                x.CurrencyCode,
                x.Price,
                x.RulesJson,
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

        IQueryable<AncillaryDefinition> query = _db.FlightAncillaryDefinitions.AsNoTracking();

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
                Type = x.Type.ToString(),
                x.CurrencyCode,
                x.Price,
                x.RulesJson,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return NotFound(new { message = "Ancillary definition not found." });

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] AncillaryUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        if (req.AirlineId == Guid.Empty)
            return BadRequest(new { message = "AirlineId is required." });

        var code = NormalizeRequired(req.Code, 80);
        var name = NormalizeRequired(req.Name, 200);
        var currencyCode = NormalizeCurrency(req.CurrencyCode);

        if (code is null)
            return BadRequest(new { message = "Code is required." });

        if (name is null)
            return BadRequest(new { message = "Name is required." });

        if (currencyCode is null)
            return BadRequest(new { message = "CurrencyCode must be 3 letters (e.g. VND, USD)." });

        if (req.Price < 0)
            return BadRequest(new { message = "Price must be >= 0." });

        var airlineExists = await _db.FlightAirlines.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.AirlineId && !x.IsDeleted, ct);

        if (!airlineExists)
            return BadRequest(new { message = "AirlineId not found." });

        var exists = await _db.FlightAncillaryDefinitions.IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.AirlineId == req.AirlineId &&
                x.Code == code, ct);

        if (exists)
            return Conflict(new { message = $"Ancillary '{code}' already exists for this airline." });

        var entity = new AncillaryDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AirlineId = req.AirlineId,
            Code = code,
            Name = name,
            Type = req.Type,
            CurrencyCode = currencyCode,
            Price = req.Price,
            RulesJson = NormalizeJson(req.RulesJson),
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.FlightAncillaryDefinitions.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new { entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] AncillaryUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightAncillaryDefinitions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Ancillary definition not found." });

        if (req.AirlineId == Guid.Empty)
            return BadRequest(new { message = "AirlineId is required." });

        var code = NormalizeRequired(req.Code, 80);
        var name = NormalizeRequired(req.Name, 200);
        var currencyCode = NormalizeCurrency(req.CurrencyCode);

        if (code is null)
            return BadRequest(new { message = "Code is required." });

        if (name is null)
            return BadRequest(new { message = "Name is required." });

        if (currencyCode is null)
            return BadRequest(new { message = "CurrencyCode must be 3 letters (e.g. VND, USD)." });

        if (req.Price < 0)
            return BadRequest(new { message = "Price must be >= 0." });

        var airlineExists = await _db.FlightAirlines.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.AirlineId && !x.IsDeleted, ct);

        if (!airlineExists)
            return BadRequest(new { message = "AirlineId not found." });

        var exists = await _db.FlightAncillaryDefinitions.IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.AirlineId == req.AirlineId &&
                x.Code == code &&
                x.Id != id, ct);

        if (exists)
            return Conflict(new { message = $"Ancillary '{code}' already exists for this airline." });

        entity.AirlineId = req.AirlineId;
        entity.Code = code;
        entity.Name = name;
        entity.Type = req.Type;
        entity.CurrencyCode = currencyCode;
        entity.Price = req.Price;
        entity.RulesJson = NormalizeJson(req.RulesJson);
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

        var entity = await _db.FlightAncillaryDefinitions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Ancillary definition not found." });

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

        var entity = await _db.FlightAncillaryDefinitions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Ancillary definition not found." });

        if (entity.IsDeleted)
        {
            var exists = await _db.FlightAncillaryDefinitions.IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.TenantId == tenantId &&
                    x.AirlineId == entity.AirlineId &&
                    x.Code == entity.Code &&
                    x.Id != id &&
                    !x.IsDeleted, ct);

            if (exists)
                return Conflict(new { message = $"Cannot restore: ancillary '{entity.Code}' already exists for this airline." });

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTimeOffset.Now;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    private static string? NormalizeRequired(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? NormalizeCurrency(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim().ToUpperInvariant();
        if (value.Length != 3)
            return null;

        foreach (var ch in value)
        {
            if (ch < 'A' || ch > 'Z')
                return null;
        }

        return value;
    }

    private static string? NormalizeJson(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        return input.Trim();
    }
}
