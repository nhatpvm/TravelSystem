using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketBooking.Api.Contracts.Flight;
using TicketBooking.Api.Services.Flight;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Flight;
using TicketBooking.Domain.Tenants;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Tests.Flight;

public sealed class FlightInventoryAndPublicQueryTests
{
    [Fact]
    public async Task HoldAsync_DecrementsCanonicalInventory_AndReleaseRestoresIt()
    {
        var tenantContext = new TenantContext();
        using var context = CreateDbContext(tenantContext);
        var seeded = await SeedFlightContextAsync(context, tenantContext);
        var adapter = new FlightTourPackageReservationAdapter(context);

        var holdResult = await adapter.HoldAsync(
            CreateHoldRequest(seeded, seeded.OldOfferId, quantity: 2),
            CancellationToken.None);

        Assert.Equal(TourPackageReservationHoldOutcomeStatus.Held, holdResult.Status);
        Assert.False(string.IsNullOrWhiteSpace(holdResult.SourceHoldToken));

        var canonicalAfterHold = await context.FlightOffers.IgnoreQueryFilters().FirstAsync(x => x.Id == seeded.LatestOfferId);
        var staleAfterHold = await context.FlightOffers.IgnoreQueryFilters().FirstAsync(x => x.Id == seeded.OldOfferId);

        Assert.Equal(1, canonicalAfterHold.SeatsAvailable);
        Assert.Equal(9, staleAfterHold.SeatsAvailable);

        await adapter.ReleaseAsync(
            new TourPackageSourceReservationReleaseRequest
            {
                UserId = seeded.UserId,
                Reservation = CreateReservation(seeded),
                Item = new TourPackageReservationItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = seeded.TenantId,
                    TourPackageReservationId = Guid.NewGuid(),
                    TourPackageComponentId = seeded.Component.Id,
                    TourPackageComponentOptionId = seeded.StaticOption.Id,
                    ComponentType = seeded.Component.ComponentType,
                    SourceType = TourPackageSourceType.Flight,
                    SourceEntityId = seeded.OldOfferId,
                    Status = TourPackageReservationItemStatus.Held,
                    Quantity = 2,
                    CurrencyCode = "JPY",
                    UnitPrice = 22_000m,
                    LineAmount = 44_000m,
                    SourceHoldToken = holdResult.SourceHoldToken,
                    CreatedAt = seeded.OperationTime
                }
            },
            CancellationToken.None);

        var canonicalAfterRelease = await context.FlightOffers.IgnoreQueryFilters().FirstAsync(x => x.Id == seeded.LatestOfferId);
        Assert.Equal(3, canonicalAfterRelease.SeatsAvailable);
    }

    [Fact]
    public async Task ConfirmAsync_KeepsHeldInventoryConsumed_UntilCancellationRestoresIt()
    {
        var tenantContext = new TenantContext();
        using var context = CreateDbContext(tenantContext);
        var seeded = await SeedFlightContextAsync(context, tenantContext);

        var reservationAdapter = new FlightTourPackageReservationAdapter(context);
        var bookingAdapter = new FlightTourPackageBookingAdapter(context);
        var cancellationAdapter = new FlightTourPackageCancellationAdapter(context);

        var holdResult = await reservationAdapter.HoldAsync(
            CreateHoldRequest(seeded, seeded.OldOfferId, quantity: 2),
            CancellationToken.None);

        Assert.Equal(TourPackageReservationHoldOutcomeStatus.Held, holdResult.Status);

        var reservation = CreateReservation(seeded);
        var reservationItem = new TourPackageReservationItem
        {
            Id = Guid.NewGuid(),
            TenantId = seeded.TenantId,
            TourPackageReservationId = reservation.Id,
            TourPackageComponentId = seeded.Component.Id,
            TourPackageComponentOptionId = seeded.StaticOption.Id,
            ComponentType = seeded.Component.ComponentType,
            SourceType = TourPackageSourceType.Flight,
            SourceEntityId = seeded.OldOfferId,
            Status = TourPackageReservationItemStatus.Held,
            Quantity = 2,
            CurrencyCode = "JPY",
            UnitPrice = 22_000m,
            LineAmount = 44_000m,
            SourceHoldToken = holdResult.SourceHoldToken,
            CreatedAt = seeded.OperationTime
        };

        var booking = CreateBooking(seeded, reservation);
        var bookingItem = new TourPackageBookingItem
        {
            Id = Guid.NewGuid(),
            TenantId = seeded.TenantId,
            TourPackageBookingId = booking.Id,
            TourPackageReservationItemId = reservationItem.Id,
            TourPackageComponentId = seeded.Component.Id,
            TourPackageComponentOptionId = seeded.StaticOption.Id,
            ComponentType = seeded.Component.ComponentType,
            SourceType = TourPackageSourceType.Flight,
            SourceEntityId = seeded.OldOfferId,
            Status = TourPackageBookingItemStatus.Pending,
            Quantity = 2,
            CurrencyCode = "JPY",
            UnitPrice = 22_000m,
            LineAmount = 44_000m,
            SourceHoldToken = holdResult.SourceHoldToken,
            CreatedAt = seeded.OperationTime
        };

        var confirmResult = await bookingAdapter.ConfirmAsync(
            new TourPackageSourceBookingConfirmRequest
            {
                UserId = seeded.UserId,
                Tour = seeded.Tour,
                Schedule = seeded.Schedule,
                Package = seeded.Package,
                Reservation = reservation,
                ReservationItem = reservationItem,
                Booking = booking,
                BookingItem = bookingItem
            },
            CancellationToken.None);

        Assert.Equal(TourPackageBookingConfirmOutcomeStatus.Confirmed, confirmResult.Status);

        var canonicalAfterConfirm = await context.FlightOffers.IgnoreQueryFilters().FirstAsync(x => x.Id == seeded.LatestOfferId);
        Assert.Equal(1, canonicalAfterConfirm.SeatsAvailable);

        var cancelResult = await cancellationAdapter.CancelAsync(
            new TourPackageSourceCancellationRequest
            {
                UserId = seeded.UserId,
                CurrentTime = seeded.OperationTime.AddDays(1),
                Tour = seeded.Tour,
                Schedule = seeded.Schedule,
                Booking = booking,
                BookingItem = bookingItem
            },
            CancellationToken.None);

        Assert.Equal(TourPackageSourceCancellationOutcomeStatus.Cancelled, cancelResult.Status);

        var canonicalAfterCancellation = await context.FlightOffers.IgnoreQueryFilters().FirstAsync(x => x.Id == seeded.LatestOfferId);
        Assert.Equal(3, canonicalAfterCancellation.SeatsAvailable);
    }

    [Fact]
    public async Task SearchAsync_UsesSegmentRouteTimezone_AndCanonicalOfferInventory()
    {
        var tenantContext = new TenantContext();
        using var context = CreateDbContext(tenantContext);
        var seeded = await SeedFlightContextAsync(context, tenantContext);
        var service = new FlightPublicQueryService(context, tenantContext, new FlightPublicTenantResolver(context));

        var response = await service.SearchAsync(
            new FlightSearchRequest
            {
                From = "HND",
                To = "CTS",
                Date = seeded.TravelDate
            },
            CancellationToken.None);

        Assert.Equal(1, response.Count);

        var item = Assert.Single(response.Items);
        Assert.Equal(seeded.LatestOfferId, item.OfferId);
        Assert.Equal(3, item.SeatsAvailable);
        Assert.Equal("JP101/JP202", item.Flight.FlightNumber);
        Assert.Equal(seeded.Leg1DepartureAt, item.Flight.DepartureAt);
        Assert.Equal(seeded.Leg2ArrivalAt, item.Flight.ArrivalAt);
        Assert.Equal("HND", item.Segments[0].From.Code);
        Assert.Equal("CTS", item.Segments[^1].To.Code);
    }

    [Fact]
    public async Task GetByOfferAsync_ReturnsSegmentSeatMaps_AndMarksSoldOutInventoryAsBooked()
    {
        var tenantContext = new TenantContext();
        using var context = CreateDbContext(tenantContext);
        var seeded = await SeedFlightContextAsync(context, tenantContext);

        var canonicalOffer = await context.FlightOffers.IgnoreQueryFilters().FirstAsync(x => x.Id == seeded.LatestOfferId);
        canonicalOffer.SeatsAvailable = 0;
        await context.SaveChangesAsync();

        var service = new FlightSeatMapPublicQueryService(context, tenantContext, new FlightPublicTenantResolver(context));
        var response = await service.GetByOfferAsync(
            new FlightSeatMapByOfferRequest
            {
                OfferId = seeded.LatestOfferId
            },
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.True(response!.UsesPooledInventory);
        Assert.Equal(0, response.SeatsAvailable);
        Assert.Equal(seeded.SeatMapAId, response.CabinSeatMap.Id);
        Assert.Equal(1, response.ActiveSeatCount);
        Assert.Equal(2, response.Segments.Count);
        Assert.Equal(seeded.SeatMapAId, response.Segments[0].CabinSeatMapId);
        Assert.True(response.Segments[0].IsPrimarySelectedMap);
        Assert.Equal(seeded.SeatMapBId, response.Segments[1].CabinSeatMapId);
        Assert.Equal(2, response.SegmentSeatMaps.Count);
        Assert.Equal(seeded.SeatMapAId, response.SegmentSeatMaps[0].CabinSeatMapId);
        Assert.Equal(seeded.SeatMapBId, response.SegmentSeatMaps[1].CabinSeatMapId);
        Assert.All(response.Seats.Where(x => x.IsActive), seat => Assert.Equal("booked", seat.Status));
        Assert.All(
            response.SegmentSeatMaps.SelectMany(x => x.Seats).Where(x => x.IsActive),
            seat => Assert.Equal("booked", seat.Status));
    }

    [Fact]
    public async Task ResolveAsync_SearchTemplateUsesSegmentRoute_AndCanonicalInventory()
    {
        var tenantContext = new TenantContext();
        using var context = CreateDbContext(tenantContext);
        var seeded = await SeedFlightContextAsync(context, tenantContext);
        var adapter = new FlightTourPackageSourceQuoteAdapter(context);

        var result = await adapter.ResolveAsync(
            new TourPackageSourceQuoteAdapterRequest
            {
                Tour = seeded.Tour,
                Schedule = seeded.Schedule,
                Package = seeded.Package,
                Component = seeded.Component,
                Option = CreateSearchTemplateOption(seeded),
                SourceEntityId = Guid.Empty,
                TotalPax = 2,
                TotalNights = 2,
                RequestedQuantity = 2
            },
            CancellationToken.None);

        Assert.True(result.WasResolved);
        Assert.True(result.IsAvailable);
        Assert.Equal(seeded.LatestOfferId, result.BoundSourceEntityId);
        Assert.Equal("JP101/JP202", result.SourceCode);
        Assert.Equal(seeded.Leg1DepartureAt, result.ServiceStartAt);
        Assert.Equal(seeded.Leg2ArrivalAt, result.ServiceEndAt);
        Assert.Equal(1, result.StopCount);
    }

    [Fact]
    public async Task GetByOfferAsync_ForAncillaries_UsesSegmentDisplayRoute()
    {
        var tenantContext = new TenantContext();
        using var context = CreateDbContext(tenantContext);
        var seeded = await SeedFlightContextAsync(context, tenantContext);
        var service = new FlightOfferAncillaryQueryService(context, tenantContext, new FlightPublicTenantResolver(context));

        var response = await service.GetByOfferAsync(
            new FlightOfferAncillaryRequest
            {
                OfferId = seeded.LatestOfferId,
                IncludeInactive = true
            },
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("JP101/JP202", response!.Flight.FlightNumber);
        Assert.Equal(seeded.Leg1DepartureAt, response.Flight.DepartureAt);
        Assert.Equal(seeded.Leg2ArrivalAt, response.Flight.ArrivalAt);
        Assert.Equal("HND", response.Flight.From.Code);
        Assert.Equal("CTS", response.Flight.To.Code);
        Assert.Single(response.Items);
    }

    private static AppDbContext CreateDbContext(TenantContext tenantContext)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options, tenantContext);
    }

    private static Task<SeededFlightContext> SeedFlightContextAsync(AppDbContext context, TenantContext tenantContext)
        => SeedFlightContextCoreAsync(context, tenantContext);

    private static async Task<SeededFlightContext> SeedFlightContextCoreAsync(AppDbContext context, TenantContext tenantContext)
    {
        var tenantId = Guid.NewGuid();
        tenantContext.SetTenant(tenantId);

        var operationTime = new DateTimeOffset(2032, 4, 1, 9, 0, 0, TimeSpan.Zero);
        var travelDate = new DateOnly(2032, 4, 10);
        var userId = Guid.NewGuid();

        var airlineId = Guid.NewGuid();
        var fareClassId = Guid.NewGuid();

        var hndAirportId = Guid.NewGuid();
        var itmAirportId = Guid.NewGuid();
        var ctsAirportId = Guid.NewGuid();

        var modelAId = Guid.NewGuid();
        var modelBId = Guid.NewGuid();
        var aircraftAId = Guid.NewGuid();
        var aircraftBId = Guid.NewGuid();

        var rootFlightId = Guid.NewGuid();
        var leg1FlightId = Guid.NewGuid();
        var leg2FlightId = Guid.NewGuid();

        var oldOfferId = Guid.NewGuid();
        var latestOfferId = Guid.NewGuid();

        var seatMapAId = Guid.NewGuid();
        var seatMapBId = Guid.NewGuid();

        var leg1DepartureAt = new DateTimeOffset(2032, 4, 9, 15, 30, 0, TimeSpan.Zero);
        var leg1ArrivalAt = new DateTimeOffset(2032, 4, 9, 17, 0, 0, TimeSpan.Zero);
        var leg2DepartureAt = new DateTimeOffset(2032, 4, 9, 18, 0, 0, TimeSpan.Zero);
        var leg2ArrivalAt = new DateTimeOffset(2032, 4, 9, 20, 0, 0, TimeSpan.Zero);

        context.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Code = "FLT",
            Name = "Flight Tenant",
            Type = TenantType.Flight,
            Status = TenantStatus.Active,
            HoldMinutes = 5,
            CreatedAt = operationTime
        });

        context.FlightAirlines.Add(new Airline
        {
            Id = airlineId,
            TenantId = tenantId,
            Code = "JPAIR",
            Name = "JP Air",
            IataCode = "JP",
            IsActive = true,
            CreatedAt = operationTime
        });

        context.FlightAirports.AddRange(
            new Airport
            {
                Id = hndAirportId,
                TenantId = tenantId,
                LocationId = Guid.NewGuid(),
                Code = "HND",
                IataCode = "HND",
                Name = "Tokyo Haneda",
                TimeZone = "Asia/Tokyo",
                IsActive = true,
                CreatedAt = operationTime
            },
            new Airport
            {
                Id = itmAirportId,
                TenantId = tenantId,
                LocationId = Guid.NewGuid(),
                Code = "ITM",
                IataCode = "ITM",
                Name = "Osaka Itami",
                TimeZone = "Asia/Tokyo",
                IsActive = true,
                CreatedAt = operationTime
            },
            new Airport
            {
                Id = ctsAirportId,
                TenantId = tenantId,
                LocationId = Guid.NewGuid(),
                Code = "CTS",
                IataCode = "CTS",
                Name = "New Chitose",
                TimeZone = "Asia/Tokyo",
                IsActive = true,
                CreatedAt = operationTime
            });

        context.FlightAircraftModels.AddRange(
            new AircraftModel
            {
                Id = modelAId,
                TenantId = tenantId,
                Code = "A320",
                Manufacturer = "Airbus",
                Model = "A320-200",
                TypicalSeatCapacity = 180,
                IsActive = true,
                CreatedAt = operationTime
            },
            new AircraftModel
            {
                Id = modelBId,
                TenantId = tenantId,
                Code = "B737",
                Manufacturer = "Boeing",
                Model = "737-800",
                TypicalSeatCapacity = 189,
                IsActive = true,
                CreatedAt = operationTime
            });

        context.FlightAircrafts.AddRange(
            new Aircraft
            {
                Id = aircraftAId,
                TenantId = tenantId,
                AircraftModelId = modelAId,
                AirlineId = airlineId,
                Code = "AC-A",
                Registration = "JA-AAA",
                IsActive = true,
                CreatedAt = operationTime
            },
            new Aircraft
            {
                Id = aircraftBId,
                TenantId = tenantId,
                AircraftModelId = modelBId,
                AirlineId = airlineId,
                Code = "AC-B",
                Registration = "JA-BBB",
                IsActive = true,
                CreatedAt = operationTime
            });

        context.FlightFlights.AddRange(
            new TicketBooking.Domain.Flight.Flight
            {
                Id = rootFlightId,
                TenantId = tenantId,
                AirlineId = airlineId,
                AircraftId = aircraftAId,
                FromAirportId = itmAirportId,
                ToAirportId = ctsAirportId,
                FlightNumber = "ROOT999",
                DepartureAt = leg2DepartureAt,
                ArrivalAt = leg2ArrivalAt,
                Status = FlightStatus.Published,
                IsActive = true,
                CreatedAt = operationTime
            },
            new TicketBooking.Domain.Flight.Flight
            {
                Id = leg1FlightId,
                TenantId = tenantId,
                AirlineId = airlineId,
                AircraftId = aircraftAId,
                FromAirportId = hndAirportId,
                ToAirportId = itmAirportId,
                FlightNumber = "JP101",
                DepartureAt = leg1DepartureAt,
                ArrivalAt = leg1ArrivalAt,
                Status = FlightStatus.Published,
                IsActive = true,
                CreatedAt = operationTime
            },
            new TicketBooking.Domain.Flight.Flight
            {
                Id = leg2FlightId,
                TenantId = tenantId,
                AirlineId = airlineId,
                AircraftId = aircraftBId,
                FromAirportId = itmAirportId,
                ToAirportId = ctsAirportId,
                FlightNumber = "JP202",
                DepartureAt = leg2DepartureAt,
                ArrivalAt = leg2ArrivalAt,
                Status = FlightStatus.Published,
                IsActive = true,
                CreatedAt = operationTime
            });

        context.FlightFareClasses.Add(new FareClass
        {
            Id = fareClassId,
            TenantId = tenantId,
            AirlineId = airlineId,
            Code = "Y",
            Name = "Economy Flex",
            CabinClass = CabinClass.Economy,
            IsRefundable = true,
            IsChangeable = true,
            IsActive = true,
            CreatedAt = operationTime
        });

        context.FlightOffers.AddRange(
            new Offer
            {
                Id = oldOfferId,
                TenantId = tenantId,
                AirlineId = airlineId,
                FlightId = rootFlightId,
                FareClassId = fareClassId,
                Status = OfferStatus.Active,
                CurrencyCode = "JPY",
                BaseFare = 18_000m,
                TaxesFees = 4_000m,
                TotalPrice = 22_000m,
                SeatsAvailable = 9,
                RequestedAt = operationTime,
                ExpiresAt = operationTime.AddDays(10),
                CreatedAt = operationTime
            },
            new Offer
            {
                Id = latestOfferId,
                TenantId = tenantId,
                AirlineId = airlineId,
                FlightId = rootFlightId,
                FareClassId = fareClassId,
                Status = OfferStatus.Active,
                CurrencyCode = "JPY",
                BaseFare = 18_000m,
                TaxesFees = 4_000m,
                TotalPrice = 22_000m,
                SeatsAvailable = 3,
                RequestedAt = operationTime.AddHours(1),
                ExpiresAt = operationTime.AddDays(10),
                CreatedAt = operationTime.AddHours(1)
            });

        context.FlightOfferSegments.AddRange(
            new OfferSegment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OfferId = latestOfferId,
                SegmentIndex = 0,
                FlightId = leg1FlightId,
                FareClassId = fareClassId,
                CabinClass = CabinClass.Economy,
                FromAirportId = hndAirportId,
                ToAirportId = itmAirportId,
                DepartureAt = leg1DepartureAt,
                ArrivalAt = leg1ArrivalAt,
                FlightNumber = "JP101",
                CreatedAt = operationTime
            },
            new OfferSegment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OfferId = latestOfferId,
                SegmentIndex = 1,
                FlightId = leg2FlightId,
                FareClassId = fareClassId,
                CabinSeatMapId = seatMapBId,
                CabinClass = CabinClass.Economy,
                FromAirportId = itmAirportId,
                ToAirportId = ctsAirportId,
                DepartureAt = leg2DepartureAt,
                ArrivalAt = leg2ArrivalAt,
                FlightNumber = "JP202",
                CreatedAt = operationTime
            });

        context.FlightCabinSeatMaps.AddRange(
            new CabinSeatMap
            {
                Id = seatMapAId,
                TenantId = tenantId,
                AircraftModelId = modelAId,
                CabinClass = CabinClass.Economy,
                Code = "A320-ECO",
                Name = "A320 Economy",
                TotalRows = 1,
                TotalColumns = 2,
                DeckCount = 1,
                IsActive = true,
                CreatedAt = operationTime
            },
            new CabinSeatMap
            {
                Id = seatMapBId,
                TenantId = tenantId,
                AircraftModelId = modelBId,
                CabinClass = CabinClass.Economy,
                Code = "B737-ECO",
                Name = "B737 Economy",
                TotalRows = 1,
                TotalColumns = 2,
                DeckCount = 1,
                IsActive = true,
                CreatedAt = operationTime
            });

        context.FlightCabinSeats.AddRange(
            new CabinSeat
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CabinSeatMapId = seatMapAId,
                SeatNumber = "1A",
                RowIndex = 0,
                ColumnIndex = 0,
                DeckIndex = 1,
                IsWindow = true,
                IsAisle = false,
                IsActive = true,
                CreatedAt = operationTime
            },
            new CabinSeat
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CabinSeatMapId = seatMapAId,
                SeatNumber = "1B",
                RowIndex = 0,
                ColumnIndex = 1,
                DeckIndex = 1,
                IsWindow = false,
                IsAisle = true,
                IsActive = false,
                CreatedAt = operationTime
            },
            new CabinSeat
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CabinSeatMapId = seatMapBId,
                SeatNumber = "2A",
                RowIndex = 0,
                ColumnIndex = 0,
                DeckIndex = 1,
                IsWindow = true,
                IsAisle = false,
                IsActive = true,
                CreatedAt = operationTime
            },
            new CabinSeat
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CabinSeatMapId = seatMapBId,
                SeatNumber = "2B",
                RowIndex = 0,
                ColumnIndex = 1,
                DeckIndex = 1,
                IsWindow = false,
                IsAisle = true,
                IsActive = true,
                CreatedAt = operationTime
            });

        context.FlightAncillaryDefinitions.Add(new AncillaryDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AirlineId = airlineId,
            Code = "MEAL-HOT",
            Name = "Hot meal",
            Type = AncillaryType.Meal,
            CurrencyCode = "JPY",
            Price = 1_200m,
            IsActive = true,
            CreatedAt = operationTime
        });

        await context.SaveChangesAsync();

        var tour = new Tour
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = "TOUR-FLT",
            Name = "Flight Tour",
            Slug = "flight-tour",
            CurrencyCode = "JPY",
            IsActive = true,
            Status = TourStatus.Active
        };

        var schedule = new TourSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tour.Id,
            Code = "SCH-FLT",
            DepartureDate = travelDate,
            ReturnDate = travelDate.AddDays(2),
            IsActive = true,
            Status = TourScheduleStatus.Open
        };

        var package = new TourPackage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tour.Id,
            Code = "PKG-FLT",
            Name = "Flight Package",
            CurrencyCode = "JPY",
            Status = TourPackageStatus.Active,
            IsActive = true
        };

        var component = new TourPackageComponent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourPackageId = package.Id,
            Code = "FLT",
            Name = "Flight",
            ComponentType = TourPackageComponentType.OutboundTransport,
            SelectionMode = TourPackageSelectionMode.RequiredSingle,
            IsActive = true
        };

        var staticOption = new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourPackageComponentId = component.Id,
            Code = "FLT-STATIC",
            Name = "Static Flight",
            SourceType = TourPackageSourceType.Flight,
            BindingMode = TourPackageBindingMode.StaticReference,
            SourceEntityId = oldOfferId,
            PricingMode = TourPackagePricingMode.PassThrough,
            CurrencyCode = "JPY",
            QuantityMode = TourPackageQuantityMode.PerPax,
            DefaultQuantity = 1,
            IsActive = true
        };

        return new SeededFlightContext
        {
            TenantId = tenantId,
            UserId = userId,
            OperationTime = operationTime,
            TravelDate = travelDate,
            OldOfferId = oldOfferId,
            LatestOfferId = latestOfferId,
            SeatMapAId = seatMapAId,
            SeatMapBId = seatMapBId,
            Leg1DepartureAt = leg1DepartureAt,
            Leg2ArrivalAt = leg2ArrivalAt,
            Tour = tour,
            Schedule = schedule,
            Package = package,
            Component = component,
            StaticOption = staticOption
        };
    }

    private static TourPackageSourceReservationHoldRequest CreateHoldRequest(
        SeededFlightContext seeded,
        Guid offerId,
        int quantity)
    {
        return new TourPackageSourceReservationHoldRequest
        {
            ReservationId = Guid.NewGuid(),
            ReservationToken = "pkg-flight-hold",
            UserId = seeded.UserId,
            Tour = seeded.Tour,
            Schedule = seeded.Schedule,
            Package = seeded.Package,
            Component = seeded.Component,
            Option = seeded.StaticOption,
            Line = new TourQuoteBuildPackageLineInput
            {
                ComponentId = seeded.Component.Id,
                ComponentCode = seeded.Component.Code,
                ComponentName = seeded.Component.Name,
                ComponentType = seeded.Component.ComponentType,
                OptionId = seeded.StaticOption.Id,
                BoundSourceEntityId = offerId,
                Code = seeded.StaticOption.Code,
                Name = seeded.StaticOption.Name,
                Quantity = quantity,
                CurrencyCode = "JPY",
                UnitPrice = 22_000m,
                PricingMode = TourPackagePricingMode.PassThrough,
                IsRequired = true,
                IsDefaultSelected = true
            }
        };
    }

    private static TourPackageReservation CreateReservation(SeededFlightContext seeded)
    {
        return new TourPackageReservation
        {
            Id = Guid.NewGuid(),
            TenantId = seeded.TenantId,
            TourId = seeded.Tour.Id,
            TourScheduleId = seeded.Schedule.Id,
            TourPackageId = seeded.Package.Id,
            UserId = seeded.UserId,
            Code = "RSV-FLT",
            HoldToken = "hold-flight",
            Status = TourPackageReservationStatus.Held,
            HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
            CurrencyCode = "JPY",
            RequestedPax = 2,
            HeldCapacitySlots = 2,
            PackageSubtotalAmount = 44_000m,
            HoldExpiresAt = seeded.OperationTime.AddMinutes(5),
            CreatedAt = seeded.OperationTime
        };
    }

    private static TourPackageBooking CreateBooking(
        SeededFlightContext seeded,
        TourPackageReservation reservation)
    {
        return new TourPackageBooking
        {
            Id = Guid.NewGuid(),
            TenantId = seeded.TenantId,
            TourId = seeded.Tour.Id,
            TourScheduleId = seeded.Schedule.Id,
            TourPackageId = seeded.Package.Id,
            TourPackageReservationId = reservation.Id,
            UserId = seeded.UserId,
            Code = "BKG-FLT",
            Status = TourPackageBookingStatus.Pending,
            HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
            CurrencyCode = "JPY",
            RequestedPax = 2,
            ConfirmedCapacitySlots = 0,
            PackageSubtotalAmount = 44_000m,
            CreatedAt = seeded.OperationTime
        };
    }

    private static TourPackageComponentOption CreateSearchTemplateOption(SeededFlightContext seeded)
    {
        return new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TenantId = seeded.TenantId,
            TourPackageComponentId = seeded.Component.Id,
            Code = "FLT-SEARCH",
            Name = "Search Flight",
            SourceType = TourPackageSourceType.Flight,
            BindingMode = TourPackageBindingMode.SearchTemplate,
            SearchTemplateJson = "{\"fromAirportCode\":\"HND\",\"toAirportCode\":\"CTS\",\"selectionStrategy\":\"Cheapest\",\"cabinClass\":\"Economy\"}",
            PricingMode = TourPackagePricingMode.PassThrough,
            CurrencyCode = "JPY",
            QuantityMode = TourPackageQuantityMode.PerPax,
            DefaultQuantity = 1,
            IsActive = true
        };
    }

    private sealed class SeededFlightContext
    {
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public DateTimeOffset OperationTime { get; set; }
        public DateOnly TravelDate { get; set; }
        public Guid OldOfferId { get; set; }
        public Guid LatestOfferId { get; set; }
        public Guid SeatMapAId { get; set; }
        public Guid SeatMapBId { get; set; }
        public DateTimeOffset Leg1DepartureAt { get; set; }
        public DateTimeOffset Leg2ArrivalAt { get; set; }
        public Tour Tour { get; set; } = null!;
        public TourSchedule Schedule { get; set; } = null!;
        public TourPackage Package { get; set; } = null!;
        public TourPackageComponent Component { get; set; } = null!;
        public TourPackageComponentOption StaticOption { get; set; } = null!;
    }
}
