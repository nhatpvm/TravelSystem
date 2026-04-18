// FILE #317: TicketBooking.Api/Controllers/Cms/Manager/CmsManagerSeoAuditController.cs
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
[Route("api/v{version:apiVersion}/manager/cms/seo-audit")]
[Authorize(Roles = "Admin,QLNX,QLVT,QLVMM,QLKS,QLTour")]
public sealed class CmsManagerSeoAuditController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public CmsManagerSeoAuditController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>
    /// Audit one saved post in current tenant.
    /// Returns score + warnings/errors for SEO/social/share readiness.
    /// </summary>
    [HttpGet("posts/{id:guid}")]
    public async Task<IActionResult> AuditSavedPost(Guid id, CancellationToken ct = default)
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

        var categoriesCount = await _db.Set<NewsPostCategory>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PostId == id && !x.IsDeleted)
            .CountAsync(ct);

        var tagsCount = await _db.Set<NewsPostTag>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PostId == id && !x.IsDeleted)
            .CountAsync(ct);

        return Ok(RunAudit(
            title: post.Title,
            slug: post.Slug,
            summary: post.Summary,
            contentMarkdown: post.ContentMarkdown,
            contentHtml: post.ContentHtml,
            seoTitle: post.SeoTitle,
            seoDescription: post.SeoDescription,
            seoKeywords: post.SeoKeywords,
            canonicalUrl: post.CanonicalUrl,
            robots: post.Robots,
            ogTitle: post.OgTitle,
            ogDescription: post.OgDescription,
            ogImageUrl: post.OgImageUrl,
            ogType: post.OgType,
            twitterCard: post.TwitterCard,
            twitterSite: post.TwitterSite,
            twitterTitle: post.TwitterTitle,
            twitterDescription: post.TwitterDescription,
            twitterImageUrl: post.TwitterImageUrl,
            schemaJsonLd: post.SchemaJsonLd,
            coverImageUrl: post.CoverImageUrl,
            wordCountFromEntity: post.WordCount,
            readingTimeMinutesFromEntity: post.ReadingTimeMinutes,
            status: post.Status,
            scheduledAt: post.ScheduledAt,
            publishedAt: post.PublishedAt,
            unpublishedAt: post.UnpublishedAt,
            siteUrl: site?.SiteUrl,
            siteDefaultRobots: site?.DefaultRobots,
            siteDefaultOgImageUrl: site?.DefaultOgImageUrl,
            siteDefaultTwitterCard: site?.DefaultTwitterCard,
            categoriesCount: categoriesCount,
            tagsCount: tagsCount));
    }

    /// <summary>
    /// Audit unsaved draft payload from editor UI.
    /// </summary>
    [HttpPost("draft")]
    public IActionResult AuditDraft([FromBody] CmsManagerSeoAuditDraftRequest req)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        if (req is null)
            return BadRequest(new { message = "Request body is required." });

        return Ok(RunAudit(
            title: req.Title,
            slug: req.Slug,
            summary: req.Summary,
            contentMarkdown: req.ContentMarkdown,
            contentHtml: req.ContentHtml,
            seoTitle: req.SeoTitle,
            seoDescription: req.SeoDescription,
            seoKeywords: req.SeoKeywords,
            canonicalUrl: req.CanonicalUrl,
            robots: req.Robots,
            ogTitle: req.OgTitle,
            ogDescription: req.OgDescription,
            ogImageUrl: req.OgImageUrl,
            ogType: req.OgType,
            twitterCard: req.TwitterCard,
            twitterSite: req.TwitterSite,
            twitterTitle: req.TwitterTitle,
            twitterDescription: req.TwitterDescription,
            twitterImageUrl: req.TwitterImageUrl,
            schemaJsonLd: req.SchemaJsonLd,
            coverImageUrl: req.CoverImageUrl,
            wordCountFromEntity: req.WordCount,
            readingTimeMinutesFromEntity: req.ReadingTimeMinutes,
            status: req.Status ?? NewsPostStatus.Draft,
            scheduledAt: req.ScheduledAt,
            publishedAt: req.PublishedAt,
            unpublishedAt: req.UnpublishedAt,
            siteUrl: req.SiteUrl,
            siteDefaultRobots: req.SiteDefaultRobots,
            siteDefaultOgImageUrl: req.SiteDefaultOgImageUrl,
            siteDefaultTwitterCard: req.SiteDefaultTwitterCard,
            categoriesCount: req.CategoryIds?.Distinct().Count() ?? 0,
            tagsCount: req.TagIds?.Distinct().Count() ?? 0));
    }

    private static object RunAudit(
        string? title,
        string? slug,
        string? summary,
        string? contentMarkdown,
        string? contentHtml,
        string? seoTitle,
        string? seoDescription,
        string? seoKeywords,
        string? canonicalUrl,
        string? robots,
        string? ogTitle,
        string? ogDescription,
        string? ogImageUrl,
        string? ogType,
        string? twitterCard,
        string? twitterSite,
        string? twitterTitle,
        string? twitterDescription,
        string? twitterImageUrl,
        string? schemaJsonLd,
        string? coverImageUrl,
        int? wordCountFromEntity,
        int? readingTimeMinutesFromEntity,
        NewsPostStatus status,
        DateTimeOffset? scheduledAt,
        DateTimeOffset? publishedAt,
        DateTimeOffset? unpublishedAt,
        string? siteUrl,
        string? siteDefaultRobots,
        string? siteDefaultOgImageUrl,
        string? siteDefaultTwitterCard,
        int categoriesCount,
        int tagsCount)
    {
        var issues = new List<CmsSeoAuditIssue>();

        var normalizedTitle = TrimOrEmpty(title);
        var normalizedSlug = NormalizeSlug(slug);
        var normalizedSummary = TrimOrEmpty(summary);
        var normalizedSeoTitle = TrimOrEmpty(seoTitle);
        var normalizedSeoDescription = TrimOrEmpty(seoDescription);
        var normalizedCanonicalUrl = TrimOrEmpty(canonicalUrl);
        var normalizedRobots = TrimOrEmpty(robots);
        var normalizedOgTitle = TrimOrEmpty(ogTitle);
        var normalizedOgDescription = TrimOrEmpty(ogDescription);
        var normalizedOgImageUrl = TrimOrEmpty(ogImageUrl);
        var normalizedTwitterCard = TrimOrEmpty(twitterCard);
        var normalizedTwitterSite = TrimOrEmpty(twitterSite);
        var normalizedTwitterTitle = TrimOrEmpty(twitterTitle);
        var normalizedTwitterDescription = TrimOrEmpty(twitterDescription);
        var normalizedTwitterImageUrl = TrimOrEmpty(twitterImageUrl);
        var normalizedSchemaJsonLd = TrimOrEmpty(schemaJsonLd);
        var normalizedCoverImageUrl = TrimOrEmpty(coverImageUrl);

        var plainText = BuildPlainText(contentMarkdown, contentHtml);
        var calculatedWordCount = CountWords(plainText);
        var finalWordCount = wordCountFromEntity.HasValue && wordCountFromEntity.Value > 0
            ? wordCountFromEntity.Value
            : calculatedWordCount;

        var finalReadingTime = readingTimeMinutesFromEntity.HasValue && readingTimeMinutesFromEntity.Value > 0
            ? readingTimeMinutesFromEntity.Value
            : EstimateReadingMinutes(finalWordCount);

        var effectiveSeoTitle = !string.IsNullOrWhiteSpace(normalizedSeoTitle) ? normalizedSeoTitle : normalizedTitle;
        var effectiveSeoDescription = !string.IsNullOrWhiteSpace(normalizedSeoDescription)
            ? normalizedSeoDescription
            : (!string.IsNullOrWhiteSpace(normalizedSummary) ? normalizedSummary : BuildExcerpt(plainText, 180));

        var effectiveCanonical = !string.IsNullOrWhiteSpace(normalizedCanonicalUrl)
            ? normalizedCanonicalUrl
            : BuildCanonical(siteUrl, normalizedSlug);

        var effectiveRobots = !string.IsNullOrWhiteSpace(normalizedRobots)
            ? normalizedRobots
            : (!string.IsNullOrWhiteSpace(siteDefaultRobots) ? siteDefaultRobots!.Trim() : "index,follow");

        var effectiveOgTitle = !string.IsNullOrWhiteSpace(normalizedOgTitle) ? normalizedOgTitle : effectiveSeoTitle;
        var effectiveOgDescription = !string.IsNullOrWhiteSpace(normalizedOgDescription) ? normalizedOgDescription : effectiveSeoDescription;
        var effectiveOgImage = FirstNonWhite(normalizedOgImageUrl, normalizedCoverImageUrl, siteDefaultOgImageUrl);
        var effectiveTwitterCard = !string.IsNullOrWhiteSpace(normalizedTwitterCard)
            ? normalizedTwitterCard
            : (!string.IsNullOrWhiteSpace(siteDefaultTwitterCard) ? siteDefaultTwitterCard!.Trim() : "summary_large_image");
        var effectiveTwitterTitle = !string.IsNullOrWhiteSpace(normalizedTwitterTitle) ? normalizedTwitterTitle : effectiveOgTitle;
        var effectiveTwitterDescription = !string.IsNullOrWhiteSpace(normalizedTwitterDescription) ? normalizedTwitterDescription : effectiveOgDescription;
        var effectiveTwitterImage = FirstNonWhite(normalizedTwitterImageUrl, effectiveOgImage);

        // Core required fields
        if (string.IsNullOrWhiteSpace(normalizedTitle))
            AddIssue(issues, "error", "title.missing", "Title is required.", "Add a clear title for the post.");

        if (string.IsNullOrWhiteSpace(normalizedSlug))
            AddIssue(issues, "error", "slug.missing", "Slug is required.", "Add a URL-safe slug.");
        else
        {
            if (normalizedSlug.Length > 300)
                AddIssue(issues, "error", "slug.too_long", "Slug is too long.", "Keep slug under 300 characters.");

            if (!Regex.IsMatch(normalizedSlug, "^[a-z0-9-]+$"))
                AddIssue(issues, "warning", "slug.format", "Slug should ideally contain only lowercase letters, numbers, and hyphens.", "Normalize slug for cleaner URLs.");
        }

        if (string.IsNullOrWhiteSpace(contentMarkdown) && string.IsNullOrWhiteSpace(contentHtml))
            AddIssue(issues, "error", "content.missing", "Content is empty.", "Add article content before publish.");

        if (finalWordCount < 300)
            AddIssue(issues, "warning", "content.thin", "Content is thin for SEO.", "Aim for at least 300 words; 600+ is often stronger.");

        if (finalWordCount > 0 && finalReadingTime <= 0)
            AddIssue(issues, "warning", "reading_time.missing", "Reading time is missing or zero.", "Recalculate reading time before publishing.");

        // SEO title
        if (string.IsNullOrWhiteSpace(effectiveSeoTitle))
        {
            AddIssue(issues, "error", "seo_title.missing", "SEO title is missing.", "Add SeoTitle or at least a strong Title.");
        }
        else
        {
            if (effectiveSeoTitle.Length < 30)
                AddIssue(issues, "warning", "seo_title.short", "SEO title is a bit short.", "Aim for about 30–60 characters.");

            if (effectiveSeoTitle.Length > 60)
                AddIssue(issues, "warning", "seo_title.long", "SEO title may be truncated in search results.", "Keep it around 50–60 characters.");
        }

        // SEO description
        if (string.IsNullOrWhiteSpace(effectiveSeoDescription))
        {
            AddIssue(issues, "error", "seo_description.missing", "SEO description is missing.", "Add SeoDescription or a useful Summary.");
        }
        else
        {
            if (effectiveSeoDescription.Length < 70)
                AddIssue(issues, "warning", "seo_description.short", "SEO description is a bit short.", "Aim for about 120–160 characters.");

            if (effectiveSeoDescription.Length > 160)
                AddIssue(issues, "warning", "seo_description.long", "SEO description may be truncated.", "Keep it around 120–160 characters.");
        }

        // Canonical / robots
        if (string.IsNullOrWhiteSpace(effectiveCanonical))
            AddIssue(issues, "warning", "canonical.missing", "Canonical URL is missing.", "Set SiteUrl or CanonicalUrl.");

        if (!string.IsNullOrWhiteSpace(effectiveCanonical) && !Uri.TryCreate(effectiveCanonical, UriKind.Absolute, out _))
            AddIssue(issues, "warning", "canonical.invalid", "Canonical URL is not absolute.", "Use a full absolute URL.");

        if (string.IsNullOrWhiteSpace(effectiveRobots))
            AddIssue(issues, "warning", "robots.missing", "Robots value is missing.", "Set robots to index,follow or another intended value.");

        if (!string.IsNullOrWhiteSpace(effectiveRobots) &&
            effectiveRobots.Contains("noindex", StringComparison.OrdinalIgnoreCase) &&
            status == NewsPostStatus.Published)
        {
            AddIssue(issues, "warning", "robots.noindex_published", "Published post is marked as noindex.", "Confirm that this published post should really be excluded from search engines.");
        }

        // OpenGraph
        if (string.IsNullOrWhiteSpace(effectiveOgTitle))
            AddIssue(issues, "warning", "og_title.missing", "Open Graph title is missing.", "Add OgTitle or rely on SeoTitle/Title fallback.");

        if (string.IsNullOrWhiteSpace(effectiveOgDescription))
            AddIssue(issues, "warning", "og_description.missing", "Open Graph description is missing.", "Add OgDescription or rely on SeoDescription fallback.");

        if (string.IsNullOrWhiteSpace(effectiveOgImage))
            AddIssue(issues, "warning", "og_image.missing", "Open Graph image is missing.", "Set OgImageUrl, CoverImageUrl, or tenant default OG image.");
        else if (!Uri.TryCreate(effectiveOgImage, UriKind.Absolute, out _))
            AddIssue(issues, "warning", "og_image.invalid", "Open Graph image URL is not absolute.", "Use a full absolute image URL.");

        if (string.IsNullOrWhiteSpace(ogType))
            AddIssue(issues, "info", "og_type.defaulted", "Open Graph type will fall back to 'article'.", "Set OgType explicitly only if you need a different type.");

        // Twitter
        if (string.IsNullOrWhiteSpace(effectiveTwitterCard))
            AddIssue(issues, "warning", "twitter_card.missing", "Twitter card is missing.", "Use summary or summary_large_image.");

        if (string.IsNullOrWhiteSpace(effectiveTwitterTitle))
            AddIssue(issues, "warning", "twitter_title.missing", "Twitter title is missing.", "Add TwitterTitle or rely on OG/SEO fallback.");

        if (string.IsNullOrWhiteSpace(effectiveTwitterDescription))
            AddIssue(issues, "warning", "twitter_description.missing", "Twitter description is missing.", "Add TwitterDescription or rely on OG/SEO fallback.");

        if (string.IsNullOrWhiteSpace(effectiveTwitterImage))
            AddIssue(issues, "info", "twitter_image.missing", "Twitter image is missing.", "Usually you want an image for better sharing.");
        else if (!Uri.TryCreate(effectiveTwitterImage, UriKind.Absolute, out _))
            AddIssue(issues, "warning", "twitter_image.invalid", "Twitter image URL is not absolute.", "Use a full absolute image URL.");

        if (string.IsNullOrWhiteSpace(normalizedTwitterSite))
            AddIssue(issues, "info", "twitter_site.missing", "Twitter site handle is missing.", "Optional but useful for richer brand metadata.");

        // Schema
        if (string.IsNullOrWhiteSpace(normalizedSchemaJsonLd))
            AddIssue(issues, "info", "schema.missing", "Schema JSON-LD is missing.", "Add article schema or tenant default schema for better rich results.");
        else if (!LooksLikeJson(normalizedSchemaJsonLd))
            AddIssue(issues, "warning", "schema.invalid_shape", "Schema JSON-LD does not look like valid JSON.", "Check the JSON-LD format.");

        // Taxonomy
        if (categoriesCount == 0)
            AddIssue(issues, "info", "category.none", "Post has no category.", "Assign at least one category for better organization.");
        if (tagsCount == 0)
            AddIssue(issues, "info", "tag.none", "Post has no tags.", "Optional, but tags can help organization and related content.");

        // Workflow checks
        if (status == NewsPostStatus.Scheduled && !scheduledAt.HasValue)
            AddIssue(issues, "warning", "schedule.missing_date", "Post status is Scheduled but ScheduledAt is empty.", "Set ScheduledAt.");

        if (status == NewsPostStatus.Published && !publishedAt.HasValue)
            AddIssue(issues, "warning", "publish.missing_date", "Post status is Published but PublishedAt is empty.", "Set PublishedAt.");

        if (status == NewsPostStatus.Unpublished && !unpublishedAt.HasValue)
            AddIssue(issues, "info", "unpublish.missing_date", "Post status is Unpublished but UnpublishedAt is empty.", "Set UnpublishedAt for clearer audit history.");

        // Keywords
        if (!string.IsNullOrWhiteSpace(seoKeywords))
        {
            var keywordCount = seoKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
            if (keywordCount > 15)
                AddIssue(issues, "info", "keywords.too_many", "There are many SEO keywords.", "Keep keywords focused and relevant.");
        }

        // Scoring
        var score = 100;
        score -= issues.Count(x => x.Severity == "error") * 15;
        score -= issues.Count(x => x.Severity == "warning") * 6;
        score -= issues.Count(x => x.Severity == "info") * 2;
        if (score < 0) score = 0;

        var grade = score switch
        {
            >= 90 => "excellent",
            >= 75 => "good",
            >= 60 => "fair",
            >= 40 => "weak",
            _ => "poor"
        };

        return new
        {
            summary = new
            {
                score,
                grade,
                errorCount = issues.Count(x => x.Severity == "error"),
                warningCount = issues.Count(x => x.Severity == "warning"),
                infoCount = issues.Count(x => x.Severity == "info")
            },
            computed = new
            {
                title = normalizedTitle,
                slug = normalizedSlug,
                wordCount = finalWordCount,
                readingTimeMinutes = finalReadingTime,
                seoTitle = effectiveSeoTitle,
                seoDescription = effectiveSeoDescription,
                canonicalUrl = effectiveCanonical,
                robots = effectiveRobots,
                ogTitle = effectiveOgTitle,
                ogDescription = effectiveOgDescription,
                ogImageUrl = effectiveOgImage,
                ogType = string.IsNullOrWhiteSpace(ogType) ? "article" : ogType,
                twitterCard = effectiveTwitterCard,
                twitterSite = normalizedTwitterSite,
                twitterTitle = effectiveTwitterTitle,
                twitterDescription = effectiveTwitterDescription,
                twitterImageUrl = effectiveTwitterImage,
                hasSchemaJsonLd = !string.IsNullOrWhiteSpace(normalizedSchemaJsonLd),
                status,
                scheduledAt,
                publishedAt,
                unpublishedAt,
                categoriesCount,
                tagsCount
            },
            issues = issues
                .OrderByDescending(x => SeverityOrder(x.Severity))
                .ThenBy(x => x.Code)
                .ToList()
        };
    }

    private static void AddIssue(List<CmsSeoAuditIssue> issues, string severity, string code, string message, string recommendation)
    {
        issues.Add(new CmsSeoAuditIssue
        {
            Severity = severity,
            Code = code,
            Message = message,
            Recommendation = recommendation
        });
    }

    private static int SeverityOrder(string severity) => severity switch
    {
        "error" => 3,
        "warning" => 2,
        "info" => 1,
        _ => 0
    };

    private static string BuildPlainText(string? markdown, string? html)
    {
        if (!string.IsNullOrWhiteSpace(markdown))
            return StripMarkdown(markdown);

        if (!string.IsNullOrWhiteSpace(html))
            return StripHtml(html);

        return string.Empty;
    }

    private static string StripMarkdown(string markdown)
    {
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
        var text = Regex.Replace(html, "<.*?>", " ");
        text = Regex.Replace(text, @"\s{2,}", " ");
        return text.Trim();
    }

    private static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Trim()
            .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Length;
    }

    private static int EstimateReadingMinutes(int wordCount)
    {
        if (wordCount <= 0)
            return 0;

        return Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
    }

    private static string BuildExcerpt(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var value = text.Trim();
        if (value.Length <= maxLength)
            return value;

        return value[..maxLength].Trim() + "...";
    }

    private static string BuildCanonical(string? siteUrl, string slug)
    {
        if (string.IsNullOrWhiteSpace(siteUrl) || string.IsNullOrWhiteSpace(slug))
            return string.Empty;

        return siteUrl.Trim().TrimEnd('/') + "/tin-tuc/" + slug;
    }

    private static string NormalizeSlug(string? slug)
    {
        slug = (slug ?? "").Trim().ToLowerInvariant();
        if (slug.Length == 0) return "";

        var parts = slug.Split(new[] { ' ', '\t', '\r', '\n', '/' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join('-', parts);
    }

    private static bool LooksLikeJson(string value)
    {
        value = value.Trim();
        return (value.StartsWith("{") && value.EndsWith("}")) ||
               (value.StartsWith("[") && value.EndsWith("]"));
    }

    private static string TrimOrEmpty(string? value) => value?.Trim() ?? "";

    private static string? FirstNonWhite(params string?[] values)
        => values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();
}

public sealed class CmsManagerSeoAuditDraftRequest
{
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }

    public string? ContentMarkdown { get; set; }
    public string? ContentHtml { get; set; }

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
    public string? TwitterTitle { get; set; }
    public string? TwitterDescription { get; set; }
    public string? TwitterImageUrl { get; set; }

    public string? SchemaJsonLd { get; set; }
    public string? CoverImageUrl { get; set; }

    public int? WordCount { get; set; }
    public int? ReadingTimeMinutes { get; set; }

    public NewsPostStatus? Status { get; set; }
    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? UnpublishedAt { get; set; }

    public List<Guid>? CategoryIds { get; set; }
    public List<Guid>? TagIds { get; set; }

    public string? SiteUrl { get; set; }
    public string? SiteDefaultRobots { get; set; }
    public string? SiteDefaultOgImageUrl { get; set; }
    public string? SiteDefaultTwitterCard { get; set; }
}

public sealed class CmsSeoAuditIssue
{
    public string Severity { get; set; } = "info";
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
    public string Recommendation { get; set; } = "";
}
