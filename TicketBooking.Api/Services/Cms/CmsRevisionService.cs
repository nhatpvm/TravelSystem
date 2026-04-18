// FILE #303: TicketBooking.Api/Services/Cms/CmsRevisionService.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Cms;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Cms;

public interface ICmsRevisionService
{
    Task<int> GetNextVersionNumberAsync(Guid tenantId, Guid postId, CancellationToken ct = default);

    Task<NewsPostRevision> CreateRevisionAsync(
        NewsPost post,
        Guid? editorUserId,
        DateTimeOffset? editedAt = null,
        string? changeNote = null,
        CancellationToken ct = default);

    Task<NewsPostRevision?> GetRevisionAsync(
        Guid tenantId,
        Guid postId,
        Guid revisionId,
        bool includeDeleted = false,
        CancellationToken ct = default);

    Task<IReadOnlyList<NewsPostRevision>> ListRevisionsAsync(
        Guid tenantId,
        Guid postId,
        bool includeDeleted = false,
        CancellationToken ct = default);

    Task<CmsRestoreRevisionResult> RestoreRevisionAsync(
        Guid tenantId,
        Guid postId,
        Guid revisionId,
        Guid? editorUserId,
        string? changeNote = null,
        bool saveChanges = true,
        CancellationToken ct = default);
}

public sealed class CmsRevisionService : ICmsRevisionService
{
    private readonly AppDbContext _db;
    private readonly ICmsReadingTimeService _readingTimeService;

    public CmsRevisionService(AppDbContext db, ICmsReadingTimeService readingTimeService)
    {
        _db = db;
        _readingTimeService = readingTimeService;
    }

    public async Task<int> GetNextVersionNumberAsync(Guid tenantId, Guid postId, CancellationToken ct = default)
    {
        var maxVersion = await _db.Set<NewsPostRevision>()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.PostId == postId && !x.IsDeleted)
            .Select(x => (int?)x.VersionNumber)
            .MaxAsync(ct);

        return (maxVersion ?? 0) + 1;
    }

    public async Task<NewsPostRevision> CreateRevisionAsync(
        NewsPost post,
        Guid? editorUserId,
        DateTimeOffset? editedAt = null,
        string? changeNote = null,
        CancellationToken ct = default)
    {
        if (post is null) throw new ArgumentNullException(nameof(post));
        if (post.Id == Guid.Empty) throw new InvalidOperationException("Post.Id is required.");
        if (post.TenantId == Guid.Empty) throw new InvalidOperationException("Post.TenantId is required.");

        var now = editedAt ?? DateTimeOffset.Now;
        var nextVersion = await GetNextVersionNumberAsync(post.TenantId, post.Id, ct);

        var revision = new NewsPostRevision
        {
            Id = Guid.NewGuid(),
            TenantId = post.TenantId,
            PostId = post.Id,
            VersionNumber = nextVersion,

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
            EditedAt = now,

            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = editorUserId
        };

        _db.Set<NewsPostRevision>().Add(revision);
        return revision;
    }

    public async Task<NewsPostRevision?> GetRevisionAsync(
        Guid tenantId,
        Guid postId,
        Guid revisionId,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        IQueryable<NewsPostRevision> query = _db.Set<NewsPostRevision>().AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        return await query.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId &&
            x.PostId == postId &&
            x.Id == revisionId, ct);
    }

    public async Task<IReadOnlyList<NewsPostRevision>> ListRevisionsAsync(
        Guid tenantId,
        Guid postId,
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        IQueryable<NewsPostRevision> query = _db.Set<NewsPostRevision>().AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var items = await query
            .Where(x => x.TenantId == tenantId && x.PostId == postId)
            .OrderByDescending(x => x.VersionNumber)
            .ThenByDescending(x => x.EditedAt)
            .ToListAsync(ct);

        return items;
    }

    public async Task<CmsRestoreRevisionResult> RestoreRevisionAsync(
        Guid tenantId,
        Guid postId,
        Guid revisionId,
        Guid? editorUserId,
        string? changeNote = null,
        bool saveChanges = true,
        CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty) throw new InvalidOperationException("tenantId is required.");
        if (postId == Guid.Empty) throw new InvalidOperationException("postId is required.");
        if (revisionId == Guid.Empty) throw new InvalidOperationException("revisionId is required.");

        var post = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == postId, ct);

        if (post is null)
            throw new InvalidOperationException("Post not found.");

        var revision = await _db.Set<NewsPostRevision>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.PostId == postId &&
                x.Id == revisionId, ct);

        if (revision is null)
            throw new InvalidOperationException("Revision not found.");

        var now = DateTimeOffset.Now;

        // Snapshot current state before restoring.
        var backupRevision = await CreateRevisionAsync(
            post,
            editorUserId,
            now,
            changeNote: $"Backup before restore from revision {revision.VersionNumber}",
            ct);

        // Restore only fields actually stored in revision table.
        // Fields not present in revision entity are preserved on post:
        // - SeoKeywords
        // - CoverMediaAssetId / CoverImageUrl
        // - OgType
        // - TwitterSite / TwitterCreator
        // - workflow timestamps unless caller later changes them
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

        var stats = _readingTimeService.Calculate(post.ContentMarkdown);
        post.WordCount = stats.WordCount;
        post.ReadingTimeMinutes = stats.ReadingTimeMinutes;

        post.EditorUserId = editorUserId;
        post.LastEditedAt = now;
        post.UpdatedAt = now;
        post.UpdatedByUserId = editorUserId;

        if (post.IsDeleted)
            post.IsDeleted = false;

        var restoreRevision = await CreateRevisionAsync(
            post,
            editorUserId,
            now,
            changeNote: string.IsNullOrWhiteSpace(changeNote)
                ? $"Restored from revision {revision.VersionNumber}"
                : changeNote,
            ct);

        if (saveChanges)
            await _db.SaveChangesAsync(ct);

        return new CmsRestoreRevisionResult
        {
            Post = post,
            SourceRevision = revision,
            BackupRevision = backupRevision,
            RestoreRevision = restoreRevision
        };
    }
}

public sealed class CmsRestoreRevisionResult
{
    public NewsPost Post { get; set; } = default!;
    public NewsPostRevision SourceRevision { get; set; } = default!;
    public NewsPostRevision BackupRevision { get; set; } = default!;
    public NewsPostRevision RestoreRevision { get; set; } = default!;
}
