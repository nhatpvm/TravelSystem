// FILE #314: TicketBooking.Api/Controllers/Cms/Manager/CmsManagerPostRevisionsController.cs
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
[Route("api/v{version:apiVersion}/manager/cms/posts/{postId:guid}/revisions")]
[Authorize(Roles = "Admin,QLNX,QLVT,QLVMM,QLKS,QLTour")]
public sealed class CmsManagerPostRevisionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public CmsManagerPostRevisionsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        Guid postId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        if (postId == Guid.Empty)
            return BadRequest(new { message = "postId is required." });

        var tenantId = _tenant.TenantId.Value;

        var postExists = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id == postId && x.TenantId == tenantId, ct);

        if (!postExists)
            return NotFound(new { message = "Post not found." });

        IQueryable<NewsPostRevision> query = _db.Set<NewsPostRevision>().AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var items = await query
            .Where(x => x.TenantId == tenantId && x.PostId == postId)
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.PostId,
                x.VersionNumber,
                x.Title,
                x.Summary,
                x.ChangeNote,
                x.EditorUserId,
                x.EditedAt,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpGet("{revisionId:guid}")]
    public async Task<IActionResult> Get(
        Guid postId,
        Guid revisionId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        if (postId == Guid.Empty)
            return BadRequest(new { message = "postId is required." });

        if (revisionId == Guid.Empty)
            return BadRequest(new { message = "revisionId is required." });

        var tenantId = _tenant.TenantId.Value;

        IQueryable<NewsPostRevision> query = _db.Set<NewsPostRevision>().AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var entity = await query.FirstOrDefaultAsync(
            x => x.Id == revisionId && x.PostId == postId && x.TenantId == tenantId,
            ct);

        if (entity is null)
            return NotFound(new { message = "Revision not found." });

        return Ok(new
        {
            entity.Id,
            entity.TenantId,
            entity.PostId,
            entity.VersionNumber,

            entity.Title,
            entity.Summary,
            entity.ContentMarkdown,
            entity.ContentHtml,

            entity.SeoTitle,
            entity.SeoDescription,
            entity.CanonicalUrl,
            entity.Robots,
            entity.OgTitle,
            entity.OgDescription,
            entity.OgImageUrl,
            entity.TwitterCard,
            entity.TwitterTitle,
            entity.TwitterDescription,
            entity.TwitterImageUrl,
            entity.SchemaJsonLd,

            entity.ChangeNote,
            entity.EditorUserId,
            entity.EditedAt,

            entity.IsDeleted,
            entity.CreatedAt,
            entity.CreatedByUserId,
            entity.UpdatedAt,
            entity.UpdatedByUserId
        });
    }

    [HttpPost("{revisionId:guid}/restore")]
    public async Task<IActionResult> Restore(
        Guid postId,
        Guid revisionId,
        [FromBody] CmsManagerRestoreRevisionRequest? req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        if (postId == Guid.Empty)
            return BadRequest(new { message = "postId is required." });

        if (revisionId == Guid.Empty)
            return BadRequest(new { message = "revisionId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();
        req ??= new CmsManagerRestoreRevisionRequest();

        var post = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == postId && x.TenantId == tenantId, ct);

        if (post is null)
            return NotFound(new { message = "Post not found." });

        if (post.IsDeleted && !req.ReviveDeletedPost)
        {
            return Conflict(new
            {
                message = "Post is deleted. Set ReviveDeletedPost=true if you want to restore content into this deleted post."
            });
        }

        var revision = await _db.Set<NewsPostRevision>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == revisionId && x.PostId == postId && x.TenantId == tenantId, ct);

        if (revision is null)
            return NotFound(new { message = "Revision not found." });

        post.Title = revision.Title;
        post.Summary = revision.Summary;
        post.ContentMarkdown = revision.ContentMarkdown;
        post.ContentHtml = revision.ContentHtml;

        post.SeoTitle = revision.SeoTitle;
        post.SeoDescription = revision.SeoDescription;
        post.CanonicalUrl = revision.CanonicalUrl;
        post.Robots = revision.Robots;
        post.OgTitle = revision.OgTitle;
        post.OgDescription = revision.OgDescription;
        post.OgImageUrl = revision.OgImageUrl;
        post.TwitterCard = revision.TwitterCard;
        post.TwitterTitle = revision.TwitterTitle;
        post.TwitterDescription = revision.TwitterDescription;
        post.TwitterImageUrl = revision.TwitterImageUrl;
        post.SchemaJsonLd = revision.SchemaJsonLd;

        post.WordCount = CountWords(post.ContentMarkdown);
        post.ReadingTimeMinutes = EstimateReadingMinutes(post.WordCount);

        if (req.ReviveDeletedPost && post.IsDeleted)
            post.IsDeleted = false;

        if (req.SetDraftAfterRestore)
        {
            post.Status = NewsPostStatus.Draft;
            post.ScheduledAt = null;
            post.PublishedAt = null;
            post.UnpublishedAt = null;
        }

        post.EditorUserId = userId;
        post.LastEditedAt = now;
        post.UpdatedAt = now;
        post.UpdatedByUserId = userId;

        var currentMax = await _db.Set<NewsPostRevision>()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.PostId == postId && !x.IsDeleted)
            .MaxAsync(x => (int?)x.VersionNumber, ct);

        var nextVersion = (currentMax ?? 0) + 1;

        await CreateRevisionSnapshotAsync(
            post,
            nextVersion,
            userId,
            now,
            req.ChangeNote ?? $"Restored from revision v{revision.VersionNumber}",
            ct);

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            ok = true,
            postId = post.Id,
            restoredFromRevisionId = revision.Id,
            restoredFromVersion = revision.VersionNumber,
            newRevisionVersion = nextVersion
        });
    }

    private async Task CreateRevisionSnapshotAsync(
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

    private static string? NullIfWhite(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
}

public sealed class CmsManagerRestoreRevisionRequest
{
    public bool ReviveDeletedPost { get; set; } = false;
    public bool SetDraftAfterRestore { get; set; } = false;
    public string? ChangeNote { get; set; }
}
