// FILE #017: TicketBooking.Infrastructure/Persistence/AppDbContext.Tenants.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Persistence.Configurations;

namespace TicketBooking.Infrastructure.Persistence
{
    /// <summary>
    /// Phase 3: Tenants module (schema: tenants).
    /// </summary>
    public sealed partial class AppDbContext
    {
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<TenantUser> TenantUsers => Set<TenantUser>();
    }

    // We keep all EF model building in the main AppDbContext file by calling ApplyTenantsModel(builder).
    // This avoids scattering OnModelCreating overrides across partials.
    public static class AppDbContextTenantsModel
    {
        public static void ApplyTenantsModel(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new TenantConfiguration());
            builder.ApplyConfiguration(new TenantUserConfiguration());
        }
    }
}