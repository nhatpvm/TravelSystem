// FILE #078: TicketBooking.Infrastructure/Persistence/AppDbContext.Train.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Persistence.Train;

namespace TicketBooking.Infrastructure.Persistence
{
    /// <summary>
    /// Phase 9: Train Level 3 module (schema: train)
    /// DbSets + model apply.
    /// </summary>
    public sealed partial class AppDbContext
    {
        public DbSet<TrainStopPoint> TrainStopPoints => Set<TrainStopPoint>();
        public DbSet<TrainRoute> TrainRoutes => Set<TrainRoute>();
        public DbSet<TrainRouteStop> TrainRouteStops => Set<TrainRouteStop>();

        public DbSet<TrainTrip> TrainTrips => Set<TrainTrip>();
        public DbSet<TrainTripStopTime> TrainTripStopTimes => Set<TrainTripStopTime>();
        public DbSet<TrainTripSegmentPrice> TrainTripSegmentPrices => Set<TrainTripSegmentPrice>();

        public DbSet<TrainCar> TrainCars => Set<TrainCar>();
        public DbSet<TrainCarSeat> TrainCarSeats => Set<TrainCarSeat>();

        public DbSet<TrainTripSeatHold> TrainTripSeatHolds => Set<TrainTripSeatHold>();
    }

    public static class AppDbContextTrainModel
    {
        public static void ApplyTrainModel(ModelBuilder builder)
        {
            // All train configurations are in TrainConfigurations.cs
            builder.ApplyConfigurationsFromAssembly(typeof(TrainStopPointConfiguration).Assembly);
        }
    }
}