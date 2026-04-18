// FILE #054: TicketBooking.Infrastructure/Persistence/AppDbContext.Catalog.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Catalog;
using TicketBooking.Infrastructure.Persistence.Catalog;

namespace TicketBooking.Infrastructure.Persistence
{
    /// <summary>
    /// Phase 6: Catalog module (schema: catalog)
    /// </summary>
    public sealed partial class AppDbContext
    {
        public DbSet<Location> Locations => Set<Location>();
        public DbSet<Provider> Providers => Set<Provider>();
    }

    public static class AppDbContextCatalogModel
    {
        public static void ApplyCatalogModel(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new LocationConfiguration());
            builder.ApplyConfiguration(new ProviderConfiguration());
        }
    }
}