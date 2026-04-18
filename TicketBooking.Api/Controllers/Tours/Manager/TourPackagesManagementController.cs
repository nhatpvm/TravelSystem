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
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/packages")]
[Route("api/v{version:apiVersion}/admin/tours/{tourId:guid}/packages")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class TourPackagesManagementController : TourPackageManagementControllerBase
{
    public TourPackagesManagementController(AppDbContext db, ITenantContext tenant)
        : base(db, tenant)
    {
    }

    [HttpGet]
    public async Task<ActionResult<TourPackagePagedResponse>> List(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] TourPackageMode? mode = null,
        [FromQuery] TourPackageStatus? status = null,
        [FromQuery] bool? isDefault = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : pageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        IQueryable<TourPackage> query = includeDeleted
            ? Db.TourPackages.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : Db.TourPackages.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        if (mode.HasValue)
            query = query.Where(x => x.Mode == mode.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (isDefault.HasValue)
            query = query.Where(x => x.IsDefault == isDefault.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(qq) ||
                x.Name.Contains(qq) ||
                (x.PricingRuleJson != null && x.PricingRuleJson.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var packageIds = items.Select(x => x.Id).ToList();
        var componentCounts = await Db.TourPackageComponents.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && packageIds.Contains(x.TourPackageId) && !x.IsDeleted)
            .GroupBy(x => x.TourPackageId)
            .Select(g => new { TourPackageId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TourPackageId, x => x.Count, ct);

        return Ok(new TourPackagePagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(x => new TourPackageListItemDto
            {
                Id = x.Id,
                TourId = x.TourId,
                Code = x.Code,
                Name = x.Name,
                Mode = x.Mode,
                Status = x.Status,
                CurrencyCode = x.CurrencyCode,
                IsDefault = x.IsDefault,
                AutoRepriceBeforeConfirm = x.AutoRepriceBeforeConfirm,
                HoldStrategy = x.HoldStrategy,
                ComponentCount = componentCounts.GetValueOrDefault(x.Id),
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList()
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourPackageDetailDto>> GetById(
        Guid tourId,
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        IQueryable<TourPackage> query = includeDeleted
            ? Db.TourPackages.IgnoreQueryFilters()
            : Db.TourPackages.Where(x => !x.IsDeleted);

        var entity = await query
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour package not found in current tenant." });

        var components = await Db.TourPackageComponents.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TourPackageId == id)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new TourPackageComponentSummaryDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                ComponentType = x.ComponentType,
                SelectionMode = x.SelectionMode,
                MinSelect = x.MinSelect,
                MaxSelect = x.MaxSelect,
                SortOrder = x.SortOrder,
                OptionCount = x.Options.Count(o => !o.IsDeleted),
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted
            })
            .ToListAsync(ct);

        return Ok(TourPackageManagementMaps.MapPackageDetail(entity, components));
    }

    [HttpPost]
    public async Task<ActionResult<TourPackageCreateResponse>> Create(
        Guid tourId,
        [FromBody] TourPackageCreateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await ValidateCreateAsync(tenantId, tourId, req, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();
        var isDefault = req.IsDefault ?? false;
        var isActive = req.IsActive ?? true;

        if (req.Status == TourPackageStatus.Active)
            isActive = true;

        if (isDefault)
            await ClearDefaultPackagesAsync(tenantId, tourId, exceptId: null, ct);

        var entity = new TourPackage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tourId,
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            Mode = req.Mode,
            Status = req.Status,
            CurrencyCode = req.CurrencyCode.Trim(),
            IsDefault = isDefault,
            AutoRepriceBeforeConfirm = req.AutoRepriceBeforeConfirm ?? true,
            HoldStrategy = req.HoldStrategy,
            PricingRuleJson = NullIfWhiteSpace(req.PricingRuleJson),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsActive = isActive,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        Db.TourPackages.Add(entity);
        await Db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", tourId, id = entity.Id },
            new TourPackageCreateResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid tourId,
        Guid id,
        [FromBody] TourPackageUpdateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await Db.TourPackages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour package not found in current tenant." });

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

        await ValidateUpdateAsync(tenantId, tourId, id, req, entity, ct);

        if (req.Code is not null) entity.Code = req.Code.Trim();
        if (req.Name is not null) entity.Name = req.Name.Trim();
        if (req.Mode.HasValue) entity.Mode = req.Mode.Value;
        if (req.Status.HasValue) entity.Status = req.Status.Value;
        if (req.CurrencyCode is not null) entity.CurrencyCode = req.CurrencyCode.Trim();
        if (req.IsDefault.HasValue) entity.IsDefault = req.IsDefault.Value;
        if (req.AutoRepriceBeforeConfirm.HasValue) entity.AutoRepriceBeforeConfirm = req.AutoRepriceBeforeConfirm.Value;
        if (req.HoldStrategy.HasValue) entity.HoldStrategy = req.HoldStrategy.Value;
        if (req.PricingRuleJson is not null) entity.PricingRuleJson = NullIfWhiteSpace(req.PricingRuleJson);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        if (entity.Status == TourPackageStatus.Active)
            entity.IsActive = true;

        if (entity.IsDefault)
            await ClearDefaultPackagesAsync(tenantId, tourId, exceptId: entity.Id, ct);

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        try
        {
            await Db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Tour package was changed by another user. Please reload and try again." });
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

    [HttpPost("{id:guid}/mark-default")]
    public async Task<IActionResult> MarkDefault(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDefault(tourId, id, true, ct);

    [HttpPost("{id:guid}/unmark-default")]
    public async Task<IActionResult> UnmarkDefault(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDefault(tourId, id, false, ct);

    private async Task<IActionResult> ToggleDeleted(Guid tourId, Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireTenantContext();
        var entity = await RequirePackageAsync(tenantId, tourId, id, ct);

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await Db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid tourId, Guid id, bool isActive, CancellationToken ct)
    {
        var tenantId = RequireTenantContext();
        var entity = await RequirePackageAsync(tenantId, tourId, id, ct);

        entity.IsActive = isActive;
        if (!isActive && entity.Status == TourPackageStatus.Active)
            entity.Status = TourPackageStatus.Inactive;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await Db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleDefault(Guid tourId, Guid id, bool isDefault, CancellationToken ct)
    {
        var tenantId = RequireTenantContext();
        var entity = await RequirePackageAsync(tenantId, tourId, id, ct);

        if (isDefault)
        {
            await ClearDefaultPackagesAsync(tenantId, tourId, entity.Id, ct);
            entity.IsActive = true;
        }

        entity.IsDefault = isDefault;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await Db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task ClearDefaultPackagesAsync(Guid tenantId, Guid tourId, Guid? exceptId, CancellationToken ct)
    {
        var packages = await Db.TourPackages.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.IsDefault && (!exceptId.HasValue || x.Id != exceptId.Value))
            .ToListAsync(ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        foreach (var item in packages)
        {
            item.IsDefault = false;
            item.UpdatedAt = now;
            item.UpdatedByUserId = userId;
        }
    }

    private async Task ValidateCreateAsync(Guid tenantId, Guid tourId, TourPackageCreateRequest req, CancellationToken ct)
    {
        ValidatePayload(req.Code, req.Name, req.CurrencyCode);

        var code = req.Code.Trim();
        var exists = await Db.TourPackages.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Code == code, ct);

        if (exists)
            throw new ArgumentException("Code already exists in current tour.");
    }

    private async Task ValidateUpdateAsync(
        Guid tenantId,
        Guid tourId,
        Guid currentId,
        TourPackageUpdateRequest req,
        TourPackage current,
        CancellationToken ct)
    {
        var nextCode = req.Code ?? current.Code;
        var nextName = req.Name ?? current.Name;
        var nextCurrency = req.CurrencyCode ?? current.CurrencyCode;

        ValidatePayload(nextCode, nextName, nextCurrency);

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var code = req.Code.Trim();
            var exists = await Db.TourPackages.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Code == code && x.Id != currentId, ct);

            if (exists)
                throw new ArgumentException("Code already exists in current tour.");
        }
    }

    private static void ValidatePayload(string code, string name, string currencyCode)
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
    }
}
