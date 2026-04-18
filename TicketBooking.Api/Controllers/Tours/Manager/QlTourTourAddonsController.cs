// FILE #251: TicketBooking.Api/Controllers/Tours/QlTourTourAddonsController.cs
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/addons")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class QlTourTourAddonsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QlTourTourAddonsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<QlTourAddonPagedResponse>> List(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] TourAddonType? type = null,
        [FromQuery] bool? required = null,
        [FromQuery] bool? defaultSelected = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : pageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        IQueryable<TourAddon> query = includeDeleted
            ? _db.TourAddons.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourAddons.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (required.HasValue)
            query = query.Where(x => x.IsRequired == required.Value);

        if (defaultSelected.HasValue)
            query = query.Where(x => x.IsDefaultSelected == defaultSelected.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(qq) ||
                x.Name.Contains(qq) ||
                (x.ShortDescription != null && x.ShortDescription.Contains(qq)) ||
                (x.Notes != null && x.Notes.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(x => x.IsRequired)
            .ThenByDescending(x => x.IsDefaultSelected)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QlTourAddonListItemDto
            {
                Id = x.Id,
                TourId = x.TourId,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                CurrencyCode = x.CurrencyCode,
                BasePrice = x.BasePrice,
                OriginalPrice = x.OriginalPrice,
                IsPerPerson = x.IsPerPerson,
                IsRequired = x.IsRequired,
                AllowQuantitySelection = x.AllowQuantitySelection,
                MinQuantity = x.MinQuantity,
                MaxQuantity = x.MaxQuantity,
                IsDefaultSelected = x.IsDefaultSelected,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new QlTourAddonPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QlTourAddonDetailDto>> GetById(
        Guid tourId,
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        IQueryable<TourAddon> query = includeDeleted
            ? _db.TourAddons.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourAddons.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        var entity = await query
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour addon not found in current tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<QlTourCreateAddonResponse>> Create(
        Guid tourId,
        [FromBody] QlTourCreateAddonRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await ValidateCreateAsync(tenantId, tourId, req, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = new TourAddon
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tourId,
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            Type = req.Type,
            ShortDescription = NullIfWhiteSpace(req.ShortDescription),
            DescriptionMarkdown = NullIfWhiteSpace(req.DescriptionMarkdown),
            DescriptionHtml = NullIfWhiteSpace(req.DescriptionHtml),
            CurrencyCode = req.CurrencyCode.Trim(),
            BasePrice = req.BasePrice,
            OriginalPrice = req.OriginalPrice,
            IsPerPerson = req.IsPerPerson ?? true,
            IsRequired = req.IsRequired ?? false,
            AllowQuantitySelection = req.AllowQuantitySelection ?? false,
            MinQuantity = req.MinQuantity,
            MaxQuantity = req.MaxQuantity,
            IsDefaultSelected = req.IsDefaultSelected ?? false,
            Notes = NullIfWhiteSpace(req.Notes),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            SortOrder = req.SortOrder ?? 0,
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.TourAddons.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", tourId, id = entity.Id },
            new QlTourCreateAddonResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid tourId,
        Guid id,
        [FromBody] QlTourUpdateAddonRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourAddons.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour addon not found in current tenant." });

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

        await ValidateUpdateAsync(tenantId, tourId, id, req, entity, ct);

        if (req.Code is not null) entity.Code = req.Code.Trim();
        if (req.Name is not null) entity.Name = req.Name.Trim();
        if (req.Type.HasValue) entity.Type = req.Type.Value;
        if (req.ShortDescription is not null) entity.ShortDescription = NullIfWhiteSpace(req.ShortDescription);
        if (req.DescriptionMarkdown is not null) entity.DescriptionMarkdown = NullIfWhiteSpace(req.DescriptionMarkdown);
        if (req.DescriptionHtml is not null) entity.DescriptionHtml = NullIfWhiteSpace(req.DescriptionHtml);
        if (req.CurrencyCode is not null) entity.CurrencyCode = req.CurrencyCode.Trim();
        if (req.BasePrice.HasValue) entity.BasePrice = req.BasePrice.Value;
        if (req.OriginalPrice.HasValue) entity.OriginalPrice = req.OriginalPrice;
        if (req.IsPerPerson.HasValue) entity.IsPerPerson = req.IsPerPerson.Value;
        if (req.IsRequired.HasValue) entity.IsRequired = req.IsRequired.Value;
        if (req.AllowQuantitySelection.HasValue) entity.AllowQuantitySelection = req.AllowQuantitySelection.Value;
        if (req.MinQuantity.HasValue) entity.MinQuantity = req.MinQuantity;
        if (req.MaxQuantity.HasValue) entity.MaxQuantity = req.MaxQuantity;
        if (req.IsDefaultSelected.HasValue) entity.IsDefaultSelected = req.IsDefaultSelected.Value;
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Tour addon was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, id, true, ct);

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, id, false, ct);

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, id, true, ct);

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, id, false, ct);

    [HttpPost("{id:guid}/mark-required")]
    public async Task<IActionResult> MarkRequired(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleRequired(tourId, id, true, ct);

    [HttpPost("{id:guid}/unmark-required")]
    public async Task<IActionResult> UnmarkRequired(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleRequired(tourId, id, false, ct);

    [HttpPost("{id:guid}/default-select")]
    public async Task<IActionResult> DefaultSelect(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDefaultSelected(tourId, id, true, ct);

    [HttpPost("{id:guid}/undo-default-select")]
    public async Task<IActionResult> UndoDefaultSelect(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDefaultSelected(tourId, id, false, ct);

    private async Task<IActionResult> ToggleDeleted(Guid tourId, Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourAddons.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour addon not found in current tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid tourId, Guid id, bool isActive, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourAddons.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour addon not found in current tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleRequired(Guid tourId, Guid id, bool isRequired, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourAddons.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour addon not found in current tenant." });

        entity.IsRequired = isRequired;
        if (isRequired)
            entity.IsActive = true;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleDefaultSelected(Guid tourId, Guid id, bool isDefaultSelected, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourAddons.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour addon not found in current tenant." });

        entity.IsDefaultSelected = isDefaultSelected;
        if (isDefaultSelected)
            entity.IsActive = true;

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

    private async Task ValidateCreateAsync(
        Guid tenantId,
        Guid tourId,
        QlTourCreateAddonRequest req,
        CancellationToken ct)
    {
        ValidatePayload(
            req.Code,
            req.Name,
            req.CurrencyCode,
            req.BasePrice,
            req.OriginalPrice,
            req.MinQuantity,
            req.MaxQuantity,
            req.AllowQuantitySelection ?? false);

        var code = req.Code.Trim();

        var duplicatedCode = await _db.TourAddons.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Code == code, ct);

        if (duplicatedCode)
            throw new ArgumentException("Code already exists in current tour.");
    }

    private async Task ValidateUpdateAsync(
        Guid tenantId,
        Guid tourId,
        Guid currentId,
        QlTourUpdateAddonRequest req,
        TourAddon current,
        CancellationToken ct)
    {
        var nextCode = req.Code ?? current.Code;
        var nextName = req.Name ?? current.Name;
        var nextCurrency = req.CurrencyCode ?? current.CurrencyCode;
        var nextBasePrice = req.BasePrice ?? current.BasePrice;
        var nextOriginalPrice = req.OriginalPrice ?? current.OriginalPrice;
        var nextMinQuantity = req.MinQuantity ?? current.MinQuantity;
        var nextMaxQuantity = req.MaxQuantity ?? current.MaxQuantity;
        var nextAllowQuantity = req.AllowQuantitySelection ?? current.AllowQuantitySelection;

        ValidatePayload(
            nextCode,
            nextName,
            nextCurrency,
            nextBasePrice,
            nextOriginalPrice,
            nextMinQuantity,
            nextMaxQuantity,
            nextAllowQuantity);

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var code = req.Code.Trim();

            var duplicatedCode = await _db.TourAddons.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Code == code && x.Id != currentId, ct);

            if (duplicatedCode)
                throw new ArgumentException("Code already exists in current tour.");
        }
    }

    private static void ValidatePayload(
        string code,
        string name,
        string currencyCode,
        decimal basePrice,
        decimal? originalPrice,
        int? minQuantity,
        int? maxQuantity,
        bool allowQuantitySelection)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("CurrencyCode is required.");

        if (code.Trim().Length > 50)
            throw new ArgumentException("Code max length is 50.");

        if (name.Trim().Length > 300)
            throw new ArgumentException("Name max length is 300.");

        if (currencyCode.Trim().Length > 10)
            throw new ArgumentException("CurrencyCode max length is 10.");

        if (basePrice < 0)
            throw new ArgumentException("BasePrice cannot be negative.");

        if (originalPrice.HasValue && originalPrice < 0)
            throw new ArgumentException("OriginalPrice cannot be negative.");

        if (minQuantity.HasValue && minQuantity <= 0)
            throw new ArgumentException("MinQuantity must be greater than 0.");

        if (maxQuantity.HasValue && maxQuantity <= 0)
            throw new ArgumentException("MaxQuantity must be greater than 0.");

        if (minQuantity.HasValue && maxQuantity.HasValue && minQuantity > maxQuantity)
            throw new ArgumentException("MinQuantity cannot be greater than MaxQuantity.");

        if (!allowQuantitySelection && (minQuantity.HasValue || maxQuantity.HasValue))
        {
            if ((minQuantity ?? 1) > 1 || (maxQuantity ?? 1) > 1)
                throw new ArgumentException("MinQuantity/MaxQuantity > 1 requires AllowQuantitySelection = true.");
        }
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

    private static QlTourAddonDetailDto MapDetail(TourAddon x)
    {
        return new QlTourAddonDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            TourId = x.TourId,
            Code = x.Code,
            Name = x.Name,
            Type = x.Type,
            ShortDescription = x.ShortDescription,
            DescriptionMarkdown = x.DescriptionMarkdown,
            DescriptionHtml = x.DescriptionHtml,
            CurrencyCode = x.CurrencyCode,
            BasePrice = x.BasePrice,
            OriginalPrice = x.OriginalPrice,
            IsPerPerson = x.IsPerPerson,
            IsRequired = x.IsRequired,
            AllowQuantitySelection = x.AllowQuantitySelection,
            MinQuantity = x.MinQuantity,
            MaxQuantity = x.MaxQuantity,
            IsDefaultSelected = x.IsDefaultSelected,
            Notes = x.Notes,
            MetadataJson = x.MetadataJson,
            SortOrder = x.SortOrder,
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

public sealed class QlTourAddonPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<QlTourAddonListItemDto> Items { get; set; } = new();
}

public sealed class QlTourAddonListItemDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourAddonType Type { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal BasePrice { get; set; }
    public decimal? OriginalPrice { get; set; }
    public bool IsPerPerson { get; set; }
    public bool IsRequired { get; set; }
    public bool AllowQuantitySelection { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool IsDefaultSelected { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class QlTourAddonDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourAddonType Type { get; set; }
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal BasePrice { get; set; }
    public decimal? OriginalPrice { get; set; }
    public bool IsPerPerson { get; set; }
    public bool IsRequired { get; set; }
    public bool AllowQuantitySelection { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool IsDefaultSelected { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreateAddonRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourAddonType Type { get; set; } = TourAddonType.Other;
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal BasePrice { get; set; }
    public decimal? OriginalPrice { get; set; }
    public bool? IsPerPerson { get; set; }
    public bool? IsRequired { get; set; }
    public bool? AllowQuantitySelection { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool? IsDefaultSelected { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class QlTourUpdateAddonRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public TourAddonType? Type { get; set; }
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? BasePrice { get; set; }
    public decimal? OriginalPrice { get; set; }
    public bool? IsPerPerson { get; set; }
    public bool? IsRequired { get; set; }
    public bool? AllowQuantitySelection { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool? IsDefaultSelected { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreateAddonResponse
{
    public Guid Id { get; set; }
}
