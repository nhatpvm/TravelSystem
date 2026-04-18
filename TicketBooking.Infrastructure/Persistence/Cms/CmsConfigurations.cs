// FILE #125: TicketBooking.Infrastructure/Persistence/Cms/CmsConfigurations.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Cms;

namespace TicketBooking.Infrastructure.Persistence.Cms
{
    /// <summary>
    /// Phase 7 - CMS/SEO configurations (schema "cms")
    /// - Multi-tenant: all tables have TenantId
    /// - Soft delete: IsDeleted + global query filter already applied in AppDbContext
    /// - Concurrency: RowVersion (your conventions will set rowversion)
    ///
    /// IMPORTANT:
    /// - Avoid cascade path issues to AspNetUsers: use DeleteBehavior.NoAction/Restrict.
    /// - Unique indexes: (TenantId, Slug), (TenantId, FromPath), etc.
    /// </summary>
    public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
    {
        public void Configure(EntityTypeBuilder<MediaAsset> b)
        {
            b.ToTable("MediaAssets", "cms");

            b.HasKey(x => x.Id);

            b.Property(x => x.StorageProvider).HasMaxLength(50).IsRequired();
            b.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();

            b.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200);
            b.Property(x => x.AltText).HasMaxLength(300);

            b.Property(x => x.PublicUrl).HasMaxLength(1000);

            b.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
            b.Property(x => x.ChecksumSha256).HasMaxLength(128);

            b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

            b.HasIndex(x => new { x.TenantId, x.Type });
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }
    }

    public sealed class NewsCategoryConfiguration : IEntityTypeConfiguration<NewsCategory>
    {
        public void Configure(EntityTypeBuilder<NewsCategory> b)
        {
            b.ToTable("NewsCategories", "cms");

            b.HasKey(x => x.Id);

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);

            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
            b.HasIndex(x => new { x.TenantId, x.SortOrder });

            // Avoid cascade cycles to identity (CreatedByUserId/UpdatedByUserId) handled by FK convention if any;
            // Here we keep no navigation, so no FK mapping is required.
        }
    }

    public sealed class NewsTagConfiguration : IEntityTypeConfiguration<NewsTag>
    {
        public void Configure(EntityTypeBuilder<NewsTag> b)
        {
            b.ToTable("NewsTags", "cms");

            b.HasKey(x => x.Id);

            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(200).IsRequired();

            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }
    }

    public sealed class NewsPostConfiguration : IEntityTypeConfiguration<NewsPost>
    {
        public void Configure(EntityTypeBuilder<NewsPost> b)
        {
            b.ToTable("NewsPosts", "cms");

            b.HasKey(x => x.Id);

            b.Property(x => x.Title).HasMaxLength(300).IsRequired();
            b.Property(x => x.Slug).HasMaxLength(300).IsRequired();
            b.Property(x => x.Summary).HasMaxLength(2000);

            b.Property(x => x.ContentMarkdown).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.ContentHtml).HasColumnType("nvarchar(max)").IsRequired();

            b.Property(x => x.CoverImageUrl).HasMaxLength(1000);

            // SEO
            b.Property(x => x.SeoTitle).HasMaxLength(300);
            b.Property(x => x.SeoDescription).HasMaxLength(2000);
            b.Property(x => x.SeoKeywords).HasMaxLength(2000);
            b.Property(x => x.CanonicalUrl).HasMaxLength(1000);
            b.Property(x => x.Robots).HasMaxLength(200);

            // OG
            b.Property(x => x.OgTitle).HasMaxLength(300);
            b.Property(x => x.OgDescription).HasMaxLength(2000);
            b.Property(x => x.OgImageUrl).HasMaxLength(1000);
            b.Property(x => x.OgType).HasMaxLength(50);

            // Twitter
            b.Property(x => x.TwitterCard).HasMaxLength(50);
            b.Property(x => x.TwitterSite).HasMaxLength(100);
            b.Property(x => x.TwitterCreator).HasMaxLength(100);
            b.Property(x => x.TwitterTitle).HasMaxLength(300);
            b.Property(x => x.TwitterDescription).HasMaxLength(2000);
            b.Property(x => x.TwitterImageUrl).HasMaxLength(1000);

            b.Property(x => x.SchemaJsonLd).HasColumnType("nvarchar(max)");

            // Workflow
            b.HasIndex(x => new { x.TenantId, x.Slug }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.HasIndex(x => new { x.TenantId, x.PublishedAt });
            b.HasIndex(x => new { x.TenantId, x.ScheduledAt });

            // Optional FK to MediaAssets (no navigation => configure FK only)
            b.HasOne<MediaAsset>()
                .WithMany()
                .HasForeignKey(x => x.CoverMediaAssetId)
                .OnDelete(DeleteBehavior.SetNull);

            // Optional FKs to Identity users (avoid cascade path)
            b.HasOne<TicketBooking.Infrastructure.Identity.AppUser>()
                .WithMany()
                .HasForeignKey(x => x.AuthorUserId)
                .OnDelete(DeleteBehavior.NoAction);

            b.HasOne<TicketBooking.Infrastructure.Identity.AppUser>()
                .WithMany()
                .HasForeignKey(x => x.EditorUserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    public sealed class NewsPostCategoryConfiguration : IEntityTypeConfiguration<NewsPostCategory>
    {
        public void Configure(EntityTypeBuilder<NewsPostCategory> b)
        {
            b.ToTable("NewsPostCategories", "cms");

            b.HasKey(x => x.Id);

            b.HasIndex(x => new { x.TenantId, x.PostId, x.CategoryId }).IsUnique();

            b.HasOne<NewsPost>()
                .WithMany()
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<NewsCategory>()
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class NewsPostTagConfiguration : IEntityTypeConfiguration<NewsPostTag>
    {
        public void Configure(EntityTypeBuilder<NewsPostTag> b)
        {
            b.ToTable("NewsPostTags", "cms");

            b.HasKey(x => x.Id);

            b.HasIndex(x => new { x.TenantId, x.PostId, x.TagId }).IsUnique();

            b.HasOne<NewsPost>()
                .WithMany()
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<NewsTag>()
                .WithMany()
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class NewsPostRevisionConfiguration : IEntityTypeConfiguration<NewsPostRevision>
    {
        public void Configure(EntityTypeBuilder<NewsPostRevision> b)
        {
            b.ToTable("NewsPostRevisions", "cms");

            b.HasKey(x => x.Id);

            b.Property(x => x.Title).HasMaxLength(300).IsRequired();
            b.Property(x => x.Summary).HasMaxLength(2000);

            b.Property(x => x.ContentMarkdown).HasColumnType("nvarchar(max)").IsRequired();
            b.Property(x => x.ContentHtml).HasColumnType("nvarchar(max)").IsRequired();

            b.Property(x => x.SeoTitle).HasMaxLength(300);
            b.Property(x => x.SeoDescription).HasMaxLength(2000);
            b.Property(x => x.CanonicalUrl).HasMaxLength(1000);
            b.Property(x => x.Robots).HasMaxLength(200);

            b.Property(x => x.OgTitle).HasMaxLength(300);
            b.Property(x => x.OgDescription).HasMaxLength(2000);
            b.Property(x => x.OgImageUrl).HasMaxLength(1000);

            b.Property(x => x.TwitterCard).HasMaxLength(50);
            b.Property(x => x.TwitterTitle).HasMaxLength(300);
            b.Property(x => x.TwitterDescription).HasMaxLength(2000);
            b.Property(x => x.TwitterImageUrl).HasMaxLength(1000);

            b.Property(x => x.SchemaJsonLd).HasColumnType("nvarchar(max)");
            b.Property(x => x.ChangeNote).HasMaxLength(1000);

            b.HasIndex(x => new { x.TenantId, x.PostId, x.VersionNumber }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.PostId, x.EditedAt });

            b.HasOne<NewsPost>()
                .WithMany()
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TicketBooking.Infrastructure.Identity.AppUser>()
                .WithMany()
                .HasForeignKey(x => x.EditorUserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    public sealed class NewsRedirectConfiguration : IEntityTypeConfiguration<NewsRedirect>
    {
        public void Configure(EntityTypeBuilder<NewsRedirect> b)
        {
            b.ToTable("NewsRedirects", "cms");

            b.HasKey(x => x.Id);

            b.Property(x => x.FromPath).HasMaxLength(500).IsRequired();
            b.Property(x => x.ToPath).HasMaxLength(1000).IsRequired();
            b.Property(x => x.Reason).HasMaxLength(500);

            b.HasIndex(x => new { x.TenantId, x.FromPath }).IsUnique();
            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }
    }

    public sealed class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
    {
        public void Configure(EntityTypeBuilder<SiteSetting> b)
        {
            b.ToTable("SiteSettings", "cms");

            b.HasKey(x => x.Id);

            // One settings row per tenant (unique)
            b.HasIndex(x => x.TenantId).IsUnique();

            b.Property(x => x.SiteName).HasMaxLength(200).IsRequired();
            b.Property(x => x.SiteUrl).HasMaxLength(1000);

            b.Property(x => x.DefaultRobots).HasMaxLength(200);
            b.Property(x => x.DefaultOgImageUrl).HasMaxLength(1000);
            b.Property(x => x.DefaultTwitterCard).HasMaxLength(50);
            b.Property(x => x.DefaultTwitterSite).HasMaxLength(100);
            b.Property(x => x.DefaultSchemaJsonLd).HasColumnType("nvarchar(max)");

            b.Property(x => x.BrandLogoUrl).HasMaxLength(1000);
            b.Property(x => x.SupportEmail).HasMaxLength(200);
            b.Property(x => x.SupportPhone).HasMaxLength(50);

            b.HasIndex(x => new { x.TenantId, x.IsActive });
        }
    }
}

