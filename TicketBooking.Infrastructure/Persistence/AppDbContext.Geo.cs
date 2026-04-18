// FILE #050: TicketBooking.Infrastructure/Persistence/AppDbContext.Geo.cs  (UPDATE)
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Geo;
using TicketBooking.Infrastructure.Persistence.Geo;

namespace TicketBooking.Infrastructure.Persistence
{
    public sealed partial class AppDbContext
    {
        public DbSet<Province> Provinces => Set<Province>();
        public DbSet<District> Districts => Set<District>();
        public DbSet<Ward> Wards => Set<Ward>();

        public DbSet<GeoSyncLog> GeoSyncLogs => Set<GeoSyncLog>();
    }

    public static class AppDbContextGeoModel
    {
        public static void ApplyGeoModel(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new ProvinceConfiguration());
            builder.ApplyConfiguration(new DistrictConfiguration());
            builder.ApplyConfiguration(new WardConfiguration());

            builder.ApplyConfiguration(new GeoSyncLogConfiguration());
        }
    }
}