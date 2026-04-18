using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Tests.Tours;

public sealed class TourPackageAuditServiceTests
{
    [Fact]
    public async Task GetOverviewAsync_ReturnsAggregatedLifecycleMetrics()
    {
        using var context = CreateDbContext();
        var seeded = await SeedAsync(context);
        var service = new TourPackageAuditService(context);

        var result = await service.GetOverviewAsync(
            seeded.TenantId,
            seeded.TourId,
            new TourPackageAuditOverviewRequest());

        Assert.Equal(2, result.BookingCount);
        Assert.Equal(1, result.ConfirmedBookingCount);
        Assert.Equal(1, result.PartiallyCancelledBookingCount);
        Assert.Equal(1, result.HeldReservationCount);
        Assert.Equal(1, result.ExpiredReservationCount);
        Assert.Equal(1, result.CompletedCancellationCount);
        Assert.Equal(1, result.ReadyRefundCount);
        Assert.Equal(1, result.CompletedRescheduleCount);
        Assert.Equal(1, result.AttentionRequiredRescheduleCount);
        Assert.Equal(3_000_000m, result.GrossBookedAmount);
        Assert.Equal(300_000m, result.TotalPenaltyAmount);
        Assert.Equal(700_000m, result.TotalRefundAmount);
        Assert.Equal(2_300_000m, result.NetAfterRefundAmount);
        Assert.Equal(3, result.AuditEventCount);
        Assert.Equal(1, result.WarningEventCount);
        Assert.Equal(0, result.ErrorEventCount);
    }

    [Fact]
    public async Task GetSourceBreakdownAsync_AggregatesItemsBySourceType()
    {
        using var context = CreateDbContext();
        var seeded = await SeedAsync(context);
        var service = new TourPackageAuditService(context);

        var result = await service.GetSourceBreakdownAsync(
            seeded.TenantId,
            seeded.TourId,
            new TourPackageAuditSourceBreakdownRequest());

        var hotel = Assert.Single(result.Where(x => x.SourceType == TourPackageSourceType.Hotel));
        var flight = Assert.Single(result.Where(x => x.SourceType == TourPackageSourceType.Flight));
        var bus = Assert.Single(result.Where(x => x.SourceType == TourPackageSourceType.Bus));

        Assert.Equal(1, hotel.ConfirmedItemCount);
        Assert.Equal(1_200_000m, hotel.GrossLineAmount);
        Assert.Equal(1, flight.RefundPendingItemCount);
        Assert.Equal(700_000m, flight.RefundAmount);
        Assert.Equal(300_000m, flight.PenaltyAmount);
        Assert.Equal(1, flight.ReadyRefundCount);
        Assert.Equal(1, bus.FailedItemCount);
        Assert.Equal(800_000m, bus.GrossLineAmount);
    }

    [Fact]
    public async Task ListEventsAsync_FiltersByBookingAndOrdersNewestFirst()
    {
        using var context = CreateDbContext();
        var seeded = await SeedAsync(context);
        var service = new TourPackageAuditService(context);

        var result = await service.ListEventsAsync(
            seeded.TenantId,
            seeded.TourId,
            new TourPackageAuditEventListRequest
            {
                BookingId = seeded.BookingTwoId
            });

        Assert.Equal(2, result.Total);
        Assert.Equal("refund.prepared", result.Items[0].EventType);
        Assert.Equal("booking.partially-cancelled", result.Items[1].EventType);
        Assert.All(result.Items, x => Assert.Equal(seeded.BookingTwoId, x.BookingId));
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options, new TenantContext());
    }

    private static async Task<AuditSeedContext> SeedAsync(AppDbContext context)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);

        var tourId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var reservationOneId = Guid.NewGuid();
        var reservationTwoId = Guid.NewGuid();
        var reservationThreeId = Guid.NewGuid();
        var bookingOneId = Guid.NewGuid();
        var bookingTwoId = Guid.NewGuid();
        var hotelItemId = Guid.NewGuid();
        var flightItemId = Guid.NewGuid();
        var busItemId = Guid.NewGuid();
        var cancellationId = Guid.NewGuid();
        var cancellationItemId = Guid.NewGuid();
        var refundId = Guid.NewGuid();

        context.Tours.Add(new Tour
        {
            Id = tourId,
            TenantId = tenantId,
            Code = "TOUR-AUDIT",
            Name = "Tour Audit",
            Slug = "tour-audit",
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
            Code = "SCH-AUDIT",
            Name = "Schedule Audit",
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
            Code = "PKG-AUDIT",
            Name = "Package Audit",
            Mode = TourPackageMode.Configurable,
            Status = TourPackageStatus.Active,
            CurrencyCode = "VND",
            IsDefault = true,
            IsActive = true,
            CreatedAt = now
        });

        context.TourPackageReservations.AddRange(
            new TourPackageReservation
            {
                Id = reservationOneId,
                TenantId = tenantId,
                TourId = tourId,
                TourScheduleId = scheduleId,
                TourPackageId = packageId,
                UserId = userId,
                Code = "TPR-AUD-1",
                HoldToken = "hold-aud-1",
                Status = TourPackageReservationStatus.Confirmed,
                HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
                CurrencyCode = "VND",
                RequestedPax = 2,
                HeldCapacitySlots = 0,
                PackageSubtotalAmount = 1_200_000m,
                IsDeleted = false,
                CreatedAt = now
            },
            new TourPackageReservation
            {
                Id = reservationTwoId,
                TenantId = tenantId,
                TourId = tourId,
                TourScheduleId = scheduleId,
                TourPackageId = packageId,
                UserId = userId,
                Code = "TPR-AUD-2",
                HoldToken = "hold-aud-2",
                Status = TourPackageReservationStatus.Held,
                HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
                CurrencyCode = "VND",
                RequestedPax = 1,
                HeldCapacitySlots = 1,
                PackageSubtotalAmount = 600_000m,
                HoldExpiresAt = now.AddMinutes(10),
                IsDeleted = false,
                CreatedAt = now.AddMinutes(2)
            },
            new TourPackageReservation
            {
                Id = reservationThreeId,
                TenantId = tenantId,
                TourId = tourId,
                TourScheduleId = scheduleId,
                TourPackageId = packageId,
                UserId = userId,
                Code = "TPR-AUD-3",
                HoldToken = "hold-aud-3",
                Status = TourPackageReservationStatus.Expired,
                HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
                CurrencyCode = "VND",
                RequestedPax = 1,
                HeldCapacitySlots = 0,
                PackageSubtotalAmount = 500_000m,
                HoldExpiresAt = now.AddMinutes(1),
                IsDeleted = false,
                CreatedAt = now.AddMinutes(1)
            });

        context.TourPackageBookings.AddRange(
            new TourPackageBooking
            {
                Id = bookingOneId,
                TenantId = tenantId,
                TourId = tourId,
                TourScheduleId = scheduleId,
                TourPackageId = packageId,
                TourPackageReservationId = reservationOneId,
                UserId = userId,
                Code = "TPB-AUD-1",
                Status = TourPackageBookingStatus.Confirmed,
                HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
                CurrencyCode = "VND",
                RequestedPax = 2,
                ConfirmedCapacitySlots = 2,
                PackageSubtotalAmount = 1_200_000m,
                ConfirmedAt = now.AddMinutes(5),
                IsDeleted = false,
                CreatedAt = now.AddMinutes(5),
                Items =
                {
                    new TourPackageBookingItem
                    {
                        Id = hotelItemId,
                        TenantId = tenantId,
                        TourPackageReservationItemId = Guid.NewGuid(),
                        TourPackageComponentId = Guid.NewGuid(),
                        TourPackageComponentOptionId = Guid.NewGuid(),
                        ComponentType = TourPackageComponentType.Accommodation,
                        SourceType = TourPackageSourceType.Hotel,
                        Status = TourPackageBookingItemStatus.Confirmed,
                        Quantity = 1,
                        CurrencyCode = "VND",
                        UnitPrice = 1_200_000m,
                        LineAmount = 1_200_000m,
                        IsDeleted = false,
                        CreatedAt = now.AddMinutes(5)
                    }
                }
            },
            new TourPackageBooking
            {
                Id = bookingTwoId,
                TenantId = tenantId,
                TourId = tourId,
                TourScheduleId = scheduleId,
                TourPackageId = packageId,
                TourPackageReservationId = reservationOneId,
                UserId = userId,
                Code = "TPB-AUD-2",
                Status = TourPackageBookingStatus.PartiallyCancelled,
                HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
                CurrencyCode = "VND",
                RequestedPax = 2,
                ConfirmedCapacitySlots = 1,
                PackageSubtotalAmount = 1_800_000m,
                ConfirmedAt = now.AddMinutes(6),
                IsDeleted = false,
                CreatedAt = now.AddMinutes(6),
                Items =
                {
                    new TourPackageBookingItem
                    {
                        Id = flightItemId,
                        TenantId = tenantId,
                        TourPackageReservationItemId = Guid.NewGuid(),
                        TourPackageComponentId = Guid.NewGuid(),
                        TourPackageComponentOptionId = Guid.NewGuid(),
                        ComponentType = TourPackageComponentType.OutboundTransport,
                        SourceType = TourPackageSourceType.Flight,
                        Status = TourPackageBookingItemStatus.RefundPending,
                        Quantity = 1,
                        CurrencyCode = "VND",
                        UnitPrice = 1_000_000m,
                        LineAmount = 1_000_000m,
                        IsDeleted = false,
                        CreatedAt = now.AddMinutes(6)
                    },
                    new TourPackageBookingItem
                    {
                        Id = busItemId,
                        TenantId = tenantId,
                        TourPackageReservationItemId = Guid.NewGuid(),
                        TourPackageComponentId = Guid.NewGuid(),
                        TourPackageComponentOptionId = Guid.NewGuid(),
                        ComponentType = TourPackageComponentType.Transfer,
                        SourceType = TourPackageSourceType.Bus,
                        Status = TourPackageBookingItemStatus.Failed,
                        Quantity = 1,
                        CurrencyCode = "VND",
                        UnitPrice = 800_000m,
                        LineAmount = 800_000m,
                        IsDeleted = false,
                        CreatedAt = now.AddMinutes(6)
                    }
                }
            });

        context.TourPackageCancellations.Add(new TourPackageCancellation
        {
            Id = cancellationId,
            TenantId = tenantId,
            TourId = tourId,
            TourScheduleId = scheduleId,
            TourPackageId = packageId,
            TourPackageBookingId = bookingTwoId,
            RequestedByUserId = userId,
            Status = TourPackageCancellationStatus.Completed,
            CurrencyCode = "VND",
            PenaltyAmount = 300_000m,
            RefundAmount = 700_000m,
            CompletedAt = now.AddMinutes(8),
            IsDeleted = false,
            CreatedAt = now.AddMinutes(7),
            Items =
            {
                new TourPackageCancellationItem
                {
                    Id = cancellationItemId,
                    TenantId = tenantId,
                    TourPackageBookingItemId = flightItemId,
                    Status = TourPackageCancellationStatus.Completed,
                    CurrencyCode = "VND",
                    GrossLineAmount = 1_000_000m,
                    PenaltyAmount = 300_000m,
                    RefundAmount = 700_000m,
                    IsDeleted = false,
                    CreatedAt = now.AddMinutes(7)
                }
            }
        });

        context.TourPackageRefunds.Add(new TourPackageRefund
        {
            Id = refundId,
            TenantId = tenantId,
            TourPackageBookingId = bookingTwoId,
            TourPackageBookingItemId = flightItemId,
            TourPackageCancellationId = cancellationId,
            TourPackageCancellationItemId = cancellationItemId,
            Status = TourPackageRefundStatus.ReadyForProvider,
            CurrencyCode = "VND",
            GrossLineAmount = 1_000_000m,
            PenaltyAmount = 300_000m,
            RefundAmount = 700_000m,
            Provider = "SePay",
            PreparedAt = now.AddMinutes(9),
            IsDeleted = false,
            CreatedAt = now.AddMinutes(9)
        });

        context.TourPackageReschedules.AddRange(
            new TourPackageReschedule
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TourId = tourId,
                SourceTourPackageBookingId = bookingOneId,
                SourceTourPackageReservationId = reservationOneId,
                SourceTourScheduleId = scheduleId,
                SourceTourPackageId = packageId,
                TargetTourScheduleId = scheduleId,
                TargetTourPackageId = packageId,
                TargetTourPackageReservationId = reservationTwoId,
                TargetTourPackageBookingId = bookingOneId,
                Code = "TPRS-AUD-1",
                ClientToken = "reschedule-1",
                Status = TourPackageRescheduleStatus.Completed,
                HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
                CurrencyCode = "VND",
                RequestedPax = 2,
                SourcePackageSubtotalAmount = 1_200_000m,
                TargetPackageSubtotalAmount = 1_300_000m,
                PriceDifferenceAmount = 100_000m,
                ConfirmedAt = now.AddMinutes(10),
                IsDeleted = false,
                CreatedAt = now.AddMinutes(10)
            },
            new TourPackageReschedule
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TourId = tourId,
                SourceTourPackageBookingId = bookingTwoId,
                SourceTourPackageReservationId = reservationOneId,
                SourceTourScheduleId = scheduleId,
                SourceTourPackageId = packageId,
                TargetTourScheduleId = scheduleId,
                TargetTourPackageId = packageId,
                TargetTourPackageReservationId = reservationTwoId,
                Code = "TPRS-AUD-2",
                ClientToken = "reschedule-2",
                Status = TourPackageRescheduleStatus.AttentionRequired,
                HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
                CurrencyCode = "VND",
                RequestedPax = 2,
                SourcePackageSubtotalAmount = 1_800_000m,
                TargetPackageSubtotalAmount = 1_900_000m,
                PriceDifferenceAmount = 100_000m,
                FailureReason = "Supplier follow-up needed",
                IsDeleted = false,
                CreatedAt = now.AddMinutes(11)
            });

        context.TourPackageAuditEvents.AddRange(
            new TourPackageAuditEvent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TourId = tourId,
                TourScheduleId = scheduleId,
                TourPackageId = packageId,
                TourPackageBookingId = bookingOneId,
                ActorUserId = userId,
                EventType = "booking.confirmed",
                Title = "Package booking confirmed",
                Status = TourPackageBookingStatus.Confirmed.ToString(),
                CurrencyCode = "VND",
                Amount = 1_200_000m,
                Severity = TourPackageAuditSeverity.Info,
                IsSystemGenerated = true,
                CreatedAt = now.AddMinutes(5),
                UpdatedAt = now.AddMinutes(5)
            },
            new TourPackageAuditEvent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TourId = tourId,
                TourScheduleId = scheduleId,
                TourPackageId = packageId,
                TourPackageBookingId = bookingTwoId,
                ActorUserId = userId,
                EventType = "booking.partially-cancelled",
                Title = "Package booking partially cancelled",
                Status = TourPackageBookingStatus.PartiallyCancelled.ToString(),
                CurrencyCode = "VND",
                Amount = 1_800_000m,
                Severity = TourPackageAuditSeverity.Warning,
                IsSystemGenerated = true,
                CreatedAt = now.AddMinutes(8),
                UpdatedAt = now.AddMinutes(8)
            },
            new TourPackageAuditEvent
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TourId = tourId,
                TourScheduleId = scheduleId,
                TourPackageId = packageId,
                TourPackageBookingId = bookingTwoId,
                TourPackageBookingItemId = flightItemId,
                TourPackageRefundId = refundId,
                ActorUserId = userId,
                SourceType = TourPackageSourceType.Flight,
                EventType = "refund.prepared",
                Title = "Refund prepared",
                Status = TourPackageRefundStatus.ReadyForProvider.ToString(),
                CurrencyCode = "VND",
                Amount = 700_000m,
                Severity = TourPackageAuditSeverity.Info,
                IsSystemGenerated = true,
                CreatedAt = now.AddMinutes(9),
                UpdatedAt = now.AddMinutes(9)
            });

        await context.SaveChangesAsync();

        return new AuditSeedContext
        {
            TenantId = tenantId,
            TourId = tourId,
            BookingTwoId = bookingTwoId
        };
    }

    private sealed class AuditSeedContext
    {
        public Guid TenantId { get; init; }
        public Guid TourId { get; init; }
        public Guid BookingTwoId { get; init; }
    }
}
