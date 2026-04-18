// FILE #316: TicketBooking.Api/Controllers/Cms/Manager/CmsManagerPreviewController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TicketBooking.Domain.Cms;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/manager/cms/preview")]
[Authorize(Roles = "Admin,QLNX,QLVT,QLVMM,QLKS,QLTour")]
public sealed class CmsManagerPreviewController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public CmsManagerPreviewController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>
    /// Preview from an existing CMS post in the current tenant.
    /// Useful before publish/unpublish and for checking SEO fallback values.
    /// </summary>
    [HttpGet("posts/{id:guid}")]
    public async Task<IActionResult> PreviewSavedPost(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;

        var post = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (post is null)
            return NotFound(new { message = "Post not found." });

        var site = await _db.Set<SiteSetting>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        var categoryIds = await _db.Set<NewsPostCategory>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PostId == id && !x.IsDeleted)
            .Select(x => x.CategoryId)
            .ToListAsync(ct);

        var tagIds = await _db.Set<NewsPostTag>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PostId == id && !x.IsDeleted)
            .Select(x => x.TagId)
            .ToListAsync(ct);

        var categories = categoryIds.Count == 0
            ? new List<object>()
            : await _db.Set<NewsCategory>()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && categoryIds.Contains(x.Id) && !x.IsDeleted)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => (object)new
                {
                    x.Id,
                    x.Name,
                    x.Slug
                })
                .ToListAsync(ct);

        var tags = tagIds.Count == 0
            ? new List<object>()
            : await _db.Set<NewsTag>()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && tagIds.Contains(x.Id) && !x.IsDeleted)
                .OrderBy(x => x.Name)
                .Select(x => (object)new
                {
                    x.Id,
                    x.Name,
                    x.Slug
                })
                .ToListAsync(ct);

        return Ok(BuildPreviewResponse(post, site, categories, tags));
    }

    /// <summary>
    /// Ad-hoc preview for unsaved draft content from the editor UI.
    /// This helps FE preview title/SEO/OpenGraph/canonical without saving first.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PreviewDraft([FromBody] CmsManagerPreviewDraftRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        if (req is null)
            return BadRequest(new { message = "Request body is required." });

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { message = "Title is required." });

        if (string.IsNullOrWhiteSpace(req.Slug))
            return BadRequest(new { message = "Slug is required." });

        var tenantId = _tenant.TenantId.Value;

        var site = await _db.Set<SiteSetting>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

        var now = DateTimeOffset.Now;
        var wordCount = CountWords(req.ContentMarkdown);
        var readingMinutes = EstimateReadingMinutes(wordCount);

        var virtualPost = new NewsPost
        {
            Id = Guid.Empty,
            TenantId = tenantId,
            Title = req.Title.Trim(),
            Slug = NormalizeSlug(req.Slug),
            Summary = NullIfWhite(req.Summary),
            ContentMarkdown = req.ContentMarkdown ?? "",
            ContentHtml = req.ContentHtml ?? "",
            CoverImageUrl = NullIfWhite(req.CoverImageUrl),

            SeoTitle = NullIfWhite(req.SeoTitle),
            SeoDescription = NullIfWhite(req.SeoDescription),
            SeoKeywords = NullIfWhite(req.SeoKeywords),
            CanonicalUrl = NullIfWhite(req.CanonicalUrl),
            Robots = NullIfWhite(req.Robots),

            OgTitle = NullIfWhite(req.OgTitle),
            OgDescription = NullIfWhite(req.OgDescription),
            OgImageUrl = NullIfWhite(req.OgImageUrl),
            OgType = NullIfWhite(req.OgType) ?? "article",

            TwitterCard = NullIfWhite(req.TwitterCard) ?? "summary_large_image",
            TwitterSite = NullIfWhite(req.TwitterSite),
            TwitterCreator = NullIfWhite(req.TwitterCreator),
            TwitterTitle = NullIfWhite(req.TwitterTitle),
            TwitterDescription = NullIfWhite(req.TwitterDescription),
            TwitterImageUrl = NullIfWhite(req.TwitterImageUrl),

            SchemaJsonLd = NullIfWhite(req.SchemaJsonLd),

            Status = req.Status ?? NewsPostStatus.Draft,
            ScheduledAt = req.ScheduledAt,
            PublishedAt = req.PublishedAt,
            UnpublishedAt = req.UnpublishedAt,

            WordCount = wordCount,
            ReadingTimeMinutes = readingMinutes,
            LastEditedAt = now,
            CreatedAt = now
        };

        var categories = (req.Categories ?? new List<CmsManagerPreviewLookupItem>())
            .Select(x => (object)new { x.Id, x.Name, x.Slug })
            .ToList();

        var tags = (req.Tags ?? new List<CmsManagerPreviewLookupItem>())
            .Select(x => (object)new { x.Id, x.Name, x.Slug })
            .ToList();

        return Ok(BuildPreviewResponse(virtualPost, site, categories, tags));
    }

    private object BuildPreviewResponse(
        NewsPost post,
        SiteSetting? site,
        List<object> categories,
        List<object> tags)
    {
        var normalizedSlug = NormalizeSlug(post.Slug);
        var baseUrl = ResolveBaseUrl(site?.SiteUrl);
        var publicPath = $"/tin-tuc/{normalizedSlug}";
        var publicUrl = $"{baseUrl}{publicPath}";

        var plainText = BuildPlainText(post);
        var summaryFallback = !string.IsNullOrWhiteSpace(post.Summary)
            ? post.Summary!.Trim()
            : BuildExcerpt(plainText, 180);

        var seoTitle = !string.IsNullOrWhiteSpace(post.SeoTitle)
            ? post.SeoTitle!.Trim()
            : post.Title.Trim();

        var seoDescription = !string.IsNullOrWhiteSpace(post.SeoDescription)
            ? post.SeoDescription!.Trim()
            : summaryFallback;

        var canonicalUrl = !string.IsNullOrWhiteSpace(post.CanonicalUrl)
            ? post.CanonicalUrl!.Trim()
            : publicUrl;

        var robots = !string.IsNullOrWhiteSpace(post.Robots)
            ? post.Robots!.Trim()
            : (!string.IsNullOrWhiteSpace(site?.DefaultRobots) ? site!.DefaultRobots!.Trim() : "index,follow");

        var ogTitle = !string.IsNullOrWhiteSpace(post.OgTitle)
            ? post.OgTitle!.Trim()
            : seoTitle;

        var ogDescription = !string.IsNullOrWhiteSpace(post.OgDescription)
            ? post.OgDescription!.Trim()
            : seoDescription;

        var ogImageUrl = FirstNonWhite(
            post.OgImageUrl,
            post.CoverImageUrl,
            site?.DefaultOgImageUrl);

        var twitterCard = !string.IsNullOrWhiteSpace(post.TwitterCard)
            ? post.TwitterCard!.Trim()
            : (!string.IsNullOrWhiteSpace(site?.DefaultTwitterCard) ? site!.DefaultTwitterCard!.Trim() : "summary_large_image");

        var twitterSite = FirstNonWhite(post.TwitterSite, site?.DefaultTwitterSite);
        var twitterTitle = !string.IsNullOrWhiteSpace(post.TwitterTitle) ? post.TwitterTitle!.Trim() : ogTitle;
        var twitterDescription = !string.IsNullOrWhiteSpace(post.TwitterDescription) ? post.TwitterDescription!.Trim() : ogDescription;
        var twitterImageUrl = FirstNonWhite(post.TwitterImageUrl, ogImageUrl);

        var schemaJsonLd = !string.IsNullOrWhiteSpace(post.SchemaJsonLd)
            ? post.SchemaJsonLd
            : site?.DefaultSchemaJsonLd;

        return new
        {
            post = new
            {
                post.Id,
                post.TenantId,
                post.Title,
                Slug = normalizedSlug,
                post.Status,
                post.Summary,
                post.PublishedAt,
                post.ScheduledAt,
                post.UnpublishedAt,
                post.WordCount,
                post.ReadingTimeMinutes,
                publicPath,
                publicUrl
            },
            content = new
            {
                post.ContentMarkdown,
                post.ContentHtml,
                plainText,
                excerpt = BuildExcerpt(plainText, 250)
            },
            seo = new
            {
                title = seoTitle,
                description = seoDescription,
                canonicalUrl,
                robots,
                keywords = post.SeoKeywords
            },
            openGraph = new
            {
                title = ogTitle,
                description = ogDescription,
                imageUrl = ogImageUrl,
                type = string.IsNullOrWhiteSpace(post.OgType) ? "article" : post.OgType
            },
            twitter = new
            {
                card = twitterCard,
                site = twitterSite,
                creator = post.TwitterCreator,
                title = twitterTitle,
                description = twitterDescription,
                imageUrl = twitterImageUrl
            },
            schema = new
            {
                jsonLd = schemaJsonLd
            },
            site = site is null
                ? null
                : new
                {
                    site.Id,
                    site.SiteName,
                    site.SiteUrl,
                    site.BrandLogoUrl,
                    site.SupportEmail,
                    site.SupportPhone,
                    site.DefaultRobots,
                    site.DefaultOgImageUrl,
                    site.DefaultTwitterCard,
                    site.DefaultTwitterSite
                },
            taxonomies = new
            {
                categories,
                tags
            },
            checks = new
            {
                hasTitle = !string.IsNullOrWhiteSpace(post.Title),
                hasSlug = !string.IsNullOrWhiteSpace(normalizedSlug),
                hasSummaryOrExcerpt = !string.IsNullOrWhiteSpace(summaryFallback),
                hasHtmlContent = !string.IsNullOrWhiteSpace(post.ContentHtml),
                hasMarkdownContent = !string.IsNullOrWhiteSpace(post.ContentMarkdown),
                hasCanonical = !string.IsNullOrWhiteSpace(canonicalUrl),
                hasOgImage = !string.IsNullOrWhiteSpace(ogImageUrl),
                hasSchema = !string.IsNullOrWhiteSpace(schemaJsonLd)
            }
        };
    }

    private static string BuildPlainText(NewsPost post)
    {
        if (!string.IsNullOrWhiteSpace(post.ContentMarkdown))
            return StripMarkdown(post.ContentMarkdown);

        if (!string.IsNullOrWhiteSpace(post.ContentHtml))
            return StripHtml(post.ContentHtml);

        return string.Empty;
    }

    private static string StripMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var text = markdown;

        text = Regex.Replace(text, @"!\[[^\]]*\]\([^)]+\)", " ");
        text = Regex.Replace(text, @"\[[^\]]+\]\([^)]+\)", " ");
        text = Regex.Replace(text, @"[#>*`~\-=_]", " ");
        text = Regex.Replace(text, @"\r|\n|\t", " ");
        text = Regex.Replace(text, @"\s{2,}", " ");

        return text.Trim();
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var text = Regex.Replace(html, "<.*?>", " ");
        text = Regex.Replace(text, @"\s{2,}", " ");
        return text.Trim();
    }

    private static string BuildExcerpt(string text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.Trim();
        if (text.Length <= maxLength)
            return text;

        return text[..maxLength].Trim() + "...";
    }

    private static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text
            .Trim()
            .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Length;
    }

    private static int EstimateReadingMinutes(int wordCount)
    {
        if (wordCount <= 0)
            return 0;

        return Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
    }

    private static string NormalizeSlug(string slug)
    {
        slug = (slug ?? "").Trim().ToLowerInvariant();
        if (slug.Length == 0) return "";

        var parts = slug.Split(new[] { ' ', '\t', '\r', '\n', '/' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join('-', parts);
    }

    private static string ResolveBaseUrl(string? siteUrl)
    {
        if (!string.IsNullOrWhiteSpace(siteUrl))
            return siteUrl.Trim().TrimEnd('/');

        return "http://localhost";
    }

    private static string? FirstNonWhite(params string?[] values)
        => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();

    private static string? NullIfWhite(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CmsManagerPreviewDraftRequest
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }

    public string ContentMarkdown { get; set; } = "";
    public string ContentHtml { get; set; } = "";

    public string? CoverImageUrl { get; set; }

    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoKeywords { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? Robots { get; set; }

    public string? OgTitle { get; set; }
    public string? OgDescription { get; set; }
    public string? OgImageUrl { get; set; }
    public string? OgType { get; set; }

    public string? TwitterCard { get; set; }
    public string? TwitterSite { get; set; }
    public string? TwitterCreator { get; set; }
    public string? TwitterTitle { get; set; }
    public string? TwitterDescription { get; set; }
    public string? TwitterImageUrl { get; set; }

    public string? SchemaJsonLd { get; set; }

    public NewsPostStatus? Status { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? UnpublishedAt { get; set; }

    public List<CmsManagerPreviewLookupItem>? Categories { get; set; }
    public List<CmsManagerPreviewLookupItem>? Tags { get; set; }
}

public sealed class CmsManagerPreviewLookupItem
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
}
