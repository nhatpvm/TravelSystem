// FILE #067: TicketBooking.Infrastructure/Persistence/AppDbContext.Bus.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Bus;
using TicketBooking.Infrastructure.Persistence.Bus;

namespace TicketBooking.Infrastructure.Persistence
{
    /// <summary>
    /// Phase 8: Bus Level 3 module (schema: bus)
    /// DbSets + model apply.
    /// </summary>
    public sealed partial class AppDbContext
    {
        public DbSet<StopPoint> BusStopPoints => Set<StopPoint>();
        public DbSet<BusRoute> BusRoutes => Set<BusRoute>();
        public DbSet<RouteStop> BusRouteStops => Set<RouteStop>();

        public DbSet<Trip> BusTrips => Set<Trip>();
        public DbSet<TripStopTime> BusTripStopTimes => Set<TripStopTime>();
        public DbSet<TripStopPickupPoint> BusTripStopPickupPoints => Set<TripStopPickupPoint>();
        public DbSet<TripStopDropoffPoint> BusTripStopDropoffPoints => Set<TripStopDropoffPoint>();

        public DbSet<TripSegmentPrice> BusTripSegmentPrices => Set<TripSegmentPrice>();
        public DbSet<TripSeatHold> BusTripSeatHolds => Set<TripSeatHold>();
    }

    public static class AppDbContextBusModel
    {
        public static void ApplyBusModel(ModelBuilder builder)
        {
            // All bus configurations are in BusConfigurations.cs
            builder.ApplyConfigurationsFromAssembly(typeof(StopPointConfiguration).Assembly);
        }
    }
}