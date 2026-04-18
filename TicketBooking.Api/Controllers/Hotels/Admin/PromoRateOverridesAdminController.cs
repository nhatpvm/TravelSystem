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
[Route("api/v{version:apiVersion}/admin/promo-rate-overrides")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class PromoRateOverridesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public PromoRateOverridesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<AdminPromoRateOverridePagedResponse>> List(
        [FromQuery] Guid? roomTypeId = null,
        [FromQuery] Guid? ratePlanId = null,
        [FromQuery] Guid? ratePlanRoomTypeId = null,
        [FromQuery] Guid? hotelId = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
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

        IQueryable<PromoRateOverride> query = includeDeleted
            ? _db.PromoRateOverrides.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.PromoRateOverrides.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (ratePlanRoomTypeId.HasValue)
            query = query.Where(x => x.RatePlanRoomTypeId == ratePlanRoomTypeId.Value);

        if (fromDate.HasValue)
            query = query.Where(x => x.EndDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(x => x.StartDate <= toDate.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                (x.PromoCode != null && x.PromoCode.Contains(qq)) ||
                (x.ConditionsJson != null && x.ConditionsJson.Contains(qq)) ||
                x.CurrencyCode.Contains(qq));
        }

        if (roomTypeId.HasValue || ratePlanId.HasValue || hotelId.HasValue)
        {
            var mappingQuery = _db.RatePlanRoomTypes.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted);

            if (roomTypeId.HasValue)
                mappingQuery = mappingQuery.Where(x => x.RoomTypeId == roomTypeId.Value);

            if (ratePlanId.HasValue)
                mappingQuery = mappingQuery.Where(x => x.RatePlanId == ratePlanId.Value);

            if (hotelId.HasValue)
            {
                var hotelRatePlanIds = await _db.RatePlans.IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenantId && x.HotelId == hotelId.Value && !x.IsDeleted)
                    .Select(x => x.Id)
                    .ToListAsync(ct);

                mappingQuery = mappingQuery.Where(x => hotelRatePlanIds.Contains(x.RatePlanId));
            }

            var mappingIds = await mappingQuery.Select(x => x.Id).ToListAsync(ct);

            if (mappingIds.Count == 0)
            {
                return Ok(new AdminPromoRateOverridePagedResponse
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = 0,
                    Items = new()
                });
            }

            query = query.Where(x => mappingIds.Contains(x.RatePlanRoomTypeId));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.AsNoTracking()
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.EndDate)
            .ThenBy(x => x.PromoCode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var contexts = await LoadContextsAsync(tenantId, rows.Select(x => x.RatePlanRoomTypeId), ct);
        var items = rows.Select(x => MapListItem(x, contexts)).ToList();

        return Ok(new AdminPromoRateOverridePagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminPromoRateOverrideDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        IQueryable<PromoRateOverride> query = includeDeleted
            ? _db.PromoRateOverrides.IgnoreQueryFilters()
            : _db.PromoRateOverrides;

        var entity = await query.AsNoTracking()
            .Where(x => x.TenantId == tenantId && (includeDeleted || !x.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Promo rate override not found in current switched tenant." });

        var contexts = await LoadContextsAsync(tenantId, new[] { entity.RatePlanRoomTypeId }, ct);
        return Ok(MapDetail(entity, contexts));
    }

    [HttpPost]
    public async Task<ActionResult<AdminCreatePromoRateOverrideResponse>> Create(
        [FromBody] AdminCreatePromoRateOverrideRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var validation = await ValidateCreateAsync(req, ct);
        if (validation is not null)
            return validation;

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = new PromoRateOverride
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RatePlanRoomTypeId = req.RatePlanRoomTypeId!.Value,
            PromoCodeId = req.PromoCodeId,
            PromoCode = FirstNonBlank(req.PromoCode, req.Code),
            StartDate = req.StartDate!.Value,
            EndDate = req.EndDate!.Value,
            OverridePrice = req.OverridePrice,
            DiscountPercent = req.DiscountPercent,
            CurrencyCode = string.IsNullOrWhiteSpace(req.CurrencyCode) ? "VND" : req.CurrencyCode!.Trim(),
            ConditionsJson = NullIfWhiteSpace(req.ConditionsJson),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.PromoRateOverrides.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new AdminCreatePromoRateOverrideResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] AdminUpdatePromoRateOverrideRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var validation = await ValidateUpdateAsync(req, ct);
        if (validation is not null)
            return validation;

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.PromoRateOverrides.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Promo rate override not found in current switched tenant." });

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

        if (req.RatePlanRoomTypeId.HasValue)
            entity.RatePlanRoomTypeId = req.RatePlanRoomTypeId.Value;

        if (req.PromoCodeId.HasValue)
            entity.PromoCodeId = req.PromoCodeId;

        if (req.PromoCode is not null || req.Code is not null)
            entity.PromoCode = FirstNonBlank(req.PromoCode, req.Code);

        if (req.StartDate.HasValue)
            entity.StartDate = req.StartDate.Value;

        if (req.EndDate.HasValue)
            entity.EndDate = req.EndDate.Value;

        if (req.OverridePrice.HasValue)
            entity.OverridePrice = req.OverridePrice;

        if (req.DiscountPercent.HasValue)
            entity.DiscountPercent = req.DiscountPercent;

        if (req.CurrencyCode is not null)
            entity.CurrencyCode = string.IsNullOrWhiteSpace(req.CurrencyCode) ? entity.CurrencyCode : req.CurrencyCode.Trim();

        if (req.ConditionsJson is not null)
            entity.ConditionsJson = NullIfWhiteSpace(req.ConditionsJson);

        if (req.IsActive.HasValue)
            entity.IsActive = req.IsActive.Value;

        if (req.IsDeleted.HasValue)
            entity.IsDeleted = req.IsDeleted.Value;

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

    private async Task<IActionResult> ToggleDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.PromoRateOverrides.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Promo rate override not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        if (!isDeleted)
            entity.IsActive = true;

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

        var entity = await _db.PromoRateOverrides.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Promo rate override not found in current switched tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<ActionResult?> ValidateCreateAsync(AdminCreatePromoRateOverrideRequest req, CancellationToken ct)
    {
        if (!req.RatePlanRoomTypeId.HasValue || req.RatePlanRoomTypeId == Guid.Empty)
            return BadRequest(new { message = "RatePlanRoomTypeId is required." });

        if (!req.StartDate.HasValue || !req.EndDate.HasValue)
            return BadRequest(new { message = "StartDate and EndDate are required." });

        if (req.EndDate.Value < req.StartDate.Value)
            return BadRequest(new { message = "EndDate must be greater than or equal to StartDate." });

        if (!req.OverridePrice.HasValue && !req.DiscountPercent.HasValue)
            return BadRequest(new { message = "At least one of OverridePrice or DiscountPercent is required." });

        if (req.OverridePrice.HasValue && req.OverridePrice.Value < 0m)
            return BadRequest(new { message = "OverridePrice must be >= 0." });

        if (req.DiscountPercent.HasValue && (req.DiscountPercent.Value < 0m || req.DiscountPercent.Value > 100m))
            return BadRequest(new { message = "DiscountPercent must be between 0 and 100." });

        if (!string.IsNullOrWhiteSpace(req.CurrencyCode) && req.CurrencyCode.Trim().Length > 10)
            return BadRequest(new { message = "CurrencyCode max length is 10." });

        var tenantId = _tenant.TenantId!.Value;
        var mappingExists = await _db.RatePlanRoomTypes.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.RatePlanRoomTypeId.Value && !x.IsDeleted, ct);

        return mappingExists
            ? null
            : BadRequest(new { message = "RatePlanRoomTypeId not found in current switched tenant." });
    }

    private async Task<ActionResult?> ValidateUpdateAsync(AdminUpdatePromoRateOverrideRequest req, CancellationToken ct)
    {
        if (req.StartDate.HasValue && req.EndDate.HasValue && req.EndDate.Value < req.StartDate.Value)
            return BadRequest(new { message = "EndDate must be greater than or equal to StartDate." });

        if (req.OverridePrice.HasValue && req.OverridePrice.Value < 0m)
            return BadRequest(new { message = "OverridePrice must be >= 0." });

        if (req.DiscountPercent.HasValue && (req.DiscountPercent.Value < 0m || req.DiscountPercent.Value > 100m))
            return BadRequest(new { message = "DiscountPercent must be between 0 and 100." });

        if (!string.IsNullOrWhiteSpace(req.CurrencyCode) && req.CurrencyCode.Trim().Length > 10)
            return BadRequest(new { message = "CurrencyCode max length is 10." });

        if (req.RatePlanRoomTypeId.HasValue)
        {
            var tenantId = _tenant.TenantId!.Value;
            var mappingExists = await _db.RatePlanRoomTypes.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == req.RatePlanRoomTypeId.Value && !x.IsDeleted, ct);

            if (!mappingExists)
                return BadRequest(new { message = "RatePlanRoomTypeId not found in current switched tenant." });
        }

        return null;
    }

    private async Task<Dictionary<Guid, PromoRateOverrideContext>> LoadContextsAsync(
        Guid tenantId,
        IEnumerable<Guid> mappingIds,
        CancellationToken ct)
    {
        var ids = mappingIds.Distinct().ToList();
        if (ids.Count == 0)
            return new();

        return await _db.RatePlanRoomTypes.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && ids.Contains(x.Id))
            .Join(
                _db.RatePlans.IgnoreQueryFilters().Where(x => x.TenantId == tenantId),
                mapping => mapping.RatePlanId,
                ratePlan => ratePlan.Id,
                (mapping, ratePlan) => new PromoRateOverrideContext
                {
                    RatePlanRoomTypeId = mapping.Id,
                    RatePlanId = mapping.RatePlanId,
                    RoomTypeId = mapping.RoomTypeId,
                    HotelId = ratePlan.HotelId
                })
            .ToDictionaryAsync(x => x.RatePlanRoomTypeId, ct);
    }

    private static AdminPromoRateOverrideListItemDto MapListItem(
        PromoRateOverride entity,
        IReadOnlyDictionary<Guid, PromoRateOverrideContext> contexts)
    {
        contexts.TryGetValue(entity.RatePlanRoomTypeId, out var context);

        return new AdminPromoRateOverrideListItemDto
        {
            Id = entity.Id,
            HotelId = context?.HotelId,
            RoomTypeId = context?.RoomTypeId,
            RatePlanId = context?.RatePlanId,
            RatePlanRoomTypeId = entity.RatePlanRoomTypeId,
            PromoCodeId = entity.PromoCodeId,
            Code = entity.PromoCode,
            PromoCode = entity.PromoCode,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            OverridePrice = entity.OverridePrice,
            DiscountPercent = entity.DiscountPercent,
            CurrencyCode = entity.CurrencyCode,
            ConditionsJson = entity.ConditionsJson,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private static AdminPromoRateOverrideDetailDto MapDetail(
        PromoRateOverride entity,
        IReadOnlyDictionary<Guid, PromoRateOverrideContext> contexts)
    {
        contexts.TryGetValue(entity.RatePlanRoomTypeId, out var context);

        return new AdminPromoRateOverrideDetailDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            HotelId = context?.HotelId,
            RoomTypeId = context?.RoomTypeId,
            RatePlanId = context?.RatePlanId,
            RatePlanRoomTypeId = entity.RatePlanRoomTypeId,
            PromoCodeId = entity.PromoCodeId,
            Code = entity.PromoCode,
            PromoCode = entity.PromoCode,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            OverridePrice = entity.OverridePrice,
            DiscountPercent = entity.DiscountPercent,
            CurrencyCode = entity.CurrencyCode,
            ConditionsJson = entity.ConditionsJson,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId,
            RowVersionBase64 = entity.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
        };
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

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? FirstNonBlank(string? primary, string? fallback)
        => NullIfWhiteSpace(primary) ?? NullIfWhiteSpace(fallback);

    private sealed class PromoRateOverrideContext
    {
        public Guid RatePlanRoomTypeId { get; set; }
        public Guid RatePlanId { get; set; }
        public Guid RoomTypeId { get; set; }
        public Guid HotelId { get; set; }
    }
}

public sealed class AdminPromoRateOverridePagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminPromoRateOverrideListItemDto> Items { get; set; } = new();
}

public sealed class AdminPromoRateOverrideListItemDto
{
    public Guid Id { get; set; }
    public Guid? HotelId { get; set; }
    public Guid? RoomTypeId { get; set; }
    public Guid? RatePlanId { get; set; }
    public Guid RatePlanRoomTypeId { get; set; }
    public Guid? PromoCodeId { get; set; }
    public string? Code { get; set; }
    public string? PromoCode { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal? OverridePrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public string? ConditionsJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AdminPromoRateOverrideDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? HotelId { get; set; }
    public Guid? RoomTypeId { get; set; }
    public Guid? RatePlanId { get; set; }
    public Guid RatePlanRoomTypeId { get; set; }
    public Guid? PromoCodeId { get; set; }
    public string? Code { get; set; }
    public string? PromoCode { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public decimal? OverridePrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public string? ConditionsJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminCreatePromoRateOverrideRequest
{
    public Guid? RatePlanRoomTypeId { get; set; }
    public Guid? PromoCodeId { get; set; }
    public string? Code { get; set; }
    public string? PromoCode { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal? OverridePrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public string? CurrencyCode { get; set; }
    public string? ConditionsJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AdminUpdatePromoRateOverrideRequest
{
    public Guid? RatePlanRoomTypeId { get; set; }
    public Guid? PromoCodeId { get; set; }
    public string? Code { get; set; }
    public string? PromoCode { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal? OverridePrice { get; set; }
    public decimal? DiscountPercent { get; set; }
    public string? CurrencyCode { get; set; }
    public string? ConditionsJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminCreatePromoRateOverrideResponse
{
    public Guid Id { get; set; }
}
