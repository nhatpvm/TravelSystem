// FILE: TicketBooking.Infrastructure/Persistence/AppDbContext.Flight.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Flight;
using TicketBooking.Infrastructure.Persistence.Flight;
using FlightEntity = TicketBooking.Domain.Flight.Flight;

namespace TicketBooking.Infrastructure.Persistence
{
    /// <summary>
    /// Flight module DbSets + model hook.
    /// File này chỉ đăng ký DbSet và hook ApplyFlightModel.
    /// Toàn bộ mapping chi tiết nằm ở FlightConfigurations.cs
    /// </summary>
    public partial class AppDbContext
    {
        public DbSet<Airline> FlightAirlines => Set<Airline>();
        public DbSet<Airport> FlightAirports => Set<Airport>();
        public DbSet<AircraftModel> FlightAircraftModels => Set<AircraftModel>();
        public DbSet<Aircraft> FlightAircrafts => Set<Aircraft>();
        public DbSet<CabinSeatMap> FlightCabinSeatMaps => Set<CabinSeatMap>();
        public DbSet<CabinSeat> FlightCabinSeats => Set<CabinSeat>();
        public DbSet<FlightEntity> FlightFlights => Set<FlightEntity>();
        public DbSet<FareClass> FlightFareClasses => Set<FareClass>();
        public DbSet<FareRule> FlightFareRules => Set<FareRule>();
        public DbSet<Offer> FlightOffers => Set<Offer>();
        public DbSet<OfferSegment> FlightOfferSegments => Set<OfferSegment>();
        public DbSet<AncillaryDefinition> FlightAncillaryDefinitions => Set<AncillaryDefinition>();
        public DbSet<OfferTaxFeeLine> FlightOfferTaxFeeLines => Set<OfferTaxFeeLine>();
    }

    public static class AppDbContextFlightModel
    {
        public static void ApplyFlightModel(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(typeof(FlightConfigurations).Assembly);
        }
    }
}

