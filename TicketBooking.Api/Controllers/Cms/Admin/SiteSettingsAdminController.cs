// FILE: TicketBooking.Api/Controllers/SiteSettingsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Cms;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

/// <summary>
/// Phase 7 (CMS/SEO) - Admin CRUD for cms.SiteSettings
/// - Admin only
/// - Multi-tenant write: requires X-TenantId (handled by TenantContextMiddleware)
/// - One row per tenant (unique index on TenantId)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/cms/site-settings")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class SiteSettingsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<SiteSettingsAdminController> _logger;

    public SiteSettingsAdminController(AppDbContext db, ITenantContext tenant, ILogger<SiteSettingsAdminController> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    // GET:
    // - If X-TenantId present => return single settings of that tenant
    // - If no X-TenantId => return list across tenants (Admin overview)
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = _db.Set<SiteSetting>().AsNoTracking();

        if (includeDeleted)
            q = q.IgnoreQueryFilters();

        if (_tenant.HasTenant && _tenant.TenantId.HasValue)
        {
            var tenantId = _tenant.TenantId.Value;

            var one = await q.FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
            if (one is null)
                return NotFound(new { message = "Site settings not found for this tenant." });

            return Ok(ToDto(one));
        }

        // Admin overview (all tenants)
        var items = await q
            .OrderBy(x => x.TenantId)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.SiteName,
                x.SiteUrl,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    // PUT: upsert (create or update) for current tenant
    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] UpsertRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        if (string.IsNullOrWhiteSpace(req.SiteName))
            return BadRequest(new { message = "SiteName is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<SiteSetting>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        if (entity is null)
        {
            entity = new SiteSetting
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                SiteName = req.SiteName.Trim(),
                SiteUrl = NullIfWhite(req.SiteUrl),
                DefaultRobots = NullIfWhite(req.DefaultRobots),
                DefaultOgImageUrl = NullIfWhite(req.DefaultOgImageUrl),
                DefaultTwitterCard = NullIfWhite(req.DefaultTwitterCard) ?? "summary_large_image",
                DefaultTwitterSite = NullIfWhite(req.DefaultTwitterSite),
                DefaultSchemaJsonLd = NullIfWhite(req.DefaultSchemaJsonLd),

                BrandLogoUrl = NullIfWhite(req.BrandLogoUrl),
                SupportEmail = NullIfWhite(req.SupportEmail),
                SupportPhone = NullIfWhite(req.SupportPhone),

                IsActive = req.IsActive ?? true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId
            };

            _db.Add(entity);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Created SiteSetting for tenant {TenantId}.", tenantId);
            return Ok(new { ok = true, id = entity.Id, created = true });
        }

        // update
        entity.SiteName = req.SiteName.Trim();
        entity.SiteUrl = NullIfWhite(req.SiteUrl);
        entity.DefaultRobots = NullIfWhite(req.DefaultRobots);
        entity.DefaultOgImageUrl = NullIfWhite(req.DefaultOgImageUrl);
        entity.DefaultTwitterCard = NullIfWhite(req.DefaultTwitterCard) ?? entity.DefaultTwitterCard ?? "summary_large_image";
        entity.DefaultTwitterSite = NullIfWhite(req.DefaultTwitterSite);
        entity.DefaultSchemaJsonLd = NullIfWhite(req.DefaultSchemaJsonLd);

        entity.BrandLogoUrl = NullIfWhite(req.BrandLogoUrl);
        entity.SupportEmail = NullIfWhite(req.SupportEmail);
        entity.SupportPhone = NullIfWhite(req.SupportPhone);

        if (req.IsActive.HasValue)
            entity.IsActive = req.IsActive.Value;

        if (entity.IsDeleted)
            entity.IsDeleted = false;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Updated SiteSetting for tenant {TenantId}.", tenantId);
        return Ok(new { ok = true, id = entity.Id, created = false });
    }

    // POST: set active flag (current tenant)
    [HttpPost("set-active")]
    public async Task<IActionResult> SetActive([FromBody] SetActiveRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<SiteSetting>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Site settings not found for this tenant." });

        entity.IsActive = req.IsActive;
        if (entity.IsDeleted) entity.IsDeleted = false;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    // DELETE: soft-delete current tenant settings
    [HttpDelete]
    public async Task<IActionResult> SoftDelete(CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<SiteSetting>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Site settings not found for this tenant." });

        if (!entity.IsDeleted)
        {
            entity.IsDeleted = true;
            entity.IsActive = false;
            entity.UpdatedAt = now;
            entity.UpdatedByUserId = userId;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    // POST: restore current tenant settings
    [HttpPost("restore")]
    public async Task<IActionResult> Restore(CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<SiteSetting>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Site settings not found for this tenant." });

        if (entity.IsDeleted)
        {
            entity.IsDeleted = false;
            entity.IsActive = true;
            entity.UpdatedAt = now;
            entity.UpdatedByUserId = userId;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    // -------------------------
    // Helpers
    // -------------------------

    private IActionResult RequireTenant()
        => BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirst("sub")?.Value ?? User.FindFirst("uid")?.Value;
        if (Guid.TryParse(raw, out var id)) return id;
        return null;
    }

    private static string? NullIfWhite(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static object ToDto(SiteSetting x) => new
    {
        x.Id,
        x.TenantId,
        x.SiteName,
        x.SiteUrl,
        x.DefaultRobots,
        x.DefaultOgImageUrl,
        x.DefaultTwitterCard,
        x.DefaultTwitterSite,
        x.DefaultSchemaJsonLd,
        x.BrandLogoUrl,
        x.SupportEmail,
        x.SupportPhone,
        x.IsActive,
        x.IsDeleted,
        x.CreatedAt,
        x.CreatedByUserId,
        x.UpdatedAt,
        x.UpdatedByUserId
    };

    // -------------------------
    // DTOs
    // -------------------------

    public sealed class UpsertRequest
    {
        public string SiteName { get; set; } = "";

        public string? SiteUrl { get; set; }

        public string? DefaultRobots { get; set; }
        public string? DefaultOgImageUrl { get; set; }
        public string? DefaultTwitterCard { get; set; }
        public string? DefaultTwitterSite { get; set; }
        public string? DefaultSchemaJsonLd { get; set; }

        public string? BrandLogoUrl { get; set; }
        public string? SupportEmail { get; set; }
        public string? SupportPhone { get; set; }

        public bool? IsActive { get; set; }
    }

    public sealed class SetActiveRequest
    {
        public bool IsActive { get; set; }
    }
}

