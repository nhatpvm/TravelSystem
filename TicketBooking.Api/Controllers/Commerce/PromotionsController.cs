using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Commerce;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Commerce;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/promotions")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminPromotionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminPromotionsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PromotionCampaignPagedResponse>> List(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] PromotionOwnerScope? ownerScope = null,
        [FromQuery] PromotionProductScope? productScope = null,
        [FromQuery] PromotionStatus? status = null,
        [FromQuery] string? q = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        IQueryable<PromotionCampaign> query = includeDeleted
            ? _db.PromotionCampaigns.IgnoreQueryFilters()
            : _db.PromotionCampaigns;

        if (tenantId.HasValue)
            query = query.Where(x => x.TenantId == tenantId.Value);

        if (ownerScope.HasValue)
            query = query.Where(x => x.OwnerScope == ownerScope.Value);

        if (productScope.HasValue && productScope.Value != PromotionProductScope.None)
            query = query.Where(x => (x.ProductScope & productScope.Value) != 0);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(keyword) ||
                x.Name.Contains(keyword) ||
                (x.Description != null && x.Description.Contains(keyword)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var tenantNames = await PromotionMap.LoadTenantNamesAsync(_db, rows.Select(x => x.TenantId), ct);

        return Ok(new PromotionCampaignPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = rows.Select(x => PromotionMap.ToDto(x, tenantNames)).ToList()
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PromotionCampaignDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        IQueryable<PromotionCampaign> query = includeDeleted
            ? _db.PromotionCampaigns.IgnoreQueryFilters()
            : _db.PromotionCampaigns;

        var entity = await query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Promotion campaign not found." });

        var tenantNames = await PromotionMap.LoadTenantNamesAsync(_db, new[] { entity.TenantId }, ct);
        return Ok(PromotionMap.ToDto(entity, tenantNames));
    }

    [HttpPost]
    public async Task<ActionResult<PromotionCreateResponse>> Create(
        [FromBody] PromotionCampaignCreateRequest req,
        CancellationToken ct = default)
    {
        PromotionCampaign entity;
        try
        {
            entity = await PromotionWrite.CreateEntityAsync(_db, req, GetCurrentUserId(), adminMode: true, tenantId: req.TenantId, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        _db.PromotionCampaigns.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new PromotionCreateResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] PromotionCampaignUpdateRequest req,
        CancellationToken ct = default)
    {
        var entity = await _db.PromotionCampaigns.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Promotion campaign not found." });

        var validation = await PromotionWrite.ApplyUpdateAsync(_db, entity, req, GetCurrentUserId(), adminMode: true, forceTenantId: null, ct);
        if (validation is not null)
            return validation;

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Promotion campaign was changed by another user. Please reload and try again." });
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
        => await ToggleStatus(id, PromotionStatus.Active, ct);

    [HttpPost("{id:guid}/pause")]
    public async Task<IActionResult> Pause(Guid id, CancellationToken ct = default)
        => await ToggleStatus(id, PromotionStatus.Paused, ct);

    [HttpGet("{id:guid}/redemptions")]
    public async Task<ActionResult<PromotionRedemptionListResponse>> ListRedemptions(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _db.PromotionRedemptions.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.PromotionCampaignId == id && !x.IsDeleted);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.RedeemedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PromotionRedemptionDto
            {
                Id = x.Id,
                PromotionCampaignId = x.PromotionCampaignId,
                TenantId = x.TenantId,
                UserId = x.UserId,
                OrderId = x.OrderId,
                ProductType = x.ProductType,
                Status = x.Status,
                PromotionCode = x.PromotionCode,
                CurrencyCode = x.CurrencyCode,
                OrderAmount = x.OrderAmount,
                DiscountAmount = x.DiscountAmount,
                PayableAmount = x.PayableAmount,
                RedeemedAt = x.RedeemedAt,
                CancelledAt = x.CancelledAt
            })
            .ToListAsync(ct);

        return Ok(new PromotionRedemptionListResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    private async Task<IActionResult> ToggleDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        var entity = await _db.PromotionCampaigns.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Promotion campaign not found." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();
        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleStatus(Guid id, PromotionStatus status, CancellationToken ct)
    {
        var entity = await _db.PromotionCampaigns.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Promotion campaign not found." });

        entity.Status = status;
        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();
        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenant/promotions")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX},{RoleNames.QLVT},{RoleNames.QLVMM},{RoleNames.QLKS},{RoleNames.QLTour}")]
public sealed class TenantPromotionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public TenantPromotionsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<PromotionCampaignPagedResponse>> List(
        [FromQuery] PromotionStatus? status = null,
        [FromQuery] string? q = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        IQueryable<PromotionCampaign> query = includeDeleted
            ? _db.PromotionCampaigns.IgnoreQueryFilters()
            : _db.PromotionCampaigns;

        query = query.Where(x => x.TenantId == tenantId);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(keyword) ||
                x.Name.Contains(keyword) ||
                (x.Description != null && x.Description.Contains(keyword)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var tenantNames = await PromotionMap.LoadTenantNamesAsync(_db, new[] { (Guid?)tenantId }, ct);
        return Ok(new PromotionCampaignPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = rows.Select(x => PromotionMap.ToDto(x, tenantNames)).ToList()
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PromotionCampaignDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        var entity = await _db.PromotionCampaigns.IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Promotion campaign not found in current tenant." });

        var tenantNames = await PromotionMap.LoadTenantNamesAsync(_db, new[] { (Guid?)tenantId }, ct);
        return Ok(PromotionMap.ToDto(entity, tenantNames));
    }

    [HttpPost]
    public async Task<ActionResult<PromotionCreateResponse>> Create(
        [FromBody] PromotionCampaignCreateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        PromotionCampaign entity;
        try
        {
            await EnsureTenantProductScopeAsync(tenantId, req.ProductScope, ct);
            entity = await PromotionWrite.CreateEntityAsync(_db, req, GetCurrentUserId(), adminMode: false, tenantId, ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        _db.PromotionCampaigns.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new PromotionCreateResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] PromotionCampaignUpdateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        if (req.ProductScope.HasValue)
            await EnsureTenantProductScopeAsync(tenantId, req.ProductScope, ct);

        var entity = await _db.PromotionCampaigns.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Promotion campaign not found in current tenant." });

        var validation = await PromotionWrite.ApplyUpdateAsync(_db, entity, req, GetCurrentUserId(), adminMode: false, forceTenantId: tenantId, ct);
        if (validation is not null)
            return validation;

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Promotion campaign was changed by another user. Please reload and try again." });
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
        => await ToggleStatus(id, PromotionStatus.Active, ct);

    [HttpPost("{id:guid}/pause")]
    public async Task<IActionResult> Pause(Guid id, CancellationToken ct = default)
        => await ToggleStatus(id, PromotionStatus.Paused, ct);

    private async Task<IActionResult> ToggleDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        var entity = await _db.PromotionCampaigns.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Promotion campaign not found in current tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();
        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleStatus(Guid id, PromotionStatus status, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        var entity = await _db.PromotionCampaigns.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Promotion campaign not found in current tenant." });

        entity.Status = status;
        entity.IsDeleted = false;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();
        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private Guid RequireTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Tenant promotion operations require X-TenantId.");

        return _tenant.TenantId.Value;
    }

    private async Task EnsureTenantProductScopeAsync(Guid tenantId, PromotionProductScope? requestedScope, CancellationToken ct)
    {
        var tenantType = await _db.Tenants.IgnoreQueryFilters()
            .Where(x => x.Id == tenantId && !x.IsDeleted)
            .Select(x => x.Type)
            .FirstOrDefaultAsync(ct);

        var allowedScope = PromotionWrite.ProductScopeForTenantType(tenantType);
        var scope = requestedScope.GetValueOrDefault(allowedScope);

        if ((scope & ~allowedScope) != 0)
            throw new InvalidOperationException("Tenant promotions can only target the product module owned by the tenant.");
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/promotions")]
public sealed class PromotionsPublicController : ControllerBase
{
    private readonly AppDbContext _db;

    public PromotionsPublicController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("quote")]
    [AllowAnonymous]
    public async Task<ActionResult<PromotionQuoteResponse>> Quote(
        [FromBody] PromotionQuoteRequest req,
        CancellationToken ct = default)
    {
        var validation = PromotionWrite.ValidateQuote(req);
        if (validation is not null)
            return validation;

        var now = DateTimeOffset.Now;
        var code = req.Code!.Trim().ToUpperInvariant();
        var productScope = PromotionWrite.ProductScopeForProductType(req.ProductType!.Value);

        var campaigns = await _db.PromotionCampaigns.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsPublic &&
                x.RequiresCode &&
                x.Code == code &&
                x.Status == PromotionStatus.Active &&
                x.StartsAt <= now &&
                (!x.EndsAt.HasValue || x.EndsAt.Value >= now) &&
                (x.ProductScope & productScope) != 0 &&
                (!x.TenantId.HasValue || (req.TenantId.HasValue && x.TenantId == req.TenantId.Value)))
            .OrderByDescending(x => x.TenantId.HasValue)
            .ThenByDescending(x => x.DiscountValue)
            .ToListAsync(ct);

        var campaign = campaigns.FirstOrDefault();
        if (campaign is null)
            return NotFound(new { message = "Promotion code is not available for this booking." });

        var limitError = await PromotionWrite.CheckUsageLimitsAsync(_db, campaign, req.TenantId, req.UserId, ct);
        if (limitError is not null)
            return limitError;

        if (!string.Equals(campaign.CurrencyCode, req.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "Promotion currency does not match order currency." });

        if (campaign.MinOrderAmount.HasValue && req.OrderAmount!.Value < campaign.MinOrderAmount.Value)
            return BadRequest(new { message = "Order amount does not meet promotion minimum amount." });

        var discountAmount = PromotionWrite.CalculateDiscount(campaign, req.OrderAmount!.Value);
        if (discountAmount <= 0)
            return BadRequest(new { message = "Promotion does not produce a discount for this order." });

        return Ok(new PromotionQuoteResponse
        {
            PromotionCampaignId = campaign.Id,
            Code = campaign.Code,
            Name = campaign.Name,
            CurrencyCode = campaign.CurrencyCode,
            OrderAmount = req.OrderAmount.Value,
            DiscountAmount = discountAmount,
            PayableAmount = Math.Max(0, req.OrderAmount.Value - discountAmount)
        });
    }
}

public class PromotionCampaignCreateRequest
{
    public Guid? TenantId { get; set; }
    public PromotionOwnerScope? OwnerScope { get; set; }
    public PromotionProductScope? ProductScope { get; set; }
    public PromotionStatus? Status { get; set; }
    public PromotionDiscountType? DiscountType { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public DateTimeOffset? StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public int? GlobalUsageLimit { get; set; }
    public int? PerUserUsageLimit { get; set; }
    public int? PerTenantUsageLimit { get; set; }
    public decimal? BudgetAmount { get; set; }
    public bool? RequiresCode { get; set; }
    public bool? IsPublic { get; set; }
    public string? RulesJson { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class PromotionCampaignUpdateRequest : PromotionCampaignCreateRequest
{
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class PromotionCreateResponse
{
    public Guid Id { get; set; }
}

public sealed class PromotionCampaignPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<PromotionCampaignDto> Items { get; set; } = new();
}

public sealed class PromotionCampaignDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantCode { get; set; }
    public string? TenantName { get; set; }
    public PromotionOwnerScope OwnerScope { get; set; }
    public PromotionProductScope ProductScope { get; set; }
    public PromotionStatus Status { get; set; }
    public PromotionDiscountType DiscountType { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal DiscountValue { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public int? GlobalUsageLimit { get; set; }
    public int? PerUserUsageLimit { get; set; }
    public int? PerTenantUsageLimit { get; set; }
    public decimal? BudgetAmount { get; set; }
    public int RedemptionCount { get; set; }
    public decimal DiscountGrantedAmount { get; set; }
    public decimal RevenueAttributedAmount { get; set; }
    public bool RequiresCode { get; set; }
    public bool IsPublic { get; set; }
    public string? RulesJson { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class PromotionQuoteRequest
{
    public string? Code { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public CustomerProductType? ProductType { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? OrderAmount { get; set; }
}

public sealed class PromotionQuoteResponse
{
    public Guid PromotionCampaignId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string CurrencyCode { get; set; } = "VND";
    public decimal OrderAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PayableAmount { get; set; }
}

public sealed class PromotionRedemptionListResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<PromotionRedemptionDto> Items { get; set; } = new();
}

public sealed class PromotionRedemptionDto
{
    public Guid Id { get; set; }
    public Guid PromotionCampaignId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? OrderId { get; set; }
    public CustomerProductType ProductType { get; set; }
    public PromotionRedemptionStatus Status { get; set; }
    public string PromotionCode { get; set; } = "";
    public string CurrencyCode { get; set; } = "VND";
    public decimal OrderAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PayableAmount { get; set; }
    public DateTimeOffset RedeemedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
}

internal static class PromotionMap
{
    public static async Task<Dictionary<Guid, (string Code, string Name)>> LoadTenantNamesAsync(
        AppDbContext db,
        IEnumerable<Guid?> tenantIds,
        CancellationToken ct)
    {
        var ids = tenantIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        if (ids.Count == 0)
            return new();

        return await db.Tenants.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => (x.Code, x.Name), ct);
    }

    public static PromotionCampaignDto ToDto(
        PromotionCampaign entity,
        IReadOnlyDictionary<Guid, (string Code, string Name)> tenantNames)
    {
        (string Code, string Name)? tenant = null;
        if (entity.TenantId.HasValue && tenantNames.TryGetValue(entity.TenantId.Value, out var tenantInfo))
            tenant = tenantInfo;

        return new PromotionCampaignDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            TenantCode = tenant?.Code,
            TenantName = tenant?.Name,
            OwnerScope = entity.OwnerScope,
            ProductScope = entity.ProductScope,
            Status = entity.Status,
            DiscountType = entity.DiscountType,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            CurrencyCode = entity.CurrencyCode,
            DiscountValue = entity.DiscountValue,
            MaxDiscountAmount = entity.MaxDiscountAmount,
            MinOrderAmount = entity.MinOrderAmount,
            StartsAt = entity.StartsAt,
            EndsAt = entity.EndsAt,
            GlobalUsageLimit = entity.GlobalUsageLimit,
            PerUserUsageLimit = entity.PerUserUsageLimit,
            PerTenantUsageLimit = entity.PerTenantUsageLimit,
            BudgetAmount = entity.BudgetAmount,
            RedemptionCount = entity.RedemptionCount,
            DiscountGrantedAmount = entity.DiscountGrantedAmount,
            RevenueAttributedAmount = entity.RevenueAttributedAmount,
            RequiresCode = entity.RequiresCode,
            IsPublic = entity.IsPublic,
            RulesJson = entity.RulesJson,
            MetadataJson = entity.MetadataJson,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            RowVersionBase64 = entity.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
        };
    }
}

internal static class PromotionWrite
{
    public static async Task<PromotionCampaign> CreateEntityAsync(
        AppDbContext db,
        PromotionCampaignCreateRequest req,
        Guid? userId,
        bool adminMode,
        Guid? tenantId,
        CancellationToken ct)
    {
        ValidateCreate(req);

        var code = NormalizeCode(req.Code);
        var ownerScope = adminMode
            ? req.OwnerScope ?? (tenantId.HasValue ? PromotionOwnerScope.Tenant : PromotionOwnerScope.Platform)
            : PromotionOwnerScope.Tenant;

        if (ownerScope == PromotionOwnerScope.Platform)
            tenantId = null;
        else if (!tenantId.HasValue)
            throw new InvalidOperationException("Tenant promotion requires TenantId.");

        if (tenantId.HasValue)
            await EnsureTenantExistsAsync(db, tenantId.Value, ct);

        await EnsureCodeUniqueAsync(db, tenantId, code, exceptId: null, ct);

        var now = DateTimeOffset.Now;
        var productScope = req.ProductScope ?? (tenantId.HasValue
            ? await ProductScopeForTenantAsync(db, tenantId.Value, ct)
            : PromotionProductScope.All);

        return new PromotionCampaign
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OwnerScope = ownerScope,
            ProductScope = productScope == PromotionProductScope.None ? PromotionProductScope.All : productScope,
            Status = req.Status ?? PromotionStatus.Active,
            DiscountType = req.DiscountType ?? PromotionDiscountType.Percent,
            Code = code,
            Name = req.Name!.Trim(),
            Description = TrimOrNull(req.Description, 2000),
            CurrencyCode = NormalizeCurrency(req.CurrencyCode),
            DiscountValue = req.DiscountValue!.Value,
            MaxDiscountAmount = req.MaxDiscountAmount,
            MinOrderAmount = req.MinOrderAmount,
            StartsAt = req.StartsAt ?? now,
            EndsAt = req.EndsAt,
            GlobalUsageLimit = req.GlobalUsageLimit,
            PerUserUsageLimit = req.PerUserUsageLimit,
            PerTenantUsageLimit = req.PerTenantUsageLimit,
            BudgetAmount = req.BudgetAmount,
            RequiresCode = req.RequiresCode ?? true,
            IsPublic = req.IsPublic ?? true,
            RulesJson = TrimOrNull(req.RulesJson, int.MaxValue),
            MetadataJson = TrimOrNull(req.MetadataJson, int.MaxValue),
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };
    }

    public static async Task<IActionResult?> ApplyUpdateAsync(
        AppDbContext db,
        PromotionCampaign entity,
        PromotionCampaignUpdateRequest req,
        Guid? userId,
        bool adminMode,
        Guid? forceTenantId,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(req.RowVersionBase64))
        {
            try
            {
                db.Entry(entity).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(req.RowVersionBase64);
            }
            catch
            {
                return new BadRequestObjectResult(new { message = "RowVersionBase64 is invalid." });
            }
        }

        var nextTenantId = forceTenantId ?? entity.TenantId;
        var nextOwnerScope = entity.OwnerScope;

        if (adminMode && req.OwnerScope.HasValue)
            nextOwnerScope = req.OwnerScope.Value;

        if (adminMode && req.TenantId.HasValue)
            nextTenantId = req.TenantId.Value;

        if (!adminMode)
        {
            nextOwnerScope = PromotionOwnerScope.Tenant;
            nextTenantId = forceTenantId;
        }

        if (nextOwnerScope == PromotionOwnerScope.Platform)
            nextTenantId = null;

        if (nextOwnerScope == PromotionOwnerScope.Tenant && !nextTenantId.HasValue)
            return new BadRequestObjectResult(new { message = "Tenant promotion requires TenantId." });

        if (nextTenantId.HasValue)
            await EnsureTenantExistsAsync(db, nextTenantId.Value, ct);

        var nextCode = string.IsNullOrWhiteSpace(req.Code) ? entity.Code : NormalizeCode(req.Code);
        var duplicate = await db.PromotionCampaigns.IgnoreQueryFilters()
            .AnyAsync(x => x.Id != entity.Id && x.TenantId == nextTenantId && x.Code == nextCode, ct);

        if (duplicate)
            return new ConflictObjectResult(new { message = "Promotion code already exists in this scope." });

        var nextDiscountType = req.DiscountType ?? entity.DiscountType;
        var nextDiscountValue = req.DiscountValue ?? entity.DiscountValue;
        var validation = ValidateValues(
            nextCode,
            req.Name ?? entity.Name,
            nextDiscountType,
            nextDiscountValue,
            req.StartsAt ?? entity.StartsAt,
            req.EndsAt ?? entity.EndsAt,
            req.MaxDiscountAmount ?? entity.MaxDiscountAmount,
            req.MinOrderAmount ?? entity.MinOrderAmount,
            req.GlobalUsageLimit ?? entity.GlobalUsageLimit,
            req.PerUserUsageLimit ?? entity.PerUserUsageLimit,
            req.PerTenantUsageLimit ?? entity.PerTenantUsageLimit,
            req.BudgetAmount ?? entity.BudgetAmount);

        if (validation is not null)
            return validation;

        entity.TenantId = nextTenantId;
        entity.OwnerScope = nextOwnerScope;
        if (req.ProductScope.HasValue) entity.ProductScope = req.ProductScope.Value == PromotionProductScope.None ? PromotionProductScope.All : req.ProductScope.Value;
        if (req.Status.HasValue) entity.Status = req.Status.Value;
        if (req.DiscountType.HasValue) entity.DiscountType = req.DiscountType.Value;
        if (!string.IsNullOrWhiteSpace(req.Code)) entity.Code = nextCode;
        if (req.Name is not null) entity.Name = req.Name.Trim();
        if (req.Description is not null) entity.Description = TrimOrNull(req.Description, 2000);
        if (req.CurrencyCode is not null) entity.CurrencyCode = NormalizeCurrency(req.CurrencyCode);
        if (req.DiscountValue.HasValue) entity.DiscountValue = req.DiscountValue.Value;
        if (req.MaxDiscountAmount.HasValue) entity.MaxDiscountAmount = req.MaxDiscountAmount;
        if (req.MinOrderAmount.HasValue) entity.MinOrderAmount = req.MinOrderAmount;
        if (req.StartsAt.HasValue) entity.StartsAt = req.StartsAt.Value;
        if (req.EndsAt.HasValue) entity.EndsAt = req.EndsAt;
        if (req.GlobalUsageLimit.HasValue) entity.GlobalUsageLimit = req.GlobalUsageLimit;
        if (req.PerUserUsageLimit.HasValue) entity.PerUserUsageLimit = req.PerUserUsageLimit;
        if (req.PerTenantUsageLimit.HasValue) entity.PerTenantUsageLimit = req.PerTenantUsageLimit;
        if (req.BudgetAmount.HasValue) entity.BudgetAmount = req.BudgetAmount;
        if (req.RequiresCode.HasValue) entity.RequiresCode = req.RequiresCode.Value;
        if (req.IsPublic.HasValue) entity.IsPublic = req.IsPublic.Value;
        if (req.RulesJson is not null) entity.RulesJson = TrimOrNull(req.RulesJson, int.MaxValue);
        if (req.MetadataJson is not null) entity.MetadataJson = TrimOrNull(req.MetadataJson, int.MaxValue);
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;
        return null;
    }

    public static ActionResult? ValidateQuote(PromotionQuoteRequest req)
    {
        if (req is null)
            return new BadRequestObjectResult(new { message = "Request body is required." });

        if (string.IsNullOrWhiteSpace(req.Code))
            return new BadRequestObjectResult(new { message = "Promotion code is required." });

        if (!req.ProductType.HasValue)
            return new BadRequestObjectResult(new { message = "ProductType is required." });

        if (string.IsNullOrWhiteSpace(req.CurrencyCode))
            return new BadRequestObjectResult(new { message = "CurrencyCode is required." });

        if (!req.OrderAmount.HasValue || req.OrderAmount.Value <= 0)
            return new BadRequestObjectResult(new { message = "OrderAmount must be greater than 0." });

        return null;
    }

    public static async Task<ActionResult?> CheckUsageLimitsAsync(
        AppDbContext db,
        PromotionCampaign campaign,
        Guid? tenantId,
        Guid? userId,
        CancellationToken ct)
    {
        if (campaign.GlobalUsageLimit.HasValue && campaign.RedemptionCount >= campaign.GlobalUsageLimit.Value)
            return new BadRequestObjectResult(new { message = "Promotion global usage limit has been reached." });

        if (campaign.BudgetAmount.HasValue && campaign.DiscountGrantedAmount >= campaign.BudgetAmount.Value)
            return new BadRequestObjectResult(new { message = "Promotion budget has been reached." });

        if (campaign.PerTenantUsageLimit.HasValue && tenantId.HasValue)
        {
            var tenantCount = await db.PromotionRedemptions.IgnoreQueryFilters()
                .CountAsync(x =>
                    x.PromotionCampaignId == campaign.Id &&
                    x.TenantId == tenantId.Value &&
                    !x.IsDeleted &&
                    x.Status != PromotionRedemptionStatus.Cancelled,
                    ct);

            if (tenantCount >= campaign.PerTenantUsageLimit.Value)
                return new BadRequestObjectResult(new { message = "Promotion tenant usage limit has been reached." });
        }

        if (campaign.PerUserUsageLimit.HasValue && userId.HasValue)
        {
            var userCount = await db.PromotionRedemptions.IgnoreQueryFilters()
                .CountAsync(x =>
                    x.PromotionCampaignId == campaign.Id &&
                    x.UserId == userId.Value &&
                    !x.IsDeleted &&
                    x.Status != PromotionRedemptionStatus.Cancelled,
                    ct);

            if (userCount >= campaign.PerUserUsageLimit.Value)
                return new BadRequestObjectResult(new { message = "Promotion user usage limit has been reached." });
        }

        return null;
    }

    public static decimal CalculateDiscount(PromotionCampaign campaign, decimal orderAmount)
    {
        var raw = campaign.DiscountType == PromotionDiscountType.Percent
            ? Math.Round(orderAmount * campaign.DiscountValue / 100m, 2)
            : campaign.DiscountValue;

        if (campaign.MaxDiscountAmount.HasValue)
            raw = Math.Min(raw, campaign.MaxDiscountAmount.Value);

        return Math.Clamp(raw, 0m, orderAmount);
    }

    public static PromotionProductScope ProductScopeForProductType(CustomerProductType productType)
        => productType switch
        {
            CustomerProductType.Bus => PromotionProductScope.Bus,
            CustomerProductType.Train => PromotionProductScope.Train,
            CustomerProductType.Flight => PromotionProductScope.Flight,
            CustomerProductType.Hotel => PromotionProductScope.Hotel,
            CustomerProductType.Tour => PromotionProductScope.Tour,
            _ => PromotionProductScope.None
        };

    public static PromotionProductScope ProductScopeForTenantType(TenantType tenantType)
        => tenantType switch
        {
            TenantType.Bus => PromotionProductScope.Bus,
            TenantType.Train => PromotionProductScope.Train,
            TenantType.Flight => PromotionProductScope.Flight,
            TenantType.Hotel => PromotionProductScope.Hotel,
            TenantType.Tour => PromotionProductScope.Tour,
            _ => PromotionProductScope.All
        };

    private static async Task<PromotionProductScope> ProductScopeForTenantAsync(AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var tenantType = await db.Tenants.IgnoreQueryFilters()
            .Where(x => x.Id == tenantId && !x.IsDeleted)
            .Select(x => x.Type)
            .FirstAsync(ct);

        return ProductScopeForTenantType(tenantType);
    }

    private static void ValidateCreate(PromotionCampaignCreateRequest req)
    {
        if (req is null)
            throw new ArgumentNullException(nameof(req));

        var validation = ValidateValues(
            NormalizeCode(req.Code),
            req.Name,
            req.DiscountType ?? PromotionDiscountType.Percent,
            req.DiscountValue,
            req.StartsAt ?? DateTimeOffset.Now,
            req.EndsAt,
            req.MaxDiscountAmount,
            req.MinOrderAmount,
            req.GlobalUsageLimit,
            req.PerUserUsageLimit,
            req.PerTenantUsageLimit,
            req.BudgetAmount);

        if (validation is not null)
            throw new InvalidOperationException("Promotion campaign payload is invalid.");
    }

    private static IActionResult? ValidateValues(
        string? code,
        string? name,
        PromotionDiscountType discountType,
        decimal? discountValue,
        DateTimeOffset startsAt,
        DateTimeOffset? endsAt,
        decimal? maxDiscountAmount,
        decimal? minOrderAmount,
        int? globalUsageLimit,
        int? perUserUsageLimit,
        int? perTenantUsageLimit,
        decimal? budgetAmount)
    {
        if (string.IsNullOrWhiteSpace(code))
            return new BadRequestObjectResult(new { message = "Code is required." });

        if (code.Trim().Length > 64)
            return new BadRequestObjectResult(new { message = "Code max length is 64." });

        if (string.IsNullOrWhiteSpace(name))
            return new BadRequestObjectResult(new { message = "Name is required." });

        if (name.Trim().Length > 200)
            return new BadRequestObjectResult(new { message = "Name max length is 200." });

        if (!discountValue.HasValue || discountValue.Value <= 0)
            return new BadRequestObjectResult(new { message = "DiscountValue must be greater than 0." });

        if (discountType == PromotionDiscountType.Percent && discountValue.Value > 100)
            return new BadRequestObjectResult(new { message = "Percent discount cannot exceed 100." });

        if (endsAt.HasValue && endsAt.Value < startsAt)
            return new BadRequestObjectResult(new { message = "EndsAt must be greater than or equal to StartsAt." });

        if (maxDiscountAmount.HasValue && maxDiscountAmount.Value < 0)
            return new BadRequestObjectResult(new { message = "MaxDiscountAmount must be >= 0." });

        if (minOrderAmount.HasValue && minOrderAmount.Value < 0)
            return new BadRequestObjectResult(new { message = "MinOrderAmount must be >= 0." });

        if (budgetAmount.HasValue && budgetAmount.Value < 0)
            return new BadRequestObjectResult(new { message = "BudgetAmount must be >= 0." });

        if (globalUsageLimit.HasValue && globalUsageLimit.Value < 0)
            return new BadRequestObjectResult(new { message = "GlobalUsageLimit must be >= 0." });

        if (perUserUsageLimit.HasValue && perUserUsageLimit.Value < 0)
            return new BadRequestObjectResult(new { message = "PerUserUsageLimit must be >= 0." });

        if (perTenantUsageLimit.HasValue && perTenantUsageLimit.Value < 0)
            return new BadRequestObjectResult(new { message = "PerTenantUsageLimit must be >= 0." });

        return null;
    }

    private static async Task EnsureTenantExistsAsync(AppDbContext db, Guid tenantId, CancellationToken ct)
    {
        var exists = await db.Tenants.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == tenantId && !x.IsDeleted, ct);

        if (!exists)
            throw new InvalidOperationException("TenantId does not exist.");
    }

    private static async Task EnsureCodeUniqueAsync(AppDbContext db, Guid? tenantId, string code, Guid? exceptId, CancellationToken ct)
    {
        var exists = await db.PromotionCampaigns.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code && (!exceptId.HasValue || x.Id != exceptId.Value), ct);

        if (exists)
            throw new InvalidOperationException("Promotion code already exists in this scope.");
    }

    private static string NormalizeCode(string? value)
        => string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToUpperInvariant();

    private static string NormalizeCurrency(string? value)
        => string.IsNullOrWhiteSpace(value) ? "VND" : value.Trim().ToUpperInvariant();

    private static string? TrimOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
