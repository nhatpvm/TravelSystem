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
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/packages/{packageId:guid}/components/{componentId:guid}/options")]
[Route("api/v{version:apiVersion}/admin/tours/{tourId:guid}/packages/{packageId:guid}/components/{componentId:guid}/options")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class TourPackageOptionsManagementController : TourPackageManagementControllerBase
{
    public TourPackageOptionsManagementController(AppDbContext db, ITenantContext tenant)
        : base(db, tenant)
    {
    }

    [HttpGet]
    public async Task<ActionResult<TourPackageOptionPagedResponse>> List(
        Guid tourId,
        Guid packageId,
        Guid componentId,
        [FromQuery] string? q = null,
        [FromQuery] TourPackageSourceType? sourceType = null,
        [FromQuery] TourPackageBindingMode? bindingMode = null,
        [FromQuery] TourPackagePricingMode? pricingMode = null,
        [FromQuery] bool? defaultSelected = null,
        [FromQuery] bool? fallback = null,
        [FromQuery] bool? dynamicCandidate = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await RequireComponentAsync(tenantId, tourId, packageId, componentId, ct);

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : pageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        IQueryable<TourPackageComponentOption> query = includeDeleted
            ? Db.TourPackageComponentOptions.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourPackageComponentId == componentId)
            : Db.TourPackageComponentOptions.Where(x => x.TenantId == tenantId && x.TourPackageComponentId == componentId && !x.IsDeleted);

        if (sourceType.HasValue)
            query = query.Where(x => x.SourceType == sourceType.Value);

        if (bindingMode.HasValue)
            query = query.Where(x => x.BindingMode == bindingMode.Value);

        if (pricingMode.HasValue)
            query = query.Where(x => x.PricingMode == pricingMode.Value);

        if (defaultSelected.HasValue)
            query = query.Where(x => x.IsDefaultSelected == defaultSelected.Value);

        if (fallback.HasValue)
            query = query.Where(x => x.IsFallback == fallback.Value);

        if (dynamicCandidate.HasValue)
            query = query.Where(x => x.IsDynamicCandidate == dynamicCandidate.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(qq) ||
                x.Name.Contains(qq) ||
                (x.Notes != null && x.Notes.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(x => x.IsDefaultSelected)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var optionIds = items.Select(x => x.Id).ToList();
        var overrideCounts = await Db.TourPackageScheduleOptionOverrides.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && optionIds.Contains(x.TourPackageComponentOptionId) && !x.IsDeleted)
            .GroupBy(x => x.TourPackageComponentOptionId)
            .Select(g => new { TourPackageComponentOptionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TourPackageComponentOptionId, x => x.Count, ct);

        return Ok(new TourPackageOptionPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(x => new TourPackageOptionListItemDto
            {
                Id = x.Id,
                TourPackageComponentId = x.TourPackageComponentId,
                Code = x.Code,
                Name = x.Name,
                SourceType = x.SourceType,
                BindingMode = x.BindingMode,
                SourceEntityId = x.SourceEntityId,
                PricingMode = x.PricingMode,
                CurrencyCode = x.CurrencyCode,
                PriceOverride = x.PriceOverride,
                CostOverride = x.CostOverride,
                MarkupPercent = x.MarkupPercent,
                MarkupAmount = x.MarkupAmount,
                QuantityMode = x.QuantityMode,
                DefaultQuantity = x.DefaultQuantity,
                MinQuantity = x.MinQuantity,
                MaxQuantity = x.MaxQuantity,
                IsDefaultSelected = x.IsDefaultSelected,
                IsFallback = x.IsFallback,
                IsDynamicCandidate = x.IsDynamicCandidate,
                SortOrder = x.SortOrder,
                OverrideCount = overrideCounts.GetValueOrDefault(x.Id),
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList()
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourPackageOptionDetailDto>> GetById(
        Guid tourId,
        Guid packageId,
        Guid componentId,
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await RequireOptionAsync(tenantId, tourId, packageId, componentId, id, ct, includeDeleted);

        var scheduleOverrides = await Db.TourPackageScheduleOptionOverrides.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TourPackageComponentOptionId == id)
            .OrderBy(x => x.TourScheduleId)
            .Select(x => new TourPackageScheduleOverrideSummaryDto
            {
                Id = x.Id,
                TourScheduleId = x.TourScheduleId,
                Status = x.Status,
                CurrencyCode = x.CurrencyCode,
                BoundSourceEntityId = x.BoundSourceEntityId,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(TourPackageManagementMaps.MapOptionDetail(entity, scheduleOverrides));
    }

    [HttpPost]
    public async Task<ActionResult<TourPackageOptionCreateResponse>> Create(
        Guid tourId,
        Guid packageId,
        Guid componentId,
        [FromBody] TourPackageOptionCreateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        var component = await RequireComponentAsync(tenantId, tourId, packageId, componentId, ct);
        await ValidateCreateAsync(tenantId, componentId, req, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();
        var isDefaultSelected = req.IsDefaultSelected ?? false;

        if (isDefaultSelected && IsSingleSelection(component.SelectionMode))
            await ClearDefaultSelectedOptionsAsync(tenantId, componentId, exceptId: null, ct);

        var entity = new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourPackageComponentId = componentId,
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            SourceType = req.SourceType,
            BindingMode = req.BindingMode,
            SourceEntityId = NormalizeGuid(req.SourceEntityId),
            SearchTemplateJson = NullIfWhiteSpace(req.SearchTemplateJson),
            RuleJson = NullIfWhiteSpace(req.RuleJson),
            PricingMode = req.PricingMode,
            CurrencyCode = req.CurrencyCode.Trim(),
            PriceOverride = req.PriceOverride,
            CostOverride = req.CostOverride,
            MarkupPercent = req.MarkupPercent,
            MarkupAmount = req.MarkupAmount,
            QuantityMode = req.QuantityMode,
            DefaultQuantity = req.DefaultQuantity ?? 1,
            MinQuantity = req.MinQuantity,
            MaxQuantity = req.MaxQuantity,
            IsDefaultSelected = isDefaultSelected,
            IsFallback = req.IsFallback ?? false,
            IsDynamicCandidate = req.IsDynamicCandidate ?? false,
            SortOrder = req.SortOrder ?? 0,
            Notes = NullIfWhiteSpace(req.Notes),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        Db.TourPackageComponentOptions.Add(entity);
        await Db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", tourId, packageId, componentId, id = entity.Id },
            new TourPackageOptionCreateResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid tourId,
        Guid packageId,
        Guid componentId,
        Guid id,
        [FromBody] TourPackageOptionUpdateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await Db.TourPackageComponentOptions.IgnoreQueryFilters()
            .Include(x => x.TourPackageComponent)
            .ThenInclude(x => x!.TourPackage)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourPackageComponentId == componentId && x.Id == id, ct);

        if (entity is null ||
            entity.TourPackageComponent is null ||
            entity.TourPackageComponent.TourPackageId != packageId ||
            entity.TourPackageComponent.TourPackage is null ||
            entity.TourPackageComponent.TourPackage.TourId != tourId)
        {
            return NotFound(new { message = "Tour package option not found in current tenant." });
        }

        if (!string.IsNullOrWhiteSpace(req.RowVersionBase64))
        {
            try
            {
                Db.Entry(entity).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(req.RowVersionBase64);
            }
            catch
            {
                return BadRequest(new { message = "RowVersionBase64 is invalid." });
            }
        }

        await ValidateUpdateAsync(tenantId, componentId, id, req, entity, ct);

        if (req.Code is not null) entity.Code = req.Code.Trim();
        if (req.Name is not null) entity.Name = req.Name.Trim();
        if (req.SourceType.HasValue) entity.SourceType = req.SourceType.Value;
        if (req.BindingMode.HasValue) entity.BindingMode = req.BindingMode.Value;
        if (req.SourceEntityId.HasValue) entity.SourceEntityId = NormalizeGuid(req.SourceEntityId);
        if (req.SearchTemplateJson is not null) entity.SearchTemplateJson = NullIfWhiteSpace(req.SearchTemplateJson);
        if (req.RuleJson is not null) entity.RuleJson = NullIfWhiteSpace(req.RuleJson);
        if (req.PricingMode.HasValue) entity.PricingMode = req.PricingMode.Value;
        if (req.CurrencyCode is not null) entity.CurrencyCode = req.CurrencyCode.Trim();
        if (req.PriceOverride.HasValue) entity.PriceOverride = req.PriceOverride;
        if (req.CostOverride.HasValue) entity.CostOverride = req.CostOverride;
        if (req.MarkupPercent.HasValue) entity.MarkupPercent = req.MarkupPercent;
        if (req.MarkupAmount.HasValue) entity.MarkupAmount = req.MarkupAmount;
        if (req.QuantityMode.HasValue) entity.QuantityMode = req.QuantityMode.Value;
        if (req.DefaultQuantity.HasValue) entity.DefaultQuantity = req.DefaultQuantity.Value;
        if (req.MinQuantity.HasValue) entity.MinQuantity = req.MinQuantity;
        if (req.MaxQuantity.HasValue) entity.MaxQuantity = req.MaxQuantity;
        if (req.IsDefaultSelected.HasValue) entity.IsDefaultSelected = req.IsDefaultSelected.Value;
        if (req.IsFallback.HasValue) entity.IsFallback = req.IsFallback.Value;
        if (req.IsDynamicCandidate.HasValue) entity.IsDynamicCandidate = req.IsDynamicCandidate.Value;
        if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        if (entity.IsDefaultSelected && IsSingleSelection(entity.TourPackageComponent.SelectionMode))
            await ClearDefaultSelectedOptionsAsync(tenantId, componentId, entity.Id, ct);

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        try
        {
            await Db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Tour package option was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid tourId, Guid packageId, Guid componentId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, packageId, componentId, id, true, ct);

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid tourId, Guid packageId, Guid componentId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, packageId, componentId, id, false, ct);

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid tourId, Guid packageId, Guid componentId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, packageId, componentId, id, true, ct);

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid tourId, Guid packageId, Guid componentId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, packageId, componentId, id, false, ct);

    [HttpPost("{id:guid}/default-select")]
    public async Task<IActionResult> DefaultSelect(Guid tourId, Guid packageId, Guid componentId, Guid id, CancellationToken ct = default)
        => await ToggleDefaultSelected(tourId, packageId, componentId, id, true, ct);

    [HttpPost("{id:guid}/undo-default-select")]
    public async Task<IActionResult> UndoDefaultSelect(Guid tourId, Guid packageId, Guid componentId, Guid id, CancellationToken ct = default)
        => await ToggleDefaultSelected(tourId, packageId, componentId, id, false, ct);

    [HttpPost("{id:guid}/mark-fallback")]
    public async Task<IActionResult> MarkFallback(Guid tourId, Guid packageId, Guid componentId, Guid id, CancellationToken ct = default)
        => await ToggleFallback(tourId, packageId, componentId, id, true, ct);

    [HttpPost("{id:guid}/unmark-fallback")]
    public async Task<IActionResult> UnmarkFallback(Guid tourId, Guid packageId, Guid componentId, Guid id, CancellationToken ct = default)
        => await ToggleFallback(tourId, packageId, componentId, id, false, ct);

    [HttpPost("{id:guid}/mark-dynamic-candidate")]
    public async Task<IActionResult> MarkDynamicCandidate(Guid tourId, Guid packageId, Guid componentId, Guid id, CancellationToken ct = default)
        => await ToggleDynamicCandidate(tourId, packageId, componentId, id, true, ct);

    [HttpPost("{id:guid}/unmark-dynamic-candidate")]
    public async Task<IActionResult> UnmarkDynamicCandidate(Guid tourId, Guid packageId, Guid componentId, Guid id, CancellationToken ct = default)
        => await ToggleDynamicCandidate(tourId, packageId, componentId, id, false, ct);

    private async Task<IActionResult> ToggleDeleted(
        Guid tourId,
        Guid packageId,
        Guid componentId,
        Guid id,
        bool isDeleted,
        CancellationToken ct)
    {
        var tenantId = RequireTenantContext();
        var entity = await RequireOptionAsync(tenantId, tourId, packageId, componentId, id, ct);

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await Db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(
        Guid tourId,
        Guid packageId,
        Guid componentId,
        Guid id,
        bool isActive,
        CancellationToken ct)
    {
        var tenantId = RequireTenantContext();
        var entity = await RequireOptionAsync(tenantId, tourId, packageId, componentId, id, ct);

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await Db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleDefaultSelected(
        Guid tourId,
        Guid packageId,
        Guid componentId,
        Guid id,
        bool isDefaultSelected,
        CancellationToken ct)
    {
        var tenantId = RequireTenantContext();
        var entity = await RequireOptionAsync(tenantId, tourId, packageId, componentId, id, ct);

        if (isDefaultSelected && IsSingleSelection(entity.TourPackageComponent!.SelectionMode))
            await ClearDefaultSelectedOptionsAsync(tenantId, componentId, entity.Id, ct);

        entity.IsDefaultSelected = isDefaultSelected;
        if (isDefaultSelected)
            entity.IsActive = true;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await Db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleFallback(
        Guid tourId,
        Guid packageId,
        Guid componentId,
        Guid id,
        bool isFallback,
        CancellationToken ct)
    {
        var tenantId = RequireTenantContext();
        var entity = await RequireOptionAsync(tenantId, tourId, packageId, componentId, id, ct);

        entity.IsFallback = isFallback;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await Db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleDynamicCandidate(
        Guid tourId,
        Guid packageId,
        Guid componentId,
        Guid id,
        bool isDynamicCandidate,
        CancellationToken ct)
    {
        var tenantId = RequireTenantContext();
        var entity = await RequireOptionAsync(tenantId, tourId, packageId, componentId, id, ct);

        entity.IsDynamicCandidate = isDynamicCandidate;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await Db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task ClearDefaultSelectedOptionsAsync(Guid tenantId, Guid componentId, Guid? exceptId, CancellationToken ct)
    {
        var items = await Db.TourPackageComponentOptions.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourPackageComponentId == componentId && x.IsDefaultSelected && (!exceptId.HasValue || x.Id != exceptId.Value))
            .ToListAsync(ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        foreach (var item in items)
        {
            item.IsDefaultSelected = false;
            item.UpdatedAt = now;
            item.UpdatedByUserId = userId;
        }
    }

    private async Task ValidateCreateAsync(Guid tenantId, Guid componentId, TourPackageOptionCreateRequest req, CancellationToken ct)
    {
        ValidatePayload(
            req.Code,
            req.Name,
            req.BindingMode,
            req.SourceEntityId,
            req.SearchTemplateJson,
            req.PricingMode,
            req.CurrencyCode,
            req.PriceOverride,
            req.CostOverride,
            req.MarkupPercent,
            req.MarkupAmount,
            req.DefaultQuantity ?? 1,
            req.MinQuantity,
            req.MaxQuantity);

        var code = req.Code.Trim();
        var exists = await Db.TourPackageComponentOptions.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourPackageComponentId == componentId && x.Code == code, ct);

        if (exists)
            throw new ArgumentException("Code already exists in current component.");
    }

    private async Task ValidateUpdateAsync(
        Guid tenantId,
        Guid componentId,
        Guid currentId,
        TourPackageOptionUpdateRequest req,
        TourPackageComponentOption current,
        CancellationToken ct)
    {
        var nextCode = req.Code ?? current.Code;
        var nextName = req.Name ?? current.Name;
        var nextBindingMode = req.BindingMode ?? current.BindingMode;
        var nextSourceEntityId = req.SourceEntityId.HasValue ? NormalizeGuid(req.SourceEntityId) : current.SourceEntityId;
        var nextSearchTemplate = req.SearchTemplateJson ?? current.SearchTemplateJson;
        var nextPricingMode = req.PricingMode ?? current.PricingMode;
        var nextCurrency = req.CurrencyCode ?? current.CurrencyCode;
        var nextPriceOverride = req.PriceOverride ?? current.PriceOverride;
        var nextCostOverride = req.CostOverride ?? current.CostOverride;
        var nextMarkupPercent = req.MarkupPercent ?? current.MarkupPercent;
        var nextMarkupAmount = req.MarkupAmount ?? current.MarkupAmount;
        var nextDefaultQuantity = req.DefaultQuantity ?? current.DefaultQuantity;
        var nextMinQuantity = req.MinQuantity ?? current.MinQuantity;
        var nextMaxQuantity = req.MaxQuantity ?? current.MaxQuantity;

        ValidatePayload(
            nextCode,
            nextName,
            nextBindingMode,
            nextSourceEntityId,
            nextSearchTemplate,
            nextPricingMode,
            nextCurrency,
            nextPriceOverride,
            nextCostOverride,
            nextMarkupPercent,
            nextMarkupAmount,
            nextDefaultQuantity,
            nextMinQuantity,
            nextMaxQuantity);

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var code = req.Code.Trim();
            var exists = await Db.TourPackageComponentOptions.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.TourPackageComponentId == componentId && x.Code == code && x.Id != currentId, ct);

            if (exists)
                throw new ArgumentException("Code already exists in current component.");
        }
    }

    private static void ValidatePayload(
        string code,
        string name,
        TourPackageBindingMode bindingMode,
        Guid? sourceEntityId,
        string? searchTemplateJson,
        TourPackagePricingMode pricingMode,
        string currencyCode,
        decimal? priceOverride,
        decimal? costOverride,
        decimal? markupPercent,
        decimal? markupAmount,
        int defaultQuantity,
        int? minQuantity,
        int? maxQuantity)
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

        if (bindingMode == TourPackageBindingMode.StaticReference && (!sourceEntityId.HasValue || sourceEntityId.Value == Guid.Empty))
            throw new ArgumentException("SourceEntityId is required for StaticReference binding.");

        if (bindingMode == TourPackageBindingMode.SearchTemplate && string.IsNullOrWhiteSpace(searchTemplateJson))
            throw new ArgumentException("SearchTemplateJson is required for SearchTemplate binding.");

        if (priceOverride.HasValue && priceOverride < 0)
            throw new ArgumentException("PriceOverride cannot be negative.");

        if (costOverride.HasValue && costOverride < 0)
            throw new ArgumentException("CostOverride cannot be negative.");

        if (markupPercent.HasValue && markupPercent < 0)
            throw new ArgumentException("MarkupPercent cannot be negative.");

        if (markupAmount.HasValue && markupAmount < 0)
            throw new ArgumentException("MarkupAmount cannot be negative.");

        if (pricingMode == TourPackagePricingMode.Override && !priceOverride.HasValue)
            throw new ArgumentException("PriceOverride is required when PricingMode = Override.");

        if (pricingMode == TourPackagePricingMode.Markup && !markupPercent.HasValue && !markupAmount.HasValue)
            throw new ArgumentException("MarkupPercent or MarkupAmount is required when PricingMode = Markup.");

        if (defaultQuantity <= 0)
            throw new ArgumentException("DefaultQuantity must be greater than 0.");

        if (minQuantity.HasValue && minQuantity <= 0)
            throw new ArgumentException("MinQuantity must be greater than 0.");

        if (maxQuantity.HasValue && maxQuantity <= 0)
            throw new ArgumentException("MaxQuantity must be greater than 0.");

        if (minQuantity.HasValue && maxQuantity.HasValue && minQuantity > maxQuantity)
            throw new ArgumentException("MinQuantity cannot be greater than MaxQuantity.");

        if (minQuantity.HasValue && defaultQuantity < minQuantity.Value)
            throw new ArgumentException("DefaultQuantity cannot be less than MinQuantity.");

        if (maxQuantity.HasValue && defaultQuantity > maxQuantity.Value)
            throw new ArgumentException("DefaultQuantity cannot be greater than MaxQuantity.");
    }

    private static bool IsSingleSelection(TourPackageSelectionMode mode)
        => mode == TourPackageSelectionMode.RequiredSingle || mode == TourPackageSelectionMode.OptionalSingle;
}
