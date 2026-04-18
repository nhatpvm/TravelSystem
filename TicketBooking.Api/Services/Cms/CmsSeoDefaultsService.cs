// FILE #302: TicketBooking.Api/Services/Cms/CmsSeoDefaultsService.cs
using System.Text.Json;
using TicketBooking.Domain.Cms;

namespace TicketBooking.Api.Services.Cms;

public interface ICmsSeoDefaultsService
{
    CmsResolvedSeoMeta BuildForPost(NewsPost post, SiteSetting? site, string baseUrl);
    CmsResolvedSeoMeta BuildForNewsIndex(string tenantCode, SiteSetting? site, string baseUrl, string? title = null, string? description = null);
    CmsResolvedSeoMeta BuildForCategory(string categoryName, string categorySlug, SiteSetting? site, string baseUrl, string? description = null);
    CmsResolvedSeoMeta BuildForTag(string tagName, string tagSlug, SiteSetting? site, string baseUrl, string? description = null);
    string ResolveCanonicalUrl(string? explicitCanonicalUrl, string baseUrl, string relativePath);
    string? ResolveImageUrl(string? explicitImageUrl, string? fallbackImageUrl, string baseUrl);
    string BuildWebsiteSchemaJsonLd(SiteSetting? site, string baseUrl);
    string BuildArticleSchemaJsonLd(NewsPost post, SiteSetting? site, string baseUrl, string canonicalUrl, string? imageUrl);
}

public sealed class CmsSeoDefaultsService : ICmsSeoDefaultsService
{
    public CmsResolvedSeoMeta BuildForPost(NewsPost post, SiteSetting? site, string baseUrl)
    {
        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
        var canonicalUrl = ResolveCanonicalUrl(
            post.CanonicalUrl,
            normalizedBaseUrl,
            $"/tin-tuc/{post.Slug}");

        var imageUrl = ResolveImageUrl(
            post.OgImageUrl,
            post.CoverImageUrl ?? site?.DefaultOgImageUrl,
            normalizedBaseUrl);

        var seoTitle = FirstNonEmpty(
            post.SeoTitle,
            post.Title,
            site?.SiteName);

        var seoDescription = FirstNonEmpty(
            post.SeoDescription,
            post.Summary,
            $"{site?.SiteName ?? "TicketBooking"} news article");

        var ogTitle = FirstNonEmpty(post.OgTitle, seoTitle);
        var ogDescription = FirstNonEmpty(post.OgDescription, seoDescription);
        var ogType = FirstNonEmpty(post.OgType, "article");

        var twitterCard = FirstNonEmpty(post.TwitterCard, site?.DefaultTwitterCard, "summary_large_image");
        var twitterSite = FirstNonEmpty(post.TwitterSite, site?.DefaultTwitterSite);
        var twitterCreator = FirstNonEmpty(post.TwitterCreator);
        var twitterTitle = FirstNonEmpty(post.TwitterTitle, ogTitle, seoTitle);
        var twitterDescription = FirstNonEmpty(post.TwitterDescription, ogDescription, seoDescription);
        var twitterImageUrl = ResolveImageUrl(post.TwitterImageUrl, imageUrl, normalizedBaseUrl);

        var robots = FirstNonEmpty(post.Robots, site?.DefaultRobots, "index,follow");

        var schemaJsonLd = !string.IsNullOrWhiteSpace(post.SchemaJsonLd)
            ? post.SchemaJsonLd!.Trim()
            : BuildArticleSchemaJsonLd(post, site, normalizedBaseUrl, canonicalUrl, imageUrl);

        return new CmsResolvedSeoMeta
        {
            Title = seoTitle ?? string.Empty,
            Description = seoDescription,
            Keywords = NullIfWhite(post.SeoKeywords),
            CanonicalUrl = canonicalUrl,
            Robots = robots,

            OgTitle = ogTitle,
            OgDescription = ogDescription,
            OgImageUrl = imageUrl,
            OgType = ogType,

            TwitterCard = twitterCard,
            TwitterSite = twitterSite,
            TwitterCreator = twitterCreator,
            TwitterTitle = twitterTitle,
            TwitterDescription = twitterDescription,
            TwitterImageUrl = twitterImageUrl,

            SchemaJsonLd = schemaJsonLd
        };
    }

    public CmsResolvedSeoMeta BuildForNewsIndex(
        string tenantCode,
        SiteSetting? site,
        string baseUrl,
        string? title = null,
        string? description = null)
    {
        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
        var canonicalUrl = ResolveCanonicalUrl(null, normalizedBaseUrl, "/tin-tuc");
        var imageUrl = ResolveImageUrl(site?.DefaultOgImageUrl, null, normalizedBaseUrl);

        var resolvedTitle = FirstNonEmpty(
            title,
            $"Tin tức - {site?.SiteName ?? "TicketBooking"}");

        var resolvedDescription = FirstNonEmpty(
            description,
            $"Trang tin tức mới nhất của {site?.SiteName ?? "TicketBooking"}.");

        return new CmsResolvedSeoMeta
        {
            Title = resolvedTitle ?? string.Empty,
            Description = resolvedDescription,
            CanonicalUrl = canonicalUrl,
            Robots = FirstNonEmpty(site?.DefaultRobots, "index,follow"),

            OgTitle = resolvedTitle,
            OgDescription = resolvedDescription,
            OgImageUrl = imageUrl,
            OgType = "website",

            TwitterCard = FirstNonEmpty(site?.DefaultTwitterCard, "summary_large_image"),
            TwitterSite = site?.DefaultTwitterSite,
            TwitterTitle = resolvedTitle,
            TwitterDescription = resolvedDescription,
            TwitterImageUrl = imageUrl,

            SchemaJsonLd = BuildCollectionPageSchemaJsonLd(
                pageTitle: resolvedTitle ?? "Tin tức",
                pageDescription: resolvedDescription,
                canonicalUrl: canonicalUrl,
                baseUrl: normalizedBaseUrl,
                site: site,
                tenantCode: tenantCode)
        };
    }

    public CmsResolvedSeoMeta BuildForCategory(
        string categoryName,
        string categorySlug,
        SiteSetting? site,
        string baseUrl,
        string? description = null)
    {
        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
        var canonicalUrl = ResolveCanonicalUrl(null, normalizedBaseUrl, $"/tin-tuc/chuyen-muc/{categorySlug}");
        var imageUrl = ResolveImageUrl(site?.DefaultOgImageUrl, null, normalizedBaseUrl);

        var title = $"{categoryName} - Tin tức - {site?.SiteName ?? "TicketBooking"}";
        var desc = FirstNonEmpty(description, $"Các bài viết thuộc chuyên mục {categoryName}.");

        return new CmsResolvedSeoMeta
        {
            Title = title,
            Description = desc,
            CanonicalUrl = canonicalUrl,
            Robots = FirstNonEmpty(site?.DefaultRobots, "index,follow"),

            OgTitle = title,
            OgDescription = desc,
            OgImageUrl = imageUrl,
            OgType = "website",

            TwitterCard = FirstNonEmpty(site?.DefaultTwitterCard, "summary_large_image"),
            TwitterSite = site?.DefaultTwitterSite,
            TwitterTitle = title,
            TwitterDescription = desc,
            TwitterImageUrl = imageUrl,

            SchemaJsonLd = BuildCollectionPageSchemaJsonLd(
                pageTitle: title,
                pageDescription: desc,
                canonicalUrl: canonicalUrl,
                baseUrl: normalizedBaseUrl,
                site: site,
                tenantCode: null)
        };
    }

    public CmsResolvedSeoMeta BuildForTag(
        string tagName,
        string tagSlug,
        SiteSetting? site,
        string baseUrl,
        string? description = null)
    {
        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
        var canonicalUrl = ResolveCanonicalUrl(null, normalizedBaseUrl, $"/tin-tuc/the/{tagSlug}");
        var imageUrl = ResolveImageUrl(site?.DefaultOgImageUrl, null, normalizedBaseUrl);

        var title = $"#{tagName} - Tin tức - {site?.SiteName ?? "TicketBooking"}";
        var desc = FirstNonEmpty(description, $"Các bài viết gắn thẻ {tagName}.");

        return new CmsResolvedSeoMeta
        {
            Title = title,
            Description = desc,
            CanonicalUrl = canonicalUrl,
            Robots = FirstNonEmpty(site?.DefaultRobots, "index,follow"),

            OgTitle = title,
            OgDescription = desc,
            OgImageUrl = imageUrl,
            OgType = "website",

            TwitterCard = FirstNonEmpty(site?.DefaultTwitterCard, "summary_large_image"),
            TwitterSite = site?.DefaultTwitterSite,
            TwitterTitle = title,
            TwitterDescription = desc,
            TwitterImageUrl = imageUrl,

            SchemaJsonLd = BuildCollectionPageSchemaJsonLd(
                pageTitle: title,
                pageDescription: desc,
                canonicalUrl: canonicalUrl,
                baseUrl: normalizedBaseUrl,
                site: site,
                tenantCode: null)
        };
    }

    public string ResolveCanonicalUrl(string? explicitCanonicalUrl, string baseUrl, string relativePath)
    {
        if (!string.IsNullOrWhiteSpace(explicitCanonicalUrl))
            return MakeAbsoluteUrl(explicitCanonicalUrl.Trim(), NormalizeBaseUrl(baseUrl));

        return MakeAbsoluteUrl(relativePath, NormalizeBaseUrl(baseUrl));
    }

    public string? ResolveImageUrl(string? explicitImageUrl, string? fallbackImageUrl, string baseUrl)
    {
        var value = FirstNonEmpty(explicitImageUrl, fallbackImageUrl);
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return MakeAbsoluteUrl(value!, NormalizeBaseUrl(baseUrl));
    }

    public string BuildWebsiteSchemaJsonLd(SiteSetting? site, string baseUrl)
    {
        if (!string.IsNullOrWhiteSpace(site?.DefaultSchemaJsonLd))
            return site!.DefaultSchemaJsonLd!.Trim();

        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
        var schema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["name"] = site?.SiteName ?? "TicketBooking",
            ["url"] = normalizedBaseUrl
        };

        if (!string.IsNullOrWhiteSpace(site?.BrandLogoUrl))
        {
            var brandLogoUrl = site!.BrandLogoUrl!;
            schema["publisher"] = new Dictionary<string, object?>
            {
                ["@type"] = "Organization",
                ["name"] = site?.SiteName ?? "TicketBooking",
                ["logo"] = new Dictionary<string, object?>
                {
                    ["@type"] = "ImageObject",
                    ["url"] = ResolveImageUrl(brandLogoUrl, null, normalizedBaseUrl)
                }
            };
        }

        return SerializeJson(schema);
    }

    public string BuildArticleSchemaJsonLd(
        NewsPost post,
        SiteSetting? site,
        string baseUrl,
        string canonicalUrl,
        string? imageUrl)
    {
        var article = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Article",
            ["headline"] = FirstNonEmpty(post.SeoTitle, post.Title),
            ["description"] = FirstNonEmpty(post.SeoDescription, post.Summary),
            ["url"] = canonicalUrl,
            ["mainEntityOfPage"] = canonicalUrl,
            ["datePublished"] = post.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            ["dateModified"] = (post.UpdatedAt ?? post.LastEditedAt ?? post.CreatedAt).ToString("yyyy-MM-ddTHH:mm:sszzz"),
            ["author"] = BuildPersonOrOrganization(post.AuthorUserId, site),
            ["publisher"] = BuildPublisher(site, baseUrl)
        };

        if (!string.IsNullOrWhiteSpace(imageUrl))
            article["image"] = new[] { imageUrl };

        if (post.ReadingTimeMinutes > 0)
            article["timeRequired"] = $"PT{post.ReadingTimeMinutes}M";

        if (post.WordCount > 0)
            article["wordCount"] = post.WordCount;

        return SerializeJson(article);
    }

    private static object BuildPublisher(SiteSetting? site, string baseUrl)
    {
        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
        var publisher = new Dictionary<string, object?>
        {
            ["@type"] = "Organization",
            ["name"] = site?.SiteName ?? "TicketBooking",
            ["url"] = normalizedBaseUrl
        };

        var logoUrl = !string.IsNullOrWhiteSpace(site?.BrandLogoUrl)
            ? MakeAbsoluteUrl(site!.BrandLogoUrl!, normalizedBaseUrl)
            : null;

        if (!string.IsNullOrWhiteSpace(logoUrl))
        {
            publisher["logo"] = new Dictionary<string, object?>
            {
                ["@type"] = "ImageObject",
                ["url"] = logoUrl
            };
        }

        return publisher;
    }

    private static object BuildPersonOrOrganization(Guid? authorUserId, SiteSetting? site)
    {
        if (authorUserId.HasValue && authorUserId.Value != Guid.Empty)
        {
            return new Dictionary<string, object?>
            {
                ["@type"] = "Person",
                ["identifier"] = authorUserId.Value.ToString()
            };
        }

        return new Dictionary<string, object?>
        {
            ["@type"] = "Organization",
            ["name"] = site?.SiteName ?? "TicketBooking"
        };
    }

    private static string BuildCollectionPageSchemaJsonLd(
        string pageTitle,
        string? pageDescription,
        string canonicalUrl,
        string baseUrl,
        SiteSetting? site,
        string? tenantCode)
    {
        var schema = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "CollectionPage",
            ["name"] = pageTitle,
            ["description"] = pageDescription,
            ["url"] = canonicalUrl,
            ["isPartOf"] = new Dictionary<string, object?>
            {
                ["@type"] = "WebSite",
                ["name"] = site?.SiteName ?? "TicketBooking",
                ["url"] = NormalizeBaseUrl(baseUrl)
            }
        };

        if (!string.IsNullOrWhiteSpace(tenantCode))
            schema["identifier"] = tenantCode;

        return SerializeJson(schema);
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return "http://localhost";

        return baseUrl.Trim().TrimEnd('/');
    }

    private static string MakeAbsoluteUrl(string urlOrPath, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(urlOrPath))
            return baseUrl;

        if (Uri.TryCreate(urlOrPath, UriKind.Absolute, out var absolute))
            return absolute.ToString();

        if (!urlOrPath.StartsWith('/'))
            urlOrPath = "/" + urlOrPath;

        return $"{baseUrl}{urlOrPath}";
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }

        return null;
    }

    private static string? NullIfWhite(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string SerializeJson(object obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }
}

public sealed class CmsResolvedSeoMeta
{
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? Keywords { get; set; }
    public string CanonicalUrl { get; set; } = "";
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
}
