// FILE #313: TicketBooking.Api/Controllers/Cms/Manager/CmsManagerSiteSettingsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketBooking.Domain.Cms;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/manager/cms/site-settings")]
[Authorize(Roles = "Admin,QLNX,QLVT,QLVMM,QLKS,QLTour")]
public sealed class CmsManagerSiteSettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public CmsManagerSiteSettingsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;

        IQueryable<SiteSetting> query = _db.Set<SiteSetting>().AsNoTracking();
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var entity = await query.FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);
        if (entity is null)
        {
            return Ok(new
            {
                Id = (Guid?)null,
                TenantId = tenantId,
                SiteName = string.Empty,
                SiteUrl = (string?)null,
                DefaultRobots = "index,follow",
                DefaultOgImageUrl = (string?)null,
                DefaultTwitterCard = "summary_large_image",
                DefaultTwitterSite = (string?)null,
                DefaultSchemaJsonLd = (string?)null,
                BrandLogoUrl = (string?)null,
                SupportEmail = (string?)null,
                SupportPhone = (string?)null,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = (DateTimeOffset?)null,
                CreatedByUserId = (string?)null,
                UpdatedAt = (DateTimeOffset?)null,
                UpdatedByUserId = (string?)null
            });
        }

        return Ok(new
        {
            entity.Id,
            entity.TenantId,
            entity.SiteName,
            entity.SiteUrl,
            entity.DefaultRobots,
            entity.DefaultOgImageUrl,
            entity.DefaultTwitterCard,
            entity.DefaultTwitterSite,
            entity.DefaultSchemaJsonLd,
            entity.BrandLogoUrl,
            entity.SupportEmail,
            entity.SupportPhone,
            entity.IsActive,
            entity.IsDeleted,
            entity.CreatedAt,
            entity.CreatedByUserId,
            entity.UpdatedAt,
            entity.UpdatedByUserId
        });
    }

    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] CmsManagerUpsertSiteSettingsRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var validation = ValidateRequest(req);
        if (validation is not null)
            return validation;

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
                SiteUrl = NormalizeUrlOrNull(req.SiteUrl),

                DefaultRobots = NullIfWhite(req.DefaultRobots),
                DefaultOgImageUrl = NormalizeUrlOrNull(req.DefaultOgImageUrl),
                DefaultTwitterCard = string.IsNullOrWhiteSpace(req.DefaultTwitterCard)
                    ? "summary_large_image"
                    : req.DefaultTwitterCard.Trim(),
                DefaultTwitterSite = NullIfWhite(req.DefaultTwitterSite),
                DefaultSchemaJsonLd = NullIfWhite(req.DefaultSchemaJsonLd),

                BrandLogoUrl = NormalizeUrlOrNull(req.BrandLogoUrl),
                SupportEmail = NullIfWhite(req.SupportEmail),
                SupportPhone = NullIfWhite(req.SupportPhone),

                IsActive = req.IsActive,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId
            };

            _db.Set<SiteSetting>().Add(entity);
            await _db.SaveChangesAsync(ct);

            return Ok(new { ok = true, id = entity.Id, created = true });
        }

        entity.SiteName = req.SiteName.Trim();
        entity.SiteUrl = NormalizeUrlOrNull(req.SiteUrl);

        entity.DefaultRobots = NullIfWhite(req.DefaultRobots);
        entity.DefaultOgImageUrl = NormalizeUrlOrNull(req.DefaultOgImageUrl);
        entity.DefaultTwitterCard = string.IsNullOrWhiteSpace(req.DefaultTwitterCard)
            ? "summary_large_image"
            : req.DefaultTwitterCard.Trim();
        entity.DefaultTwitterSite = NullIfWhite(req.DefaultTwitterSite);
        entity.DefaultSchemaJsonLd = NullIfWhite(req.DefaultSchemaJsonLd);

        entity.BrandLogoUrl = NormalizeUrlOrNull(req.BrandLogoUrl);
        entity.SupportEmail = NullIfWhite(req.SupportEmail);
        entity.SupportPhone = NullIfWhite(req.SupportPhone);

        entity.IsActive = req.IsActive;

        if (entity.IsDeleted)
            entity.IsDeleted = false;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true, id = entity.Id, created = false });
    }

    [HttpPost("set-active")]
    public async Task<IActionResult> SetActive([FromBody] CmsManagerSetSiteSettingsActiveRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<SiteSetting>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Site settings not found for this tenant." });

        entity.IsActive = req.IsActive;

        if (entity.IsDeleted && req.IsActive)
            entity.IsDeleted = false;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpDelete]
    public async Task<IActionResult> SoftDelete(CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<SiteSetting>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Site settings not found for this tenant." });

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("restore")]
    public async Task<IActionResult> Restore(CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<SiteSetting>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Site settings not found for this tenant." });

        entity.IsDeleted = false;
        entity.IsActive = true;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private IActionResult? ValidateRequest(CmsManagerUpsertSiteSettingsRequest req)
    {
        if (req is null)
            return BadRequest(new { message = "Request body is required." });

        if (string.IsNullOrWhiteSpace(req.SiteName))
            return BadRequest(new { message = "SiteName is required." });

        if (req.SiteName.Trim().Length > 200)
            return BadRequest(new { message = "SiteName max length is 200." });

        if (req.SiteUrl is not null && req.SiteUrl.Trim().Length > 1000)
            return BadRequest(new { message = "SiteUrl max length is 1000." });

        if (req.DefaultRobots is not null && req.DefaultRobots.Trim().Length > 200)
            return BadRequest(new { message = "DefaultRobots max length is 200." });

        if (req.DefaultOgImageUrl is not null && req.DefaultOgImageUrl.Trim().Length > 1000)
            return BadRequest(new { message = "DefaultOgImageUrl max length is 1000." });

        if (req.DefaultTwitterCard is not null && req.DefaultTwitterCard.Trim().Length > 50)
            return BadRequest(new { message = "DefaultTwitterCard max length is 50." });

        if (req.DefaultTwitterSite is not null && req.DefaultTwitterSite.Trim().Length > 100)
            return BadRequest(new { message = "DefaultTwitterSite max length is 100." });

        if (req.BrandLogoUrl is not null && req.BrandLogoUrl.Trim().Length > 1000)
            return BadRequest(new { message = "BrandLogoUrl max length is 1000." });

        if (req.SupportEmail is not null && req.SupportEmail.Trim().Length > 200)
            return BadRequest(new { message = "SupportEmail max length is 200." });

        if (req.SupportPhone is not null && req.SupportPhone.Trim().Length > 50)
            return BadRequest(new { message = "SupportPhone max length is 50." });

        return null;
    }

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? NullIfWhite(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeUrlOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimEnd('/');
}

public sealed class CmsManagerUpsertSiteSettingsRequest
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

    public bool IsActive { get; set; } = true;
}

public sealed class CmsManagerSetSiteSettingsActiveRequest
{
    public bool IsActive { get; set; }
}
