using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Xml.Linq;
using TicketBooking.Api.Services.Cms;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
public sealed class CmsPublicController : ControllerBase
{
    private readonly ICmsPublicQueryService _queries;

    public CmsPublicController(ICmsPublicQueryService queries)
    {
        _queries = queries;
    }

    [HttpGet("api/v{version:apiVersion}/cms/resolve-redirect")]
    public async Task<IActionResult> ResolveRedirect(
        [FromQuery] string tenantCode,
        [FromQuery] string fromPath,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return BadRequest(new { message = "tenantCode is required." });

        if (string.IsNullOrWhiteSpace(fromPath))
            return BadRequest(new { message = "fromPath is required." });

        var redirect = await _queries.ResolveRedirectAsync(tenantCode, fromPath, ct);
        if (redirect is null)
            return NotFound(new { message = "Redirect not found." });

        if (string.IsNullOrWhiteSpace(redirect.ToPath))
            return NotFound(new { message = "Redirect target is empty." });

        Response.Headers.Location = redirect.ToPath.Trim();
        return StatusCode((int)redirect.StatusCode);
    }

    [HttpGet("api/v{version:apiVersion}/cms/site-info")]
    public async Task<IActionResult> GetSiteInfo(
        [FromQuery] string tenantCode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return BadRequest(new { message = "tenantCode is required." });

        var site = await _queries.GetSiteInfoAsync(tenantCode, ct);
        return site is null
            ? NotFound(new { message = "Tenant not found." })
            : Ok(site);
    }

    [HttpGet("api/v{version:apiVersion}/cms/news-index")]
    public async Task<IActionResult> GetNewsIndex(
        [FromQuery] string tenantCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return BadRequest(new { message = "tenantCode is required." });

        var result = await _queries.GetNewsIndexAsync(
            tenantCode,
            ResolveBaseUrl(null),
            page,
            pageSize,
            q,
            ct);

        return result is null
            ? NotFound(new { message = "Tenant not found." })
            : Ok(result);
    }

    [HttpGet("api/v{version:apiVersion}/cms/posts")]
    public Task<IActionResult> ListPosts(
        [FromQuery] string tenantCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? q = null,
        CancellationToken ct = default)
        => GetNewsIndex(tenantCode, page, pageSize, q, ct);

    [HttpGet("api/v{version:apiVersion}/cms/posts/{slug}")]
    public async Task<IActionResult> GetPostBySlug(
        [FromRoute] string slug,
        [FromQuery] string tenantCode,
        [FromQuery] bool includeContent = true,
        [FromQuery] bool bumpView = false,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return BadRequest(new { message = "tenantCode is required." });

        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest(new { message = "slug is required." });

        var result = await _queries.GetPublishedPostBySlugAsync(
            tenantCode,
            slug,
            ResolveBaseUrl(null),
            bumpView,
            ct);

        if (result is null)
            return NotFound(new { message = "Post not found." });

        if (!includeContent)
        {
            result.Post.ContentMarkdown = string.Empty;
            result.Post.ContentHtml = string.Empty;
        }

        return Ok(result);
    }

    [HttpGet("api/v{version:apiVersion}/cms/categories")]
    public async Task<IActionResult> ListCategories(
        [FromQuery] string tenantCode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return BadRequest(new { message = "tenantCode is required." });

        var items = await _queries.ListCategoriesAsync(tenantCode, ct);
        return Ok(new { items });
    }

    [HttpGet("api/v{version:apiVersion}/cms/categories/{slug}")]
    public async Task<IActionResult> GetCategoryPage(
        [FromRoute] string slug,
        [FromQuery] string tenantCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return BadRequest(new { message = "tenantCode is required." });

        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest(new { message = "slug is required." });

        var result = await _queries.GetCategoryPageAsync(
            tenantCode,
            slug,
            ResolveBaseUrl(null),
            page,
            pageSize,
            ct);

        return result is null
            ? NotFound(new { message = "Category not found." })
            : Ok(result);
    }

    [HttpGet("api/v{version:apiVersion}/cms/tags")]
    public async Task<IActionResult> ListTags(
        [FromQuery] string tenantCode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return BadRequest(new { message = "tenantCode is required." });

        var items = await _queries.ListTagsAsync(tenantCode, ct);
        return Ok(new { items });
    }

    [HttpGet("api/v{version:apiVersion}/cms/tags/{slug}")]
    public async Task<IActionResult> GetTagPage(
        [FromRoute] string slug,
        [FromQuery] string tenantCode,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return BadRequest(new { message = "tenantCode is required." });

        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest(new { message = "slug is required." });

        var result = await _queries.GetTagPageAsync(
            tenantCode,
            slug,
            ResolveBaseUrl(null),
            page,
            pageSize,
            ct);

        return result is null
            ? NotFound(new { message = "Tag not found." })
            : Ok(result);
    }

    [HttpGet("robots.txt")]
    public async Task<IActionResult> Robots([FromQuery] string tenantCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return Content("User-agent: *\nDisallow: /", "text/plain", Encoding.UTF8);

        var site = await _queries.GetSiteInfoAsync(tenantCode, ct);
        if (site is null)
            return Content("User-agent: *\nDisallow: /", "text/plain", Encoding.UTF8);

        var baseUrl = ResolveBaseUrl(site.SiteUrl);
        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Disallow: /api/");
        sb.AppendLine("Disallow: /swagger/");
        sb.AppendLine("Disallow: /admin/");
        sb.AppendLine();
        sb.AppendLine($"Sitemap: {baseUrl}/sitemap.xml?tenantCode={Uri.EscapeDataString(tenantCode)}");

        return Content(sb.ToString(), "text/plain", Encoding.UTF8);
    }

    [HttpGet("sitemap.xml")]
    public async Task<IActionResult> Sitemap([FromQuery] string tenantCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return BadRequest(new { message = "tenantCode is required." });

        var items = await _queries.GetSitemapItemsAsync(tenantCode, ResolveBaseUrl(null), ct);
        if (items.Count == 0)
            return NotFound(new { message = "Tenant not found." });

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urlset = new XElement(ns + "urlset");

        foreach (var item in items)
        {
            urlset.Add(new XElement(ns + "url",
                new XElement(ns + "loc", item.Url),
                new XElement(ns + "lastmod", item.LastModifiedAt.ToString("yyyy-MM-ddTHH:mm:sszzz")),
                new XElement(ns + "changefreq", item.ChangeFrequency),
                new XElement(ns + "priority", item.Priority)));
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlset);
        return Content(doc.ToString(SaveOptions.DisableFormatting), "application/xml", Encoding.UTF8);
    }

    [HttpGet("rss.xml")]
    public async Task<IActionResult> Rss([FromQuery] string tenantCode, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tenantCode))
            return BadRequest(new { message = "tenantCode is required." });

        var site = await _queries.GetSiteInfoAsync(tenantCode, ct);
        if (site is null)
            return NotFound(new { message = "Tenant not found." });

        var baseUrl = ResolveBaseUrl(site.SiteUrl);
        var posts = await _queries.GetRssItemsAsync(tenantCode, baseUrl, 30, ct);

        XNamespace atom = "http://www.w3.org/2005/Atom";
        var channel = new XElement("channel",
            new XElement("title", $"{site.SiteName} - Tin tức"),
            new XElement("link", $"{baseUrl}/tin-tuc"),
            new XElement("description", $"{site.SiteName} news feed"),
            new XElement("language", "vi-VN"),
            new XElement("lastBuildDate", DateTimeOffset.Now.ToString("r")),
            new XElement(atom + "link",
                new XAttribute("href", $"{baseUrl}/rss.xml?tenantCode={Uri.EscapeDataString(tenantCode)}"),
                new XAttribute("rel", "self"),
                new XAttribute("type", "application/rss+xml"))
        );

        foreach (var post in posts)
        {
            channel.Add(new XElement("item",
                new XElement("title", post.Title),
                new XElement("link", post.Link),
                new XElement("guid", post.Link),
                new XElement("pubDate", post.PublishedAt.ToString("r")),
                new XElement("description", post.Summary ?? string.Empty)
            ));
        }

        var rss = new XElement("rss",
            new XAttribute("version", "2.0"),
            new XAttribute(XNamespace.Xmlns + "atom", atom),
            channel);

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), rss);
        return Content(doc.ToString(SaveOptions.DisableFormatting), "application/xml", Encoding.UTF8);
    }

    private string ResolveBaseUrl(string? configuredSiteUrl)
    {
        if (!string.IsNullOrWhiteSpace(configuredSiteUrl))
            return configuredSiteUrl.Trim().TrimEnd('/');

        var scheme = Request.Scheme;
        var host = Request.Host.Value;
        if (string.IsNullOrWhiteSpace(host))
            return "http://localhost";

        return $"{scheme}://{host}";
    }
}
