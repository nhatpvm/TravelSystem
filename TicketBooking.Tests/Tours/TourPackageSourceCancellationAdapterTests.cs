using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Bus;
using TicketBooking.Domain.Hotels;
using TicketBooking.Domain.Tours;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Tests.Tours;

public sealed class TourPackageSourceCancellationAdapterTests
{
    [Fact]
    public async Task HotelAdapter_CancelAsync_RestoresSoldInventory()
    {
        using var context = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);
        var roomTypeId = Guid.NewGuid();

        context.RoomTypeInventories.AddRange(
            new RoomTypeInventory
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RoomTypeId = roomTypeId,
                Date = new DateOnly(2026, 4, 10),
                TotalUnits = 10,
                SoldUnits = 2,
                HeldUnits = 0,
                Status = InventoryStatus.Open,
                CreatedAt = now
            },
            new RoomTypeInventory
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RoomTypeId = roomTypeId,
                Date = new DateOnly(2026, 4, 11),
                TotalUnits = 10,
                SoldUnits = 2,
                HeldUnits = 0,
                Status = InventoryStatus.Open,
                CreatedAt = now
            });

        context.InventoryHolds.Add(new InventoryHold
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RoomTypeId = roomTypeId,
            CheckInDate = new DateOnly(2026, 4, 10),
            CheckOutDate = new DateOnly(2026, 4, 12),
            Units = 2,
            Status = HoldStatus.Confirmed,
            HoldExpiresAt = now.AddMinutes(5),
            CorrelationId = "hotel-token",
            CreatedAt = now
        });

        await context.SaveChangesAsync();

        var adapter = new HotelTourPackageCancellationAdapter(context);
        var result = await adapter.CancelAsync(CreateRequest(TourPackageSourceType.Hotel, "hotel-token", now));

        var inventories = await context.RoomTypeInventories.IgnoreQueryFilters().OrderBy(x => x.Date).ToListAsync();
        var hold = await context.InventoryHolds.IgnoreQueryFilters().SingleAsync();

        Assert.Equal(TourPackageSourceCancellationOutcomeStatus.Cancelled, result.Status);
        Assert.All(inventories, x => Assert.Equal(0, x.SoldUnits));
        Assert.Equal(HoldStatus.Cancelled, hold.Status);
    }

    [Fact]
    public async Task BusAdapter_CancelAsync_CancelsConfirmedSeatHolds()
    {
        using var context = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);
        var tripId = Guid.NewGuid();

        context.BusTrips.Add(new Trip
        {
            Id = tripId,
            TenantId = tenantId,
            ProviderId = Guid.NewGuid(),
            RouteId = Guid.NewGuid(),
            VehicleId = Guid.NewGuid(),
            Code = "BUS-01",
            Name = "Bus Trip",
            Status = TripStatus.Published,
            DepartureAt = now.AddDays(2),
            ArrivalAt = now.AddDays(2).AddHours(4),
            IsActive = true,
            CreatedAt = now
        });

        context.BusTripSeatHolds.Add(new TripSeatHold
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
            HoldToken = "bus-token",
            HoldExpiresAt = now.AddMinutes(5),
            CreatedAt = now
        });

        await context.SaveChangesAsync();

        var adapter = new BusTourPackageCancellationAdapter(context);
        var result = await adapter.CancelAsync(CreateRequest(TourPackageSourceType.Bus, "bus-token", now));

        var hold = await context.BusTripSeatHolds.IgnoreQueryFilters().SingleAsync();

        Assert.Equal(TourPackageSourceCancellationOutcomeStatus.Cancelled, result.Status);
        Assert.Equal(SeatHoldStatus.Cancelled, hold.Status);
    }

    [Fact]
    public async Task TrainAdapter_CancelAsync_CancelsConfirmedSeatHolds()
    {
        using var context = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);
        var tripId = Guid.NewGuid();

        context.TrainTrips.Add(new TrainTrip
        {
            Id = tripId,
            TenantId = tenantId,
            ProviderId = Guid.NewGuid(),
            RouteId = Guid.NewGuid(),
            TrainNumber = "SE1",
            Code = "TR-01",
            Name = "Train Trip",
            Status = TrainTripStatus.Published,
            DepartureAt = now.AddDays(2),
            ArrivalAt = now.AddDays(2).AddHours(5),
            IsActive = true,
            CreatedAt = now
        });

        context.TrainTripSeatHolds.Add(new TrainTripSeatHold
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TripId = tripId,
            TrainCarSeatId = Guid.NewGuid(),
            FromTripStopTimeId = Guid.NewGuid(),
            ToTripStopTimeId = Guid.NewGuid(),
            FromStopIndex = 0,
            ToStopIndex = 1,
            Status = TrainSeatHoldStatus.Confirmed,
            HoldToken = "train-token",
            HoldExpiresAt = now.AddMinutes(5),
            CreatedAt = now
        });

        await context.SaveChangesAsync();

        var adapter = new TrainTourPackageCancellationAdapter(context);
        var result = await adapter.CancelAsync(CreateRequest(TourPackageSourceType.Train, "train-token", now));

        var hold = await context.TrainTripSeatHolds.IgnoreQueryFilters().SingleAsync();

        Assert.Equal(TourPackageSourceCancellationOutcomeStatus.Cancelled, result.Status);
        Assert.Equal(TrainSeatHoldStatus.Cancelled, hold.Status);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options, new TenantContext());
    }

    private static TourPackageSourceCancellationRequest CreateRequest(
        TourPackageSourceType sourceType,
        string holdToken,
        DateTimeOffset now)
    {
        return new TourPackageSourceCancellationRequest
        {
            CurrentTime = now,
            Tour = new Tour { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), Code = "TOUR", Name = "Tour", Slug = "tour", CurrencyCode = "VND", CreatedAt = now },
            Schedule = new TourSchedule { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), TourId = Guid.NewGuid(), Code = "SCH", DepartureDate = new DateOnly(2026, 4, 10), ReturnDate = new DateOnly(2026, 4, 11), CreatedAt = now },
            Booking = new TourPackageBooking { Id = Guid.NewGuid(), TenantId = Guid.NewGuid(), TourId = Guid.NewGuid(), TourScheduleId = Guid.NewGuid(), TourPackageId = Guid.NewGuid(), TourPackageReservationId = Guid.NewGuid(), Code = "TPB", CurrencyCode = "VND", CreatedAt = now },
            BookingItem = new TourPackageBookingItem
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                TourPackageBookingId = Guid.NewGuid(),
                TourPackageReservationItemId = Guid.NewGuid(),
                TourPackageComponentId = Guid.NewGuid(),
                TourPackageComponentOptionId = Guid.NewGuid(),
                SourceType = sourceType,
                SourceHoldToken = holdToken,
                Status = TourPackageBookingItemStatus.Confirmed,
                Quantity = 1,
                CurrencyCode = "VND",
                UnitPrice = 100_000m,
                LineAmount = 100_000m,
                CreatedAt = now
            }
        };
    }
}
