//FILE: TicketBooking.Api/Controllers/Admin/FlightOffersAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/flight/offers")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class FlightOffersAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public FlightOffersAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public sealed class OfferUpsertRequest
    {
        public Guid AirlineId { get; set; }
        public Guid FlightId { get; set; }
        public Guid FareClassId { get; set; }

        public OfferStatus Status { get; set; } = OfferStatus.Active;

        public string CurrencyCode { get; set; } = "VND";
        public decimal BaseFare { get; set; }
        public decimal TaxesFees { get; set; }
        public decimal TotalPrice { get; set; }

        public int SeatsAvailable { get; set; } = 9;

        public DateTimeOffset RequestedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }

        public string? ConditionsJson { get; set; }
        public string? MetadataJson { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? airlineId,
        [FromQuery] Guid? flightId,
        [FromQuery] Guid? fareClassId,
        [FromQuery] OfferStatus? status,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool onlyNotExpired = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);

        IQueryable<Offer> query = _db.FlightOffers.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (airlineId.HasValue && airlineId.Value != Guid.Empty)
            query = query.Where(x => x.AirlineId == airlineId.Value);

        if (flightId.HasValue && flightId.Value != Guid.Empty)
            query = query.Where(x => x.FlightId == flightId.Value);

        if (fareClassId.HasValue && fareClassId.Value != Guid.Empty)
            query = query.Where(x => x.FareClassId == fareClassId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (onlyNotExpired)
            query = query.Where(x => x.ExpiresAt > now);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.RequestedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.FlightId,
                x.FareClassId,
                Status = x.Status.ToString(),
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice,
                x.SeatsAvailable,
                x.RequestedAt,
                x.ExpiresAt,
                x.ConditionsJson,
                x.MetadataJson,
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

        IQueryable<Offer> query = _db.FlightOffers.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.FlightId,
                x.FareClassId,
                Status = x.Status.ToString(),
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice,
                x.SeatsAvailable,
                x.RequestedAt,
                x.ExpiresAt,
                x.ConditionsJson,
                x.MetadataJson,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return NotFound(new { message = "Offer not found." });

        var segmentCount = await _db.FlightOfferSegments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.OfferId == id && !x.IsDeleted, ct);

        var taxFeeLineCount = await _db.FlightOfferTaxFeeLines
            .IgnoreQueryFilters()
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.OfferId == id && !x.IsDeleted, ct);

        return Ok(new
        {
            item,
            segmentCount,
            taxFeeLineCount
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] OfferUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var validation = await ValidateUpsertAsync(tenantId, req, ct);
        if (validation is not null)
            return validation;

        var entity = new Offer
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AirlineId = req.AirlineId,
            FlightId = req.FlightId,
            FareClassId = req.FareClassId,
            Status = req.Status,
            CurrencyCode = NormalizeCurrency(req.CurrencyCode)!,
            BaseFare = req.BaseFare,
            TaxesFees = req.TaxesFees,
            TotalPrice = req.TotalPrice,
            SeatsAvailable = req.SeatsAvailable,
            RequestedAt = req.RequestedAt,
            ExpiresAt = req.ExpiresAt,
            ConditionsJson = NormalizeJson(req.ConditionsJson),
            MetadataJson = NormalizeJson(req.MetadataJson),
            IsDeleted = false,
            CreatedAt = DateTimeOffset.Now
        };

        _db.FlightOffers.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new { entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] OfferUpsertRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        var entity = await _db.FlightOffers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Offer not found." });

        var validation = await ValidateUpsertAsync(tenantId, req, ct);
        if (validation is not null)
            return validation;

        entity.AirlineId = req.AirlineId;
        entity.FlightId = req.FlightId;
        entity.FareClassId = req.FareClassId;
        entity.Status = req.Status;
        entity.CurrencyCode = NormalizeCurrency(req.CurrencyCode)!;
        entity.BaseFare = req.BaseFare;
        entity.TaxesFees = req.TaxesFees;
        entity.TotalPrice = req.TotalPrice;
        entity.SeatsAvailable = req.SeatsAvailable;
        entity.RequestedAt = req.RequestedAt;
        entity.ExpiresAt = req.ExpiresAt;
        entity.ConditionsJson = NormalizeJson(req.ConditionsJson);
        entity.MetadataJson = NormalizeJson(req.MetadataJson);
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

        var entity = await _db.FlightOffers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Offer not found." });

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

        var entity = await _db.FlightOffers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Offer not found." });

        if (entity.IsDeleted)
        {
            entity.IsDeleted = false;
            entity.UpdatedAt = DateTimeOffset.Now;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    private async Task<IActionResult?> ValidateUpsertAsync(
        Guid tenantId,
        OfferUpsertRequest req,
        CancellationToken ct)
    {
        if (req.AirlineId == Guid.Empty)
            return BadRequest(new { message = "AirlineId is required." });

        if (req.FlightId == Guid.Empty)
            return BadRequest(new { message = "FlightId is required." });

        if (req.FareClassId == Guid.Empty)
            return BadRequest(new { message = "FareClassId is required." });

        var currencyCode = NormalizeCurrency(req.CurrencyCode);
        if (currencyCode is null)
            return BadRequest(new { message = "CurrencyCode must be 3 letters (e.g. VND, USD)." });

        if (req.BaseFare < 0)
            return BadRequest(new { message = "BaseFare must be >= 0." });

        if (req.TaxesFees < 0)
            return BadRequest(new { message = "TaxesFees must be >= 0." });

        if (req.TotalPrice < 0)
            return BadRequest(new { message = "TotalPrice must be >= 0." });

        if (req.SeatsAvailable < 0)
            return BadRequest(new { message = "SeatsAvailable must be >= 0." });

        if (req.ExpiresAt <= req.RequestedAt)
            return BadRequest(new { message = "ExpiresAt must be after RequestedAt." });

        var airlineExists = await _db.FlightAirlines.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.AirlineId && !x.IsDeleted, ct);

        if (!airlineExists)
            return BadRequest(new { message = "AirlineId not found." });

        var flight = await _db.FlightFlights.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.Id == req.FlightId && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.AirlineId
            })
            .FirstOrDefaultAsync(ct);

        if (flight is null)
            return BadRequest(new { message = "FlightId not found." });

        if (flight.AirlineId != req.AirlineId)
            return BadRequest(new { message = "Flight does not belong to the specified AirlineId." });

        var fareClass = await _db.FlightFareClasses.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.Id == req.FareClassId && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.AirlineId
            })
            .FirstOrDefaultAsync(ct);

        if (fareClass is null)
            return BadRequest(new { message = "FareClassId not found." });

        if (fareClass.AirlineId != req.AirlineId)
            return BadRequest(new { message = "FareClass does not belong to the specified AirlineId." });

        if (currencyCode is null)
            return BadRequest(new { message = "CurrencyCode must be 3 letters (e.g. VND, USD)." });

        return null;
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
