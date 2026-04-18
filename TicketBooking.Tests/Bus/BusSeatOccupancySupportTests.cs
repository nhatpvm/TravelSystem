using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketBooking.Api.Services.Bus;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Bus;
using TicketBooking.Domain.Fleet;
using TicketBooking.Domain.Tenants;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Tests.Bus;

public sealed class BusSeatOccupancySupportTests
{
    [Fact]
    public async Task CountActiveSeatOccupancyAsync_CountsConfirmedAndActiveHeldOnly()
    {
        using var context = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);

        context.BusTripSeatHolds.AddRange(
            new TripSeatHold
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TripId = tripId,
                SeatId = Guid.NewGuid(),
                FromTripStopTimeId = Guid.NewGuid(),
                ToTripStopTimeId = Guid.NewGuid(),
                FromStopIndex = 0,
                ToStopIndex = 1,
                Status = SeatHoldStatus.Held,
                HoldToken = "held-active",
                HoldExpiresAt = now.AddMinutes(5),
                CreatedAt = now
            },
            new TripSeatHold
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TripId = tripId,
                SeatId = Guid.NewGuid(),
                FromTripStopTimeId = Guid.NewGuid(),
                ToTripStopTimeId = Guid.NewGuid(),
                FromStopIndex = 0,
                ToStopIndex = 1,
                Status = SeatHoldStatus.Confirmed,
                HoldToken = "confirmed",
                HoldExpiresAt = now.AddMinutes(-30),
                CreatedAt = now
            },
            new TripSeatHold
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TripId = tripId,
                SeatId = Guid.NewGuid(),
                FromTripStopTimeId = Guid.NewGuid(),
                ToTripStopTimeId = Guid.NewGuid(),
                FromStopIndex = 0,
                ToStopIndex = 1,
                Status = SeatHoldStatus.Held,
                HoldToken = "held-expired",
                HoldExpiresAt = now.AddMinutes(-1),
                CreatedAt = now
            },
            new TripSeatHold
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TripId = tripId,
                SeatId = Guid.NewGuid(),
                FromTripStopTimeId = Guid.NewGuid(),
                ToTripStopTimeId = Guid.NewGuid(),
                FromStopIndex = 0,
                ToStopIndex = 1,
                Status = SeatHoldStatus.Cancelled,
                HoldToken = "cancelled",
                HoldExpiresAt = now.AddMinutes(5),
                CreatedAt = now
            });

        await context.SaveChangesAsync();

        var count = await BusSeatOccupancySupport.CountActiveSeatOccupancyAsync(
            context,
            tenantId,
            tripId,
            now,
            CancellationToken.None);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task HoldAsync_RejectsReservationWhenOnlyRemainingSeatIsAlreadyConfirmed()
    {
        using var context = CreateDbContext();
        var seeded = await SeedBusTripAsync(context);
        var adapter = new BusTourPackageReservationAdapter(context);

        context.BusTripSeatHolds.Add(new TripSeatHold
        {
            Id = Guid.NewGuid(),
            TenantId = seeded.TenantId,
            TripId = seeded.TripId,
            SeatId = seeded.SeatId,
            FromTripStopTimeId = seeded.FromStopTimeId,
            ToTripStopTimeId = seeded.ToStopTimeId,
            FromStopIndex = 0,
            ToStopIndex = 1,
            Status = SeatHoldStatus.Confirmed,
            HoldToken = "confirmed-seat",
            HoldExpiresAt = seeded.Now.AddMinutes(10),
            CreatedAt = seeded.Now
        });
        await context.SaveChangesAsync();

        var result = await adapter.HoldAsync(
            new TourPackageSourceReservationHoldRequest
            {
                ReservationId = Guid.NewGuid(),
                ReservationToken = "pkg-resv",
                UserId = Guid.NewGuid(),
                Tour = seeded.Tour,
                Schedule = seeded.Schedule,
                Package = seeded.Package,
                Component = seeded.Component,
                Option = seeded.Option,
                Line = new TourQuoteBuildPackageLineInput
                {
                    ComponentId = seeded.Component.Id,
                    ComponentCode = seeded.Component.Code,
                    ComponentName = seeded.Component.Name,
                    ComponentType = seeded.Component.ComponentType,
                    OptionId = seeded.Option.Id,
                    BoundSourceEntityId = seeded.SegmentPriceId,
                    Code = seeded.Option.Code,
                    Name = seeded.Option.Name,
                    Quantity = 1,
                    CurrencyCode = "VND",
                    UnitPrice = 250_000m,
                    PricingMode = TourPackagePricingMode.PassThrough,
                    IsRequired = true,
                    IsDefaultSelected = true
                }
            },
            CancellationToken.None);

        Assert.Equal(TourPackageReservationHoldOutcomeStatus.Failed, result.Status);
        Assert.Contains("enough available seats", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        var activeHeldCount = await context.BusTripSeatHolds.CountAsync(x =>
            x.TripId == seeded.TripId &&
            x.Status == SeatHoldStatus.Held);

        Assert.Equal(0, activeHeldCount);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options, new TenantContext());
    }

    private static async Task<SeededBusReservationContext> SeedBusTripAsync(AppDbContext context)
    {
        var tenantId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);

        var providerId = Guid.NewGuid();
        var seatMapId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var fromStopTimeId = Guid.NewGuid();
        var toStopTimeId = Guid.NewGuid();
        var segmentPriceId = Guid.NewGuid();
        var seatId = Guid.NewGuid();

        context.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Code = "BUS",
            Name = "Bus Tenant",
            Type = TenantType.Bus,
            Status = TenantStatus.Active,
            HoldMinutes = 5,
            CreatedAt = now
        });

        context.SeatMaps.Add(new SeatMap
        {
            Id = seatMapId,
            TenantId = tenantId,
            VehicleType = VehicleType.Bus,
            Code = "SM-BUS",
            Name = "Bus Seat Map",
            TotalRows = 1,
            TotalColumns = 1,
            DeckCount = 1,
            IsActive = true,
            CreatedAt = now
        });

        context.Vehicles.Add(new Vehicle
        {
            Id = vehicleId,
            TenantId = tenantId,
            ProviderId = providerId,
            VehicleType = VehicleType.Bus,
            SeatMapId = seatMapId,
            Code = "BUS-01",
            Name = "Bus 01",
            SeatCapacity = 1,
            IsActive = true,
            CreatedAt = now
        });

        context.Seats.Add(new Seat
        {
            Id = seatId,
            TenantId = tenantId,
            SeatMapId = seatMapId,
            SeatNumber = "A1",
            RowIndex = 0,
            ColumnIndex = 0,
            DeckIndex = 1,
            SeatType = SeatType.Standard,
            SeatClass = SeatClass.Economy,
            IsActive = true,
            CreatedAt = now
        });

        context.BusTrips.Add(new Trip
        {
            Id = tripId,
            TenantId = tenantId,
            ProviderId = providerId,
            RouteId = Guid.NewGuid(),
            VehicleId = vehicleId,
            Code = "TRIP-01",
            Name = "Trip 01",
            Status = TripStatus.Published,
            DepartureAt = now.AddDays(5),
            ArrivalAt = now.AddDays(5).AddHours(2),
            IsActive = true,
            CreatedAt = now
        });

        context.BusTripStopTimes.AddRange(
            new TripStopTime
            {
                Id = fromStopTimeId,
                TenantId = tenantId,
                TripId = tripId,
                StopPointId = Guid.NewGuid(),
                StopIndex = 0,
                DepartAt = now.AddDays(5),
                ArriveAt = now.AddDays(5),
                IsActive = true,
                CreatedAt = now
            },
            new TripStopTime
            {
                Id = toStopTimeId,
                TenantId = tenantId,
                TripId = tripId,
                StopPointId = Guid.NewGuid(),
                StopIndex = 1,
                ArriveAt = now.AddDays(5).AddHours(2),
                IsActive = true,
                CreatedAt = now
            });

        context.BusTripSegmentPrices.Add(new TripSegmentPrice
        {
            Id = segmentPriceId,
            TenantId = tenantId,
            TripId = tripId,
            FromTripStopTimeId = fromStopTimeId,
            ToTripStopTimeId = toStopTimeId,
            FromStopIndex = 0,
            ToStopIndex = 1,
            CurrencyCode = "VND",
            BaseFare = 250_000m,
            TaxesFees = 0m,
            TotalPrice = 250_000m,
            IsActive = true,
            CreatedAt = now
        });

        var tour = new Tour
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = "TOUR-01",
            Name = "Bus Package Tour",
            Slug = "bus-package-tour",
            Status = TourStatus.Active,
            CurrencyCode = "VND",
            DurationDays = 1,
            DurationNights = 0,
            IsActive = true,
            CreatedAt = now
        };

        var schedule = new TourSchedule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tour.Id,
            Code = "SCH-01",
            DepartureDate = DateOnly.FromDateTime(now.AddDays(5).Date),
            ReturnDate = DateOnly.FromDateTime(now.AddDays(5).Date),
            Status = TourScheduleStatus.Open,
            IsActive = true,
            CreatedAt = now
        };

        var package = new TourPackage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tour.Id,
            Code = "PKG-01",
            Name = "Default Package",
            Mode = TourPackageMode.Fixed,
            Status = TourPackageStatus.Active,
            CurrencyCode = "VND",
            IsActive = true,
            CreatedAt = now
        };

        var component = new TourPackageComponent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourPackageId = package.Id,
            Code = "OUTBOUND",
            Name = "Outbound Transport",
            ComponentType = TourPackageComponentType.OutboundTransport,
            SelectionMode = TourPackageSelectionMode.RequiredSingle,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = now
        };

        var option = new TourPackageComponentOption
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourPackageComponentId = component.Id,
            Code = "BUS",
            Name = "Bus Option",
            SourceType = TourPackageSourceType.Bus,
            BindingMode = TourPackageBindingMode.StaticReference,
            SourceEntityId = segmentPriceId,
            PricingMode = TourPackagePricingMode.PassThrough,
            CurrencyCode = "VND",
            DefaultQuantity = 1,
            IsDefaultSelected = true,
            IsActive = true,
            CreatedAt = now
        };

        await context.SaveChangesAsync();

        return new SeededBusReservationContext(
            tenantId,
            tripId,
            fromStopTimeId,
            toStopTimeId,
            seatId,
            segmentPriceId,
            now,
            tour,
            schedule,
            package,
            component,
            option);
    }

    private sealed record SeededBusReservationContext(
        Guid TenantId,
        Guid TripId,
        Guid FromStopTimeId,
        Guid ToStopTimeId,
        Guid SeatId,
        Guid SegmentPriceId,
        DateTimeOffset Now,
        Tour Tour,
        TourSchedule Schedule,
        TourPackage Package,
        TourPackageComponent Component,
        TourPackageComponentOption Option);
}
