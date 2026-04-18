using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Tours;

public abstract class TourPackageManagementControllerBase : ControllerBase
{
    protected readonly AppDbContext Db;
    protected readonly ITenantContext Tenant;

    protected TourPackageManagementControllerBase(AppDbContext db, ITenantContext tenant)
    {
        Db = db;
        Tenant = tenant;
    }

    protected Guid RequireTenantContext()
    {
        if (!Tenant.HasTenant || !Tenant.TenantId.HasValue || Tenant.TenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Tour package configuration operations require tenant context.");

        return Tenant.TenantId.Value;
    }

    protected Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    protected static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    protected static Guid? NormalizeGuid(Guid? value)
        => value.HasValue && value.Value != Guid.Empty ? value.Value : null;

    protected async Task EnsureTourExistsAsync(Guid tenantId, Guid tourId, CancellationToken ct)
    {
        var exists = await Db.Tours.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == tourId && !x.IsDeleted, ct);

        if (!exists)
            throw new KeyNotFoundException("Tour not found in current tenant.");
    }

    protected async Task EnsureScheduleExistsAsync(Guid tenantId, Guid tourId, Guid scheduleId, CancellationToken ct)
    {
        var exists = await Db.TourSchedules.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == scheduleId && !x.IsDeleted, ct);

        if (!exists)
            throw new KeyNotFoundException("Tour schedule not found in current tenant.");
    }

    protected async Task<TourPackage> RequirePackageAsync(
        Guid tenantId,
        Guid tourId,
        Guid packageId,
        CancellationToken ct,
        bool includeDeleted = true)
    {
        IQueryable<TourPackage> query = includeDeleted
            ? Db.TourPackages.IgnoreQueryFilters()
            : Db.TourPackages.Where(x => !x.IsDeleted);

        var entity = await query.FirstOrDefaultAsync(
            x => x.TenantId == tenantId && x.TourId == tourId && x.Id == packageId,
            ct);

        if (entity is null)
            throw new KeyNotFoundException("Tour package not found in current tenant.");

        return entity;
    }

    protected async Task<TourPackageComponent> RequireComponentAsync(
        Guid tenantId,
        Guid tourId,
        Guid packageId,
        Guid componentId,
        CancellationToken ct,
        bool includeDeleted = true)
    {
        IQueryable<TourPackageComponent> query = includeDeleted
            ? Db.TourPackageComponents.IgnoreQueryFilters()
            : Db.TourPackageComponents.Where(x => !x.IsDeleted);

        var entity = await query
            .Include(x => x.TourPackage)
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.TourPackageId == packageId && x.Id == componentId,
                ct);

        if (entity is null || entity.TourPackage is null || entity.TourPackage.TourId != tourId)
            throw new KeyNotFoundException("Tour package component not found in current tenant.");

        return entity;
    }

    protected async Task<TourPackageComponentOption> RequireOptionAsync(
        Guid tenantId,
        Guid tourId,
        Guid packageId,
        Guid componentId,
        Guid optionId,
        CancellationToken ct,
        bool includeDeleted = true)
    {
        IQueryable<TourPackageComponentOption> query = includeDeleted
            ? Db.TourPackageComponentOptions.IgnoreQueryFilters()
            : Db.TourPackageComponentOptions.Where(x => !x.IsDeleted);

        var entity = await query
            .Include(x => x.TourPackageComponent)
            .ThenInclude(x => x!.TourPackage)
            .FirstOrDefaultAsync(
                x => x.TenantId == tenantId && x.TourPackageComponentId == componentId && x.Id == optionId,
                ct);

        if (entity is null ||
            entity.TourPackageComponent is null ||
            entity.TourPackageComponent.TourPackageId != packageId ||
            entity.TourPackageComponent.TourPackage is null ||
            entity.TourPackageComponent.TourPackage.TourId != tourId)
        {
            throw new KeyNotFoundException("Tour package option not found in current tenant.");
        }

        return entity;
    }
}

internal static class TourPackageManagementMaps
{
    public static TourPackageDetailDto MapPackageDetail(
        TourPackage entity,
        IReadOnlyList<TourPackageComponentSummaryDto> components)
    {
        return new TourPackageDetailDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            TourId = entity.TourId,
            Code = entity.Code,
            Name = entity.Name,
            Mode = entity.Mode,
            Status = entity.Status,
            CurrencyCode = entity.CurrencyCode,
            IsDefault = entity.IsDefault,
            AutoRepriceBeforeConfirm = entity.AutoRepriceBeforeConfirm,
            HoldStrategy = entity.HoldStrategy,
            PricingRuleJson = entity.PricingRuleJson,
            MetadataJson = entity.MetadataJson,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId,
            RowVersionBase64 = entity.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null,
            Components = components.ToList()
        };
    }

    public static TourPackageComponentDetailDto MapComponentDetail(
        TourPackageComponent entity,
        IReadOnlyList<TourPackageOptionSummaryDto> options)
    {
        return new TourPackageComponentDetailDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            TourPackageId = entity.TourPackageId,
            Code = entity.Code,
            Name = entity.Name,
            ComponentType = entity.ComponentType,
            SelectionMode = entity.SelectionMode,
            MinSelect = entity.MinSelect,
            MaxSelect = entity.MaxSelect,
            DayOffsetFromDeparture = entity.DayOffsetFromDeparture,
            NightCount = entity.NightCount,
            SortOrder = entity.SortOrder,
            Notes = entity.Notes,
            MetadataJson = entity.MetadataJson,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId,
            RowVersionBase64 = entity.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null,
            Options = options.ToList()
        };
    }

    public static TourPackageOptionDetailDto MapOptionDetail(
        TourPackageComponentOption entity,
        IReadOnlyList<TourPackageScheduleOverrideSummaryDto> scheduleOverrides)
    {
        return new TourPackageOptionDetailDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            TourPackageComponentId = entity.TourPackageComponentId,
            Code = entity.Code,
            Name = entity.Name,
            SourceType = entity.SourceType,
            BindingMode = entity.BindingMode,
            SourceEntityId = entity.SourceEntityId,
            SearchTemplateJson = entity.SearchTemplateJson,
            RuleJson = entity.RuleJson,
            PricingMode = entity.PricingMode,
            CurrencyCode = entity.CurrencyCode,
            PriceOverride = entity.PriceOverride,
            CostOverride = entity.CostOverride,
            MarkupPercent = entity.MarkupPercent,
            MarkupAmount = entity.MarkupAmount,
            QuantityMode = entity.QuantityMode,
            DefaultQuantity = entity.DefaultQuantity,
            MinQuantity = entity.MinQuantity,
            MaxQuantity = entity.MaxQuantity,
            IsDefaultSelected = entity.IsDefaultSelected,
            IsFallback = entity.IsFallback,
            IsDynamicCandidate = entity.IsDynamicCandidate,
            SortOrder = entity.SortOrder,
            Notes = entity.Notes,
            MetadataJson = entity.MetadataJson,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId,
            RowVersionBase64 = entity.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null,
            ScheduleOverrides = scheduleOverrides.ToList()
        };
    }

    public static TourPackageScheduleOverrideDetailDto MapOverrideDetail(
        TourPackageScheduleOptionOverride entity,
        TourPackageScheduleOverridePathDto path)
    {
        return new TourPackageScheduleOverrideDetailDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            TourScheduleId = entity.TourScheduleId,
            TourPackageComponentOptionId = entity.TourPackageComponentOptionId,
            TourPackageId = path.TourPackageId,
            TourPackageCode = path.TourPackageCode,
            TourPackageName = path.TourPackageName,
            TourPackageComponentId = path.TourPackageComponentId,
            TourPackageComponentCode = path.TourPackageComponentCode,
            TourPackageComponentName = path.TourPackageComponentName,
            TourPackageOptionCode = path.TourPackageOptionCode,
            TourPackageOptionName = path.TourPackageOptionName,
            Status = entity.Status,
            CurrencyCode = entity.CurrencyCode,
            PriceOverride = entity.PriceOverride,
            CostOverride = entity.CostOverride,
            BoundSourceEntityId = entity.BoundSourceEntityId,
            BoundSnapshotJson = entity.BoundSnapshotJson,
            RuleOverrideJson = entity.RuleOverrideJson,
            Notes = entity.Notes,
            MetadataJson = entity.MetadataJson,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId,
            RowVersionBase64 = entity.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
        };
    }
}

public sealed class TourPackagePagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TourPackageListItemDto> Items { get; set; } = new();
}

public sealed class TourPackageListItemDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPackageMode Mode { get; set; }
    public TourPackageStatus Status { get; set; }
    public string CurrencyCode { get; set; } = "";
    public bool IsDefault { get; set; }
    public bool AutoRepriceBeforeConfirm { get; set; }
    public TourPackageHoldStrategy HoldStrategy { get; set; }
    public int ComponentCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class TourPackageDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPackageMode Mode { get; set; }
    public TourPackageStatus Status { get; set; }
    public string CurrencyCode { get; set; } = "";
    public bool IsDefault { get; set; }
    public bool AutoRepriceBeforeConfirm { get; set; }
    public TourPackageHoldStrategy HoldStrategy { get; set; }
    public string? PricingRuleJson { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
    public List<TourPackageComponentSummaryDto> Components { get; set; } = new();
}

public sealed class TourPackageComponentSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPackageComponentType ComponentType { get; set; }
    public TourPackageSelectionMode SelectionMode { get; set; }
    public int? MinSelect { get; set; }
    public int? MaxSelect { get; set; }
    public int SortOrder { get; set; }
    public int OptionCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class TourPackageCreateRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPackageMode Mode { get; set; } = TourPackageMode.Fixed;
    public TourPackageStatus Status { get; set; } = TourPackageStatus.Draft;
    public string CurrencyCode { get; set; } = "VND";
    public bool? IsDefault { get; set; }
    public bool? AutoRepriceBeforeConfirm { get; set; }
    public TourPackageHoldStrategy HoldStrategy { get; set; } = TourPackageHoldStrategy.AllOrNothing;
    public string? PricingRuleJson { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class TourPackageUpdateRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public TourPackageMode? Mode { get; set; }
    public TourPackageStatus? Status { get; set; }
    public string? CurrencyCode { get; set; }
    public bool? IsDefault { get; set; }
    public bool? AutoRepriceBeforeConfirm { get; set; }
    public TourPackageHoldStrategy? HoldStrategy { get; set; }
    public string? PricingRuleJson { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class TourPackageCreateResponse
{
    public Guid Id { get; set; }
}

public sealed class TourPackageComponentPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TourPackageComponentListItemDto> Items { get; set; } = new();
}

public sealed class TourPackageComponentListItemDto
{
    public Guid Id { get; set; }
    public Guid TourPackageId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPackageComponentType ComponentType { get; set; }
    public TourPackageSelectionMode SelectionMode { get; set; }
    public int? MinSelect { get; set; }
    public int? MaxSelect { get; set; }
    public int? DayOffsetFromDeparture { get; set; }
    public int? NightCount { get; set; }
    public int SortOrder { get; set; }
    public int OptionCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class TourPackageComponentDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourPackageId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPackageComponentType ComponentType { get; set; }
    public TourPackageSelectionMode SelectionMode { get; set; }
    public int? MinSelect { get; set; }
    public int? MaxSelect { get; set; }
    public int? DayOffsetFromDeparture { get; set; }
    public int? NightCount { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
    public List<TourPackageOptionSummaryDto> Options { get; set; } = new();
}

public sealed class TourPackageOptionSummaryDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPackageSourceType SourceType { get; set; }
    public TourPackageBindingMode BindingMode { get; set; }
    public TourPackagePricingMode PricingMode { get; set; }
    public bool IsDefaultSelected { get; set; }
    public bool IsFallback { get; set; }
    public bool IsDynamicCandidate { get; set; }
    public int SortOrder { get; set; }
    public int OverrideCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class TourPackageComponentCreateRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPackageComponentType ComponentType { get; set; } = TourPackageComponentType.Other;
    public TourPackageSelectionMode SelectionMode { get; set; } = TourPackageSelectionMode.RequiredSingle;
    public int? MinSelect { get; set; }
    public int? MaxSelect { get; set; }
    public int? DayOffsetFromDeparture { get; set; }
    public int? NightCount { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class TourPackageComponentUpdateRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public TourPackageComponentType? ComponentType { get; set; }
    public TourPackageSelectionMode? SelectionMode { get; set; }
    public int? MinSelect { get; set; }
    public int? MaxSelect { get; set; }
    public int? DayOffsetFromDeparture { get; set; }
    public int? NightCount { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class TourPackageComponentCreateResponse
{
    public Guid Id { get; set; }
}

public sealed class TourPackageOptionPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TourPackageOptionListItemDto> Items { get; set; } = new();
}

public sealed class TourPackageOptionListItemDto
{
    public Guid Id { get; set; }
    public Guid TourPackageComponentId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPackageSourceType SourceType { get; set; }
    public TourPackageBindingMode BindingMode { get; set; }
    public Guid? SourceEntityId { get; set; }
    public TourPackagePricingMode PricingMode { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal? PriceOverride { get; set; }
    public decimal? CostOverride { get; set; }
    public decimal? MarkupPercent { get; set; }
    public decimal? MarkupAmount { get; set; }
    public TourPackageQuantityMode QuantityMode { get; set; }
    public int DefaultQuantity { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool IsDefaultSelected { get; set; }
    public bool IsFallback { get; set; }
    public bool IsDynamicCandidate { get; set; }
    public int SortOrder { get; set; }
    public int OverrideCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class TourPackageOptionDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourPackageComponentId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPackageSourceType SourceType { get; set; }
    public TourPackageBindingMode BindingMode { get; set; }
    public Guid? SourceEntityId { get; set; }
    public string? SearchTemplateJson { get; set; }
    public string? RuleJson { get; set; }
    public TourPackagePricingMode PricingMode { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal? PriceOverride { get; set; }
    public decimal? CostOverride { get; set; }
    public decimal? MarkupPercent { get; set; }
    public decimal? MarkupAmount { get; set; }
    public TourPackageQuantityMode QuantityMode { get; set; }
    public int DefaultQuantity { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool IsDefaultSelected { get; set; }
    public bool IsFallback { get; set; }
    public bool IsDynamicCandidate { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
    public List<TourPackageScheduleOverrideSummaryDto> ScheduleOverrides { get; set; } = new();
}

public sealed class TourPackageScheduleOverrideSummaryDto
{
    public Guid Id { get; set; }
    public Guid TourScheduleId { get; set; }
    public TourPackageScheduleOverrideStatus Status { get; set; }
    public string? CurrencyCode { get; set; }
    public Guid? BoundSourceEntityId { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class TourPackageOptionCreateRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPackageSourceType SourceType { get; set; } = TourPackageSourceType.Other;
    public TourPackageBindingMode BindingMode { get; set; } = TourPackageBindingMode.StaticReference;
    public Guid? SourceEntityId { get; set; }
    public string? SearchTemplateJson { get; set; }
    public string? RuleJson { get; set; }
    public TourPackagePricingMode PricingMode { get; set; } = TourPackagePricingMode.Included;
    public string CurrencyCode { get; set; } = "VND";
    public decimal? PriceOverride { get; set; }
    public decimal? CostOverride { get; set; }
    public decimal? MarkupPercent { get; set; }
    public decimal? MarkupAmount { get; set; }
    public TourPackageQuantityMode QuantityMode { get; set; } = TourPackageQuantityMode.PerBooking;
    public int? DefaultQuantity { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool? IsDefaultSelected { get; set; }
    public bool? IsFallback { get; set; }
    public bool? IsDynamicCandidate { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class TourPackageOptionUpdateRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public TourPackageSourceType? SourceType { get; set; }
    public TourPackageBindingMode? BindingMode { get; set; }
    public Guid? SourceEntityId { get; set; }
    public string? SearchTemplateJson { get; set; }
    public string? RuleJson { get; set; }
    public TourPackagePricingMode? PricingMode { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? PriceOverride { get; set; }
    public decimal? CostOverride { get; set; }
    public decimal? MarkupPercent { get; set; }
    public decimal? MarkupAmount { get; set; }
    public TourPackageQuantityMode? QuantityMode { get; set; }
    public int? DefaultQuantity { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool? IsDefaultSelected { get; set; }
    public bool? IsFallback { get; set; }
    public bool? IsDynamicCandidate { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class TourPackageOptionCreateResponse
{
    public Guid Id { get; set; }
}

public sealed class TourPackageScheduleOverridePagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TourPackageScheduleOverrideListItemDto> Items { get; set; } = new();
}

public sealed class TourPackageScheduleOverrideListItemDto
{
    public Guid Id { get; set; }
    public Guid TourScheduleId { get; set; }
    public Guid TourPackageId { get; set; }
    public Guid TourPackageComponentId { get; set; }
    public Guid TourPackageComponentOptionId { get; set; }
    public string TourPackageCode { get; set; } = "";
    public string TourPackageName { get; set; } = "";
    public string TourPackageComponentCode { get; set; } = "";
    public string TourPackageComponentName { get; set; } = "";
    public string TourPackageOptionCode { get; set; } = "";
    public string TourPackageOptionName { get; set; } = "";
    public TourPackageScheduleOverrideStatus Status { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? PriceOverride { get; set; }
    public decimal? CostOverride { get; set; }
    public Guid? BoundSourceEntityId { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class TourPackageScheduleOverrideDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourScheduleId { get; set; }
    public Guid TourPackageId { get; set; }
    public string TourPackageCode { get; set; } = "";
    public string TourPackageName { get; set; } = "";
    public Guid TourPackageComponentId { get; set; }
    public string TourPackageComponentCode { get; set; } = "";
    public string TourPackageComponentName { get; set; } = "";
    public Guid TourPackageComponentOptionId { get; set; }
    public string TourPackageOptionCode { get; set; } = "";
    public string TourPackageOptionName { get; set; } = "";
    public TourPackageScheduleOverrideStatus Status { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? PriceOverride { get; set; }
    public decimal? CostOverride { get; set; }
    public Guid? BoundSourceEntityId { get; set; }
    public string? BoundSnapshotJson { get; set; }
    public string? RuleOverrideJson { get; set; }
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

public sealed class TourPackageScheduleOverridePathDto
{
    public Guid TourPackageId { get; set; }
    public string TourPackageCode { get; set; } = "";
    public string TourPackageName { get; set; } = "";
    public Guid TourPackageComponentId { get; set; }
    public string TourPackageComponentCode { get; set; } = "";
    public string TourPackageComponentName { get; set; } = "";
    public string TourPackageOptionCode { get; set; } = "";
    public string TourPackageOptionName { get; set; } = "";
}

public sealed class TourPackageScheduleOverrideCreateRequest
{
    public Guid TourPackageComponentOptionId { get; set; }
    public TourPackageScheduleOverrideStatus Status { get; set; } = TourPackageScheduleOverrideStatus.Active;
    public string? CurrencyCode { get; set; }
    public decimal? PriceOverride { get; set; }
    public decimal? CostOverride { get; set; }
    public Guid? BoundSourceEntityId { get; set; }
    public string? BoundSnapshotJson { get; set; }
    public string? RuleOverrideJson { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class TourPackageScheduleOverrideUpdateRequest
{
    public Guid? TourPackageComponentOptionId { get; set; }
    public TourPackageScheduleOverrideStatus? Status { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? PriceOverride { get; set; }
    public decimal? CostOverride { get; set; }
    public Guid? BoundSourceEntityId { get; set; }
    public string? BoundSnapshotJson { get; set; }
    public string? RuleOverrideJson { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class TourPackageScheduleOverrideCreateResponse
{
    public Guid Id { get; set; }
}
