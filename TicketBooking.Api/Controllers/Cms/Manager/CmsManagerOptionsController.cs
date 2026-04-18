// FILE #315: TicketBooking.Api/Controllers/Cms/Manager/CmsManagerOptionsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Cms;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/manager/cms/options")]
[Authorize(Roles = "Admin,QLNX,QLVT,QLVMM,QLKS,QLTour")]
public sealed class CmsManagerOptionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public CmsManagerOptionsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>
    /// Returns lightweight lookup/options data for CMS manager screens:
    /// - statuses
    /// - redirect status codes
    /// - media types
    /// - categories
    /// - tags
    /// - media assets
    /// - site settings summary
    /// Useful for create/edit forms to avoid calling many endpoints separately.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int categoryLimit = 200,
        [FromQuery] int tagLimit = 200,
        [FromQuery] int mediaLimit = 100,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        categoryLimit = Math.Clamp(categoryLimit, 1, 500);
        tagLimit = Math.Clamp(tagLimit, 1, 500);
        mediaLimit = Math.Clamp(mediaLimit, 1, 300);

        var tenantId = _tenant.TenantId.Value;

        var categoriesQuery = _db.Set<NewsCategory>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!includeInactive)
            categoriesQuery = categoriesQuery.Where(x => x.IsActive);

        var categories = await categoriesQuery
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Take(categoryLimit)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Slug,
                x.SortOrder,
                x.IsActive
            })
            .ToListAsync(ct);

        var tagsQuery = _db.Set<NewsTag>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!includeInactive)
            tagsQuery = tagsQuery.Where(x => x.IsActive);

        var tags = await tagsQuery
            .OrderBy(x => x.Name)
            .Take(tagLimit)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Slug,
                x.IsActive
            })
            .ToListAsync(ct);

        var mediaQuery = _db.Set<MediaAsset>()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!includeInactive)
            mediaQuery = mediaQuery.Where(x => x.IsActive);

        var mediaAssets = await mediaQuery
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Take(mediaLimit)
            .Select(x => new
            {
                x.Id,
                x.Type,
                x.FileName,
                x.Title,
                x.AltText,
                x.PublicUrl,
                x.MimeType,
                x.Width,
                x.Height,
                x.IsActive
            })
            .ToListAsync(ct);

        var siteSettings = await _db.Set<SiteSetting>()
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId)
            .Select(x => new
            {
                x.Id,
                x.SiteName,
                x.SiteUrl,
                x.DefaultRobots,
                x.DefaultOgImageUrl,
                x.DefaultTwitterCard,
                x.DefaultTwitterSite,
                x.BrandLogoUrl,
                x.SupportEmail,
                x.SupportPhone,
                x.IsActive,
                x.IsDeleted
            })
            .FirstOrDefaultAsync(ct);

        var result = new
        {
            statuses = Enum.GetValues<NewsPostStatus>()
                .Select(x => new
                {
                    value = (int)x,
                    code = x.ToString(),
                    name = x.ToString()
                })
                .ToList(),

            redirectStatusCodes = Enum.GetValues<RedirectStatusCode>()
                .Select(x => new
                {
                    value = (int)x,
                    code = x.ToString(),
                    name = x.ToString()
                })
                .ToList(),

            mediaTypes = Enum.GetValues<MediaAssetType>()
                .Select(x => new
                {
                    value = (int)x,
                    code = x.ToString(),
                    name = x.ToString()
                })
                .ToList(),

            robotsSuggestions = new[]
            {
                "index,follow",
                "noindex,follow",
                "index,nofollow",
                "noindex,nofollow"
            },

            twitterCardSuggestions = new[]
            {
                "summary",
                "summary_large_image"
            },

            categories,
            tags,
            mediaAssets,
            siteSettings
        };

        return Ok(result);
    }
}
