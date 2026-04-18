// FILE #059: TicketBooking.Infrastructure/Persistence/AppDbContext.Fleet.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Fleet;
using TicketBooking.Infrastructure.Persistence.Fleet;

namespace TicketBooking.Infrastructure.Persistence
{
    /// <summary>
    /// Phase 6: Fleet module (schema: fleet)
    /// </summary>
    public sealed partial class AppDbContext
    {
        public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
        public DbSet<SeatMap> SeatMaps => Set<SeatMap>();
        public DbSet<Seat> Seats => Set<Seat>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<BusVehicleDetail> BusVehicleDetails => Set<BusVehicleDetail>();
    }

    public static class AppDbContextFleetModel
    {
        public static void ApplyFleetModel(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new VehicleModelConfiguration());
            builder.ApplyConfiguration(new SeatMapConfiguration());
            builder.ApplyConfiguration(new SeatConfiguration());
            builder.ApplyConfiguration(new VehicleConfiguration());
            builder.ApplyConfiguration(new BusVehicleDetailConfiguration());
        }
    }
}