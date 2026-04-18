// FILE #318: TicketBooking.Api/Controllers/Cms/Manager/CmsManagerPostsController.cs
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
[Route("api/v{version:apiVersion}/manager/cms/posts")]
[Authorize(Roles = "Admin,QLNX,QLVT,QLVMM,QLKS,QLTour")]
public sealed class CmsManagerPostsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public CmsManagerPostsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q = null,
        [FromQuery] NewsPostStatus? status = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        var tenantId = _tenant.TenantId.Value;

        IQueryable<NewsPost> query = _db.Set<NewsPost>().AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Title.Contains(keyword) ||
                x.Slug.Contains(keyword) ||
                (x.Summary != null && x.Summary.Contains(keyword)) ||
                (x.SeoKeywords != null && x.SeoKeywords.Contains(keyword)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.PublishedAt ?? x.ScheduledAt ?? x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.Title,
                x.Slug,
                x.Status,
                x.Summary,
                x.CoverMediaAssetId,
                x.CoverImageUrl,
                x.ScheduledAt,
                x.PublishedAt,
                x.UnpublishedAt,
                x.AuthorUserId,
                x.ViewCount,
                x.WordCount,
                x.ReadingTimeMinutes,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        var authorIds = items
            .Where(x => x.AuthorUserId.HasValue)
            .Select(x => x.AuthorUserId!.Value)
            .Distinct()
            .ToList();

        var authorNames = authorIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users
                .AsNoTracking()
                .Where(x => authorIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FullName ?? x.UserName ?? x.Email ?? "Admin", ct);

        var postIds = items.Select(x => x.Id).ToList();
        Dictionary<Guid, (string Name, string Slug)> primaryCategories;
        if (postIds.Count == 0)
        {
            primaryCategories = new Dictionary<Guid, (string Name, string Slug)>();
        }
        else
        {
            var categoryRows = await _db.Set<NewsPostCategory>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && postIds.Contains(x.PostId) && !x.IsDeleted)
                .Join(
                    _db.Set<NewsCategory>().AsNoTracking(),
                    map => map.CategoryId,
                    category => category.Id,
                    (map, category) => new
                    {
                        map.PostId,
                        category.Name,
                        category.Slug,
                        category.SortOrder
                    })
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .ToListAsync(ct);

            primaryCategories = categoryRows
                .GroupBy(x => x.PostId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var first = group.First();
                        return (first.Name, first.Slug);
                    });
        }

        var resultItems = items.Select(x => new
        {
            x.Id,
            x.TenantId,
            x.Title,
            x.Slug,
            x.Status,
            x.Summary,
            x.CoverMediaAssetId,
            x.CoverImageUrl,
            x.ScheduledAt,
            x.PublishedAt,
            x.UnpublishedAt,
            x.ViewCount,
            x.WordCount,
            x.ReadingTimeMinutes,
            x.IsDeleted,
            x.CreatedAt,
            x.UpdatedAt,
            AuthorName = x.AuthorUserId.HasValue && authorNames.TryGetValue(x.AuthorUserId.Value, out var authorName)
                ? authorName
                : null,
            PrimaryCategoryName = primaryCategories.TryGetValue(x.Id, out var category) ? category.Name : null,
            PrimaryCategorySlug = primaryCategories.TryGetValue(x.Id, out var categorySlug) ? categorySlug.Slug : null
        });

        return Ok(new { page, pageSize, total, items = resultItems });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;

        IQueryable<NewsPost> query = _db.Set<NewsPost>().AsQueryable();
        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var entity = await query.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (entity is null)
            return NotFound(new { message = "Post not found." });

        var categories = await _db.Set<NewsPostCategory>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PostId == entity.Id && !x.IsDeleted)
            .Join(
                _db.Set<NewsCategory>().AsNoTracking(),
                x => x.CategoryId,
                c => c.Id,
                (x, c) => new
                {
                    c.Id,
                    c.Name,
                    c.Slug,
                    c.SortOrder
                })
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        var tags = await _db.Set<NewsPostTag>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PostId == entity.Id && !x.IsDeleted)
            .Join(
                _db.Set<NewsTag>().AsNoTracking(),
                x => x.TagId,
                t => t.Id,
                (x, t) => new
                {
                    t.Id,
                    t.Name,
                    t.Slug
                })
            .OrderBy(x => x.Name)
            .ToListAsync(ct);

        return Ok(new
        {
            entity.Id,
            entity.TenantId,
            entity.Title,
            entity.Slug,
            entity.Summary,
            entity.ContentMarkdown,
            entity.ContentHtml,
            entity.CoverMediaAssetId,
            entity.CoverImageUrl,

            entity.SeoTitle,
            entity.SeoDescription,
            entity.SeoKeywords,
            entity.CanonicalUrl,
            entity.Robots,

            entity.OgTitle,
            entity.OgDescription,
            entity.OgImageUrl,
            entity.OgType,

            entity.TwitterCard,
            entity.TwitterSite,
            entity.TwitterCreator,
            entity.TwitterTitle,
            entity.TwitterDescription,
            entity.TwitterImageUrl,

            entity.SchemaJsonLd,

            entity.Status,
            entity.ScheduledAt,
            entity.PublishedAt,
            entity.UnpublishedAt,

            entity.AuthorUserId,
            entity.EditorUserId,
            entity.LastEditedAt,

            entity.ViewCount,
            entity.WordCount,
            entity.ReadingTimeMinutes,

            entity.IsDeleted,
            entity.CreatedAt,
            entity.CreatedByUserId,
            entity.UpdatedAt,
            entity.UpdatedByUserId,

            Categories = categories,
            Tags = tags
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CmsManagerUpsertPostRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var validation = ValidateUpsertRequest(req);
        if (validation is not null)
            return validation;

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();
        var slug = NormalizeSlug(req.Slug);

        var exists = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Slug == slug, ct);

        if (exists)
            return Conflict(new { message = "Slug already exists in this tenant." });

        var refsValidation = await ValidateReferencesAsync(tenantId, req, ct);
        if (refsValidation is not null)
            return refsValidation;

        var wordCount = CountWords(req.ContentMarkdown);
        var readingMinutes = EstimateReadingMinutes(wordCount);
        var status = DeriveStatus(req.ScheduledAt, req.PublishedAt, req.UnpublishedAt);

        var entity = new NewsPost
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,

            Title = req.Title.Trim(),
            Slug = slug,
            Summary = NullIfWhite(req.Summary),

            ContentMarkdown = req.ContentMarkdown,
            ContentHtml = req.ContentHtml,

            CoverMediaAssetId = req.CoverMediaAssetId,
            CoverImageUrl = NullIfWhite(req.CoverImageUrl),

            SeoTitle = NullIfWhite(req.SeoTitle),
            SeoDescription = NullIfWhite(req.SeoDescription),
            SeoKeywords = NullIfWhite(req.SeoKeywords),
            CanonicalUrl = NullIfWhite(req.CanonicalUrl),
            Robots = NullIfWhite(req.Robots),

            OgTitle = NullIfWhite(req.OgTitle),
            OgDescription = NullIfWhite(req.OgDescription),
            OgImageUrl = NullIfWhite(req.OgImageUrl),
            OgType = string.IsNullOrWhiteSpace(req.OgType) ? "article" : req.OgType.Trim(),

            TwitterCard = string.IsNullOrWhiteSpace(req.TwitterCard) ? "summary_large_image" : req.TwitterCard.Trim(),
            TwitterSite = NullIfWhite(req.TwitterSite),
            TwitterCreator = NullIfWhite(req.TwitterCreator),
            TwitterTitle = NullIfWhite(req.TwitterTitle),
            TwitterDescription = NullIfWhite(req.TwitterDescription),
            TwitterImageUrl = NullIfWhite(req.TwitterImageUrl),

            SchemaJsonLd = NullIfWhite(req.SchemaJsonLd),

            Status = status,
            ScheduledAt = req.ScheduledAt,
            PublishedAt = req.PublishedAt,
            UnpublishedAt = req.UnpublishedAt,

            AuthorUserId = req.AuthorUserId ?? userId,
            EditorUserId = userId,
            LastEditedAt = now,

            ViewCount = 0,
            WordCount = wordCount,
            ReadingTimeMinutes = readingMinutes,

            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId
        };

        _db.Set<NewsPost>().Add(entity);

        await UpsertMappingsAsync(entity, req.CategoryIds, req.TagIds, now, userId, ct);
        await CreateRevisionAsync(entity, 1, userId, now, req.ChangeNote ?? "Initial version", ct);

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CmsManagerUpsertPostRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var validation = ValidateUpsertRequest(req);
        if (validation is not null)
            return validation;

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();
        var slug = NormalizeSlug(req.Slug);

        var entity = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Post not found." });

        var exists = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Slug == slug && x.Id != id, ct);

        if (exists)
            return Conflict(new { message = "Slug already exists in this tenant." });

        var refsValidation = await ValidateReferencesAsync(tenantId, req, ct);
        if (refsValidation is not null)
            return refsValidation;

        var wordCount = CountWords(req.ContentMarkdown);
        var readingMinutes = EstimateReadingMinutes(wordCount);

        entity.Title = req.Title.Trim();
        entity.Slug = slug;
        entity.Summary = NullIfWhite(req.Summary);

        entity.ContentMarkdown = req.ContentMarkdown;
        entity.ContentHtml = req.ContentHtml;

        entity.CoverMediaAssetId = req.CoverMediaAssetId;
        entity.CoverImageUrl = NullIfWhite(req.CoverImageUrl);

        entity.SeoTitle = NullIfWhite(req.SeoTitle);
        entity.SeoDescription = NullIfWhite(req.SeoDescription);
        entity.SeoKeywords = NullIfWhite(req.SeoKeywords);
        entity.CanonicalUrl = NullIfWhite(req.CanonicalUrl);
        entity.Robots = NullIfWhite(req.Robots);

        entity.OgTitle = NullIfWhite(req.OgTitle);
        entity.OgDescription = NullIfWhite(req.OgDescription);
        entity.OgImageUrl = NullIfWhite(req.OgImageUrl);
        entity.OgType = string.IsNullOrWhiteSpace(req.OgType) ? "article" : req.OgType.Trim();

        entity.TwitterCard = string.IsNullOrWhiteSpace(req.TwitterCard) ? "summary_large_image" : req.TwitterCard.Trim();
        entity.TwitterSite = NullIfWhite(req.TwitterSite);
        entity.TwitterCreator = NullIfWhite(req.TwitterCreator);
        entity.TwitterTitle = NullIfWhite(req.TwitterTitle);
        entity.TwitterDescription = NullIfWhite(req.TwitterDescription);
        entity.TwitterImageUrl = NullIfWhite(req.TwitterImageUrl);

        entity.SchemaJsonLd = NullIfWhite(req.SchemaJsonLd);

        entity.ScheduledAt = req.ScheduledAt;
        entity.PublishedAt = req.PublishedAt;
        entity.UnpublishedAt = req.UnpublishedAt;
        entity.Status = DeriveStatus(req.ScheduledAt, req.PublishedAt, req.UnpublishedAt);

        if (req.AuthorUserId.HasValue)
            entity.AuthorUserId = req.AuthorUserId;

        entity.EditorUserId = userId;
        entity.LastEditedAt = now;

        entity.WordCount = wordCount;
        entity.ReadingTimeMinutes = readingMinutes;

        if (entity.IsDeleted)
            entity.IsDeleted = false;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await UpsertMappingsAsync(entity, req.CategoryIds, req.TagIds, now, userId, ct);

        var nextVersion = await GetNextRevisionVersionAsync(tenantId, entity.Id, ct);
        await CreateRevisionAsync(entity, nextVersion, userId, now, req.ChangeNote ?? $"Revision {nextVersion}", ct);

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Post not found." });

        entity.IsDeleted = true;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Post not found." });

        var dup = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Slug == entity.Slug && x.Id != id && !x.IsDeleted, ct);

        if (dup)
            return Conflict(new { message = "Cannot restore because another active post already uses the same slug." });

        entity.IsDeleted = false;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, [FromBody] CmsManagerPostActionRequest? req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Post not found." });

        if (string.IsNullOrWhiteSpace(entity.Title) ||
            string.IsNullOrWhiteSpace(entity.Slug) ||
            string.IsNullOrWhiteSpace(entity.ContentMarkdown) ||
            string.IsNullOrWhiteSpace(entity.ContentHtml))
        {
            return BadRequest(new { message = "Post must have Title, Slug, ContentMarkdown, and ContentHtml before publishing." });
        }

        entity.PublishedAt = now;
        entity.ScheduledAt = null;
        entity.UnpublishedAt = null;
        entity.Status = NewsPostStatus.Published;
        entity.EditorUserId = userId;
        entity.LastEditedAt = now;
        entity.IsDeleted = false;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        var nextVersion = await GetNextRevisionVersionAsync(tenantId, entity.Id, ct);
        await CreateRevisionAsync(entity, nextVersion, userId, now, req?.ChangeNote ?? "Publish", ct);

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id, [FromBody] CmsManagerPostActionRequest? req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Post not found." });

        entity.UnpublishedAt = now;
        entity.Status = NewsPostStatus.Unpublished;
        entity.EditorUserId = userId;
        entity.LastEditedAt = now;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        var nextVersion = await GetNextRevisionVersionAsync(tenantId, entity.Id, ct);
        await CreateRevisionAsync(entity, nextVersion, userId, now, req?.ChangeNote ?? "Unpublish", ct);

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/schedule")]
    public async Task<IActionResult> Schedule(Guid id, [FromBody] CmsManagerSchedulePostRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        if (req is null || !req.ScheduledAt.HasValue)
            return BadRequest(new { message = "ScheduledAt is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Post not found." });

        entity.ScheduledAt = req.ScheduledAt;
        entity.PublishedAt = null;
        entity.UnpublishedAt = null;
        entity.Status = NewsPostStatus.Scheduled;
        entity.EditorUserId = userId;
        entity.LastEditedAt = now;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        var nextVersion = await GetNextRevisionVersionAsync(tenantId, entity.Id, ct);
        await CreateRevisionAsync(entity, nextVersion, userId, now, req.ChangeNote ?? "Schedule", ct);

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, [FromBody] CmsManagerPostActionRequest? req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Post not found." });

        entity.Status = NewsPostStatus.Archived;
        entity.EditorUserId = userId;
        entity.LastEditedAt = now;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        var nextVersion = await GetNextRevisionVersionAsync(tenantId, entity.Id, ct);
        await CreateRevisionAsync(entity, nextVersion, userId, now, req?.ChangeNote ?? "Archive", ct);

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private IActionResult? ValidateUpsertRequest(CmsManagerUpsertPostRequest req)
    {
        if (req is null)
            return BadRequest(new { message = "Request body is required." });

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { message = "Title is required." });

        if (string.IsNullOrWhiteSpace(req.Slug))
            return BadRequest(new { message = "Slug is required." });

        if (string.IsNullOrWhiteSpace(req.ContentMarkdown))
            return BadRequest(new { message = "ContentMarkdown is required." });

        if (string.IsNullOrWhiteSpace(req.ContentHtml))
            return BadRequest(new { message = "ContentHtml is required." });

        if (req.Title.Trim().Length > 300)
            return BadRequest(new { message = "Title max length is 300." });

        if (NormalizeSlug(req.Slug).Length > 300)
            return BadRequest(new { message = "Slug max length is 300." });

        if (req.Summary is not null && req.Summary.Trim().Length > 2000)
            return BadRequest(new { message = "Summary max length is 2000." });

        if (req.SeoTitle is not null && req.SeoTitle.Trim().Length > 300)
            return BadRequest(new { message = "SeoTitle max length is 300." });

        if (req.SeoDescription is not null && req.SeoDescription.Trim().Length > 2000)
            return BadRequest(new { message = "SeoDescription max length is 2000." });

        if (req.SeoKeywords is not null && req.SeoKeywords.Trim().Length > 2000)
            return BadRequest(new { message = "SeoKeywords max length is 2000." });

        if (req.CanonicalUrl is not null && req.CanonicalUrl.Trim().Length > 1000)
            return BadRequest(new { message = "CanonicalUrl max length is 1000." });

        if (req.Robots is not null && req.Robots.Trim().Length > 200)
            return BadRequest(new { message = "Robots max length is 200." });

        if (req.OgTitle is not null && req.OgTitle.Trim().Length > 300)
            return BadRequest(new { message = "OgTitle max length is 300." });

        if (req.OgDescription is not null && req.OgDescription.Trim().Length > 2000)
            return BadRequest(new { message = "OgDescription max length is 2000." });

        if (req.OgImageUrl is not null && req.OgImageUrl.Trim().Length > 1000)
            return BadRequest(new { message = "OgImageUrl max length is 1000." });

        if (req.OgType is not null && req.OgType.Trim().Length > 50)
            return BadRequest(new { message = "OgType max length is 50." });

        if (req.TwitterCard is not null && req.TwitterCard.Trim().Length > 50)
            return BadRequest(new { message = "TwitterCard max length is 50." });

        if (req.TwitterSite is not null && req.TwitterSite.Trim().Length > 100)
            return BadRequest(new { message = "TwitterSite max length is 100." });

        if (req.TwitterCreator is not null && req.TwitterCreator.Trim().Length > 100)
            return BadRequest(new { message = "TwitterCreator max length is 100." });

        if (req.TwitterTitle is not null && req.TwitterTitle.Trim().Length > 300)
            return BadRequest(new { message = "TwitterTitle max length is 300." });

        if (req.TwitterDescription is not null && req.TwitterDescription.Trim().Length > 2000)
            return BadRequest(new { message = "TwitterDescription max length is 2000." });

        if (req.TwitterImageUrl is not null && req.TwitterImageUrl.Trim().Length > 1000)
            return BadRequest(new { message = "TwitterImageUrl max length is 1000." });

        return null;
    }

    private async Task<IActionResult?> ValidateReferencesAsync(Guid tenantId, CmsManagerUpsertPostRequest req, CancellationToken ct)
    {
        if (req.CoverMediaAssetId.HasValue)
        {
            var mediaExists = await _db.Set<MediaAsset>()
                .IgnoreQueryFilters()
                .AnyAsync(x => x.Id == req.CoverMediaAssetId.Value && x.TenantId == tenantId && !x.IsDeleted, ct);

            if (!mediaExists)
                return BadRequest(new { message = "CoverMediaAssetId is invalid for this tenant." });
        }

        if (req.CategoryIds is not null && req.CategoryIds.Count > 0)
        {
            var ids = req.CategoryIds.Where(x => x != Guid.Empty).Distinct().ToList();
            if (ids.Count != req.CategoryIds.Count)
                return BadRequest(new { message = "CategoryIds contains invalid or duplicate values." });

            var count = await _db.Set<NewsCategory>()
                .IgnoreQueryFilters()
                .CountAsync(x => x.TenantId == tenantId && ids.Contains(x.Id) && !x.IsDeleted, ct);

            if (count != ids.Count)
                return BadRequest(new { message = "One or more CategoryIds are invalid for this tenant." });
        }

        if (req.TagIds is not null && req.TagIds.Count > 0)
        {
            var ids = req.TagIds.Where(x => x != Guid.Empty).Distinct().ToList();
            if (ids.Count != req.TagIds.Count)
                return BadRequest(new { message = "TagIds contains invalid or duplicate values." });

            var count = await _db.Set<NewsTag>()
                .IgnoreQueryFilters()
                .CountAsync(x => x.TenantId == tenantId && ids.Contains(x.Id) && !x.IsDeleted, ct);

            if (count != ids.Count)
                return BadRequest(new { message = "One or more TagIds are invalid for this tenant." });
        }

        return null;
    }

    private async Task UpsertMappingsAsync(
        NewsPost post,
        List<Guid>? categoryIds,
        List<Guid>? tagIds,
        DateTimeOffset now,
        Guid? userId,
        CancellationToken ct)
    {
        if (categoryIds is not null)
        {
            var existing = await _db.Set<NewsPostCategory>()
                .IgnoreQueryFilters()
                .Where(x => x.TenantId == post.TenantId && x.PostId == post.Id)
                .ToListAsync(ct);

            foreach (var row in existing)
            {
                row.IsDeleted = true;
                row.UpdatedAt = now;
                row.UpdatedByUserId = userId;
            }

            foreach (var categoryId in categoryIds.Distinct())
            {
                var row = existing.FirstOrDefault(x => x.CategoryId == categoryId);
                if (row is null)
                {
                    _db.Set<NewsPostCategory>().Add(new NewsPostCategory
                    {
                        Id = Guid.NewGuid(),
                        TenantId = post.TenantId,
                        PostId = post.Id,
                        CategoryId = categoryId,
                        IsDeleted = false,
                        CreatedAt = now,
                        CreatedByUserId = userId
                    });
                }
                else
                {
                    row.IsDeleted = false;
                    row.UpdatedAt = now;
                    row.UpdatedByUserId = userId;
                }
            }
        }

        if (tagIds is not null)
        {
            var existing = await _db.Set<NewsPostTag>()
                .IgnoreQueryFilters()
                .Where(x => x.TenantId == post.TenantId && x.PostId == post.Id)
                .ToListAsync(ct);

            foreach (var row in existing)
            {
                row.IsDeleted = true;
                row.UpdatedAt = now;
                row.UpdatedByUserId = userId;
            }

            foreach (var tagId in tagIds.Distinct())
            {
                var row = existing.FirstOrDefault(x => x.TagId == tagId);
                if (row is null)
                {
                    _db.Set<NewsPostTag>().Add(new NewsPostTag
                    {
                        Id = Guid.NewGuid(),
                        TenantId = post.TenantId,
                        PostId = post.Id,
                        TagId = tagId,
                        IsDeleted = false,
                        CreatedAt = now,
                        CreatedByUserId = userId
                    });
                }
                else
                {
                    row.IsDeleted = false;
                    row.UpdatedAt = now;
                    row.UpdatedByUserId = userId;
                }
            }
        }
    }

    private async Task<int> GetNextRevisionVersionAsync(Guid tenantId, Guid postId, CancellationToken ct)
    {
        var currentMax = await _db.Set<NewsPostRevision>()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.PostId == postId && !x.IsDeleted)
            .MaxAsync(x => (int?)x.VersionNumber, ct);

        return (currentMax ?? 0) + 1;
    }

    private async Task CreateRevisionAsync(
        NewsPost post,
        int versionNumber,
        Guid? editorUserId,
        DateTimeOffset editedAt,
        string? changeNote,
        CancellationToken ct)
    {
        _db.Set<NewsPostRevision>().Add(new NewsPostRevision
        {
            Id = Guid.NewGuid(),
            TenantId = post.TenantId,
            PostId = post.Id,
            VersionNumber = versionNumber,

            Title = post.Title,
            Summary = post.Summary,
            ContentMarkdown = post.ContentMarkdown,
            ContentHtml = post.ContentHtml,

            SeoTitle = post.SeoTitle,
            SeoDescription = post.SeoDescription,
            CanonicalUrl = post.CanonicalUrl,
            Robots = post.Robots,
            OgTitle = post.OgTitle,
            OgDescription = post.OgDescription,
            OgImageUrl = post.OgImageUrl,
            TwitterCard = post.TwitterCard,
            TwitterTitle = post.TwitterTitle,
            TwitterDescription = post.TwitterDescription,
            TwitterImageUrl = post.TwitterImageUrl,
            SchemaJsonLd = post.SchemaJsonLd,

            ChangeNote = NullIfWhite(changeNote),
            EditorUserId = editorUserId,
            EditedAt = editedAt,

            IsDeleted = false,
            CreatedAt = editedAt,
            CreatedByUserId = editorUserId
        });

        await Task.CompletedTask;
    }

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static NewsPostStatus DeriveStatus(DateTimeOffset? scheduledAt, DateTimeOffset? publishedAt, DateTimeOffset? unpublishedAt)
    {
        if (unpublishedAt.HasValue) return NewsPostStatus.Unpublished;
        if (publishedAt.HasValue) return NewsPostStatus.Published;
        if (scheduledAt.HasValue) return NewsPostStatus.Scheduled;
        return NewsPostStatus.Draft;
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

        var mins = (int)Math.Ceiling(wordCount / 200.0);
        return Math.Max(1, mins);
    }

    private static string NormalizeSlug(string slug)
    {
        slug = (slug ?? "").Trim().ToLowerInvariant();
        if (slug.Length == 0) return "";

        var parts = slug.Split(new[] { ' ', '\t', '\r', '\n', '/' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join('-', parts);
    }

    private static string? NullIfWhite(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CmsManagerUpsertPostRequest
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }

    public string ContentMarkdown { get; set; } = "";
    public string ContentHtml { get; set; } = "";

    public Guid? CoverMediaAssetId { get; set; }
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

    public DateTimeOffset? ScheduledAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? UnpublishedAt { get; set; }

    public Guid? AuthorUserId { get; set; }

    public List<Guid>? CategoryIds { get; set; }
    public List<Guid>? TagIds { get; set; }

    public string? ChangeNote { get; set; }
}

public sealed class CmsManagerPostActionRequest
{
    public string? ChangeNote { get; set; }
}

public sealed class CmsManagerSchedulePostRequest
{
    public DateTimeOffset? ScheduledAt { get; set; }
    public string? ChangeNote { get; set; }
}



