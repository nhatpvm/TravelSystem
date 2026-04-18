// FILE #306: TicketBooking.Api/Services/Cms/CmsPublicQueryService.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Cms;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Cms;

public interface ICmsPublicQueryService
{
    Task<Guid> ResolveTenantIdAsync(string tenantCode, CancellationToken ct = default);

    Task<CmsPublicSiteInfo?> GetSiteInfoAsync(string tenantCode, CancellationToken ct = default);

    Task<CmsPublicRedirectResult?> ResolveRedirectAsync(
        string tenantCode,
        string fromPath,
        CancellationToken ct = default);

    Task<CmsPublicNewsIndexResult?> GetNewsIndexAsync(
        string tenantCode,
        string baseUrl,
        int page = 1,
        int pageSize = 10,
        string? q = null,
        CancellationToken ct = default);

    Task<CmsPublicPostDetailResult?> GetPublishedPostBySlugAsync(
        string tenantCode,
        string slug,
        string baseUrl,
        bool bumpView = false,
        CancellationToken ct = default);

    Task<CmsPublicCategoryPageResult?> GetCategoryPageAsync(
        string tenantCode,
        string categorySlug,
        string baseUrl,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    Task<CmsPublicTagPageResult?> GetTagPageAsync(
        string tenantCode,
        string tagSlug,
        string baseUrl,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    Task<IReadOnlyList<CmsPublicCategoryItem>> ListCategoriesAsync(
        string tenantCode,
        CancellationToken ct = default);

    Task<IReadOnlyList<CmsPublicTagItem>> ListTagsAsync(
        string tenantCode,
        CancellationToken ct = default);

    Task<IReadOnlyList<CmsPublicSitemapItem>> GetSitemapItemsAsync(
        string tenantCode,
        string baseUrl,
        CancellationToken ct = default);

    Task<IReadOnlyList<CmsPublicRssItem>> GetRssItemsAsync(
        string tenantCode,
        string baseUrl,
        int take = 30,
        CancellationToken ct = default);
}

public sealed class CmsPublicQueryService : ICmsPublicQueryService
{
    private readonly AppDbContext _db;
    private readonly ICmsSeoDefaultsService _seoDefaults;
    private readonly ICmsSlugService _slugService;
    private readonly ICmsRedirectService _redirectService;

    public CmsPublicQueryService(
        AppDbContext db,
        ICmsSeoDefaultsService seoDefaults,
        ICmsSlugService slugService,
        ICmsRedirectService redirectService)
    {
        _db = db;
        _seoDefaults = seoDefaults;
        _slugService = slugService;
        _redirectService = redirectService;
    }

    public async Task<Guid> ResolveTenantIdAsync(string tenantCode, CancellationToken ct = default)
    {
        var code = NormalizeTenantCode(tenantCode);
        if (string.IsNullOrWhiteSpace(code))
            return Guid.Empty;

        return await _db.Set<Tenant>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Code == code)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<CmsPublicSiteInfo?> GetSiteInfoAsync(string tenantCode, CancellationToken ct = default)
    {
        var tenantId = await ResolveTenantIdAsync(tenantCode, ct);
        if (tenantId == Guid.Empty)
            return null;

        var tenant = await _db.Set<Tenant>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Id == tenantId && !x.IsDeleted)
            .Select(x => new { x.Id, x.Code, x.Name })
            .FirstOrDefaultAsync(ct);

        if (tenant is null)
            return null;

        var site = await GetSiteSettingByTenantIdAsync(tenantId, ct);

        return new CmsPublicSiteInfo
        {
            TenantId = tenant.Id,
            TenantCode = tenant.Code,
            TenantName = tenant.Name,
            SiteName = site?.SiteName ?? tenant.Name ?? "TicketBooking",
            SiteUrl = site?.SiteUrl,
            BrandLogoUrl = site?.BrandLogoUrl,
            SupportEmail = site?.SupportEmail,
            SupportPhone = site?.SupportPhone,
            DefaultRobots = site?.DefaultRobots,
            DefaultOgImageUrl = site?.DefaultOgImageUrl,
            DefaultTwitterCard = site?.DefaultTwitterCard,
            DefaultTwitterSite = site?.DefaultTwitterSite,
            DefaultSchemaJsonLd = site?.DefaultSchemaJsonLd
        };
    }

    public async Task<CmsPublicRedirectResult?> ResolveRedirectAsync(
        string tenantCode,
        string fromPath,
        CancellationToken ct = default)
    {
        var tenantId = await ResolveTenantIdAsync(tenantCode, ct);
        if (tenantId == Guid.Empty)
            return null;

        var redirect = await _redirectService.GetByFromPathAsync(
            tenantId,
            fromPath,
            includeDeleted: false,
            ct: ct);

        if (redirect is null || redirect.IsDeleted || !redirect.IsActive)
            return null;

        return new CmsPublicRedirectResult
        {
            FromPath = redirect.FromPath,
            ToPath = redirect.ToPath,
            StatusCode = redirect.StatusCode,
            IsRegex = redirect.IsRegex,
            Reason = redirect.Reason
        };
    }

    public async Task<CmsPublicNewsIndexResult?> GetNewsIndexAsync(
        string tenantCode,
        string baseUrl,
        int page = 1,
        int pageSize = 10,
        string? q = null,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

        var tenantId = await ResolveTenantIdAsync(tenantCode, ct);
        if (tenantId == Guid.Empty)
            return null;

        var site = await GetSiteSettingByTenantIdAsync(tenantId, ct);
        var now = DateTimeOffset.Now;

        var query = BuildPublishedPostsBaseQuery(tenantId, now);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Title.Contains(keyword) ||
                (x.Summary != null && x.Summary.Contains(keyword)) ||
                (x.SeoKeywords != null && x.SeoKeywords.Contains(keyword)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CmsPublicPostListItem
            {
                Id = x.Id,
                TenantId = x.TenantId,
                Title = x.Title,
                Slug = x.Slug,
                Summary = x.Summary,
                CoverMediaAssetId = x.CoverMediaAssetId,
                CoverImageUrl = x.CoverImageUrl,
                PublishedAt = x.PublishedAt,
                ViewCount = x.ViewCount,
                WordCount = x.WordCount,
                ReadingTimeMinutes = x.ReadingTimeMinutes
            })
            .ToListAsync(ct);

        await EnrichPostListItemsAsync(tenantId, items, ct);

        var seo = _seoDefaults.BuildForNewsIndex(
            tenantCode: tenantCode,
            site: site,
            baseUrl: baseUrl,
            title: null,
            description: string.IsNullOrWhiteSpace(q)
                ? null
                : $"Kết quả tìm kiếm tin tức cho từ khóa '{q.Trim()}'.");

        return new CmsPublicNewsIndexResult
        {
            TenantId = tenantId,
            TenantCode = NormalizeTenantCode(tenantCode),
            Page = page,
            PageSize = pageSize,
            Total = total,
            Query = string.IsNullOrWhiteSpace(q) ? null : q.Trim(),
            Seo = seo,
            Items = items
        };
    }

    public async Task<CmsPublicPostDetailResult?> GetPublishedPostBySlugAsync(
        string tenantCode,
        string slug,
        string baseUrl,
        bool bumpView = false,
        CancellationToken ct = default)
    {
        var tenantId = await ResolveTenantIdAsync(tenantCode, ct);
        if (tenantId == Guid.Empty)
            return null;

        var normalizedSlug = _slugService.Normalize(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
            return null;

        var now = DateTimeOffset.Now;

        var post = await BuildPublishedPostsBaseQuery(tenantId, now)
            .FirstOrDefaultAsync(x => x.Slug == normalizedSlug, ct);

        if (post is null)
            return null;

        if (bumpView)
        {
            post.ViewCount += 1;
            post.UpdatedAt = now;
            await _db.SaveChangesAsync(ct);
        }

        var site = await GetSiteSettingByTenantIdAsync(tenantId, ct);
        var seo = _seoDefaults.BuildForPost(post, site, baseUrl);

        var categoryIds = await _db.Set<NewsPostCategory>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PostId == post.Id && !x.IsDeleted)
            .Select(x => x.CategoryId)
            .ToListAsync(ct);

        var tagIds = await _db.Set<NewsPostTag>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.PostId == post.Id && !x.IsDeleted)
            .Select(x => x.TagId)
            .ToListAsync(ct);

        var categories = categoryIds.Count == 0
            ? new List<CmsPublicCategoryItem>()
            : await _db.Set<NewsCategory>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.IsActive && categoryIds.Contains(x.Id))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new CmsPublicCategoryItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    Slug = x.Slug,
                    Description = x.Description,
                    SortOrder = x.SortOrder,
                    PostCount = 0
                })
                .ToListAsync(ct);

        var tags = tagIds.Count == 0
            ? new List<CmsPublicTagItem>()
            : await _db.Set<NewsTag>()
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.IsActive && tagIds.Contains(x.Id))
                .OrderBy(x => x.Name)
                .Select(x => new CmsPublicTagItem
                {
                    Id = x.Id,
                    Name = x.Name,
                    Slug = x.Slug,
                    PostCount = 0
                })
                .ToListAsync(ct);

        var latestPosts = await BuildPublishedPostsBaseQuery(tenantId, now)
            .Where(x => x.Id != post.Id)
            .OrderByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new CmsPublicPostListItem
            {
                Id = x.Id,
                TenantId = x.TenantId,
                Title = x.Title,
                Slug = x.Slug,
                Summary = x.Summary,
                CoverMediaAssetId = x.CoverMediaAssetId,
                CoverImageUrl = x.CoverImageUrl,
                PublishedAt = x.PublishedAt,
                ViewCount = x.ViewCount,
                WordCount = x.WordCount,
                ReadingTimeMinutes = x.ReadingTimeMinutes
            })
            .ToListAsync(ct);

        await EnrichPostListItemsAsync(tenantId, latestPosts, ct);

        var detail = new CmsPublicPostDetail
        {
            Id = post.Id,
            TenantId = post.TenantId,
            Title = post.Title,
            Slug = post.Slug,
            Summary = post.Summary,
            ContentMarkdown = post.ContentMarkdown,
            ContentHtml = post.ContentHtml,
            CoverMediaAssetId = post.CoverMediaAssetId,
            CoverImageUrl = post.CoverImageUrl,
            PublishedAt = post.PublishedAt,
            ViewCount = post.ViewCount,
            WordCount = post.WordCount,
            ReadingTimeMinutes = post.ReadingTimeMinutes,
            SeoTitle = post.SeoTitle,
            SeoDescription = post.SeoDescription,
            SeoKeywords = post.SeoKeywords,
            CanonicalUrl = post.CanonicalUrl,
            Robots = post.Robots,
            OgTitle = post.OgTitle,
            OgDescription = post.OgDescription,
            OgImageUrl = post.OgImageUrl,
            OgType = post.OgType,
            TwitterCard = post.TwitterCard,
            TwitterSite = post.TwitterSite,
            TwitterCreator = post.TwitterCreator,
            TwitterTitle = post.TwitterTitle,
            TwitterDescription = post.TwitterDescription,
            TwitterImageUrl = post.TwitterImageUrl,
            SchemaJsonLd = post.SchemaJsonLd
        };

        await EnrichPostDetailAsync(tenantId, detail, ct);

        return new CmsPublicPostDetailResult
        {
            TenantId = tenantId,
            TenantCode = NormalizeTenantCode(tenantCode),
            Seo = seo,
            Post = detail,
            Categories = categories,
            Tags = tags,
            LatestPosts = latestPosts
        };
    }

    public async Task<CmsPublicCategoryPageResult?> GetCategoryPageAsync(
        string tenantCode,
        string categorySlug,
        string baseUrl,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

        var tenantId = await ResolveTenantIdAsync(tenantCode, ct);
        if (tenantId == Guid.Empty)
            return null;

        var normalizedSlug = _slugService.Normalize(categorySlug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
            return null;

        var category = await _db.Set<NewsCategory>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                !x.IsDeleted &&
                x.IsActive &&
                x.Slug == normalizedSlug, ct);

        if (category is null)
            return null;

        var now = DateTimeOffset.Now;

        var postQuery =
            from post in BuildPublishedPostsBaseQuery(tenantId, now)
            join map in _db.Set<NewsPostCategory>().IgnoreQueryFilters().AsNoTracking()
                on post.Id equals map.PostId
            where map.TenantId == tenantId
                  && !map.IsDeleted
                  && map.CategoryId == category.Id
            select post;

        var total = await postQuery.CountAsync(ct);

        var items = await postQuery
            .OrderByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CmsPublicPostListItem
            {
                Id = x.Id,
                TenantId = x.TenantId,
                Title = x.Title,
                Slug = x.Slug,
                Summary = x.Summary,
                CoverMediaAssetId = x.CoverMediaAssetId,
                CoverImageUrl = x.CoverImageUrl,
                PublishedAt = x.PublishedAt,
                ViewCount = x.ViewCount,
                WordCount = x.WordCount,
                ReadingTimeMinutes = x.ReadingTimeMinutes
            })
            .ToListAsync(ct);

        await EnrichPostListItemsAsync(tenantId, items, ct);

        var site = await GetSiteSettingByTenantIdAsync(tenantId, ct);
        var seo = _seoDefaults.BuildForCategory(
            categoryName: category.Name,
            categorySlug: category.Slug,
            site: site,
            baseUrl: baseUrl,
            description: category.Description);

        return new CmsPublicCategoryPageResult
        {
            TenantId = tenantId,
            TenantCode = NormalizeTenantCode(tenantCode),
            Category = new CmsPublicCategoryItem
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                SortOrder = category.SortOrder,
                PostCount = total
            },
            Seo = seo,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    public async Task<CmsPublicTagPageResult?> GetTagPageAsync(
        string tenantCode,
        string tagSlug,
        string baseUrl,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : (pageSize > 100 ? 100 : pageSize);

        var tenantId = await ResolveTenantIdAsync(tenantCode, ct);
        if (tenantId == Guid.Empty)
            return null;

        var normalizedSlug = _slugService.Normalize(tagSlug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
            return null;

        var tag = await _db.Set<NewsTag>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                !x.IsDeleted &&
                x.IsActive &&
                x.Slug == normalizedSlug, ct);

        if (tag is null)
            return null;

        var now = DateTimeOffset.Now;

        var postQuery =
            from post in BuildPublishedPostsBaseQuery(tenantId, now)
            join map in _db.Set<NewsPostTag>().IgnoreQueryFilters().AsNoTracking()
                on post.Id equals map.PostId
            where map.TenantId == tenantId
                  && !map.IsDeleted
                  && map.TagId == tag.Id
            select post;

        var total = await postQuery.CountAsync(ct);

        var items = await postQuery
            .OrderByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new CmsPublicPostListItem
            {
                Id = x.Id,
                TenantId = x.TenantId,
                Title = x.Title,
                Slug = x.Slug,
                Summary = x.Summary,
                CoverMediaAssetId = x.CoverMediaAssetId,
                CoverImageUrl = x.CoverImageUrl,
                PublishedAt = x.PublishedAt,
                ViewCount = x.ViewCount,
                WordCount = x.WordCount,
                ReadingTimeMinutes = x.ReadingTimeMinutes
            })
            .ToListAsync(ct);

        await EnrichPostListItemsAsync(tenantId, items, ct);

        var site = await GetSiteSettingByTenantIdAsync(tenantId, ct);
        var seo = _seoDefaults.BuildForTag(
            tagName: tag.Name,
            tagSlug: tag.Slug,
            site: site,
            baseUrl: baseUrl,
            description: $"Các bài viết gắn thẻ {tag.Name}.");

        return new CmsPublicTagPageResult
        {
            TenantId = tenantId,
            TenantCode = NormalizeTenantCode(tenantCode),
            Tag = new CmsPublicTagItem
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug,
                PostCount = total
            },
            Seo = seo,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    public async Task<IReadOnlyList<CmsPublicCategoryItem>> ListCategoriesAsync(
        string tenantCode,
        CancellationToken ct = default)
    {
        var tenantId = await ResolveTenantIdAsync(tenantCode, ct);
        if (tenantId == Guid.Empty)
            return Array.Empty<CmsPublicCategoryItem>();

        var now = DateTimeOffset.Now;

        var items = await (
            from category in _db.Set<NewsCategory>().IgnoreQueryFilters().AsNoTracking()
            where category.TenantId == tenantId
                  && !category.IsDeleted
                  && category.IsActive
            join map in _db.Set<NewsPostCategory>().IgnoreQueryFilters().AsNoTracking()
                on category.Id equals map.CategoryId into categoryMaps
            select new CmsPublicCategoryItem
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                Description = category.Description,
                SortOrder = category.SortOrder,
                PostCount = (
                    from m in categoryMaps
                    join post in BuildPublishedPostsBaseQuery(tenantId, now)
                        on m.PostId equals post.Id
                    where !m.IsDeleted
                    select m.PostId
                ).Distinct().Count()
            })
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return items;
    }

    public async Task<IReadOnlyList<CmsPublicTagItem>> ListTagsAsync(
        string tenantCode,
        CancellationToken ct = default)
    {
        var tenantId = await ResolveTenantIdAsync(tenantCode, ct);
        if (tenantId == Guid.Empty)
            return Array.Empty<CmsPublicTagItem>();

        var now = DateTimeOffset.Now;

        var items = await (
            from tag in _db.Set<NewsTag>().IgnoreQueryFilters().AsNoTracking()
            where tag.TenantId == tenantId
                  && !tag.IsDeleted
                  && tag.IsActive
            join map in _db.Set<NewsPostTag>().IgnoreQueryFilters().AsNoTracking()
                on tag.Id equals map.TagId into tagMaps
            select new CmsPublicTagItem
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug,
                PostCount = (
                    from m in tagMaps
                    join post in BuildPublishedPostsBaseQuery(tenantId, now)
                        on m.PostId equals post.Id
                    where !m.IsDeleted
                    select m.PostId
                ).Distinct().Count()
            })
            .OrderByDescending(x => x.PostCount)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        return items;
    }

    public async Task<IReadOnlyList<CmsPublicSitemapItem>> GetSitemapItemsAsync(
        string tenantCode,
        string baseUrl,
        CancellationToken ct = default)
    {
        var tenantId = await ResolveTenantIdAsync(tenantCode, ct);
        if (tenantId == Guid.Empty)
            return Array.Empty<CmsPublicSitemapItem>();

        var now = DateTimeOffset.Now;
        var items = new List<CmsPublicSitemapItem>();

        items.Add(new CmsPublicSitemapItem
        {
            Url = $"{NormalizeBaseUrl(baseUrl)}/tin-tuc",
            ChangeFrequency = "daily",
            Priority = "0.8",
            LastModifiedAt = now
        });

        var posts = await BuildPublishedPostsBaseQuery(tenantId, now)
            .OrderByDescending(x => x.PublishedAt)
            .Select(x => new
            {
                x.Slug,
                LastModifiedAt = x.UpdatedAt ?? x.PublishedAt ?? x.CreatedAt
            })
            .ToListAsync(ct);

        foreach (var post in posts)
        {
            items.Add(new CmsPublicSitemapItem
            {
                Url = $"{NormalizeBaseUrl(baseUrl)}/tin-tuc/{post.Slug}",
                ChangeFrequency = "weekly",
                Priority = "0.7",
                LastModifiedAt = post.LastModifiedAt
            });
        }

        return items;
    }

    public async Task<IReadOnlyList<CmsPublicRssItem>> GetRssItemsAsync(
        string tenantCode,
        string baseUrl,
        int take = 30,
        CancellationToken ct = default)
    {
        take = take < 1 ? 30 : (take > 100 ? 100 : take);

        var tenantId = await ResolveTenantIdAsync(tenantCode, ct);
        if (tenantId == Guid.Empty)
            return Array.Empty<CmsPublicRssItem>();

        var normalizedBaseUrl = NormalizeBaseUrl(baseUrl);
        var now = DateTimeOffset.Now;

        var items = await BuildPublishedPostsBaseQuery(tenantId, now)
            .OrderByDescending(x => x.PublishedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new CmsPublicRssItem
            {
                Title = x.Title,
                Slug = x.Slug,
                Summary = x.Summary,
                PublishedAt = x.PublishedAt ?? x.CreatedAt,
                Link = $"{normalizedBaseUrl}/tin-tuc/{x.Slug}"
            })
            .ToListAsync(ct);

        return items;
    }

    private IQueryable<NewsPost> BuildPublishedPostsBaseQuery(Guid tenantId, DateTimeOffset now)
    {
        return _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                !x.IsDeleted &&
                x.Status == NewsPostStatus.Published &&
                x.PublishedAt.HasValue &&
                x.PublishedAt.Value <= now &&
                (!x.UnpublishedAt.HasValue || x.UnpublishedAt.Value > now));
    }

    private async Task<SiteSetting?> GetSiteSettingByTenantIdAsync(Guid tenantId, CancellationToken ct)
    {
        return await _db.Set<SiteSetting>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                !x.IsDeleted &&
                x.IsActive, ct);
    }

    private static string NormalizeTenantCode(string? tenantCode)
        => string.IsNullOrWhiteSpace(tenantCode) ? string.Empty : tenantCode.Trim();

    private static string NormalizeBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            return "http://localhost";

        return baseUrl.Trim().TrimEnd('/');
    }

    private async Task EnrichPostListItemsAsync(Guid tenantId, List<CmsPublicPostListItem> items, CancellationToken ct)
    {
        if (items.Count == 0)
            return;

        var postIds = items.Select(x => x.Id).Distinct().ToList();

        var authorRefs = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && postIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                UserId = x.AuthorUserId ?? x.CreatedByUserId
            })
            .ToListAsync(ct);

        var userIds = authorRefs
            .Where(x => x.UserId.HasValue)
            .Select(x => x.UserId!.Value)
            .Distinct()
            .ToList();

        var authorNames = userIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _db.Users
                .AsNoTracking()
                .Where(x => userIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    DisplayName = !string.IsNullOrWhiteSpace(x.FullName)
                        ? x.FullName!
                        : (!string.IsNullOrWhiteSpace(x.UserName) ? x.UserName! : (x.Email ?? "Admin"))
                })
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName, ct);

        var primaryCategories = await (
            from map in _db.Set<NewsPostCategory>().IgnoreQueryFilters().AsNoTracking()
            join category in _db.Set<NewsCategory>().IgnoreQueryFilters().AsNoTracking()
                on map.CategoryId equals category.Id
            where map.TenantId == tenantId
                  && postIds.Contains(map.PostId)
                  && !map.IsDeleted
                  && !category.IsDeleted
                  && category.IsActive
            select new
            {
                map.PostId,
                category.Name,
                category.Slug,
                category.SortOrder
            })
            .ToListAsync(ct);

        var primaryCategoryMap = primaryCategories
            .GroupBy(x => x.PostId)
            .ToDictionary(
                x => x.Key,
                x => x.OrderBy(y => y.SortOrder).ThenBy(y => y.Name).First());

        var authorRefMap = authorRefs.ToDictionary(x => x.Id, x => x.UserId);

        foreach (var item in items)
        {
            if (authorRefMap.TryGetValue(item.Id, out var userId)
                && userId.HasValue
                && authorNames.TryGetValue(userId.Value, out var authorName))
            {
                item.AuthorName = authorName;
            }

            if (primaryCategoryMap.TryGetValue(item.Id, out var category))
            {
                item.PrimaryCategoryName = category.Name;
                item.PrimaryCategorySlug = category.Slug;
            }
        }
    }

    private async Task EnrichPostDetailAsync(Guid tenantId, CmsPublicPostDetail post, CancellationToken ct)
    {
        var items = new List<CmsPublicPostListItem>
        {
            new()
            {
                Id = post.Id,
                TenantId = post.TenantId
            }
        };

        await EnrichPostListItemsAsync(tenantId, items, ct);

        var enriched = items[0];
        post.AuthorName = enriched.AuthorName;
        post.PrimaryCategoryName = enriched.PrimaryCategoryName;
        post.PrimaryCategorySlug = enriched.PrimaryCategorySlug;
    }
}

public sealed class CmsPublicSiteInfo
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = "";
    public string? TenantName { get; set; }

    public string SiteName { get; set; } = "TicketBooking";
    public string? SiteUrl { get; set; }
    public string? BrandLogoUrl { get; set; }
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }

    public string? DefaultRobots { get; set; }
    public string? DefaultOgImageUrl { get; set; }
    public string? DefaultTwitterCard { get; set; }
    public string? DefaultTwitterSite { get; set; }
    public string? DefaultSchemaJsonLd { get; set; }
}

public sealed class CmsPublicRedirectResult
{
    public string FromPath { get; set; } = "";
    public string ToPath { get; set; } = "";
    public RedirectStatusCode StatusCode { get; set; }
    public bool IsRegex { get; set; }
    public string? Reason { get; set; }
}

public sealed class CmsPublicPostListItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public Guid? CoverMediaAssetId { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? AuthorName { get; set; }
    public string? PrimaryCategoryName { get; set; }
    public string? PrimaryCategorySlug { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public int WordCount { get; set; }
    public int ReadingTimeMinutes { get; set; }
}

public sealed class CmsPublicPostDetail
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }

    public string ContentMarkdown { get; set; } = "";
    public string ContentHtml { get; set; } = "";

    public Guid? CoverMediaAssetId { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? AuthorName { get; set; }
    public string? PrimaryCategoryName { get; set; }
    public string? PrimaryCategorySlug { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public int WordCount { get; set; }
    public int ReadingTimeMinutes { get; set; }

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
}

public sealed class CmsPublicCategoryItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public int PostCount { get; set; }
}

public sealed class CmsPublicTagItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public int PostCount { get; set; }
}

public sealed class CmsPublicNewsIndexResult
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = "";
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public string? Query { get; set; }
    public CmsResolvedSeoMeta Seo { get; set; } = new();
    public List<CmsPublicPostListItem> Items { get; set; } = new();
}

public sealed class CmsPublicPostDetailResult
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = "";
    public CmsResolvedSeoMeta Seo { get; set; } = new();
    public CmsPublicPostDetail Post { get; set; } = new();
    public List<CmsPublicCategoryItem> Categories { get; set; } = new();
    public List<CmsPublicTagItem> Tags { get; set; } = new();
    public List<CmsPublicPostListItem> LatestPosts { get; set; } = new();
}

public sealed class CmsPublicCategoryPageResult
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = "";
    public CmsPublicCategoryItem Category { get; set; } = new();
    public CmsResolvedSeoMeta Seo { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<CmsPublicPostListItem> Items { get; set; } = new();
}

public sealed class CmsPublicTagPageResult
{
    public Guid TenantId { get; set; }
    public string TenantCode { get; set; } = "";
    public CmsPublicTagItem Tag { get; set; } = new();
    public CmsResolvedSeoMeta Seo { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<CmsPublicPostListItem> Items { get; set; } = new();
}

public sealed class CmsPublicSitemapItem
{
    public string Url { get; set; } = "";
    public DateTimeOffset LastModifiedAt { get; set; }
    public string ChangeFrequency { get; set; } = "weekly";
    public string Priority { get; set; } = "0.7";
}

public sealed class CmsPublicRssItem
{
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Summary { get; set; }
    public string Link { get; set; } = "";
    public DateTimeOffset PublishedAt { get; set; }
}
