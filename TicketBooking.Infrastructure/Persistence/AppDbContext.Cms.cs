// FILE #126: TicketBooking.Infrastructure/Persistence/AppDbContext.Cms.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Cms;
using TicketBooking.Infrastructure.Persistence.Cms;

namespace TicketBooking.Infrastructure.Persistence
{
    /// <summary>
    /// Phase 7 - CMS/SEO DbSets + model hook.
    /// Pattern matches Bus/Train/Flight: DbSets + ApplyCmsModel(ModelBuilder).
    /// </summary>
    public partial class AppDbContext
    {
        // DbSets
        public DbSet<MediaAsset> CmsMediaAssets => Set<MediaAsset>();
        public DbSet<NewsCategory> CmsNewsCategories => Set<NewsCategory>();
        public DbSet<NewsTag> CmsNewsTags => Set<NewsTag>();
        public DbSet<NewsPost> CmsNewsPosts => Set<NewsPost>();
        public DbSet<NewsPostCategory> CmsNewsPostCategories => Set<NewsPostCategory>();
        public DbSet<NewsPostTag> CmsNewsPostTags => Set<NewsPostTag>();
        public DbSet<NewsPostRevision> CmsNewsPostRevisions => Set<NewsPostRevision>();
        public DbSet<NewsRedirect> CmsNewsRedirects => Set<NewsRedirect>();
        public DbSet<SiteSetting> CmsSiteSettings => Set<SiteSetting>();
    }

    /// <summary>
    /// Model applier (called from AppDbContext.OnModelCreating).
    /// Ensures CmsConfigurations are registered.
    /// </summary>
    public static class AppDbContextCmsModel
    {
        public static void ApplyCmsModel(ModelBuilder builder)
        {
            // Register all CMS configurations in this assembly
            builder.ApplyConfigurationsFromAssembly(typeof(MediaAssetConfiguration).Assembly);
        }
    }
}

