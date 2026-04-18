//FILE: TicketBooking.Api/Controllers/Admin/FlightAirlinesAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/flight/airlines")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class FlightAirlinesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public FlightAirlinesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public sealed class AirlineUpsertRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? IataCode { get; set; }
        public string? IcaoCode { get; set; }
        public string? LogoUrl { get; set; }
        public string? WebsiteUrl { get; set; }
        public string? SupportPhone { get; set; }
        public string? SupportEmail { get; set; }
        public bool IsActive { get; set; } = true;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
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

        IQueryable<Airline> query = _db.FlightAirlines.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var uq = q.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.Code.ToUpper().Contains(uq) ||
                x.Name.ToUpper().Contains(uq) ||
                (x.IataCode != null && x.IataCode.ToUpper().Contains(uq)) ||
                (x.IcaoCode != null && x.IcaoCode.ToUpper().Contains(uq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.IataCode,
                x.IcaoCode,
                x.LogoUrl,
                x.WebsiteUrl,
                x.SupportPhone,
                x.SupportEmail,
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

        IQueryable<Airline> query = _db.FlightAirlines.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.IataCode,
                x.IcaoCode,
                x.LogoUrl,
                x.WebsiteUrl,
                x.SupportPhone,
                x.SupportEmail,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return NotFound(new { message = "Airline not found." });

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] AirlineUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var code = NormalizeRequired(req.Code, 50);
        var name = NormalizeRequired(req.Name, 200);

        if (code is null)
            return BadRequest(new { message = "Code is required." });

        if (name is null)
            return BadRequest(new { message = "Name is required." });

        var iataCode = NormalizeCode(req.IataCode, 8);
        var icaoCode = NormalizeCode(req.IcaoCode, 8);

        var codeExists = await _db.FlightAirlines.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (codeExists)
            return Conflict(new { message = $"Airline code '{code}' already exists." });

        if (!string.IsNullOrWhiteSpace(iataCode))
        {
            var iataExists = await _db.FlightAirlines.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.IataCode == iataCode, ct);

            if (iataExists)
                return Conflict(new { message = $"IATA '{iataCode}' already exists." });
        }

        if (!string.IsNullOrWhiteSpace(icaoCode))
        {
            var icaoExists = await _db.FlightAirlines.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.IcaoCode == icaoCode, ct);

            if (icaoExists)
                return Conflict(new { message = $"ICAO '{icaoCode}' already exists." });
        }

        var entity = new Airline
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = name,
            IataCode = iataCode,
            IcaoCode = icaoCode,
            LogoUrl = TrimOrNull(req.LogoUrl, 1000),
            WebsiteUrl = TrimOrNull(req.WebsiteUrl, 1000),
            SupportPhone = TrimOrNull(req.SupportPhone, 50),
            SupportEmail = TrimOrNull(req.SupportEmail, 200),
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.FlightAirlines.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new { entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] AirlineUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightAirlines.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Airline not found." });

        var code = NormalizeRequired(req.Code, 50);
        var name = NormalizeRequired(req.Name, 200);

        if (code is null)
            return BadRequest(new { message = "Code is required." });

        if (name is null)
            return BadRequest(new { message = "Name is required." });

        var iataCode = NormalizeCode(req.IataCode, 8);
        var icaoCode = NormalizeCode(req.IcaoCode, 8);

        var codeExists = await _db.FlightAirlines.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code && x.Id != id, ct);

        if (codeExists)
            return Conflict(new { message = $"Airline code '{code}' already exists." });

        if (!string.IsNullOrWhiteSpace(iataCode))
        {
            var iataExists = await _db.FlightAirlines.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.IataCode == iataCode && x.Id != id, ct);

            if (iataExists)
                return Conflict(new { message = $"IATA '{iataCode}' already exists." });
        }

        if (!string.IsNullOrWhiteSpace(icaoCode))
        {
            var icaoExists = await _db.FlightAirlines.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.IcaoCode == icaoCode && x.Id != id, ct);

            if (icaoExists)
                return Conflict(new { message = $"ICAO '{icaoCode}' already exists." });
        }

        entity.Code = code;
        entity.Name = name;
        entity.IataCode = iataCode;
        entity.IcaoCode = icaoCode;
        entity.LogoUrl = TrimOrNull(req.LogoUrl, 1000);
        entity.WebsiteUrl = TrimOrNull(req.WebsiteUrl, 1000);
        entity.SupportPhone = TrimOrNull(req.SupportPhone, 50);
        entity.SupportEmail = TrimOrNull(req.SupportEmail, 200);
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

        var entity = await _db.FlightAirlines.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Airline not found." });

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

        var entity = await _db.FlightAirlines.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Airline not found." });

        if (entity.IsDeleted)
        {
            var codeExists = await _db.FlightAirlines.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Code == entity.Code && x.Id != id && !x.IsDeleted, ct);

            if (codeExists)
                return Conflict(new { message = $"Cannot restore: airline code '{entity.Code}' already exists." });

            if (!string.IsNullOrWhiteSpace(entity.IataCode))
            {
                var iataExists = await _db.FlightAirlines.IgnoreQueryFilters()
                    .AnyAsync(x => x.TenantId == tenantId && x.IataCode == entity.IataCode && x.Id != id && !x.IsDeleted, ct);

                if (iataExists)
                    return Conflict(new { message = $"Cannot restore: IATA '{entity.IataCode}' already exists." });
            }

            if (!string.IsNullOrWhiteSpace(entity.IcaoCode))
            {
                var icaoExists = await _db.FlightAirlines.IgnoreQueryFilters()
                    .AnyAsync(x => x.TenantId == tenantId && x.IcaoCode == entity.IcaoCode && x.Id != id && !x.IsDeleted, ct);

                if (icaoExists)
                    return Conflict(new { message = $"Cannot restore: ICAO '{entity.IcaoCode}' already exists." });
            }

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

    private static string? NormalizeCode(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim().ToUpperInvariant();
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static string? TrimOrNull(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim();
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
