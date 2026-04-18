using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Tests.Tours;

public sealed class TourPackageBookingOpsServiceTests
{
    [Fact]
    public async Task ListAsync_ReturnsAggregatedCancellationAndRefundCounts()
    {
        using var context = CreateDbContext();
        var seeded = await SeedAsync(context);
        var service = new TourPackageBookingOpsService(context);

        var result = await service.ListAsync(
            seeded.TenantId,
            seeded.TourId,
            new TourPackageBookingOpsListRequest
            {
                Status = TourPackageBookingStatus.PartiallyCancelled
            });

        var item = Assert.Single(result.Items);
        Assert.Equal(seeded.BookingId, item.Id);
        Assert.Equal(1, item.CancellationCount);
        Assert.Equal(1, item.CompletedCancellationCount);
        Assert.Equal(1, item.RefundCount);
        Assert.Equal(1, item.ReadyRefundCount);
        Assert.Equal(700_000m, item.TotalRefundAmount);
    }

    [Fact]
    public async Task GetTimelineAsync_ReturnsNewestEventsFirst()
    {
        using var context = CreateDbContext();
        var seeded = await SeedAsync(context);
        var service = new TourPackageBookingOpsService(context);

        var result = await service.GetTimelineAsync(seeded.TenantId, seeded.TourId, seeded.BookingId);

        Assert.NotEmpty(result);
        Assert.Equal("refund.prepared", result[0].EventType);
        Assert.Contains(result, x => x.EventType == "booking.confirmed");
        Assert.Contains(result, x => x.EventType == "cancellation.completed");
        Assert.True(result.Zip(result.Skip(1)).All(x => x.First.OccurredAt >= x.Second.OccurredAt));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options, new TenantContext());
    }

    private static async Task<OpsSeedContext> SeedAsync(AppDbContext context)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var bookingItemId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);

        context.Tours.Add(new Tour
        {
            Id = tourId,
            TenantId = tenantId,
            Code = "TOUR-OPS",
            Name = "Tour Ops",
            Slug = "tour-ops",
            Status = TourStatus.Active,
            CurrencyCode = "VND",
            DurationDays = 2,
            DurationNights = 1,
            IsActive = true,
            CreatedAt = now
        });

        context.TourSchedules.Add(new TourSchedule
        {
            Id = scheduleId,
            TenantId = tenantId,
            TourId = tourId,
            Code = "SCH-OPS",
            Name = "Schedule Ops",
            DepartureDate = new DateOnly(2026, 4, 10),
            ReturnDate = new DateOnly(2026, 4, 11),
            Status = TourScheduleStatus.Open,
            IsActive = true,
            CreatedAt = now
        });

        context.TourPackages.Add(new TourPackage
        {
            Id = packageId,
            TenantId = tenantId,
            TourId = tourId,
            Code = "PKG-OPS",
            Name = "Package Ops",
            Mode = TourPackageMode.Configurable,
            Status = TourPackageStatus.Active,
            CurrencyCode = "VND",
            IsDefault = true,
            IsActive = true,
            CreatedAt = now
        });

        context.TourPackageReservations.Add(new TourPackageReservation
        {
            Id = reservationId,
            TenantId = tenantId,
            TourId = tourId,
            TourScheduleId = scheduleId,
            TourPackageId = packageId,
            UserId = userId,
            Code = "TPR-OPS",
            HoldToken = "hold-ops",
            Status = TourPackageReservationStatus.Confirmed,
            HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
            CurrencyCode = "VND",
            RequestedPax = 2,
            HeldCapacitySlots = 0,
            PackageSubtotalAmount = 1_000_000m,
            HoldExpiresAt = now.AddMinutes(5),
            UpdatedAt = now.AddMinutes(10),
            IsDeleted = false,
            CreatedAt = now
        });

        context.TourPackageBookings.Add(new TourPackageBooking
        {
            Id = bookingId,
            TenantId = tenantId,
            TourId = tourId,
            TourScheduleId = scheduleId,
            TourPackageId = packageId,
            TourPackageReservationId = reservationId,
            UserId = userId,
            Code = "TPB-OPS",
            Status = TourPackageBookingStatus.PartiallyCancelled,
            HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
            CurrencyCode = "VND",
            RequestedPax = 2,
            ConfirmedCapacitySlots = 2,
            PackageSubtotalAmount = 1_000_000m,
            ConfirmedAt = now.AddMinutes(10),
            UpdatedAt = now.AddMinutes(20),
            IsDeleted = false,
            CreatedAt = now.AddMinutes(9),
            Items =
            {
                new TourPackageBookingItem
                {
                    Id = bookingItemId,
                    TenantId = tenantId,
                    TourPackageReservationItemId = Guid.NewGuid(),
                    TourPackageComponentId = Guid.NewGuid(),
                    TourPackageComponentOptionId = Guid.NewGuid(),
                    ComponentType = TourPackageComponentType.Activity,
                    SourceType = TourPackageSourceType.Other,
                    Status = TourPackageBookingItemStatus.RefundPending,
                    Quantity = 1,
                    CurrencyCode = "VND",
                    UnitPrice = 1_000_000m,
                    LineAmount = 1_000_000m,
                    IsDeleted = false,
                    CreatedAt = now.AddMinutes(9)
                }
            }
        });

        var cancellationId = Guid.NewGuid();
        var cancellationItemId = Guid.NewGuid();
        context.TourPackageCancellations.Add(new TourPackageCancellation
        {
            Id = cancellationId,
            TenantId = tenantId,
            TourId = tourId,
            TourScheduleId = scheduleId,
            TourPackageId = packageId,
            TourPackageBookingId = bookingId,
            RequestedByUserId = userId,
            Status = TourPackageCancellationStatus.Completed,
            CurrencyCode = "VND",
            PenaltyAmount = 300_000m,
            RefundAmount = 700_000m,
            ReasonCode = "customer-change-plan",
            CompletedAt = now.AddMinutes(30),
            UpdatedAt = now.AddMinutes(30),
            IsDeleted = false,
            CreatedAt = now.AddMinutes(25),
            Items =
            {
                new TourPackageCancellationItem
                {
                    Id = cancellationItemId,
                    TenantId = tenantId,
                    TourPackageBookingItemId = bookingItemId,
                    Status = TourPackageCancellationStatus.Completed,
                    CurrencyCode = "VND",
                    GrossLineAmount = 1_000_000m,
                    PenaltyAmount = 300_000m,
                    RefundAmount = 700_000m,
                    IsDeleted = false,
                    CreatedAt = now.AddMinutes(25),
                    UpdatedAt = now.AddMinutes(30)
                }
            }
        });

        context.TourPackageRefunds.Add(new TourPackageRefund
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourPackageBookingId = bookingId,
            TourPackageBookingItemId = bookingItemId,
            TourPackageCancellationId = cancellationId,
            TourPackageCancellationItemId = cancellationItemId,
            Status = TourPackageRefundStatus.ReadyForProvider,
            CurrencyCode = "VND",
            GrossLineAmount = 1_000_000m,
            PenaltyAmount = 300_000m,
            RefundAmount = 700_000m,
            Provider = "SePay",
            PreparedAt = now.AddMinutes(35),
            UpdatedAt = now.AddMinutes(35),
            IsDeleted = false,
            CreatedAt = now.AddMinutes(35),
            Attempts =
            {
                new TourPackageRefundAttempt
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Status = TourPackageRefundStatus.ReadyForProvider,
                    Provider = "SePay",
                    AttemptedAt = now.AddMinutes(35),
                    IsDeleted = false,
                    CreatedAt = now.AddMinutes(35)
                }
            }
        });

        await context.SaveChangesAsync();

        return new OpsSeedContext
        {
            TenantId = tenantId,
            TourId = tourId,
            BookingId = bookingId
        };
    }

    private sealed class OpsSeedContext
    {
        public Guid TenantId { get; init; }
        public Guid TourId { get; init; }
        public Guid BookingId { get; init; }
    }
}
