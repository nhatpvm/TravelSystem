// FILE #319: TicketBooking.Api/Services/Cms/CmsPublishingBackgroundService.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Cms;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Cms;

/// <summary>
/// Background worker for CMS workflow automation:
/// - auto publish posts when ScheduledAt <= now
/// - auto unpublish posts when UnpublishedAt <= now
///
/// Register in Program.cs:
/// builder.Services.AddHostedService<CmsPublishingBackgroundService>();
/// </summary>
public sealed class CmsPublishingBackgroundService : BackgroundService
{
    private static readonly TimeSpan LoopInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(10);
    private const int BatchSize = 200;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CmsPublishingBackgroundService> _logger;

    public CmsPublishingBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<CmsPublishingBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(LoopInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CmsPublishingBackgroundService failed while processing CMS publish/unpublish workflow.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.Now;

        var toPublish = await db.Set<NewsPost>()
            .Where(x =>
                !x.IsDeleted &&
                x.Status == NewsPostStatus.Scheduled &&
                x.ScheduledAt.HasValue &&
                x.ScheduledAt.Value <= now)
            .OrderBy(x => x.ScheduledAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        var toUnpublish = await db.Set<NewsPost>()
            .Where(x =>
                !x.IsDeleted &&
                x.UnpublishedAt.HasValue &&
                x.UnpublishedAt.Value <= now &&
                x.Status != NewsPostStatus.Unpublished)
            .OrderBy(x => x.UnpublishedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (toPublish.Count == 0 && toUnpublish.Count == 0)
            return;

        var changedPosts = new List<(NewsPost Post, string ChangeNote)>();

        foreach (var post in toPublish)
        {
            post.Status = NewsPostStatus.Published;
            post.PublishedAt ??= post.ScheduledAt ?? now;
            post.ScheduledAt = null;
            post.UnpublishedAt = null;
            post.LastEditedAt = now;
            post.UpdatedAt = now;
            post.UpdatedByUserId = null;

            changedPosts.Add((post, "Auto publish from schedule"));
        }

        foreach (var post in toUnpublish)
        {
            post.Status = NewsPostStatus.Unpublished;
            post.LastEditedAt = now;
            post.UpdatedAt = now;
            post.UpdatedByUserId = null;

            changedPosts.Add((post, "Auto unpublish by schedule"));
        }

        var changedPostIds = changedPosts
            .Select(x => x.Post.Id)
            .Distinct()
            .ToList();

        var maxVersions = changedPostIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await db.Set<NewsPostRevision>()
                .IgnoreQueryFilters()
                .Where(x => changedPostIds.Contains(x.PostId) && !x.IsDeleted)
                .GroupBy(x => x.PostId)
                .Select(g => new
                {
                    PostId = g.Key,
                    MaxVersion = g.Max(x => x.VersionNumber)
                })
                .ToDictionaryAsync(x => x.PostId, x => x.MaxVersion, ct);

        foreach (var item in changedPosts)
        {
            var nextVersion = maxVersions.TryGetValue(item.Post.Id, out var maxVersion)
                ? maxVersion + 1
                : 1;

            maxVersions[item.Post.Id] = nextVersion;

            db.Set<NewsPostRevision>().Add(CreateRevision(item.Post, nextVersion, now, item.ChangeNote));
        }

        await db.SaveChangesAsync(ct);

        if (toPublish.Count > 0)
        {
            _logger.LogInformation(
                "CMS auto-published {Count} post(s) at {Now}. PostIds: {PostIds}",
                toPublish.Count,
                now,
                string.Join(", ", toPublish.Select(x => x.Id)));
        }

        if (toUnpublish.Count > 0)
        {
            _logger.LogInformation(
                "CMS auto-unpublished {Count} post(s) at {Now}. PostIds: {PostIds}",
                toUnpublish.Count,
                now,
                string.Join(", ", toUnpublish.Select(x => x.Id)));
        }
    }

    private static NewsPostRevision CreateRevision(
        NewsPost post,
        int versionNumber,
        DateTimeOffset editedAt,
        string changeNote)
    {
        return new NewsPostRevision
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

            ChangeNote = changeNote,
            EditorUserId = null,
            EditedAt = editedAt,

            IsDeleted = false,
            CreatedAt = editedAt,
            CreatedByUserId = null
        };
    }
}
