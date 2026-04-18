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
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/packages/{packageId:guid}/components")]
[Route("api/v{version:apiVersion}/admin/tours/{tourId:guid}/packages/{packageId:guid}/components")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class TourPackageComponentsManagementController : TourPackageManagementControllerBase
{
    public TourPackageComponentsManagementController(AppDbContext db, ITenantContext tenant)
        : base(db, tenant)
    {
    }

    [HttpGet]
    public async Task<ActionResult<TourPackageComponentPagedResponse>> List(
        Guid tourId,
        Guid packageId,
        [FromQuery] string? q = null,
        [FromQuery] TourPackageComponentType? componentType = null,
        [FromQuery] TourPackageSelectionMode? selectionMode = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await RequirePackageAsync(tenantId, tourId, packageId, ct);

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : pageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        IQueryable<TourPackageComponent> query = includeDeleted
            ? Db.TourPackageComponents.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourPackageId == packageId)
            : Db.TourPackageComponents.Where(x => x.TenantId == tenantId && x.TourPackageId == packageId && !x.IsDeleted);

        if (componentType.HasValue)
            query = query.Where(x => x.ComponentType == componentType.Value);

        if (selectionMode.HasValue)
            query = query.Where(x => x.SelectionMode == selectionMode.Value);

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
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var componentIds = items.Select(x => x.Id).ToList();
        var optionCounts = await Db.TourPackageComponentOptions.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && componentIds.Contains(x.TourPackageComponentId) && !x.IsDeleted)
            .GroupBy(x => x.TourPackageComponentId)
            .Select(g => new { TourPackageComponentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TourPackageComponentId, x => x.Count, ct);

        return Ok(new TourPackageComponentPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items.Select(x => new TourPackageComponentListItemDto
            {
                Id = x.Id,
                TourPackageId = x.TourPackageId,
                Code = x.Code,
                Name = x.Name,
                ComponentType = x.ComponentType,
                SelectionMode = x.SelectionMode,
                MinSelect = x.MinSelect,
                MaxSelect = x.MaxSelect,
                DayOffsetFromDeparture = x.DayOffsetFromDeparture,
                NightCount = x.NightCount,
                SortOrder = x.SortOrder,
                OptionCount = optionCounts.GetValueOrDefault(x.Id),
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList()
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourPackageComponentDetailDto>> GetById(
        Guid tourId,
        Guid packageId,
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await RequireComponentAsync(tenantId, tourId, packageId, id, ct, includeDeleted);

        var options = await Db.TourPackageComponentOptions.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.TourPackageComponentId == id)
            .OrderByDescending(x => x.IsDefaultSelected)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new TourPackageOptionSummaryDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                SourceType = x.SourceType,
                BindingMode = x.BindingMode,
                PricingMode = x.PricingMode,
                IsDefaultSelected = x.IsDefaultSelected,
                IsFallback = x.IsFallback,
                IsDynamicCandidate = x.IsDynamicCandidate,
                SortOrder = x.SortOrder,
                OverrideCount = x.ScheduleOverrides.Count(o => !o.IsDeleted),
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted
            })
            .ToListAsync(ct);

        return Ok(TourPackageManagementMaps.MapComponentDetail(entity, options));
    }

    [HttpPost]
    public async Task<ActionResult<TourPackageComponentCreateResponse>> Create(
        Guid tourId,
        Guid packageId,
        [FromBody] TourPackageComponentCreateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await RequirePackageAsync(tenantId, tourId, packageId, ct);
        await ValidateCreateAsync(tenantId, packageId, req, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = new TourPackageComponent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourPackageId = packageId,
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            ComponentType = req.ComponentType,
            SelectionMode = req.SelectionMode,
            MinSelect = req.MinSelect,
            MaxSelect = req.MaxSelect,
            DayOffsetFromDeparture = req.DayOffsetFromDeparture,
            NightCount = req.NightCount,
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

        Db.TourPackageComponents.Add(entity);
        await Db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", tourId, packageId, id = entity.Id },
            new TourPackageComponentCreateResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid tourId,
        Guid packageId,
        Guid id,
        [FromBody] TourPackageComponentUpdateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenantContext();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await Db.TourPackageComponents.IgnoreQueryFilters()
            .Include(x => x.TourPackage)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourPackageId == packageId && x.Id == id, ct);

        if (entity is null || entity.TourPackage is null || entity.TourPackage.TourId != tourId)
            return NotFound(new { message = "Tour package component not found in current tenant." });

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

        await ValidateUpdateAsync(tenantId, packageId, id, req, entity, ct);

        if (req.Code is not null) entity.Code = req.Code.Trim();
        if (req.Name is not null) entity.Name = req.Name.Trim();
        if (req.ComponentType.HasValue) entity.ComponentType = req.ComponentType.Value;
        if (req.SelectionMode.HasValue) entity.SelectionMode = req.SelectionMode.Value;
        if (req.MinSelect.HasValue) entity.MinSelect = req.MinSelect;
        if (req.MaxSelect.HasValue) entity.MaxSelect = req.MaxSelect;
        if (req.DayOffsetFromDeparture.HasValue) entity.DayOffsetFromDeparture = req.DayOffsetFromDeparture;
        if (req.NightCount.HasValue) entity.NightCount = req.NightCount;
        if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;
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
            return Conflict(new { message = "Tour package component was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid tourId, Guid packageId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, packageId, id, true, ct);

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid tourId, Guid packageId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, packageId, id, false, ct);

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid tourId, Guid packageId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, packageId, id, true, ct);

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid tourId, Guid packageId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, packageId, id, false, ct);

    private async Task<IActionResult> ToggleDeleted(Guid tourId, Guid packageId, Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireTenantContext();
        var entity = await RequireComponentAsync(tenantId, tourId, packageId, id, ct);

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await Db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid tourId, Guid packageId, Guid id, bool isActive, CancellationToken ct)
    {
        var tenantId = RequireTenantContext();
        var entity = await RequireComponentAsync(tenantId, tourId, packageId, id, ct);

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await Db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task ValidateCreateAsync(Guid tenantId, Guid packageId, TourPackageComponentCreateRequest req, CancellationToken ct)
    {
        ValidatePayload(req.Code, req.Name, req.SelectionMode, req.MinSelect, req.MaxSelect, req.NightCount);

        var code = req.Code.Trim();
        var exists = await Db.TourPackageComponents.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourPackageId == packageId && x.Code == code, ct);

        if (exists)
            throw new ArgumentException("Code already exists in current package.");
    }

    private async Task ValidateUpdateAsync(
        Guid tenantId,
        Guid packageId,
        Guid currentId,
        TourPackageComponentUpdateRequest req,
        TourPackageComponent current,
        CancellationToken ct)
    {
        var nextCode = req.Code ?? current.Code;
        var nextName = req.Name ?? current.Name;
        var nextSelectionMode = req.SelectionMode ?? current.SelectionMode;
        var nextMinSelect = req.MinSelect ?? current.MinSelect;
        var nextMaxSelect = req.MaxSelect ?? current.MaxSelect;
        var nextNightCount = req.NightCount ?? current.NightCount;

        ValidatePayload(nextCode, nextName, nextSelectionMode, nextMinSelect, nextMaxSelect, nextNightCount);

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var code = req.Code.Trim();
            var exists = await Db.TourPackageComponents.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.TourPackageId == packageId && x.Code == code && x.Id != currentId, ct);

            if (exists)
                throw new ArgumentException("Code already exists in current package.");
        }
    }

    private static void ValidatePayload(
        string code,
        string name,
        TourPackageSelectionMode selectionMode,
        int? minSelect,
        int? maxSelect,
        int? nightCount)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");

        if (code.Trim().Length > 50)
            throw new ArgumentException("Code max length is 50.");

        if (name.Trim().Length > 300)
            throw new ArgumentException("Name max length is 300.");

        if (minSelect.HasValue && minSelect <= 0)
            throw new ArgumentException("MinSelect must be greater than 0.");

        if (maxSelect.HasValue && maxSelect <= 0)
            throw new ArgumentException("MaxSelect must be greater than 0.");

        if (minSelect.HasValue && maxSelect.HasValue && minSelect > maxSelect)
            throw new ArgumentException("MinSelect cannot be greater than MaxSelect.");

        if (nightCount.HasValue && nightCount < 0)
            throw new ArgumentException("NightCount cannot be negative.");

        if (selectionMode == TourPackageSelectionMode.RequiredSingle)
        {
            if (minSelect.HasValue && minSelect.Value != 1)
                throw new ArgumentException("RequiredSingle requires MinSelect = 1 when specified.");

            if (maxSelect.HasValue && maxSelect.Value != 1)
                throw new ArgumentException("RequiredSingle requires MaxSelect = 1 when specified.");
        }

        if (selectionMode == TourPackageSelectionMode.OptionalSingle)
        {
            if (minSelect.HasValue && minSelect.Value > 1)
                throw new ArgumentException("OptionalSingle cannot have MinSelect > 1.");

            if (maxSelect.HasValue && maxSelect.Value > 1)
                throw new ArgumentException("OptionalSingle cannot have MaxSelect > 1.");
        }
    }
}
