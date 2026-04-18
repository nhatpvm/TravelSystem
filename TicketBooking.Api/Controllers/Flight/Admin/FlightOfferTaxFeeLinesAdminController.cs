//FILE: TicketBooking.Api/Controllers/Admin/FlightOfferTaxFeeLinesAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/flight/offers/tax-fee-lines")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class FlightOfferTaxFeeLinesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public FlightOfferTaxFeeLinesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public sealed class TaxFeeLineUpsertRequest
    {
        public Guid OfferId { get; set; }
        public int SortOrder { get; set; }
        public TaxFeeLineType LineType { get; set; } = TaxFeeLineType.Fee;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = "VND";
        public decimal Amount { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid offerId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        if (offerId == Guid.Empty)
            return BadRequest(new { message = "offerId is required." });

        var tenantId = _tenant.TenantId.Value;

        IQueryable<OfferTaxFeeLine> query = _db.FlightOfferTaxFeeLines.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var items = await query
            .Where(x => x.TenantId == tenantId && x.OfferId == offerId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.OfferId,
                x.SortOrder,
                LineType = x.LineType.ToString(),
                x.Code,
                x.Name,
                x.CurrencyCode,
                x.Amount,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new
        {
            count = items.Count,
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

        IQueryable<OfferTaxFeeLine> query = _db.FlightOfferTaxFeeLines.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.OfferId,
                x.SortOrder,
                LineType = x.LineType.ToString(),
                x.Code,
                x.Name,
                x.CurrencyCode,
                x.Amount,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return NotFound(new { message = "Offer tax/fee line not found." });

        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] TaxFeeLineUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        if (req.OfferId == Guid.Empty)
            return BadRequest(new { message = "OfferId is required." });

        if (req.SortOrder < 0)
            return BadRequest(new { message = "SortOrder must be >= 0." });

        var code = NormalizeRequired(req.Code, 50);
        var name = NormalizeRequired(req.Name, 200);
        var currencyCode = NormalizeCurrency(req.CurrencyCode);

        if (code is null)
            return BadRequest(new { message = "Code is required." });

        if (name is null)
            return BadRequest(new { message = "Name is required." });

        if (currencyCode is null)
            return BadRequest(new { message = "CurrencyCode must be 3 letters (e.g. VND, USD)." });

        if (req.Amount < 0)
            return BadRequest(new { message = "Amount must be >= 0." });

        var offerExists = await _db.FlightOffers.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.OfferId && !x.IsDeleted, ct);

        if (!offerExists)
            return BadRequest(new { message = "OfferId not found." });

        var exists = await _db.FlightOfferTaxFeeLines.IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.OfferId == req.OfferId &&
                x.SortOrder == req.SortOrder &&
                x.Code == code, ct);

        if (exists)
            return Conflict(new { message = "A line with same OfferId + SortOrder + Code already exists." });

        var entity = new OfferTaxFeeLine
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OfferId = req.OfferId,
            SortOrder = req.SortOrder,
            LineType = req.LineType,
            Code = code,
            Name = name,
            CurrencyCode = currencyCode,
            Amount = req.Amount,
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.FlightOfferTaxFeeLines.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new { entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] TaxFeeLineUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightOfferTaxFeeLines.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Offer tax/fee line not found." });

        if (req.OfferId == Guid.Empty)
            return BadRequest(new { message = "OfferId is required." });

        if (req.SortOrder < 0)
            return BadRequest(new { message = "SortOrder must be >= 0." });

        var code = NormalizeRequired(req.Code, 50);
        var name = NormalizeRequired(req.Name, 200);
        var currencyCode = NormalizeCurrency(req.CurrencyCode);

        if (code is null)
            return BadRequest(new { message = "Code is required." });

        if (name is null)
            return BadRequest(new { message = "Name is required." });

        if (currencyCode is null)
            return BadRequest(new { message = "CurrencyCode must be 3 letters (e.g. VND, USD)." });

        if (req.Amount < 0)
            return BadRequest(new { message = "Amount must be >= 0." });

        var offerExists = await _db.FlightOffers.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.OfferId && !x.IsDeleted, ct);

        if (!offerExists)
            return BadRequest(new { message = "OfferId not found." });

        var exists = await _db.FlightOfferTaxFeeLines.IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.OfferId == req.OfferId &&
                x.SortOrder == req.SortOrder &&
                x.Code == code &&
                x.Id != id, ct);

        if (exists)
            return Conflict(new { message = "A line with same OfferId + SortOrder + Code already exists." });

        entity.OfferId = req.OfferId;
        entity.SortOrder = req.SortOrder;
        entity.LineType = req.LineType;
        entity.Code = code;
        entity.Name = name;
        entity.CurrencyCode = currencyCode;
        entity.Amount = req.Amount;
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

        var entity = await _db.FlightOfferTaxFeeLines.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Offer tax/fee line not found." });

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

        var entity = await _db.FlightOfferTaxFeeLines.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Offer tax/fee line not found." });

        if (entity.IsDeleted)
        {
            var exists = await _db.FlightOfferTaxFeeLines.IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.TenantId == tenantId &&
                    x.OfferId == entity.OfferId &&
                    x.SortOrder == entity.SortOrder &&
                    x.Code == entity.Code &&
                    x.Id != id &&
                    !x.IsDeleted, ct);

            if (exists)
                return Conflict(new { message = "Cannot restore: another active line with same OfferId + SortOrder + Code exists." });

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
}
