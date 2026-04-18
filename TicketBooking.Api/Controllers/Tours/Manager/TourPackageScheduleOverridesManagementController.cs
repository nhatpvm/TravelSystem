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
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/schedules/{scheduleId:guid}/package-overrides")]
[Route("api/v{version:apiVersion}/admin/tours/{tourId:guid}/schedules/{scheduleId:guid}/package-overrides")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class TourPackageScheduleOverridesManagementController : TourPackageManagementControllerBase
{
    public TourPackageScheduleOverridesManagementController(AppDbContext db, ITenantContext tenant)
        : base(db, tenant)
    {
    }

    [HttpGet]
    public async Task<ActionResult<TourPackageScheduleOverridePagedResponse>> List(
        Guid tourId,
        Guid scheduleId,
        [FromQuery] Guid? packageId = null,
        [FromQuery] Guid? componentId = null,
        [FromQuery] Guid? optionId = null,
        [FromQuery] TourPackageScheduleOverrideStatus? status = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : pageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        IQueryable<TourPackageScheduleOptionOverride> query = includeDeleted
            ? Db.TourPackageScheduleOptionOverrides.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId)
            : Db.TourPackageScheduleOptionOverrides.Where(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && !x.IsDeleted);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (optionId.HasValue && optionId.Value != Guid.Empty)
            query = query.Where(x => x.TourPackageComponentOptionId == optionId.Value);

        if (componentId.HasValue && componentId.Value != Guid.Empty)
        {
            query = query.Where(x =>
                x.TourPackageComponentOption != null &&
                x.TourPackageComponentOption.TourPackageComponentId == componentId.Value);
        }

        if (packageId.HasValue && packageId.Value != Guid.Empty)
        {
            query = query.Where(x =>
                x.TourPackageComponentOption != null &&
                x.TourPackageComponentOption.TourPackageComponent != null &&
                x.TourPackageComponentOption.TourPackageComponent.TourPackageId == packageId.Value);
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .Include(x => x.TourPackageComponentOption)
            .ThenInclude(x => x!.TourPackageComponent)
            .ThenInclude(x => x!.TourPackage)
            .OrderBy(x => x.TourPackageComponentOption!.TourPackageComponent!.SortOrder)
            .ThenBy(x => x.TourPackageComponentOption!.SortOrder)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new TourPackageScheduleOverridePagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(x => new TourPackageScheduleOverrideListItemDto
            {
                Id = x.Id,
                TourScheduleId = x.TourScheduleId,
                TourPackageId = x.TourPackageComponentOption!.TourPackageComponent!.TourPackageId,
                TourPackageComponentId = x.TourPackageComponentOption.TourPackageComponentId,
                TourPackageComponentOptionId = x.TourPackageComponentOptionId,
                TourPackageCode = x.TourPackageComponentOption.TourPackageComponent.TourPackage!.Code,
                TourPackageName = x.TourPackageComponentOption.TourPackageComponent.TourPackage.Name,
                TourPackageComponentCode = x.TourPackageComponentOption.TourPackageComponent.Code,
                TourPackageComponentName = x.TourPackageComponentOption.TourPackageComponent.Name,
                TourPackageOptionCode = x.TourPackageComponentOption.Code,
                TourPackageOptionName = x.TourPackageComponentOption.Name,
                Status = x.Status,
                CurrencyCode = x.CurrencyCode,
                PriceOverride = x.PriceOverride,
                CostOverride = x.CostOverride,
                BoundSourceEntityId = x.BoundSourceEntityId,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList()
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourPackageScheduleOverrideDetailDto>> GetById(
        Guid tourId,
        Guid scheduleId,
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        IQueryable<TourPackageScheduleOptionOverride> query = includeDeleted
            ? Db.TourPackageScheduleOptionOverrides.IgnoreQueryFilters()
            : Db.TourPackageScheduleOptionOverrides.Where(x => !x.IsDeleted);

        var entity = await query
            .AsNoTracking()
            .Include(x => x.TourPackageComponentOption)
            .ThenInclude(x => x!.TourPackageComponent)
            .ThenInclude(x => x!.TourPackage)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && x.Id == id, ct);

        if (entity is null ||
            entity.TourPackageComponentOption is null ||
            entity.TourPackageComponentOption.TourPackageComponent is null ||
            entity.TourPackageComponentOption.TourPackageComponent.TourPackage is null ||
            entity.TourPackageComponentOption.TourPackageComponent.TourPackage.TourId != tourId)
        {
            return NotFound(new { message = "Tour package schedule override not found in current tenant." });
        }

        var path = BuildPath(entity.TourPackageComponentOption);
        return Ok(TourPackageManagementMaps.MapOverrideDetail(entity, path));
    }

    [HttpPost]
    public async Task<ActionResult<TourPackageScheduleOverrideCreateResponse>> Create(
        Guid tourId,
        Guid scheduleId,
        [FromBody] TourPackageScheduleOverrideCreateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        if (req.TourPackageComponentOptionId == Guid.Empty)
            return BadRequest(new { message = "TourPackageComponentOptionId is required." });

        await EnsureOptionBelongsToTourAsync(tenantId, tourId, req.TourPackageComponentOptionId, ct);
        await ValidateCreateAsync(tenantId, scheduleId, req, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = new TourPackageScheduleOptionOverride
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourScheduleId = scheduleId,
            TourPackageComponentOptionId = req.TourPackageComponentOptionId,
            Status = req.Status,
            CurrencyCode = NullIfWhiteSpace(req.CurrencyCode),
            PriceOverride = req.PriceOverride,
            CostOverride = req.CostOverride,
            BoundSourceEntityId = NormalizeGuid(req.BoundSourceEntityId),
            BoundSnapshotJson = NullIfWhiteSpace(req.BoundSnapshotJson),
            RuleOverrideJson = NullIfWhiteSpace(req.RuleOverrideJson),
            Notes = NullIfWhiteSpace(req.Notes),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        Db.TourPackageScheduleOptionOverrides.Add(entity);
        await Db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", tourId, scheduleId, id = entity.Id },
            new TourPackageScheduleOverrideCreateResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid tourId,
        Guid scheduleId,
        Guid id,
        [FromBody] TourPackageScheduleOverrideUpdateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        var entity = await Db.TourPackageScheduleOptionOverrides.IgnoreQueryFilters()
            .Include(x => x.TourPackageComponentOption)
            .ThenInclude(x => x!.TourPackageComponent)
            .ThenInclude(x => x!.TourPackage)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && x.Id == id, ct);

        if (entity is null ||
            entity.TourPackageComponentOption is null ||
            entity.TourPackageComponentOption.TourPackageComponent is null ||
            entity.TourPackageComponentOption.TourPackageComponent.TourPackage is null ||
            entity.TourPackageComponentOption.TourPackageComponent.TourPackage.TourId != tourId)
        {
            return NotFound(new { message = "Tour package schedule override not found in current tenant." });
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

        if (req.TourPackageComponentOptionId.HasValue && req.TourPackageComponentOptionId.Value != Guid.Empty)
            await EnsureOptionBelongsToTourAsync(tenantId, tourId, req.TourPackageComponentOptionId.Value, ct);

        await ValidateUpdateAsync(tenantId, scheduleId, id, req, entity, ct);

        if (req.TourPackageComponentOptionId.HasValue && req.TourPackageComponentOptionId.Value != Guid.Empty)
            entity.TourPackageComponentOptionId = req.TourPackageComponentOptionId.Value;

        if (req.Status.HasValue) entity.Status = req.Status.Value;
        if (req.CurrencyCode is not null) entity.CurrencyCode = NullIfWhiteSpace(req.CurrencyCode);
        if (req.PriceOverride.HasValue) entity.PriceOverride = req.PriceOverride;
        if (req.CostOverride.HasValue) entity.CostOverride = req.CostOverride;
        if (req.BoundSourceEntityId.HasValue) entity.BoundSourceEntityId = NormalizeGuid(req.BoundSourceEntityId);
        if (req.BoundSnapshotJson is not null) entity.BoundSnapshotJson = NullIfWhiteSpace(req.BoundSnapshotJson);
        if (req.RuleOverrideJson is not null) entity.RuleOverrideJson = NullIfWhiteSpace(req.RuleOverrideJson);
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        try
        {
            await Db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Tour package schedule override was changed by another user. Please reload and try again." });
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

    private async Task<IActionResult> ToggleDeleted(Guid tourId, Guid scheduleId, Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        var entity = await Db.TourPackageScheduleOptionOverrides.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour package schedule override not found in current tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await Db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid tourId, Guid scheduleId, Guid id, bool isActive, CancellationToken ct)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await EnsureScheduleExistsAsync(tenantId, tourId, scheduleId, ct);

        var entity = await Db.TourPackageScheduleOptionOverrides.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourScheduleId == scheduleId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour package schedule override not found in current tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await Db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task EnsureOptionBelongsToTourAsync(Guid tenantId, Guid tourId, Guid optionId, CancellationToken ct)
    {
        var exists = await Db.TourPackageComponentOptions.IgnoreQueryFilters()
            .Include(x => x.TourPackageComponent)
            .ThenInclude(x => x!.TourPackage)
            .AnyAsync(
                x => x.TenantId == tenantId &&
                    x.Id == optionId &&
                    x.TourPackageComponent != null &&
                    x.TourPackageComponent.TourPackage != null &&
                    x.TourPackageComponent.TourPackage.TourId == tourId,
                ct);

        if (!exists)
            throw new ArgumentException("TourPackageComponentOptionId does not belong to current tour.");
    }

    private async Task ValidateCreateAsync(
        Guid tenantId,
        Guid scheduleId,
        TourPackageScheduleOverrideCreateRequest req,
        CancellationToken ct)
    {
        ValidatePayload(
            req.Status,
            req.CurrencyCode,
            req.PriceOverride,
            req.CostOverride,
            req.BoundSourceEntityId,
            req.BoundSnapshotJson);

        var exists = await Db.TourPackageScheduleOptionOverrides.IgnoreQueryFilters()
            .AnyAsync(
                x => x.TenantId == tenantId &&
                    x.TourScheduleId == scheduleId &&
                    x.TourPackageComponentOptionId == req.TourPackageComponentOptionId,
                ct);

        if (exists)
            throw new ArgumentException("Schedule override already exists for this option.");
    }

    private async Task ValidateUpdateAsync(
        Guid tenantId,
        Guid scheduleId,
        Guid currentId,
        TourPackageScheduleOverrideUpdateRequest req,
        TourPackageScheduleOptionOverride current,
        CancellationToken ct)
    {
        var nextOptionId = req.TourPackageComponentOptionId.HasValue && req.TourPackageComponentOptionId.Value != Guid.Empty
            ? req.TourPackageComponentOptionId.Value
            : current.TourPackageComponentOptionId;

        var nextStatus = req.Status ?? current.Status;
        var nextCurrency = req.CurrencyCode ?? current.CurrencyCode;
        var nextPriceOverride = req.PriceOverride ?? current.PriceOverride;
        var nextCostOverride = req.CostOverride ?? current.CostOverride;
        var nextBoundSourceEntityId = req.BoundSourceEntityId.HasValue ? NormalizeGuid(req.BoundSourceEntityId) : current.BoundSourceEntityId;
        var nextBoundSnapshotJson = req.BoundSnapshotJson ?? current.BoundSnapshotJson;

        ValidatePayload(
            nextStatus,
            nextCurrency,
            nextPriceOverride,
            nextCostOverride,
            nextBoundSourceEntityId,
            nextBoundSnapshotJson);

        if (nextOptionId != current.TourPackageComponentOptionId)
        {
            var exists = await Db.TourPackageScheduleOptionOverrides.IgnoreQueryFilters()
                .AnyAsync(
                    x => x.TenantId == tenantId &&
                        x.TourScheduleId == scheduleId &&
                        x.TourPackageComponentOptionId == nextOptionId &&
                        x.Id != currentId,
                    ct);

            if (exists)
                throw new ArgumentException("Schedule override already exists for this option.");
        }
    }

    private static void ValidatePayload(
        TourPackageScheduleOverrideStatus status,
        string? currencyCode,
        decimal? priceOverride,
        decimal? costOverride,
        Guid? boundSourceEntityId,
        string? boundSnapshotJson)
    {
        if (!string.IsNullOrWhiteSpace(currencyCode) && currencyCode.Trim().Length > 10)
            throw new ArgumentException("CurrencyCode max length is 10.");

        if (priceOverride.HasValue && priceOverride < 0)
            throw new ArgumentException("PriceOverride cannot be negative.");

        if (costOverride.HasValue && costOverride < 0)
            throw new ArgumentException("CostOverride cannot be negative.");

        if (status == TourPackageScheduleOverrideStatus.Pinned &&
            (!boundSourceEntityId.HasValue || boundSourceEntityId.Value == Guid.Empty) &&
            string.IsNullOrWhiteSpace(boundSnapshotJson))
        {
            throw new ArgumentException("Pinned override requires BoundSourceEntityId or BoundSnapshotJson.");
        }
    }

    private static TourPackageScheduleOverridePathDto BuildPath(TourPackageComponentOption option)
    {
        return new TourPackageScheduleOverridePathDto
        {
            TourPackageId = option.TourPackageComponent!.TourPackageId,
            TourPackageCode = option.TourPackageComponent.TourPackage!.Code,
            TourPackageName = option.TourPackageComponent.TourPackage.Name,
            TourPackageComponentId = option.TourPackageComponentId,
            TourPackageComponentCode = option.TourPackageComponent.Code,
            TourPackageComponentName = option.TourPackageComponent.Name,
            TourPackageOptionCode = option.Code,
            TourPackageOptionName = option.Name
        };
    }
}
