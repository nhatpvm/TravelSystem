// FILE #248: TicketBooking.Api/Controllers/Tours/QlTourTourPricingController.cs
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/schedules/{scheduleId:guid}/prices")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class QlTourTourPricingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QlTourTourPricingController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<QlTourPricePagedResponse>> List(
        Guid tourId,
        Guid scheduleId,
        [FromQuery] TourPriceType? priceType = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();

        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : pageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        IQueryable<TourSchedulePrice> query = includeDeleted
            ? _db.TourSchedulePrices.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId)
            : _db.TourSchedulePrices
                .Where(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && !x.IsDeleted);

        if (priceType.HasValue)
            query = query.Where(x => x.PriceType == priceType.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.PriceType)
            .ThenBy(x => x.Price)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QlTourPriceListItemDto
            {
                Id = x.Id,
                TourScheduleId = x.TourScheduleId,
                PriceType = x.PriceType,
                CurrencyCode = x.CurrencyCode,
                Price = x.Price,
                OriginalPrice = x.OriginalPrice,
                Taxes = x.Taxes,
                Fees = x.Fees,
                MinAge = x.MinAge,
                MaxAge = x.MaxAge,
                MinQuantity = x.MinQuantity,
                MaxQuantity = x.MaxQuantity,
                IsDefault = x.IsDefault,
                IsIncludedTax = x.IsIncludedTax,
                IsIncludedFee = x.IsIncludedFee,
                Label = x.Label,
                Notes = x.Notes,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new QlTourPricePagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QlTourPriceDetailDto>> GetById(
        Guid tourId,
        Guid scheduleId,
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();

        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        IQueryable<TourSchedulePrice> query = includeDeleted
            ? _db.TourSchedulePrices.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId)
            : _db.TourSchedulePrices
                .Where(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && !x.IsDeleted);

        var entity = await query
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule price not found in current tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<QlTourCreatePriceResponse>> Create(
        Guid tourId,
        Guid scheduleId,
        [FromBody] QlTourCreatePriceRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();

        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);
        await ValidateCreateAsync(tenantId, scheduleId, req, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        if (req.IsDefault)
        {
            var oldDefaults = await _db.TourSchedulePrices.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && x.IsDefault)
                .ToListAsync(ct);

            foreach (var old in oldDefaults)
            {
                old.IsDefault = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }
        }

        var entity = new TourSchedulePrice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourScheduleId = scheduleId,
            PriceType = req.PriceType,
            CurrencyCode = req.CurrencyCode.Trim(),
            Price = req.Price,
            OriginalPrice = req.OriginalPrice,
            Taxes = req.Taxes,
            Fees = req.Fees,
            MinAge = req.MinAge,
            MaxAge = req.MaxAge,
            MinQuantity = req.MinQuantity,
            MaxQuantity = req.MaxQuantity,
            IsDefault = req.IsDefault,
            IsIncludedTax = req.IsIncludedTax ?? true,
            IsIncludedFee = req.IsIncludedFee ?? true,
            Label = NullIfWhiteSpace(req.Label),
            Notes = NullIfWhiteSpace(req.Notes),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.TourSchedulePrices.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", tourId, scheduleId, id = entity.Id },
            new QlTourCreatePriceResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid tourId,
        Guid scheduleId,
        Guid id,
        [FromBody] QlTourUpdatePriceRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();

        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        var entity = await _db.TourSchedulePrices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule price not found in current tenant." });

        if (!string.IsNullOrWhiteSpace(req.RowVersionBase64))
        {
            try
            {
                var bytes = Convert.FromBase64String(req.RowVersionBase64);
                _db.Entry(entity).Property(x => x.RowVersion).OriginalValue = bytes;
            }
            catch
            {
                return BadRequest(new { message = "RowVersionBase64 is invalid." });
            }
        }

        await ValidateUpdateAsync(tenantId, scheduleId, id, req, entity, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var willBeDefault = req.IsDefault ?? entity.IsDefault;
        if (willBeDefault)
        {
            var oldDefaults = await _db.TourSchedulePrices.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && x.Id != id && x.IsDefault)
                .ToListAsync(ct);

            foreach (var old in oldDefaults)
            {
                old.IsDefault = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }
        }

        if (req.PriceType.HasValue) entity.PriceType = req.PriceType.Value;
        if (req.CurrencyCode is not null) entity.CurrencyCode = req.CurrencyCode.Trim();
        if (req.Price.HasValue) entity.Price = req.Price.Value;
        if (req.OriginalPrice.HasValue) entity.OriginalPrice = req.OriginalPrice;
        if (req.Taxes.HasValue) entity.Taxes = req.Taxes;
        if (req.Fees.HasValue) entity.Fees = req.Fees;
        if (req.MinAge.HasValue) entity.MinAge = req.MinAge;
        if (req.MaxAge.HasValue) entity.MaxAge = req.MaxAge;
        if (req.MinQuantity.HasValue) entity.MinQuantity = req.MinQuantity;
        if (req.MaxQuantity.HasValue) entity.MaxQuantity = req.MaxQuantity;
        if (req.IsDefault.HasValue) entity.IsDefault = req.IsDefault.Value;
        if (req.IsIncludedTax.HasValue) entity.IsIncludedTax = req.IsIncludedTax.Value;
        if (req.IsIncludedFee.HasValue) entity.IsIncludedFee = req.IsIncludedFee.Value;
        if (req.Label is not null) entity.Label = NullIfWhiteSpace(req.Label);
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Tour schedule price was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid tourId, Guid scheduleId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, scheduleId, id, true, ct);

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid tourId, Guid scheduleId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, scheduleId, id, false, ct);

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid tourId, Guid scheduleId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, scheduleId, id, true, ct);

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid tourId, Guid scheduleId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, scheduleId, id, false, ct);

    [HttpPost("{id:guid}/set-default")]
    public async Task<IActionResult> SetDefault(Guid tourId, Guid scheduleId, Guid id, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();

        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        var entity = await _db.TourSchedulePrices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule price not found in current tenant." });

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var oldDefaults = await _db.TourSchedulePrices.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && x.Id != id && x.IsDefault)
            .ToListAsync(ct);

        foreach (var old in oldDefaults)
        {
            old.IsDefault = false;
            old.UpdatedAt = now;
            old.UpdatedByUserId = userId;
        }

        entity.IsDefault = true;
        entity.IsActive = true;
        entity.IsDeleted = false;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleDeleted(
        Guid tourId,
        Guid scheduleId,
        Guid id,
        bool isDeleted,
        CancellationToken ct)
    {
        var tenantId = RequireTenant();

        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        var entity = await _db.TourSchedulePrices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule price not found in current tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(
        Guid tourId,
        Guid scheduleId,
        Guid id,
        bool isActive,
        CancellationToken ct)
    {
        var tenantId = RequireTenant();

        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        var entity = await _db.TourSchedulePrices.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour schedule price not found in current tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task EnsureTourExistsAsync(Guid tenantId, Guid tourId, CancellationToken ct)
    {
        var exists = await _db.Tours.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == tourId && !x.IsDeleted, ct);

        if (!exists)
            throw new KeyNotFoundException("Tour not found in current tenant.");
    }

    private async Task EnsureScheduleExistsAsync(Guid tenantId, Guid tourId, Guid scheduleId, CancellationToken ct)
    {
        var exists = await _db.TourSchedules.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == scheduleId && !x.IsDeleted, ct);

        if (!exists)
            throw new KeyNotFoundException("Tour schedule not found in current tenant.");
    }

    private async Task ValidateCreateAsync(
        Guid tenantId,
        Guid scheduleId,
        QlTourCreatePriceRequest req,
        CancellationToken ct)
    {
        ValidateBusinessRules(
            req.CurrencyCode,
            req.Price,
            req.OriginalPrice,
            req.Taxes,
            req.Fees,
            req.MinAge,
            req.MaxAge,
            req.MinQuantity,
            req.MaxQuantity);

        var overlappingPrices = await _db.TourSchedulePrices.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourScheduleId == scheduleId &&
                x.PriceType == req.PriceType)
            .ToListAsync(ct);

        if (overlappingPrices.Any(x => TourPricingResolver.QuantityRangesOverlap(
                req.MinQuantity,
                req.MaxQuantity,
                x.MinQuantity,
                x.MaxQuantity)))
        {
            throw new ArgumentException("Price quantity range overlaps an existing row for the same PriceType.");
        }
    }

    private async Task ValidateUpdateAsync(
        Guid tenantId,
        Guid scheduleId,
        Guid currentId,
        QlTourUpdatePriceRequest req,
        TourSchedulePrice current,
        CancellationToken ct)
    {
        ValidateBusinessRules(
            req.CurrencyCode ?? current.CurrencyCode,
            req.Price ?? current.Price,
            req.OriginalPrice ?? current.OriginalPrice,
            req.Taxes ?? current.Taxes,
            req.Fees ?? current.Fees,
            req.MinAge ?? current.MinAge,
            req.MaxAge ?? current.MaxAge,
            req.MinQuantity ?? current.MinQuantity,
            req.MaxQuantity ?? current.MaxQuantity);

        var nextType = req.PriceType ?? current.PriceType;
        var nextMinQuantity = req.MinQuantity ?? current.MinQuantity;
        var nextMaxQuantity = req.MaxQuantity ?? current.MaxQuantity;

        var overlappingPrices = await _db.TourSchedulePrices.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourScheduleId == scheduleId &&
                x.PriceType == nextType &&
                x.Id != currentId)
            .ToListAsync(ct);

        if (overlappingPrices.Any(x => TourPricingResolver.QuantityRangesOverlap(
                nextMinQuantity,
                nextMaxQuantity,
                x.MinQuantity,
                x.MaxQuantity)))
        {
            throw new ArgumentException("Price quantity range overlaps an existing row for the same PriceType.");
        }
    }

    private static void ValidateBusinessRules(
        string currencyCode,
        decimal price,
        decimal? originalPrice,
        decimal? taxes,
        decimal? fees,
        int? minAge,
        int? maxAge,
        int? minQuantity,
        int? maxQuantity)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("CurrencyCode is required.");

        if (currencyCode.Trim().Length > 10)
            throw new ArgumentException("CurrencyCode max length is 10.");

        if (price < 0)
            throw new ArgumentException("Price cannot be negative.");

        if (originalPrice.HasValue && originalPrice < 0)
            throw new ArgumentException("OriginalPrice cannot be negative.");

        if (taxes.HasValue && taxes < 0)
            throw new ArgumentException("Taxes cannot be negative.");

        if (fees.HasValue && fees < 0)
            throw new ArgumentException("Fees cannot be negative.");

        if (minAge.HasValue && minAge < 0)
            throw new ArgumentException("MinAge cannot be negative.");

        if (maxAge.HasValue && maxAge < 0)
            throw new ArgumentException("MaxAge cannot be negative.");

        if (minAge.HasValue && maxAge.HasValue && minAge > maxAge)
            throw new ArgumentException("MinAge cannot be greater than MaxAge.");

        if (minQuantity.HasValue && minQuantity <= 0)
            throw new ArgumentException("MinQuantity must be greater than 0.");

        if (maxQuantity.HasValue && maxQuantity <= 0)
            throw new ArgumentException("MaxQuantity must be greater than 0.");

        if (minQuantity.HasValue && maxQuantity.HasValue && minQuantity > maxQuantity)
            throw new ArgumentException("MinQuantity cannot be greater than MaxQuantity.");
    }

    private Guid RequireTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            throw new InvalidOperationException("QLTour operations require tenant context.");

        return _tenant.TenantId.Value;
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static QlTourPriceDetailDto MapDetail(TourSchedulePrice x)
    {
        return new QlTourPriceDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            TourScheduleId = x.TourScheduleId,
            PriceType = x.PriceType,
            CurrencyCode = x.CurrencyCode,
            Price = x.Price,
            OriginalPrice = x.OriginalPrice,
            Taxes = x.Taxes,
            Fees = x.Fees,
            MinAge = x.MinAge,
            MaxAge = x.MaxAge,
            MinQuantity = x.MinQuantity,
            MaxQuantity = x.MaxQuantity,
            IsDefault = x.IsDefault,
            IsIncludedTax = x.IsIncludedTax,
            IsIncludedFee = x.IsIncludedFee,
            Label = x.Label,
            Notes = x.Notes,
            MetadataJson = x.MetadataJson,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            CreatedByUserId = x.CreatedByUserId,
            UpdatedAt = x.UpdatedAt,
            UpdatedByUserId = x.UpdatedByUserId,
            RowVersionBase64 = x.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
        };
    }
}

public sealed class QlTourPricePagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<QlTourPriceListItemDto> Items { get; set; } = new();
}

public sealed class QlTourPriceListItemDto
{
    public Guid Id { get; set; }
    public Guid TourScheduleId { get; set; }
    public TourPriceType PriceType { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? Taxes { get; set; }
    public decimal? Fees { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool IsDefault { get; set; }
    public bool IsIncludedTax { get; set; }
    public bool IsIncludedFee { get; set; }
    public string? Label { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class QlTourPriceDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourScheduleId { get; set; }
    public TourPriceType PriceType { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? Taxes { get; set; }
    public decimal? Fees { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool IsDefault { get; set; }
    public bool IsIncludedTax { get; set; }
    public bool IsIncludedFee { get; set; }
    public string? Label { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreatePriceRequest
{
    public TourPriceType PriceType { get; set; } = TourPriceType.Adult;
    public string CurrencyCode { get; set; } = "VND";
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? Taxes { get; set; }
    public decimal? Fees { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool IsDefault { get; set; }
    public bool? IsIncludedTax { get; set; }
    public bool? IsIncludedFee { get; set; }
    public string? Label { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class QlTourUpdatePriceRequest
{
    public TourPriceType? PriceType { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? Price { get; set; }
    public decimal? OriginalPrice { get; set; }
    public decimal? Taxes { get; set; }
    public decimal? Fees { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsIncludedTax { get; set; }
    public bool? IsIncludedFee { get; set; }
    public string? Label { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreatePriceResponse
{
    public Guid Id { get; set; }
}
