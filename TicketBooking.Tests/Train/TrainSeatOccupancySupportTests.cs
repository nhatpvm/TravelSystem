using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketBooking.Api.Controllers;
using TicketBooking.Api.Services.Train;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Catalog;
using TicketBooking.Domain.Tenants;
using TicketBooking.Domain.Tours;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Tests.Train;

public sealed class TrainSeatOccupancySupportTests
{
    [Fact]
    public async Task CountActiveSeatOccupancyAsync_CountsConfirmedAndActiveHeldOnly()
    {
        using var context = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);

        context.TrainTripSeatHolds.AddRange(
            new TrainTripSeatHold
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TripId = tripId,
                TrainCarSeatId = Guid.NewGuid(),
                FromTripStopTimeId = Guid.NewGuid(),
                ToTripStopTimeId = Guid.NewGuid(),
                FromStopIndex = 0,
                ToStopIndex = 1,
                Status = TrainSeatHoldStatus.Held,
                HoldToken = "held-active",
                HoldExpiresAt = now.AddMinutes(5),
                CreatedAt = now
            },
            new TrainTripSeatHold
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
                HoldToken = "confirmed",
                HoldExpiresAt = now.AddMinutes(-30),
                CreatedAt = now
            },
            new TrainTripSeatHold
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TripId = tripId,
                TrainCarSeatId = Guid.NewGuid(),
                FromTripStopTimeId = Guid.NewGuid(),
                ToTripStopTimeId = Guid.NewGuid(),
                FromStopIndex = 0,
                ToStopIndex = 1,
                Status = TrainSeatHoldStatus.Held,
                HoldToken = "held-expired",
                HoldExpiresAt = now.AddMinutes(-1),
                CreatedAt = now
            },
            new TrainTripSeatHold
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TripId = tripId,
                TrainCarSeatId = Guid.NewGuid(),
                FromTripStopTimeId = Guid.NewGuid(),
                ToTripStopTimeId = Guid.NewGuid(),
                FromStopIndex = 0,
                ToStopIndex = 1,
                Status = TrainSeatHoldStatus.Cancelled,
                HoldToken = "cancelled",
                HoldExpiresAt = now.AddMinutes(5),
                CreatedAt = now
            });

        await context.SaveChangesAsync();

        var count = await TrainSeatOccupancySupport.CountActiveSeatOccupancyAsync(
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
        var seeded = await SeedTrainReservationContextAsync(context);
        var adapter = new TrainTourPackageReservationAdapter(context);

        context.TrainTripSeatHolds.Add(new TrainTripSeatHold
        {
            Id = Guid.NewGuid(),
            TenantId = seeded.TenantId,
            TripId = seeded.TripId,
            TrainCarSeatId = seeded.SeatId,
            FromTripStopTimeId = seeded.FromStopTimeId,
            ToTripStopTimeId = seeded.ToStopTimeId,
            FromStopIndex = 0,
            ToStopIndex = 1,
            Status = TrainSeatHoldStatus.Confirmed,
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

        var activeHeldCount = await context.TrainTripSeatHolds.CountAsync(x =>
            x.TripId == seeded.TripId &&
            x.Status == TrainSeatHoldStatus.Held);

        Assert.Equal(0, activeHeldCount);
    }

    [Fact]
    public async Task ReleaseByToken_CancelsOnlyHeldRows_ForMatchingUser()
    {
        using var context = CreateDbContext();
        var tenantContext = new TenantContext();
        var controller = new TrainSeatHoldsController(context, tenantContext);
        var userId = Guid.NewGuid();
        var token = "release-me";
        var now = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);

        var heldId = Guid.NewGuid();
        var confirmedId = Guid.NewGuid();

        context.TrainTripSeatHolds.AddRange(
            new TrainTripSeatHold
            {
                Id = heldId,
                TenantId = Guid.NewGuid(),
                TripId = Guid.NewGuid(),
                TrainCarSeatId = Guid.NewGuid(),
                FromTripStopTimeId = Guid.NewGuid(),
                ToTripStopTimeId = Guid.NewGuid(),
                FromStopIndex = 0,
                ToStopIndex = 1,
                Status = TrainSeatHoldStatus.Held,
                UserId = userId,
                HoldToken = token,
                HoldExpiresAt = now.AddMinutes(5),
                CreatedAt = now
            },
            new TrainTripSeatHold
            {
                Id = confirmedId,
                TenantId = Guid.NewGuid(),
                TripId = Guid.NewGuid(),
                TrainCarSeatId = Guid.NewGuid(),
                FromTripStopTimeId = Guid.NewGuid(),
                ToTripStopTimeId = Guid.NewGuid(),
                FromStopIndex = 0,
                ToStopIndex = 1,
                Status = TrainSeatHoldStatus.Confirmed,
                UserId = userId,
                HoldToken = token,
                HoldExpiresAt = now.AddMinutes(5),
                CreatedAt = now
            });

        await context.SaveChangesAsync();

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString())
                    },
                    "TestAuth"))
            }
        };

        var result = await controller.ReleaseByToken(token, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value)).RootElement;

        Assert.True(payload.GetProperty("ok").GetBoolean());
        Assert.Equal(1, payload.GetProperty("released").GetInt32());

        var held = await context.TrainTripSeatHolds.IgnoreQueryFilters().FirstAsync(x => x.Id == heldId);
        var confirmed = await context.TrainTripSeatHolds.IgnoreQueryFilters().FirstAsync(x => x.Id == confirmedId);

        Assert.Equal(TrainSeatHoldStatus.Cancelled, held.Status);
        Assert.Equal(TrainSeatHoldStatus.Confirmed, confirmed.Status);
    }

    [Fact]
    public async Task SearchTrips_UsesBoardingDepartureTime_AndCountsConfirmedOccupancy()
    {
        using var context = CreateDbContext();
        var tenantContext = new TenantContext();
        var controller = new TrainSearchTripsController(context, tenantContext);
        var seeded = await SeedTrainSearchContextAsync(context);

        var result = await controller.SearchTrips(
            seeded.FromLocationId,
            seeded.ToLocationId,
            seeded.DepartDate,
            passengers: 1,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var root = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value)).RootElement;
        var items = root.GetProperty("items");

        Assert.Equal(1, items.GetArrayLength());

        var item = items[0];
        Assert.Equal(seeded.MiddleDepartAt, item.GetProperty("departureAt").GetDateTimeOffset());
        Assert.Equal(seeded.EndArriveAt, item.GetProperty("arrivalAt").GetDateTimeOffset());
        Assert.Equal(0, item.GetProperty("availableSeatCount").GetInt32());
        Assert.Equal(1, item.GetProperty("occupiedSeatCount").GetInt32());
        Assert.False(item.GetProperty("canBook").GetBoolean());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options, new TenantContext());
    }

    private static async Task<SeededTrainReservationContext> SeedTrainReservationContextAsync(AppDbContext context)
    {
        var tenantId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var carId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var fromStopTimeId = Guid.NewGuid();
        var toStopTimeId = Guid.NewGuid();
        var segmentPriceId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);

        context.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Code = "TRAIN",
            Name = "Train Tenant",
            Type = TenantType.Train,
            Status = TenantStatus.Active,
            HoldMinutes = 5,
            CreatedAt = now
        });

        context.Providers.Add(new Provider
        {
            Id = providerId,
            TenantId = tenantId,
            Type = ProviderType.Train,
            Code = "TRAIN-PROV",
            Name = "Train Provider",
            Slug = "train-provider",
            IsActive = true,
            CreatedAt = now
        });

        context.TrainTrips.Add(new TrainTrip
        {
            Id = tripId,
            TenantId = tenantId,
            ProviderId = providerId,
            RouteId = routeId,
            TrainNumber = "SE1",
            Code = "TRAIN-01",
            Name = "Train 01",
            Status = TrainTripStatus.Published,
            DepartureAt = now.AddDays(5),
            ArrivalAt = now.AddDays(5).AddHours(2),
            IsActive = true,
            CreatedAt = now
        });

        context.TrainTripStopTimes.AddRange(
            new TrainTripStopTime
            {
                Id = fromStopTimeId,
                TenantId = tenantId,
                TripId = tripId,
                StopPointId = Guid.NewGuid(),
                StopIndex = 0,
                ArriveAt = now.AddDays(5),
                DepartAt = now.AddDays(5),
                IsActive = true,
                CreatedAt = now
            },
            new TrainTripStopTime
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

        context.TrainTripSegmentPrices.Add(new TrainTripSegmentPrice
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

        context.TrainCars.Add(new TrainCar
        {
            Id = carId,
            TenantId = tenantId,
            TripId = tripId,
            CarNumber = "01",
            CarType = TrainCarType.SeatCoach,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = now
        });

        context.TrainCarSeats.Add(new TrainCarSeat
        {
            Id = seatId,
            TenantId = tenantId,
            CarId = carId,
            SeatNumber = "A1",
            SeatType = TrainSeatType.Seat,
            RowIndex = 0,
            ColumnIndex = 0,
            IsActive = true,
            CreatedAt = now
        });

        var tour = new Tour
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = "TOUR-TRAIN",
            Name = "Train Package Tour",
            Slug = "train-package-tour",
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
            Code = "SCH-TRAIN",
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
            Code = "PKG-TRAIN",
            Name = "Default Train Package",
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
            Name = "Outbound Train",
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
            Code = "TRAIN",
            Name = "Train Option",
            SourceType = TourPackageSourceType.Train,
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

        return new SeededTrainReservationContext(
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

    private static async Task<SeededTrainSearchContext> SeedTrainSearchContextAsync(AppDbContext context)
    {
        var tenantId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var tripId = Guid.NewGuid();
        var routeId = Guid.NewGuid();
        var carId = Guid.NewGuid();
        var seatId = Guid.NewGuid();
        var fromLocationId = Guid.NewGuid();
        var toLocationId = Guid.NewGuid();
        var startLocationId = Guid.NewGuid();
        var startStopPointId = Guid.NewGuid();
        var middleStopPointId = Guid.NewGuid();
        var endStopPointId = Guid.NewGuid();
        var startDepartAt = new DateTimeOffset(2026, 4, 2, 23, 0, 0, TimeSpan.FromHours(7));
        var middleDepartAt = new DateTimeOffset(2026, 4, 3, 1, 15, 0, TimeSpan.FromHours(7));
        var endArriveAt = new DateTimeOffset(2026, 4, 3, 3, 30, 0, TimeSpan.FromHours(7));

        context.Providers.Add(new Provider
        {
            Id = providerId,
            TenantId = tenantId,
            Type = ProviderType.Train,
            Code = "TRAIN-SEARCH",
            Name = "Train Search Provider",
            Slug = "train-search-provider",
            IsActive = true,
            CreatedAt = startDepartAt
        });

        context.TrainStopPoints.AddRange(
            new TrainStopPoint
            {
                Id = startStopPointId,
                TenantId = tenantId,
                LocationId = startLocationId,
                Name = "Start Station",
                IsActive = true,
                CreatedAt = startDepartAt
            },
            new TrainStopPoint
            {
                Id = middleStopPointId,
                TenantId = tenantId,
                LocationId = fromLocationId,
                Name = "Middle Station",
                IsActive = true,
                CreatedAt = startDepartAt
            },
            new TrainStopPoint
            {
                Id = endStopPointId,
                TenantId = tenantId,
                LocationId = toLocationId,
                Name = "End Station",
                IsActive = true,
                CreatedAt = startDepartAt
            });

        context.TrainTrips.Add(new TrainTrip
        {
            Id = tripId,
            TenantId = tenantId,
            ProviderId = providerId,
            RouteId = routeId,
            TrainNumber = "SE2",
            Code = "TRAIN-SEARCH-01",
            Name = "Overnight Train",
            Status = TrainTripStatus.Published,
            DepartureAt = startDepartAt,
            ArrivalAt = endArriveAt,
            IsActive = true,
            CreatedAt = startDepartAt
        });

        var firstStopTimeId = Guid.NewGuid();
        var middleStopTimeId = Guid.NewGuid();
        var endStopTimeId = Guid.NewGuid();

        context.TrainTripStopTimes.AddRange(
            new TrainTripStopTime
            {
                Id = firstStopTimeId,
                TenantId = tenantId,
                TripId = tripId,
                StopPointId = startStopPointId,
                StopIndex = 0,
                ArriveAt = startDepartAt,
                DepartAt = startDepartAt,
                IsActive = true,
                CreatedAt = startDepartAt
            },
            new TrainTripStopTime
            {
                Id = middleStopTimeId,
                TenantId = tenantId,
                TripId = tripId,
                StopPointId = middleStopPointId,
                StopIndex = 1,
                ArriveAt = middleDepartAt.AddMinutes(-5),
                DepartAt = middleDepartAt,
                IsActive = true,
                CreatedAt = startDepartAt
            },
            new TrainTripStopTime
            {
                Id = endStopTimeId,
                TenantId = tenantId,
                TripId = tripId,
                StopPointId = endStopPointId,
                StopIndex = 2,
                ArriveAt = endArriveAt,
                DepartAt = null,
                IsActive = true,
                CreatedAt = startDepartAt
            });

        context.TrainTripSegmentPrices.Add(new TrainTripSegmentPrice
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TripId = tripId,
            FromTripStopTimeId = middleStopTimeId,
            ToTripStopTimeId = endStopTimeId,
            FromStopIndex = 1,
            ToStopIndex = 2,
            CurrencyCode = "VND",
            BaseFare = 200_000m,
            TaxesFees = 0m,
            TotalPrice = 200_000m,
            IsActive = true,
            CreatedAt = startDepartAt
        });

        context.TrainCars.Add(new TrainCar
        {
            Id = carId,
            TenantId = tenantId,
            TripId = tripId,
            CarNumber = "01",
            CarType = TrainCarType.SeatCoach,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = startDepartAt
        });

        context.TrainCarSeats.Add(new TrainCarSeat
        {
            Id = seatId,
            TenantId = tenantId,
            CarId = carId,
            SeatNumber = "A1",
            SeatType = TrainSeatType.Seat,
            RowIndex = 0,
            ColumnIndex = 0,
            IsActive = true,
            CreatedAt = startDepartAt
        });

        context.TrainTripSeatHolds.Add(new TrainTripSeatHold
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TripId = tripId,
            TrainCarSeatId = seatId,
            FromTripStopTimeId = middleStopTimeId,
            ToTripStopTimeId = endStopTimeId,
            FromStopIndex = 1,
            ToStopIndex = 2,
            Status = TrainSeatHoldStatus.Confirmed,
            HoldToken = "confirmed-search-seat",
            HoldExpiresAt = middleDepartAt.AddHours(1),
            CreatedAt = startDepartAt
        });

        await context.SaveChangesAsync();

        return new SeededTrainSearchContext(
            fromLocationId,
            toLocationId,
            DateOnly.FromDateTime(middleDepartAt.DateTime),
            middleDepartAt,
            endArriveAt);
    }

    private sealed record SeededTrainReservationContext(
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

    private sealed record SeededTrainSearchContext(
        Guid FromLocationId,
        Guid ToLocationId,
        DateOnly DepartDate,
        DateTimeOffset MiddleDepartAt,
        DateTimeOffset EndArriveAt);
}
