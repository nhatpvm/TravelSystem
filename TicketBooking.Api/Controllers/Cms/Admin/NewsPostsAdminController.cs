// FILE #135 (UPDATE): TicketBooking.Api/Controllers/NewsPostsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using TicketBooking.Domain.Cms;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/cms/posts")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class NewsPostsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public NewsPostsAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private IActionResult RequireTenant()
        => BadRequest(new { message = "X-TenantId is required for admin write requests." });

    private static string NormalizeSlug(string slug)
    {
        slug = (slug ?? "").Trim().ToLowerInvariant();
        if (slug.Length == 0) return "";
        var parts = slug.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join('-', parts);
    }

    private static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        var s = text.Trim();
        var parts = s.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length;
    }

    private static int EstimateReadingMinutes(int wordCount)
    {
        // 200 wpm ~ common estimate
        if (wordCount <= 0) return 0;
        var mins = (int)Math.Ceiling(wordCount / 200.0);
        return Math.Max(1, mins);
    }

    private static NewsPostStatus DeriveStatus(NewsPostStatus current, DateTimeOffset? scheduledAt, DateTimeOffset? publishedAt, DateTimeOffset? unpublishedAt)
    {
        // Keep it deterministic:
        // - UnpublishedAt set -> Unpublished
        // - PublishedAt set -> Published
        // - ScheduledAt set -> Scheduled
        // else Draft
        if (unpublishedAt.HasValue) return NewsPostStatus.Unpublished;
        if (publishedAt.HasValue) return NewsPostStatus.Published;
        if (scheduledAt.HasValue) return NewsPostStatus.Scheduled;
        return NewsPostStatus.Draft;
    }

    public class CreateRequest
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
        public Guid? EditorUserId { get; set; }
        public DateTimeOffset? LastEditedAt { get; set; }

        // optional: map categories/tags
        public List<Guid>? CategoryIds { get; set; }
        public List<Guid>? TagIds { get; set; }

        // optional: revision note
        public string? ChangeNote { get; set; }
    }

    public sealed class UpdateRequest : CreateRequest
    {
        public bool Revise { get; set; } = true; // default: create a revision snapshot on update
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
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 200) pageSize = 200;

        var query = _db.Set<NewsPost>().AsQueryable();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenant.HasTenant && _tenant.TenantId.HasValue)
            query = query.Where(x => x.TenantId == _tenant.TenantId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(x =>
                x.Title.Contains(q) ||
                x.Slug.Contains(q));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.PublishedAt ?? x.ScheduledAt ?? x.CreatedAt)
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
                x.ViewCount,
                x.WordCount,
                x.ReadingTimeMinutes,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        var query = _db.Set<NewsPost>().AsQueryable();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenant.HasTenant && _tenant.TenantId.HasValue)
            query = query.Where(x => x.TenantId == _tenant.TenantId.Value);

        var entity = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound(new { message = "Post not found." });

        var categories = await _db.Set<NewsPostCategory>().IgnoreQueryFilters()
            .Where(x => x.TenantId == entity.TenantId && x.PostId == entity.Id && !x.IsDeleted)
            .Select(x => x.CategoryId)
            .ToListAsync(ct);

        var tags = await _db.Set<NewsPostTag>().IgnoreQueryFilters()
            .Where(x => x.TenantId == entity.TenantId && x.PostId == entity.Id && !x.IsDeleted)
            .Select(x => x.TagId)
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

            CategoryIds = categories,
            TagIds = tags
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { message = "Title is required." });

        if (string.IsNullOrWhiteSpace(req.Slug))
            return BadRequest(new { message = "Slug is required." });

        if (string.IsNullOrWhiteSpace(req.ContentMarkdown))
            return BadRequest(new { message = "ContentMarkdown is required." });

        if (string.IsNullOrWhiteSpace(req.ContentHtml))
            return BadRequest(new { message = "ContentHtml is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var slug = NormalizeSlug(req.Slug);

        var slugExists = await _db.Set<NewsPost>().IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Slug == slug, ct);

        if (slugExists)
            return Conflict(new { message = "Slug already exists in this tenant." });

        // Basic analytics
        var wordCount = CountWords(req.ContentMarkdown);
        var readingMin = EstimateReadingMinutes(wordCount);

        var status = DeriveStatus(NewsPostStatus.Draft, req.ScheduledAt, req.PublishedAt, req.UnpublishedAt);

        var entity = new NewsPost
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,

            Title = req.Title.Trim(),
            Slug = slug,
            Summary = string.IsNullOrWhiteSpace(req.Summary) ? null : req.Summary.Trim(),

            ContentMarkdown = req.ContentMarkdown,
            ContentHtml = req.ContentHtml,

            CoverMediaAssetId = req.CoverMediaAssetId,
            CoverImageUrl = string.IsNullOrWhiteSpace(req.CoverImageUrl) ? null : req.CoverImageUrl.Trim(),

            SeoTitle = string.IsNullOrWhiteSpace(req.SeoTitle) ? null : req.SeoTitle.Trim(),
            SeoDescription = string.IsNullOrWhiteSpace(req.SeoDescription) ? null : req.SeoDescription.Trim(),
            SeoKeywords = string.IsNullOrWhiteSpace(req.SeoKeywords) ? null : req.SeoKeywords.Trim(),
            CanonicalUrl = string.IsNullOrWhiteSpace(req.CanonicalUrl) ? null : req.CanonicalUrl.Trim(),
            Robots = string.IsNullOrWhiteSpace(req.Robots) ? null : req.Robots.Trim(),

            OgTitle = string.IsNullOrWhiteSpace(req.OgTitle) ? null : req.OgTitle.Trim(),
            OgDescription = string.IsNullOrWhiteSpace(req.OgDescription) ? null : req.OgDescription.Trim(),
            OgImageUrl = string.IsNullOrWhiteSpace(req.OgImageUrl) ? null : req.OgImageUrl.Trim(),
            OgType = string.IsNullOrWhiteSpace(req.OgType) ? "article" : req.OgType.Trim(),

            TwitterCard = string.IsNullOrWhiteSpace(req.TwitterCard) ? "summary_large_image" : req.TwitterCard.Trim(),
            TwitterSite = string.IsNullOrWhiteSpace(req.TwitterSite) ? null : req.TwitterSite.Trim(),
            TwitterCreator = string.IsNullOrWhiteSpace(req.TwitterCreator) ? null : req.TwitterCreator.Trim(),
            TwitterTitle = string.IsNullOrWhiteSpace(req.TwitterTitle) ? null : req.TwitterTitle.Trim(),
            TwitterDescription = string.IsNullOrWhiteSpace(req.TwitterDescription) ? null : req.TwitterDescription.Trim(),
            TwitterImageUrl = string.IsNullOrWhiteSpace(req.TwitterImageUrl) ? null : req.TwitterImageUrl.Trim(),

            SchemaJsonLd = string.IsNullOrWhiteSpace(req.SchemaJsonLd) ? null : req.SchemaJsonLd,

            Status = status,
            ScheduledAt = req.ScheduledAt,
            PublishedAt = req.PublishedAt,
            UnpublishedAt = req.UnpublishedAt,

            AuthorUserId = req.AuthorUserId ?? userId,
            EditorUserId = req.EditorUserId ?? userId,
            LastEditedAt = req.LastEditedAt ?? now,

            ViewCount = 0,
            WordCount = wordCount,
            ReadingTimeMinutes = readingMin,

            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId
        };

        _db.Set<NewsPost>().Add(entity);

        // Categories/Tags mappings
        await UpsertMappingsAsync(entity, categoryIds: req.CategoryIds, tagIds: req.TagIds, now, userId, ct);

        // Create revision v1
        await CreateRevisionAsync(entity, versionNumber: 1, editorUserId: entity.EditorUserId, editedAt: now, changeNote: req.ChangeNote ?? "Initial version", ct);

        await _db.SaveChangesAsync(ct);
        return Ok(new { id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest(new { message = "Title is required." });

        if (string.IsNullOrWhiteSpace(req.Slug))
            return BadRequest(new { message = "Slug is required." });

        if (string.IsNullOrWhiteSpace(req.ContentMarkdown))
            return BadRequest(new { message = "ContentMarkdown is required." });

        if (string.IsNullOrWhiteSpace(req.ContentHtml))
            return BadRequest(new { message = "ContentHtml is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsPost>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Post not found." });

        var slug = NormalizeSlug(req.Slug);

        var slugExists = await _db.Set<NewsPost>().IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Slug == slug && x.Id != id, ct);

        if (slugExists)
            return Conflict(new { message = "Slug already exists in this tenant." });

        // Update analytics based on markdown
        var wordCount = CountWords(req.ContentMarkdown);
        var readingMin = EstimateReadingMinutes(wordCount);

        entity.Title = req.Title.Trim();
        entity.Slug = slug;
        entity.Summary = string.IsNullOrWhiteSpace(req.Summary) ? null : req.Summary.Trim();

        entity.ContentMarkdown = req.ContentMarkdown;
        entity.ContentHtml = req.ContentHtml;

        entity.CoverMediaAssetId = req.CoverMediaAssetId;
        entity.CoverImageUrl = string.IsNullOrWhiteSpace(req.CoverImageUrl) ? null : req.CoverImageUrl.Trim();

        entity.SeoTitle = string.IsNullOrWhiteSpace(req.SeoTitle) ? null : req.SeoTitle.Trim();
        entity.SeoDescription = string.IsNullOrWhiteSpace(req.SeoDescription) ? null : req.SeoDescription.Trim();
        entity.SeoKeywords = string.IsNullOrWhiteSpace(req.SeoKeywords) ? null : req.SeoKeywords.Trim();
        entity.CanonicalUrl = string.IsNullOrWhiteSpace(req.CanonicalUrl) ? null : req.CanonicalUrl.Trim();
        entity.Robots = string.IsNullOrWhiteSpace(req.Robots) ? null : req.Robots.Trim();

        entity.OgTitle = string.IsNullOrWhiteSpace(req.OgTitle) ? null : req.OgTitle.Trim();
        entity.OgDescription = string.IsNullOrWhiteSpace(req.OgDescription) ? null : req.OgDescription.Trim();
        entity.OgImageUrl = string.IsNullOrWhiteSpace(req.OgImageUrl) ? null : req.OgImageUrl.Trim();
        entity.OgType = string.IsNullOrWhiteSpace(req.OgType) ? "article" : req.OgType.Trim();

        entity.TwitterCard = string.IsNullOrWhiteSpace(req.TwitterCard) ? "summary_large_image" : req.TwitterCard.Trim();
        entity.TwitterSite = string.IsNullOrWhiteSpace(req.TwitterSite) ? null : req.TwitterSite.Trim();
        entity.TwitterCreator = string.IsNullOrWhiteSpace(req.TwitterCreator) ? null : req.TwitterCreator.Trim();
        entity.TwitterTitle = string.IsNullOrWhiteSpace(req.TwitterTitle) ? null : req.TwitterTitle.Trim();
        entity.TwitterDescription = string.IsNullOrWhiteSpace(req.TwitterDescription) ? null : req.TwitterDescription.Trim();
        entity.TwitterImageUrl = string.IsNullOrWhiteSpace(req.TwitterImageUrl) ? null : req.TwitterImageUrl.Trim();

        entity.SchemaJsonLd = string.IsNullOrWhiteSpace(req.SchemaJsonLd) ? null : req.SchemaJsonLd;

        entity.ScheduledAt = req.ScheduledAt;
        entity.PublishedAt = req.PublishedAt;
        entity.UnpublishedAt = req.UnpublishedAt;

        entity.Status = DeriveStatus(entity.Status, req.ScheduledAt, req.PublishedAt, req.UnpublishedAt);

        entity.EditorUserId = req.EditorUserId ?? userId;
        entity.LastEditedAt = req.LastEditedAt ?? now;

        entity.WordCount = wordCount;
        entity.ReadingTimeMinutes = readingMin;

        if (entity.IsDeleted) entity.IsDeleted = false;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await UpsertMappingsAsync(entity, categoryIds: req.CategoryIds, tagIds: req.TagIds, now, userId, ct);

        if (req.Revise)
        {
            var nextVersion = ((await _db.Set<NewsPostRevision>().IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.PostId == entity.Id && !x.IsDeleted)
                .MaxAsync(x => (int?)x.VersionNumber, ct)) ?? 0) + 1;

            await CreateRevisionAsync(entity, nextVersion, entity.EditorUserId, now, req.ChangeNote ?? $"Revision {nextVersion}", ct);
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsPost>()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Post not found." });

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
            return RequireTenant();

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsPost>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Post not found." });

        entity.IsDeleted = false;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsPost>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Post not found." });

        entity.PublishedAt = now;
        entity.UnpublishedAt = null;
        entity.ScheduledAt = null;
        entity.Status = NewsPostStatus.Published;
        entity.EditorUserId = userId;
        entity.LastEditedAt = now;

        if (entity.IsDeleted) entity.IsDeleted = false;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        // revision snapshot
        var nextVersion = ((await _db.Set<NewsPostRevision>().IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.PostId == entity.Id && !x.IsDeleted)
            .MaxAsync(x => (int?)x.VersionNumber, ct)) ?? 0) + 1;

        await CreateRevisionAsync(entity, nextVersion, entity.EditorUserId, now, "Publish", ct);

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/unpublish")]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsPost>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Post not found." });

        entity.UnpublishedAt = now;
        entity.Status = NewsPostStatus.Unpublished;
        entity.EditorUserId = userId;
        entity.LastEditedAt = now;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        var nextVersion = ((await _db.Set<NewsPostRevision>().IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.PostId == entity.Id && !x.IsDeleted)
            .MaxAsync(x => (int?)x.VersionNumber, ct)) ?? 0) + 1;

        await CreateRevisionAsync(entity, nextVersion, entity.EditorUserId, now, "Unpublish", ct);

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpGet("{id:guid}/revisions")]
    public async Task<IActionResult> ListRevisions(Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        var query = _db.Set<NewsPostRevision>().AsQueryable();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenant.HasTenant && _tenant.TenantId.HasValue)
            query = query.Where(x => x.TenantId == _tenant.TenantId.Value);

        var items = await query
            .Where(x => x.PostId == id)
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.PostId,
                x.VersionNumber,
                x.Title,
                x.Summary,
                x.EditorUserId,
                x.EditedAt,
                x.ChangeNote,
                x.IsDeleted,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    private async Task UpsertMappingsAsync(
        NewsPost post,
        List<Guid>? categoryIds,
        List<Guid>? tagIds,
        DateTimeOffset now,
        Guid? userId,
        CancellationToken ct)
    {
        // If null -> do not touch. If empty list -> clear all.
        if (categoryIds is not null)
        {
            var existing = await _db.Set<NewsPostCategory>().IgnoreQueryFilters()
                .Where(x => x.TenantId == post.TenantId && x.PostId == post.Id)
                .ToListAsync(ct);

            // mark all deleted first
            foreach (var row in existing)
            {
                row.IsDeleted = true;
                row.UpdatedAt = now;
                row.UpdatedByUserId = userId;
            }

            // then revive or add
            foreach (var cid in categoryIds.Distinct())
            {
                var row = existing.FirstOrDefault(x => x.CategoryId == cid);
                if (row is null)
                {
                    _db.Set<NewsPostCategory>().Add(new NewsPostCategory
                    {
                        Id = Guid.NewGuid(),
                        TenantId = post.TenantId,
                        PostId = post.Id,
                        CategoryId = cid,
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
            var existing = await _db.Set<NewsPostTag>().IgnoreQueryFilters()
                .Where(x => x.TenantId == post.TenantId && x.PostId == post.Id)
                .ToListAsync(ct);

            foreach (var row in existing)
            {
                row.IsDeleted = true;
                row.UpdatedAt = now;
                row.UpdatedByUserId = userId;
            }

            foreach (var tid in tagIds.Distinct())
            {
                var row = existing.FirstOrDefault(x => x.TagId == tid);
                if (row is null)
                {
                    _db.Set<NewsPostTag>().Add(new NewsPostTag
                    {
                        Id = Guid.NewGuid(),
                        TenantId = post.TenantId,
                        PostId = post.Id,
                        TagId = tid,
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

    private async Task CreateRevisionAsync(
        NewsPost post,
        int versionNumber,
        Guid? editorUserId,
        DateTimeOffset editedAt,
        string? changeNote,
        CancellationToken ct)
    {
        var rev = new NewsPostRevision
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

            ChangeNote = string.IsNullOrWhiteSpace(changeNote) ? null : changeNote.Trim(),
            EditorUserId = editorUserId,
            EditedAt = editedAt,

            IsDeleted = false,
            CreatedAt = editedAt,
            CreatedByUserId = editorUserId
        };

        _db.Set<NewsPostRevision>().Add(rev);
        await Task.CompletedTask;
    }
}

