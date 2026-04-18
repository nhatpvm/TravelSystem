// FILE #124: TicketBooking.Domain/Cms/CmsEntities.cs
using System;

namespace TicketBooking.Domain.Cms
{
    public enum NewsPostStatus
    {
        Draft = 1,
        Scheduled = 2,
        Published = 3,
        Unpublished = 4,
        Archived = 5
    }

    public enum MediaAssetType
    {
        Image = 1,
        Video = 2,
        Document = 3,
        Other = 99
    }

    public enum RedirectStatusCode
    {
        MovedPermanently301 = 301,
        Found302 = 302,
        TemporaryRedirect307 = 307,
        PermanentRedirect308 = 308
    }

    /// <summary>
    /// cms.MediaAssets
    /// Used by CMS posts and other modules later (shared asset store).
    /// </summary>
    public sealed class MediaAsset
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public MediaAssetType Type { get; set; } = MediaAssetType.Image;

        public string FileName { get; set; } = "";
        public string? Title { get; set; }
        public string? AltText { get; set; }

        public string StorageProvider { get; set; } = "local";   // local/s3/azure...
        public string StorageKey { get; set; } = "";             // path/key in storage
        public string? PublicUrl { get; set; }                   // optional CDN/public url

        public string MimeType { get; set; } = "application/octet-stream";
        public long SizeBytes { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string? ChecksumSha256 { get; set; }

        public string? MetadataJson { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// cms.NewsCategories
    /// </summary>
    public sealed class NewsCategory
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";                  // unique per tenant
        public string? Description { get; set; }
        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// cms.NewsTags
    /// </summary>
    public sealed class NewsTag
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";                  // unique per tenant

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// cms.NewsPosts
    /// - SEO fields: canonical/robots/OG/Twitter/Schema JSON-LD
    /// - Content: Markdown + Html
    /// - Workflow: ScheduledAt/PublishedAt/UnpublishedAt + Status
    /// </summary>
    public sealed class NewsPost
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        // Identity
        public string Title { get; set; } = "";
        public string Slug { get; set; } = "";                  // unique per tenant
        public string? Summary { get; set; }

        // Content
        public string ContentMarkdown { get; set; } = "";
        public string ContentHtml { get; set; } = "";

        // Cover/Thumbnail
        public Guid? CoverMediaAssetId { get; set; }
        public string? CoverImageUrl { get; set; }              // optional shortcut for external

        // SEO basic
        public string? SeoTitle { get; set; }                   // if null => Title
        public string? SeoDescription { get; set; }             // if null => Summary
        public string? SeoKeywords { get; set; }                // comma separated
        public string? CanonicalUrl { get; set; }
        public string? Robots { get; set; }                     // e.g. "index,follow" / "noindex,nofollow"

        // Open Graph
        public string? OgTitle { get; set; }
        public string? OgDescription { get; set; }
        public string? OgImageUrl { get; set; }
        public string? OgType { get; set; } = "article";

        // Twitter
        public string? TwitterCard { get; set; } = "summary_large_image";
        public string? TwitterSite { get; set; }
        public string? TwitterCreator { get; set; }
        public string? TwitterTitle { get; set; }
        public string? TwitterDescription { get; set; }
        public string? TwitterImageUrl { get; set; }

        // Structured data (JSON-LD)
        public string? SchemaJsonLd { get; set; }

        // Workflow
        public NewsPostStatus Status { get; set; } = NewsPostStatus.Draft;
        public DateTimeOffset? ScheduledAt { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public DateTimeOffset? UnpublishedAt { get; set; }

        // Authoring
        public Guid? AuthorUserId { get; set; }
        public Guid? EditorUserId { get; set; }
        public DateTimeOffset? LastEditedAt { get; set; }

        // Analytics (simple)
        public int ViewCount { get; set; }
        public int WordCount { get; set; }
        public int ReadingTimeMinutes { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// cms.NewsPostCategories (many-to-many)
    /// </summary>
    public sealed class NewsPostCategory
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid PostId { get; set; }
        public Guid CategoryId { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// cms.NewsPostTags (many-to-many)
    /// </summary>
    public sealed class NewsPostTag
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid PostId { get; set; }
        public Guid TagId { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// cms.NewsPostRevisions
    /// Keeps a history of edits (audit nội dung).
    /// </summary>
    public sealed class NewsPostRevision
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid PostId { get; set; }
        public int VersionNumber { get; set; }

        public string Title { get; set; } = "";
        public string? Summary { get; set; }
        public string ContentMarkdown { get; set; } = "";
        public string ContentHtml { get; set; } = "";

        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? CanonicalUrl { get; set; }
        public string? Robots { get; set; }
        public string? OgTitle { get; set; }
        public string? OgDescription { get; set; }
        public string? OgImageUrl { get; set; }
        public string? TwitterCard { get; set; }
        public string? TwitterTitle { get; set; }
        public string? TwitterDescription { get; set; }
        public string? TwitterImageUrl { get; set; }
        public string? SchemaJsonLd { get; set; }

        public string? ChangeNote { get; set; }                // why this revision
        public Guid? EditorUserId { get; set; }
        public DateTimeOffset EditedAt { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// cms.NewsRedirects
    /// Redirect 301/302 when slug/url changes.
    /// </summary>
    public sealed class NewsRedirect
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public string FromPath { get; set; } = "";             // unique per tenant
        public string ToPath { get; set; } = "";

        public RedirectStatusCode StatusCode { get; set; } = RedirectStatusCode.MovedPermanently301;

        public bool IsRegex { get; set; }
        public string? Reason { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// cms.SiteSettings
    /// Default SEO/brand settings per tenant.
    /// </summary>
    public sealed class SiteSetting
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public string SiteName { get; set; } = "TicketBooking";
        public string? SiteUrl { get; set; }                   // base url for canonical

        // Default SEO
        public string? DefaultRobots { get; set; }             // e.g. "index,follow"
        public string? DefaultOgImageUrl { get; set; }
        public string? DefaultTwitterCard { get; set; } = "summary_large_image";
        public string? DefaultTwitterSite { get; set; }
        public string? DefaultSchemaJsonLd { get; set; }       // org/website schema

        public string? BrandLogoUrl { get; set; }
        public string? SupportEmail { get; set; }
        public string? SupportPhone { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}

