using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Bus;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Tests.Tours;

public sealed class TourPackageCancellationServiceTests
{
    [Fact]
    public async Task CancelItemsAsync_AllowsCustomerToCancelOptionalItem_AndUsesItemSnapshotRefundMath()
    {
        using var context = CreateDbContext(out var tenantContext);
        var seeded = await SeedBookingAsync(context, firstItemOptional: true, includeSecondItem: true);
        var service = CreateService(context, tenantContext);

        var result = await service.CancelItemsAsync(
            seeded.TourId,
            seeded.BookingId,
            new TourPackageCancellationCreateRequest
            {
                BookingItemIds = new List<Guid> { seeded.FirstBookingItemId },
                ReasonCode = "customer-change-plan",
                ReasonText = "Khach doi ke hoach"
            },
            seeded.UserId,
            isAdmin: false);

        var booking = await context.TourPackageBookings
            .IgnoreQueryFilters()
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == seeded.BookingId);

        Assert.False(result.Reused);
        Assert.Equal(TourPackageCancellationStatus.Completed, result.Cancellation.Status);
        Assert.Single(result.Cancellation.Items);
        Assert.Equal(300_000m, result.Cancellation.Items[0].PenaltyAmount);
        Assert.Equal(700_000m, result.Cancellation.Items[0].RefundAmount);
        Assert.Equal(TourPackageRefundStatus.ReadyForProvider, result.Cancellation.Items[0].Refund!.Status);
        Assert.Equal(TourPackageBookingStatus.PartiallyCancelled, booking.Status);
        Assert.Equal(TourPackageBookingItemStatus.RefundPending, booking.Items.Single(x => x.Id == seeded.FirstBookingItemId).Status);
        Assert.Equal(TourPackageBookingItemStatus.Confirmed, booking.Items.Single(x => x.Id == seeded.SecondBookingItemId).Status);
    }

    [Fact]
    public async Task CancelItemsAsync_RejectsCustomerCancellationForRequiredItem()
    {
        using var context = CreateDbContext(out var tenantContext);
        var seeded = await SeedBookingAsync(context, firstItemOptional: false, includeSecondItem: false);
        var service = CreateService(context, tenantContext);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelItemsAsync(
            seeded.TourId,
            seeded.BookingId,
            new TourPackageCancellationCreateRequest
            {
                BookingItemIds = new List<Guid> { seeded.FirstBookingItemId },
                ReasonCode = "customer-change-plan"
            },
            seeded.UserId,
            isAdmin: false));

        Assert.Contains("optional package items only", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelItemsAsync_AllowsAdminOverride_WhenSupplierRejects()
    {
        using var context = CreateDbContext(out var tenantContext);
        var seeded = await SeedBookingAsync(
            context,
            firstItemOptional: false,
            includeSecondItem: false,
            firstSourceType: TourPackageSourceType.Bus,
            firstSourceEntityId: await SeedBusSegmentPriceAsync(context, Guid.NewGuid()));

        var service = CreateService(
            context,
            tenantContext,
            new TestCancellationAdapter(
                TourPackageSourceType.Bus,
                new TourPackageSourceCancellationResult
                {
                    Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                    ErrorMessage = "Supplier denied cancellation."
                }));

        var result = await service.CancelItemsAsync(
            seeded.TourId,
            seeded.BookingId,
            new TourPackageCancellationCreateRequest
            {
                BookingItemIds = new List<Guid> { seeded.FirstBookingItemId },
                ReasonCode = "ops-force-cancel",
                OverrideNote = "Approved by operations manager."
            },
            seeded.UserId,
            isAdmin: true);

        var bookingItem = await context.TourPackageBookingItems
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == seeded.FirstBookingItemId);

        Assert.Equal(TourPackageCancellationStatus.Completed, result.Cancellation.Status);
        Assert.True(result.Cancellation.IsAdminOverride);
        Assert.Equal(TourPackageBookingItemStatus.RefundPending, bookingItem.Status);
        Assert.Contains("Admin override", result.Cancellation.Items[0].SupplierNote);
    }

    [Fact]
    public async Task CancelItemsAsync_IsIdempotentForRepeatedRequest()
    {
        using var context = CreateDbContext(out var tenantContext);
        var seeded = await SeedBookingAsync(context, firstItemOptional: true, includeSecondItem: false);
        var service = CreateService(context, tenantContext);

        var request = new TourPackageCancellationCreateRequest
        {
            BookingItemIds = new List<Guid> { seeded.FirstBookingItemId },
            ReasonCode = "customer-change-plan"
        };

        var first = await service.CancelItemsAsync(seeded.TourId, seeded.BookingId, request, seeded.UserId, isAdmin: false);
        var second = await service.CancelItemsAsync(seeded.TourId, seeded.BookingId, request, seeded.UserId, isAdmin: false);

        Assert.False(first.Reused);
        Assert.True(second.Reused);
        Assert.Equal(first.Cancellation.Id, second.Cancellation.Id);
    }

    private static TourPackageCancellationService CreateService(
        AppDbContext context,
        TenantContext tenantContext,
        params ITourPackageSourceCancellationAdapter[] adapters)
    {
        return new TourPackageCancellationService(
            context,
            tenantContext,
            new TourLocalTimeService(context),
            adapters);
    }

    private static AppDbContext CreateDbContext(out TenantContext tenantContext)
    {
        tenantContext = new TenantContext();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options, tenantContext);
    }

    private static async Task<SeededBookingContext> SeedBookingAsync(
        AppDbContext context,
        bool firstItemOptional,
        bool includeSecondItem,
        TourPackageSourceType firstSourceType = TourPackageSourceType.Other,
        Guid? firstSourceEntityId = null)
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var departureDate = DateOnly.FromDateTime(now.UtcDateTime.Date).AddDays(5);
        var returnDate = departureDate.AddDays(1);

        var tourId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var reservationId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var firstComponentId = Guid.NewGuid();
        var firstOptionId = Guid.NewGuid();
        var firstBookingItemId = Guid.NewGuid();
        var secondComponentId = Guid.NewGuid();
        var secondOptionId = Guid.NewGuid();
        var secondBookingItemId = includeSecondItem ? Guid.NewGuid() : Guid.Empty;

        context.Tours.Add(new Tour
        {
            Id = tourId,
            TenantId = tenantId,
            Code = "TOUR-01",
            Name = "Tour Package",
            Slug = "tour-package",
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
            Code = "SCH-01",
            DepartureDate = departureDate,
            ReturnDate = returnDate,
            Status = TourScheduleStatus.Open,
            IsActive = true,
            CreatedAt = now
        });

        context.TourPolicies.Add(new TourPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tourId,
            Code = "CANCEL-01",
            Name = "Cancellation Policy",
            Type = TourPolicyType.Cancellation,
            PolicyJson = """
            {"rules":[{"fromDay":7,"feePercent":0},{"fromDay":3,"toDay":6,"feePercent":30},{"sameDay":true,"feePercent":100}]}
            """,
            IsActive = true,
            CreatedAt = now
        });

        var package = new TourPackage
        {
            Id = packageId,
            TenantId = tenantId,
            TourId = tourId,
            Code = "PKG-01",
            Name = "Main Package",
            Mode = TourPackageMode.Configurable,
            Status = TourPackageStatus.Active,
            CurrencyCode = "VND",
            IsDefault = true,
            IsActive = true,
            CreatedAt = now
        };

        package.Components.Add(new TourPackageComponent
        {
            Id = firstComponentId,
            TenantId = tenantId,
            TourPackageId = packageId,
            Code = "COMP-01",
            Name = "First Component",
            ComponentType = TourPackageComponentType.Activity,
            SelectionMode = firstItemOptional ? TourPackageSelectionMode.OptionalSingle : TourPackageSelectionMode.RequiredSingle,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = now,
            Options =
            {
                new TourPackageComponentOption
                {
                    Id = firstOptionId,
                    TenantId = tenantId,
                    TourPackageComponentId = firstComponentId,
                    Code = "OPT-01",
                    Name = "First Option",
                    SourceType = firstSourceType,
                    BindingMode = TourPackageBindingMode.StaticReference,
                    SourceEntityId = firstSourceEntityId,
                    PricingMode = TourPackagePricingMode.Override,
                    CurrencyCode = "VND",
                    IsActive = true,
                    CreatedAt = now
                }
            }
        });

        if (includeSecondItem)
        {
            package.Components.Add(new TourPackageComponent
            {
                Id = secondComponentId,
                TenantId = tenantId,
                TourPackageId = packageId,
                Code = "COMP-02",
                Name = "Second Component",
                ComponentType = TourPackageComponentType.Support,
                SelectionMode = TourPackageSelectionMode.RequiredSingle,
                SortOrder = 2,
                IsActive = true,
                CreatedAt = now,
                Options =
                {
                    new TourPackageComponentOption
                    {
                        Id = secondOptionId,
                        TenantId = tenantId,
                        TourPackageComponentId = secondComponentId,
                        Code = "OPT-02",
                        Name = "Second Option",
                        SourceType = TourPackageSourceType.Other,
                        BindingMode = TourPackageBindingMode.ManualFulfillment,
                        PricingMode = TourPackagePricingMode.Override,
                        CurrencyCode = "VND",
                        IsActive = true,
                        CreatedAt = now
                    }
                }
            });
        }

        context.TourPackages.Add(package);

        context.TourPackageReservations.Add(new TourPackageReservation
        {
            Id = reservationId,
            TenantId = tenantId,
            TourId = tourId,
            TourScheduleId = scheduleId,
            TourPackageId = packageId,
            UserId = userId,
            Code = "TPR-01",
            HoldToken = "hold-token",
            Status = TourPackageReservationStatus.Confirmed,
            HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
            CurrencyCode = "VND",
            RequestedPax = 1,
            HeldCapacitySlots = 0,
            PackageSubtotalAmount = 1_500_000m,
            HoldExpiresAt = now.AddMinutes(5),
            IsDeleted = false,
            CreatedAt = now
        });

        var booking = new TourPackageBooking
        {
            Id = bookingId,
            TenantId = tenantId,
            TourId = tourId,
            TourScheduleId = scheduleId,
            TourPackageId = packageId,
            TourPackageReservationId = reservationId,
            UserId = userId,
            Code = "TPB-01",
            Status = TourPackageBookingStatus.Confirmed,
            HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
            CurrencyCode = "VND",
            RequestedPax = 1,
            ConfirmedCapacitySlots = 0,
            PackageSubtotalAmount = 1_500_000m,
            ConfirmedAt = now,
            IsDeleted = false,
            CreatedAt = now
        };

        booking.Items.Add(new TourPackageBookingItem
        {
            Id = firstBookingItemId,
            TenantId = tenantId,
            TourPackageBookingId = bookingId,
            TourPackageReservationItemId = Guid.NewGuid(),
            TourPackageComponentId = firstComponentId,
            TourPackageComponentOptionId = firstOptionId,
            ComponentType = TourPackageComponentType.Activity,
            SourceType = firstSourceType,
            SourceEntityId = firstSourceEntityId,
            Status = TourPackageBookingItemStatus.Confirmed,
            Quantity = 1,
            CurrencyCode = "VND",
            UnitPrice = 1_000_000m,
            LineAmount = 1_000_000m,
            SourceHoldToken = "source-token",
            IsDeleted = false,
            CreatedAt = now
        });

        if (includeSecondItem)
        {
            booking.Items.Add(new TourPackageBookingItem
            {
                Id = secondBookingItemId,
                TenantId = tenantId,
                TourPackageBookingId = bookingId,
                TourPackageReservationItemId = Guid.NewGuid(),
                TourPackageComponentId = secondComponentId,
                TourPackageComponentOptionId = secondOptionId,
                ComponentType = TourPackageComponentType.Support,
                SourceType = TourPackageSourceType.Other,
                Status = TourPackageBookingItemStatus.Confirmed,
                Quantity = 1,
                CurrencyCode = "VND",
                UnitPrice = 500_000m,
                LineAmount = 500_000m,
                IsDeleted = false,
                CreatedAt = now
            });
        }

        context.TourPackageBookings.Add(booking);
        await context.SaveChangesAsync();

        return new SeededBookingContext
        {
            TenantId = tenantId,
            UserId = userId,
            TourId = tourId,
            BookingId = bookingId,
            FirstBookingItemId = firstBookingItemId,
            SecondBookingItemId = secondBookingItemId
        };
    }

    private static async Task<Guid> SeedBusSegmentPriceAsync(AppDbContext context, Guid tenantId)
    {
        var id = Guid.NewGuid();
        context.BusTripSegmentPrices.Add(new TripSegmentPrice
        {
            Id = id,
            TenantId = tenantId,
            TripId = Guid.NewGuid(),
            FromTripStopTimeId = Guid.NewGuid(),
            ToTripStopTimeId = Guid.NewGuid(),
            FromStopIndex = 0,
            ToStopIndex = 1,
            CurrencyCode = "VND",
            BaseFare = 100_000m,
            TotalPrice = 100_000m,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await context.SaveChangesAsync();
        return id;
    }

    private sealed class TestCancellationAdapter : ITourPackageSourceCancellationAdapter
    {
        private readonly TourPackageSourceType _sourceType;
        private readonly TourPackageSourceCancellationResult _result;

        public TestCancellationAdapter(TourPackageSourceType sourceType, TourPackageSourceCancellationResult result)
        {
            _sourceType = sourceType;
            _result = result;
        }

        public bool CanHandle(TourPackageSourceType sourceType)
            => sourceType == _sourceType;

        public Task<TourPackageSourceCancellationResult> CancelAsync(
            TourPackageSourceCancellationRequest request,
            CancellationToken ct = default)
            => Task.FromResult(_result);
    }

    private sealed class SeededBookingContext
    {
        public Guid TenantId { get; init; }
        public Guid UserId { get; init; }
        public Guid TourId { get; init; }
        public Guid BookingId { get; init; }
        public Guid FirstBookingItemId { get; init; }
        public Guid SecondBookingItemId { get; init; }
    }
}
