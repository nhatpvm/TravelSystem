// FILE #038: TicketBooking.Infrastructure/Seed/SeedQueryExtensions.cs
using Microsoft.EntityFrameworkCore;

namespace TicketBooking.Infrastructure.Seed
{
    /// <summary>
    /// Helpers to make seeds resilient when global query filters (IsDeleted/TenantId) are enabled.
    /// Use QueryUnfiltered() when seeding/updating lookup data.
    /// </summary>
    public static class SeedQueryExtensions
    {
        public static IQueryable<T> QueryUnfiltered<T>(this DbSet<T> set) where T : class
            => set.IgnoreQueryFilters();
    }
}