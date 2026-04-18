// FILE #128: TicketBooking.Infrastructure/Seed/CmsDemoSeed.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using TicketBooking.Domain.Cms;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Infrastructure.Seed
{
    /// <summary>
    /// Phase 7 - CMS/SEO demo seed (idempotent, multi-tenant)
    /// - Uses tenant NX001 (as your main demo tenant)
    /// - Seeds:
    ///   - SiteSettings (1 per tenant)
    ///   - Categories (2)
    ///   - Tags (5)
    ///   - Posts (2) (1 Published, 1 Draft/Scheduled)
    ///   - PostCategories + PostTags mappings
    ///   - Revisions (2 per post)
    ///   - Redirects (1)
    ///
    /// Notes:
    /// - Uses IgnoreQueryFilters() to be safe with soft delete + tenant filters.
    /// - Uses DateTimeOffset.Now (+07) as your system time convention.
    /// - ContentHtml is a simple demo conversion; real conversion can be done in CMS editor layer later.
    /// </summary>
    public static class CmsDemoSeed
    {
        private const string TenantCode = "NX001";

        public static async Task SeedAsync(AppDbContext db, UserManager<AppUser> userManager, ILogger logger, CancellationToken ct = default)
        {
            var now = DateTimeOffset.Now;

            var tenant = await db.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == TenantCode, ct);

            if (tenant is null || tenant.IsDeleted)
            {
                logger.LogWarning("CmsDemoSeed skipped: tenant {TenantCode} not found.", TenantCode);
                return;
            }

            // Use Admin as default author/editor if exists
            var admin = await userManager.FindByNameAsync("admin");
            var authorUserId = admin?.Id;

            // 1) SiteSettings
            await EnsureSiteSettingsAsync(db, tenant.Id, now, authorUserId, ct);

            // 2) Categories
            var catNews = await EnsureCategoryAsync(db, tenant.Id, "Tin Tức", "tin-tuc", "Bài viết cập nhật thông tin.", 1, now, authorUserId, ct);
            var catGuide = await EnsureCategoryAsync(db, tenant.Id, "Hướng Dẫn", "huong-dan", "Hướng dẫn đặt vé và lưu ý.", 2, now, authorUserId, ct);

            // 3) Tags
            var tagBus = await EnsureTagAsync(db, tenant.Id, "Xe Khách", "xe-khach", now, authorUserId, ct);
            var tagPromo = await EnsureTagAsync(db, tenant.Id, "Khuyến Mãi", "khuyen-mai", now, authorUserId, ct);
            var tagTravel = await EnsureTagAsync(db, tenant.Id, "Du Lịch", "du-lich", now, authorUserId, ct);
            var tagSafety = await EnsureTagAsync(db, tenant.Id, "An Toàn", "an-toan", now, authorUserId, ct);
            var tagTips = await EnsureTagAsync(db, tenant.Id, "Mẹo Hay", "meo-hay", now, authorUserId, ct);

            // 4) Posts (2)
            var post1 = await EnsurePostAsync(db, tenant.Id,
                title: "Ra Mắt Hệ Thống TicketBooking V3 (Demo)",
                slug: "ra-mat-ticketbooking-v3",
                summary: "Giới thiệu tổng quan hệ thống đặt vé xe/tàu/máy bay/tour/khách sạn.",
                status: NewsPostStatus.Published,
                scheduledAt: null,
                publishedAt: now.AddMinutes(-30),
                authorUserId: authorUserId,
                editorUserId: authorUserId,
                now: now,
                ct: ct);

            var post2 = await EnsurePostAsync(db, tenant.Id,
                title: "Hướng Dẫn Đặt Vé Xe Theo Sơ Đồ Ghế (Demo)",
                slug: "huong-dan-dat-ve-xe-theo-so-do-ghe",
                summary: "Các bước chọn chuyến, chọn ghế, giữ chỗ và thanh toán.",
                status: NewsPostStatus.Scheduled,
                scheduledAt: now.AddDays(1).Date.AddHours(9),
                publishedAt: null,
                authorUserId: authorUserId,
                editorUserId: authorUserId,
                now: now,
                ct: ct);

            // 5) Map categories/tags
            await EnsurePostCategoryAsync(db, tenant.Id, post1.Id, catNews.Id, now, authorUserId, ct);
            await EnsurePostTagAsync(db, tenant.Id, post1.Id, tagBus.Id, now, authorUserId, ct);
            await EnsurePostTagAsync(db, tenant.Id, post1.Id, tagTravel.Id, now, authorUserId, ct);

            await EnsurePostCategoryAsync(db, tenant.Id, post2.Id, catGuide.Id, now, authorUserId, ct);
            await EnsurePostTagAsync(db, tenant.Id, post2.Id, tagTips.Id, now, authorUserId, ct);
            await EnsurePostTagAsync(db, tenant.Id, post2.Id, tagSafety.Id, now, authorUserId, ct);

            // 6) Revisions (2 per post)
            await EnsureRevisionAsync(db, tenant.Id, post1, version: 1, "Khởi tạo bài viết", authorUserId, now.AddMinutes(-29), ct);
            await EnsureRevisionAsync(db, tenant.Id, post1, version: 2, "Bổ sung SEO/OG", authorUserId, now.AddMinutes(-28), ct);

            await EnsureRevisionAsync(db, tenant.Id, post2, version: 1, "Khởi tạo hướng dẫn", authorUserId, now.AddMinutes(-10), ct);
            await EnsureRevisionAsync(db, tenant.Id, post2, version: 2, "Bổ sung checklist giữ chỗ", authorUserId, now.AddMinutes(-9), ct);

            // 7) Redirects (demo)
            await EnsureRedirectAsync(db, tenant.Id,
                fromPath: "/tin-tuc/ra-mat-he-thong",
                toPath: "/tin-tuc/ra-mat-ticketbooking-v3",
                reason: "Đổi slug bài viết demo",
                now: now,
                userId: authorUserId,
                ct: ct);

            logger.LogInformation("CmsDemoSeed completed for tenant {TenantCode}.", TenantCode);
        }

        // ---------------------------
        // SiteSettings
        // ---------------------------
        private static async Task EnsureSiteSettingsAsync(AppDbContext db, Guid tenantId, DateTimeOffset now, Guid? userId, CancellationToken ct)
        {
            var settings = await db.CmsSiteSettings.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId, ct);

            if (settings is null)
            {
                settings = new SiteSetting
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SiteName = "TicketBooking V3",
                    SiteUrl = "https://ticketbooking.local",
                    DefaultRobots = "index,follow",
                    DefaultOgImageUrl = "https://ticketbooking.local/assets/og-default.png",
                    DefaultTwitterCard = "summary_large_image",
                    DefaultTwitterSite = "@ticketbooking",
                    DefaultSchemaJsonLd = "{\"@context\":\"https://schema.org\",\"@type\":\"WebSite\",\"name\":\"TicketBooking V3\"}",
                    BrandLogoUrl = "https://ticketbooking.local/assets/logo.png",
                    SupportEmail = "support@ticketbooking.local",
                    SupportPhone = "0900 000 000",
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedByUserId = userId
                };
                db.CmsSiteSettings.Add(settings);
                await db.SaveChangesAsync(ct);
                return;
            }

            var changed = false;
            if (settings.IsDeleted) { settings.IsDeleted = false; changed = true; }
            if (!settings.IsActive) { settings.IsActive = true; changed = true; }
            if (settings.SiteName != "TicketBooking V3") { settings.SiteName = "TicketBooking V3"; changed = true; }

            if (changed)
            {
                settings.UpdatedAt = now;
                settings.UpdatedByUserId = userId;
                await db.SaveChangesAsync(ct);
            }
        }

        // ---------------------------
        // Categories/Tags
        // ---------------------------
        private static async Task<NewsCategory> EnsureCategoryAsync(
            AppDbContext db,
            Guid tenantId,
            string name,
            string slug,
            string? description,
            int sortOrder,
            DateTimeOffset now,
            Guid? userId,
            CancellationToken ct)
        {
            slug = NormalizeSlug(slug);

            var entity = await db.CmsNewsCategories.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Slug == slug, ct);

            if (entity is null)
            {
                entity = new NewsCategory
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = NormalizeTitle(name),
                    Slug = slug,
                    Description = description,
                    SortOrder = sortOrder,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedByUserId = userId
                };

                db.CmsNewsCategories.Add(entity);
                await db.SaveChangesAsync(ct);
                return entity;
            }

            var changed = false;
            var normName = NormalizeTitle(name);

            if (entity.IsDeleted) { entity.IsDeleted = false; changed = true; }
            if (!entity.IsActive) { entity.IsActive = true; changed = true; }
            if (entity.Name != normName) { entity.Name = normName; changed = true; }
            if (entity.Description != description) { entity.Description = description; changed = true; }
            if (entity.SortOrder != sortOrder) { entity.SortOrder = sortOrder; changed = true; }

            if (changed)
            {
                entity.UpdatedAt = now;
                entity.UpdatedByUserId = userId;
                await db.SaveChangesAsync(ct);
            }

            return entity;
        }

        private static async Task<NewsTag> EnsureTagAsync(
            AppDbContext db,
            Guid tenantId,
            string name,
            string slug,
            DateTimeOffset now,
            Guid? userId,
            CancellationToken ct)
        {
            slug = NormalizeSlug(slug);

            var entity = await db.CmsNewsTags.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Slug == slug, ct);

            if (entity is null)
            {
                entity = new NewsTag
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Name = NormalizeTitle(name),
                    Slug = slug,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedByUserId = userId
                };

                db.CmsNewsTags.Add(entity);
                await db.SaveChangesAsync(ct);
                return entity;
            }

            var changed = false;
            var normName = NormalizeTitle(name);

            if (entity.IsDeleted) { entity.IsDeleted = false; changed = true; }
            if (!entity.IsActive) { entity.IsActive = true; changed = true; }
            if (entity.Name != normName) { entity.Name = normName; changed = true; }

            if (changed)
            {
                entity.UpdatedAt = now;
                entity.UpdatedByUserId = userId;
                await db.SaveChangesAsync(ct);
            }

            return entity;
        }

        // ---------------------------
        // Posts
        // ---------------------------
        private static async Task<NewsPost> EnsurePostAsync(
            AppDbContext db,
            Guid tenantId,
            string title,
            string slug,
            string summary,
            NewsPostStatus status,
            DateTimeOffset? scheduledAt,
            DateTimeOffset? publishedAt,
            Guid? authorUserId,
            Guid? editorUserId,
            DateTimeOffset now,
            CancellationToken ct)
        {
            slug = NormalizeSlug(slug);

            var post = await db.CmsNewsPosts.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Slug == slug, ct);

            var md = BuildDemoMarkdown(title);
            var html = DemoMarkdownToHtml(md);

            var wordCount = CountWords(md);
            var reading = Math.Max(1, (int)Math.Ceiling(wordCount / 200m)); // ~200w/min

            if (post is null)
            {
                post = new NewsPost
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Title = NormalizeTitle(title),
                    Slug = slug,
                    Summary = summary,

                    ContentMarkdown = md,
                    ContentHtml = html,

                    // SEO defaults
                    SeoTitle = NormalizeTitle(title),
                    SeoDescription = summary,
                    CanonicalUrl = null,
                    Robots = "index,follow",

                    OgTitle = NormalizeTitle(title),
                    OgDescription = summary,
                    OgImageUrl = null,
                    OgType = "article",

                    TwitterCard = "summary_large_image",
                    TwitterTitle = NormalizeTitle(title),
                    TwitterDescription = summary,
                    TwitterImageUrl = null,

                    SchemaJsonLd = "{\"@context\":\"https://schema.org\",\"@type\":\"Article\"}",

                    Status = status,
                    ScheduledAt = scheduledAt,
                    PublishedAt = publishedAt,
                    UnpublishedAt = null,

                    AuthorUserId = authorUserId,
                    EditorUserId = editorUserId,
                    LastEditedAt = now,

                    ViewCount = 0,
                    WordCount = wordCount,
                    ReadingTimeMinutes = reading,

                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedByUserId = authorUserId
                };

                db.CmsNewsPosts.Add(post);
                await db.SaveChangesAsync(ct);
                return post;
            }

            var changed = false;
            var normTitle = NormalizeTitle(title);

            if (post.IsDeleted) { post.IsDeleted = false; changed = true; }
            if (post.Title != normTitle) { post.Title = normTitle; changed = true; }
            if (post.Summary != summary) { post.Summary = summary; changed = true; }

            // keep content stable for idempotency; if you want update, set changed accordingly
            if (post.ContentMarkdown != md) { post.ContentMarkdown = md; changed = true; }
            if (post.ContentHtml != html) { post.ContentHtml = html; changed = true; }

            if (post.Status != status) { post.Status = status; changed = true; }
            if (post.ScheduledAt != scheduledAt) { post.ScheduledAt = scheduledAt; changed = true; }
            if (post.PublishedAt != publishedAt) { post.PublishedAt = publishedAt; changed = true; }

            if (post.SeoTitle != normTitle) { post.SeoTitle = normTitle; changed = true; }
            if (post.SeoDescription != summary) { post.SeoDescription = summary; changed = true; }

            if (post.WordCount != wordCount) { post.WordCount = wordCount; changed = true; }
            if (post.ReadingTimeMinutes != reading) { post.ReadingTimeMinutes = reading; changed = true; }

            if (changed)
            {
                post.LastEditedAt = now;
                post.EditorUserId = editorUserId;
                post.UpdatedAt = now;
                post.UpdatedByUserId = editorUserId;
                await db.SaveChangesAsync(ct);
            }

            return post;
        }

        // ---------------------------
        // Mappings
        // ---------------------------
        private static async Task EnsurePostCategoryAsync(
            AppDbContext db,
            Guid tenantId,
            Guid postId,
            Guid categoryId,
            DateTimeOffset now,
            Guid? userId,
            CancellationToken ct)
        {
            var existing = await db.CmsNewsPostCategories.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.PostId == postId && x.CategoryId == categoryId, ct);

            if (existing is null)
            {
                db.CmsNewsPostCategories.Add(new NewsPostCategory
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PostId = postId,
                    CategoryId = categoryId,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedByUserId = userId
                });
                await db.SaveChangesAsync(ct);
                return;
            }

            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.UpdatedAt = now;
                existing.UpdatedByUserId = userId;
                await db.SaveChangesAsync(ct);
            }
        }

        private static async Task EnsurePostTagAsync(
            AppDbContext db,
            Guid tenantId,
            Guid postId,
            Guid tagId,
            DateTimeOffset now,
            Guid? userId,
            CancellationToken ct)
        {
            var existing = await db.CmsNewsPostTags.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.PostId == postId && x.TagId == tagId, ct);

            if (existing is null)
            {
                db.CmsNewsPostTags.Add(new NewsPostTag
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    PostId = postId,
                    TagId = tagId,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedByUserId = userId
                });
                await db.SaveChangesAsync(ct);
                return;
            }

            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.UpdatedAt = now;
                existing.UpdatedByUserId = userId;
                await db.SaveChangesAsync(ct);
            }
        }

        // ---------------------------
        // Revisions + Redirects
        // ---------------------------
        private static async Task EnsureRevisionAsync(
            AppDbContext db,
            Guid tenantId,
            NewsPost post,
            int version,
            string changeNote,
            Guid? editorUserId,
            DateTimeOffset editedAt,
            CancellationToken ct)
        {
            var existing = await db.CmsNewsPostRevisions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.PostId == post.Id && x.VersionNumber == version, ct);

            if (existing is not null && !existing.IsDeleted)
                return;

            if (existing is not null)
            {
                existing.IsDeleted = false;
                existing.UpdatedAt = editedAt;
                existing.UpdatedByUserId = editorUserId;
                await db.SaveChangesAsync(ct);
                return;
            }

            var rev = new NewsPostRevision
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PostId = post.Id,
                VersionNumber = version,

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
                EditorUserId = editorUserId,
                EditedAt = editedAt,

                IsDeleted = false,
                CreatedAt = editedAt,
                CreatedByUserId = editorUserId
            };

            db.CmsNewsPostRevisions.Add(rev);
            await db.SaveChangesAsync(ct);
        }

        private static async Task EnsureRedirectAsync(
            AppDbContext db,
            Guid tenantId,
            string fromPath,
            string toPath,
            string reason,
            DateTimeOffset now,
            Guid? userId,
            CancellationToken ct)
        {
            fromPath = NormalizePath(fromPath);
            toPath = NormalizePath(toPath);

            var existing = await db.CmsNewsRedirects.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.FromPath == fromPath, ct);

            if (existing is null)
            {
                var r = new NewsRedirect
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    FromPath = fromPath,
                    ToPath = toPath,
                    StatusCode = RedirectStatusCode.MovedPermanently301,
                    IsRegex = false,
                    Reason = reason,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedByUserId = userId
                };
                db.CmsNewsRedirects.Add(r);
                await db.SaveChangesAsync(ct);
                return;
            }

            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }
            if (existing.ToPath != toPath) { existing.ToPath = toPath; changed = true; }
            if (existing.Reason != reason) { existing.Reason = reason; changed = true; }
            if (existing.StatusCode != RedirectStatusCode.MovedPermanently301) { existing.StatusCode = RedirectStatusCode.MovedPermanently301; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = now;
                existing.UpdatedByUserId = userId;
                await db.SaveChangesAsync(ct);
            }
        }

        // ---------------------------
        // Utils
        // ---------------------------
        private static string NormalizeSlug(string slug)
        {
            slug = (slug ?? "").Trim().ToLowerInvariant();
            slug = slug.Replace(' ', '-');
            slug = RemoveDiacritics(slug);
            slug = string.Concat(slug.Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_'));
            slug = slug.Replace("--", "-");
            if (slug.Length > 300) slug = slug[..300];
            return slug;
        }

        private static string NormalizeTitle(string s)
        {
            s = (s ?? "").Trim();
            if (s.Length == 0) return "";
            // Title Case basic
            return string.Join(' ', s.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p[1..].ToLowerInvariant() : "")));
        }

        private static string NormalizePath(string path)
        {
            path = (path ?? "").Trim();
            if (path.Length == 0) return "/";
            if (!path.StartsWith('/')) path = "/" + path;
            if (path.Length > 500) path = path[..500];
            return path;
        }

        private static string RemoveDiacritics(string input)
        {
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var ch in normalized)
            {
                var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }

            var no = sb.ToString().Normalize(NormalizationForm.FormC);
            return no.Replace('đ', 'd').Replace('Đ', 'D');
        }

        private static int CountWords(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown)) return 0;
            var text = markdown
                .Replace("#", " ")
                .Replace("*", " ")
                .Replace("`", " ")
                .Replace(">", " ")
                .Replace("-", " ")
                .Replace("_", " ");

            return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        }

        private static string BuildDemoMarkdown(string title)
        {
            return $@"# {title}

Chào mừng bạn đến với **TicketBooking V3**.

## Hệ thống hỗ trợ
- Đặt vé **Xe khách / Tàu / Máy bay**
- Đặt **Tour du lịch**
- Đặt phòng **Khách sạn**

## Điểm nổi bật
- Multi-tenant theo đối tác (NX/VT/VMM/KS/TOUR)
- Giữ chỗ (HOLD) chống đặt trùng
- Booking snapshot & workflow chuẩn

> Đây là bài viết demo phục vụ Phase 7 CMS/SEO.
";
        }

        private static string DemoMarkdownToHtml(string markdown)
        {
            // Minimal conversion for seed demo; real conversion will be in CMS editor later.
            var html = markdown
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");

            html = html.Replace("\r\n", "\n");

            // headings
            html = html.Replace("\n# ", "\n<h1>").Replace("\n## ", "\n<h2>");

            // close tags naive
            var lines = html.Split('\n');
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                if (line.StartsWith("<h1>"))
                    sb.AppendLine(line + "</h1>");
                else if (line.StartsWith("<h2>"))
                    sb.AppendLine(line + "</h2>");
                else if (line.StartsWith("- "))
                    sb.AppendLine("<li>" + line[2..] + "</li>");
                else if (line.StartsWith("> "))
                    sb.AppendLine("<blockquote>" + line[2..] + "</blockquote>");
                else if (line.Trim().Length == 0)
                    sb.AppendLine("");
                else
                    sb.AppendLine("<p>" + line + "</p>");
            }

            return sb.ToString()
                .Replace("**", "<b>").Replace("<b><b>", "</b>"); // cheap bold demo
        }
    }
}

