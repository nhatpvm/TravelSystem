// FILE #225: TicketBooking.Api/Controllers/Hotels/MealPlansAdminController.cs
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Hotels;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Hotels;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/meal-plans")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class MealPlansAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public MealPlansAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // =========================================================
    // DICTIONARY CRUD
    // =========================================================

    [HttpGet]
    public async Task<ActionResult<AdminMealPlanPagedResponse>> List(
        [FromQuery] string? q = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        IQueryable<MealPlan> query = includeDeleted
            ? _db.MealPlans.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.MealPlans.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(qq) ||
                x.Name.Contains(qq) ||
                (x.Description != null && x.Description.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminMealPlanListItemDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Category = null,
                SortOrder = null,
                IncludesBreakfast = null,
                IncludesLunch = null,
                IncludesDinner = null,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new AdminMealPlanPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminMealPlanDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        IQueryable<MealPlan> query = includeDeleted
            ? _db.MealPlans.IgnoreQueryFilters()
            : _db.MealPlans;

        query = query.Where(x => x.TenantId == tenantId);

        if (!includeDeleted)
            query = query.Where(x => !x.IsDeleted);

        var entity = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Meal plan not found in current switched tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<AdminCreateMealPlanResponse>> Create(
        [FromBody] AdminCreateMealPlanRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateCreate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var code = req.Code!.Trim();

        var exists = await _db.MealPlans.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (exists)
            return Conflict(new { message = "Meal plan code already exists in current switched tenant." });

        var entity = new MealPlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = req.Name!.Trim(),
            Description = NullIfWhiteSpace(req.Description),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.MealPlans.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new AdminCreateMealPlanResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] AdminUpdateMealPlanRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateUpdate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.MealPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Meal plan not found in current switched tenant." });

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

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var code = req.Code.Trim();

            var exists = await _db.MealPlans.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Code == code && x.Id != id, ct);

            if (exists)
                return Conflict(new { message = "Meal plan code already exists in current switched tenant." });

            entity.Code = code;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
            entity.Name = req.Name.Trim();

        if (req.Description is not null)
            entity.Description = NullIfWhiteSpace(req.Description);

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
            return Conflict(new { message = "Data was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        => await ToggleDeleted(id, true, ct);

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
        => await ToggleDeleted(id, false, ct);

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct = default)
        => await ToggleActive(id, true, ct);

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
        => await ToggleActive(id, false, ct);

    // =========================================================
    // ROOM TYPE LINKING
    // =========================================================

    [HttpGet("room-types/{roomTypeId:guid}/links")]
    public async Task<ActionResult<List<AdminRoomTypeMealPlanLinkDto>>> GetRoomTypeLinks(
        Guid roomTypeId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        var roomTypeExists = await _db.RoomTypes.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == roomTypeId && !x.IsDeleted, ct);

        if (!roomTypeExists)
            return NotFound(new { message = "Room type not found in current switched tenant." });

        IQueryable<RoomTypeMealPlan> query = includeDeleted
            ? _db.RoomTypeMealPlans.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.RoomTypeMealPlans.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        var links = await query
            .Where(x => x.RoomTypeId == roomTypeId)
            .ToListAsync(ct);

        var mealPlanIds = links.Select(x => x.MealPlanId).Distinct().ToList();

        var mealPlans = await _db.MealPlans.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && mealPlanIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var result = links
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => mealPlans.TryGetValue(x.MealPlanId, out var m) ? m.Name : string.Empty)
            .Select(x =>
            {
                mealPlans.TryGetValue(x.MealPlanId, out var mealPlan);

                return new AdminRoomTypeMealPlanLinkDto
                {
                    Id = x.Id,
                    RoomTypeId = x.RoomTypeId,
                    MealPlanId = x.MealPlanId,
                    MealPlanCode = mealPlan?.Code,
                    MealPlanName = mealPlan?.Name,
                    AdditionalPrice = null,
                    CurrencyCode = null,
                    IsDefault = x.IsDefault,
                    IsIncluded = null,
                    IsDeleted = x.IsDeleted
                };
            })
            .ToList();

        return Ok(result);
    }

    [HttpPut("room-types/{roomTypeId:guid}/links")]
    public async Task<IActionResult> ReplaceRoomTypeLinks(
        Guid roomTypeId,
        [FromBody] AdminReplaceRoomTypeMealPlanLinksRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var roomTypeExists = await _db.RoomTypes.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == roomTypeId && !x.IsDeleted, ct);

        if (!roomTypeExists)
            return NotFound(new { message = "Room type not found in current switched tenant." });

        var mealPlanIds = req.Items.Select(x => x.MealPlanId).Distinct().ToList();

        if (mealPlanIds.Count > 0)
        {
            var validIds = await _db.MealPlans.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted && mealPlanIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(ct);

            var invalidIds = mealPlanIds.Except(validIds).ToList();
            if (invalidIds.Count > 0)
                return BadRequest(new { message = "One or more MealPlanId values are invalid in current switched tenant." });
        }

        ValidateRoomTypeLinks(req);

        var existingLinks = await _db.RoomTypeMealPlans.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.RoomTypeId == roomTypeId)
            .ToListAsync(ct);

        var requestedLinks = req.Items
            .GroupBy(x => x.MealPlanId)
            .Select(g => new
            {
                MealPlanId = g.Key,
                IsDefault = g.First().IsDefault ?? false
            })
            .ToList();

        var existingByMealPlanId = existingLinks.ToDictionary(x => x.MealPlanId);
        var requestedMealPlanIds = requestedLinks.Select(x => x.MealPlanId).ToHashSet();

        foreach (var item in requestedLinks)
        {
            if (existingByMealPlanId.TryGetValue(item.MealPlanId, out var existing))
            {
                existing.IsDefault = item.IsDefault;
                existing.IsActive = true;
                existing.IsDeleted = false;
                existing.UpdatedAt = now;
                existing.UpdatedByUserId = userId;
                continue;
            }

            _db.RoomTypeMealPlans.Add(new RoomTypeMealPlan
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RoomTypeId = roomTypeId,
                MealPlanId = item.MealPlanId,
                IsDefault = item.IsDefault,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId,
                UpdatedAt = now,
                UpdatedByUserId = userId
            });
        }

        foreach (var existing in existingLinks)
        {
            if (requestedMealPlanIds.Contains(existing.MealPlanId) || existing.IsDeleted)
                continue;

            existing.IsActive = false;
            existing.IsDeleted = true;
            existing.UpdatedAt = now;
            existing.UpdatedByUserId = userId;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.MealPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Meal plan not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        if (!isDeleted) entity.IsActive = true;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid id, bool isActive, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.MealPlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Meal plan not found in current switched tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private void RequireAdminWriteTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Admin write requires switched tenant context (X-TenantId).");
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static void ValidateCreate(AdminCreateMealPlanRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Code))
            throw new ArgumentException("Code is required.");

        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");

        if (req.Code.Length > 80)
            throw new ArgumentException("Code max length is 80.");

        if (req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

    }

    private static void ValidateUpdate(AdminUpdateMealPlanRequest req)
    {
        if (req.Code is not null && req.Code.Length > 80)
            throw new ArgumentException("Code max length is 80.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

    }

    private static AdminMealPlanListItemDto MapListItem(MealPlan x)
    {
        return new AdminMealPlanListItemDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Category = null,
            SortOrder = null,
            IncludesBreakfast = null,
            IncludesLunch = null,
            IncludesDinner = null,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }

    private static AdminMealPlanDetailDto MapDetail(MealPlan x)
    {
        return new AdminMealPlanDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            Category = null,
            SortOrder = null,
            IncludesBreakfast = null,
            IncludesLunch = null,
            IncludesDinner = null,
            MetadataJson = null,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            CreatedByUserId = x.CreatedByUserId,
            UpdatedAt = x.UpdatedAt,
            UpdatedByUserId = x.UpdatedByUserId,
            RowVersionBase64 = x.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
        };
    }

    private static void ValidateRoomTypeLinks(AdminReplaceRoomTypeMealPlanLinksRequest req)
    {
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class AdminMealPlanPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminMealPlanListItemDto> Items { get; set; } = new();
}

public sealed class AdminMealPlanListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public int? SortOrder { get; set; }
    public bool? IncludesBreakfast { get; set; }
    public bool? IncludesLunch { get; set; }
    public bool? IncludesDinner { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AdminMealPlanDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int? SortOrder { get; set; }
    public bool? IncludesBreakfast { get; set; }
    public bool? IncludesLunch { get; set; }
    public bool? IncludesDinner { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminRoomTypeMealPlanLinkDto
{
    public Guid Id { get; set; }
    public Guid RoomTypeId { get; set; }
    public Guid MealPlanId { get; set; }
    public string? MealPlanCode { get; set; }
    public string? MealPlanName { get; set; }
    public decimal? AdditionalPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsIncluded { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class AdminCreateMealPlanRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int? SortOrder { get; set; }
    public bool? IncludesBreakfast { get; set; }
    public bool? IncludesLunch { get; set; }
    public bool? IncludesDinner { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AdminUpdateMealPlanRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int? SortOrder { get; set; }
    public bool? IncludesBreakfast { get; set; }
    public bool? IncludesLunch { get; set; }
    public bool? IncludesDinner { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminReplaceRoomTypeMealPlanLinksRequest
{
    public List<AdminReplaceRoomTypeMealPlanLinkItem> Items { get; set; } = new();
}

public sealed class AdminReplaceRoomTypeMealPlanLinkItem
{
    public Guid MealPlanId { get; set; }
    public decimal? AdditionalPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public bool? IsDefault { get; set; }
    public bool? IsIncluded { get; set; }
}

public sealed class AdminCreateMealPlanResponse
{
    public Guid Id { get; set; }
}

