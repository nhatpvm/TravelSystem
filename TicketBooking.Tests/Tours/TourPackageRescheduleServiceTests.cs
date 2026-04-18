using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Application.Services.Tours;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Tests.Tours;

public sealed class TourPackageRescheduleServiceTests
{
    [Fact]
    public async Task HoldAndConfirmAsync_CompletesReplacementBooking_AndCancelsSourceBooking()
    {
        using var context = CreateDbContext(out var tenantContext);
        var seeded = await SeedAsync(context, TourPackageSourceType.Other, TourPackageBindingMode.ManualFulfillment);
        var service = CreateService(context, tenantContext);

        var hold = await service.HoldAsync(
            seeded.TourId,
            seeded.SourceBookingId,
            new TourPackageRescheduleHoldRequest
            {
                TargetScheduleId = seeded.TargetScheduleId,
                ReasonCode = "reschedule",
                ReasonText = "Khach doi ngay di"
            },
            seeded.UserId,
            isAdmin: false);

        var confirm = await service.ConfirmAsync(
            seeded.TourId,
            seeded.SourceBookingId,
            hold.Reschedule.Id,
            new TourPackageRescheduleConfirmRequest
            {
                Notes = "Confirmed replacement booking"
            },
            seeded.UserId,
            isAdmin: false);

        var sourceBooking = await context.TourPackageBookings
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == seeded.SourceBookingId);

        var replacementBooking = await context.TourPackageBookings
            .FirstAsync(x => x.Id == confirm.Reschedule.TargetBookingId);

        Assert.False(hold.Reused);
        Assert.False(confirm.Reused);
        Assert.Equal(TourPackageRescheduleStatus.Completed, confirm.Reschedule.Status);
        Assert.Equal(TourPackageBookingStatus.Cancelled, sourceBooking.Status);
        Assert.All(sourceBooking.Items, x => Assert.Equal(TourPackageBookingItemStatus.Cancelled, x.Status));
        Assert.Equal(TourPackageBookingStatus.Confirmed, replacementBooking.Status);
        Assert.NotNull(confirm.Reschedule.SourceCancellationId);
    }

    [Fact]
    public async Task ReleaseAsync_ReleasesTargetReservation_AndMarksRescheduleReleased()
    {
        using var context = CreateDbContext(out var tenantContext);
        var seeded = await SeedAsync(context, TourPackageSourceType.Other, TourPackageBindingMode.ManualFulfillment);
        var service = CreateService(context, tenantContext);

        var hold = await service.HoldAsync(
            seeded.TourId,
            seeded.SourceBookingId,
            new TourPackageRescheduleHoldRequest
            {
                TargetScheduleId = seeded.TargetScheduleId,
                ReasonCode = "reschedule"
            },
            seeded.UserId,
            isAdmin: false);

        var released = await service.ReleaseAsync(
            seeded.TourId,
            seeded.SourceBookingId,
            hold.Reschedule.Id,
            seeded.UserId,
            isAdmin: false);

        var reservation = await context.TourPackageReservations
            .FirstAsync(x => x.Id == hold.Reschedule.TargetReservationId);

        Assert.Equal(TourPackageRescheduleStatus.Released, released.Status);
        Assert.Equal(TourPackageReservationStatus.Released, reservation.Status);
    }

    [Fact]
    public async Task ConfirmAsync_WhenSourceCancellationIsRejected_MarksAttentionRequired()
    {
        using var context = CreateDbContext(out var tenantContext);
        var seeded = await SeedAsync(context, TourPackageSourceType.Bus, TourPackageBindingMode.StaticReference);
        var service = CreateService(
            context,
            tenantContext,
            reservationAdapters: new ITourPackageSourceReservationAdapter[]
            {
                new TestReservationAdapter(TourPackageSourceType.Bus)
            },
            bookingAdapters: new ITourPackageSourceBookingAdapter[]
            {
                new TestBookingAdapter(TourPackageSourceType.Bus)
            },
            cancellationAdapters: new ITourPackageSourceCancellationAdapter[]
            {
                new TestCancellationAdapter(
                    TourPackageSourceType.Bus,
                    new TourPackageSourceCancellationResult
                    {
                        Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                        ErrorMessage = "Supplier blocked source cancellation."
                    })
            });

        var hold = await service.HoldAsync(
            seeded.TourId,
            seeded.SourceBookingId,
            new TourPackageRescheduleHoldRequest
            {
                TargetScheduleId = seeded.TargetScheduleId,
                ReasonCode = "reschedule"
            },
            seeded.UserId,
            isAdmin: false);

        var confirm = await service.ConfirmAsync(
            seeded.TourId,
            seeded.SourceBookingId,
            hold.Reschedule.Id,
            new TourPackageRescheduleConfirmRequest(),
            seeded.UserId,
            isAdmin: false);

        var sourceBooking = await context.TourPackageBookings
            .FirstAsync(x => x.Id == seeded.SourceBookingId);

        Assert.Equal(TourPackageRescheduleStatus.AttentionRequired, confirm.Reschedule.Status);
        Assert.Equal(TourPackageBookingStatus.Confirmed, sourceBooking.Status);
        Assert.NotNull(confirm.Reschedule.TargetBookingId);
        Assert.Contains("supplier blocked", confirm.Reschedule.FailureReason ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HoldAndConfirmAsync_WritesAuditTrailEvents()
    {
        using var context = CreateDbContext(out var tenantContext);
        var seeded = await SeedAsync(context, TourPackageSourceType.Other, TourPackageBindingMode.ManualFulfillment);
        var service = CreateService(context, tenantContext, enableAudit: true);

        var hold = await service.HoldAsync(
            seeded.TourId,
            seeded.SourceBookingId,
            new TourPackageRescheduleHoldRequest
            {
                TargetScheduleId = seeded.TargetScheduleId,
                ReasonCode = "reschedule"
            },
            seeded.UserId,
            isAdmin: false);

        await service.ConfirmAsync(
            seeded.TourId,
            seeded.SourceBookingId,
            hold.Reschedule.Id,
            new TourPackageRescheduleConfirmRequest(),
            seeded.UserId,
            isAdmin: false);

        var eventTypes = await context.TourPackageAuditEvents
            .OrderBy(x => x.CreatedAt)
            .Select(x => x.EventType)
            .ToListAsync();

        Assert.Contains("reservation.held", eventTypes);
        Assert.Contains("booking.confirmed", eventTypes);
        Assert.Contains("cancellation.completed", eventTypes);
        Assert.Contains(eventTypes, x => x is "refund.prepared" or "refund.rejected");
        Assert.Contains("reschedule.completed", eventTypes);
    }

    private static TourPackageRescheduleService CreateService(
        AppDbContext context,
        TenantContext tenantContext,
        bool enableAudit = false,
        IEnumerable<ITourPackageSourceReservationAdapter>? reservationAdapters = null,
        IEnumerable<ITourPackageSourceBookingAdapter>? bookingAdapters = null,
        IEnumerable<ITourPackageSourceCancellationAdapter>? cancellationAdapters = null)
    {
        var localTimeService = new TourLocalTimeService(context);
        var auditService = enableAudit ? new TourPackageAuditService(context) : null;
        var actualReservationAdapters = reservationAdapters?.ToArray() ?? Array.Empty<ITourPackageSourceReservationAdapter>();
        var reservationService = new TourPackageReservationService(
            context,
            tenantContext,
            new TourBookabilityService(),
            new TourPackageQuoteBuilder(),
            new TourPackageSourceQuoteResolver(Array.Empty<ITourPackageSourceQuoteAdapter>()),
            localTimeService,
            actualReservationAdapters,
            auditService);
        var bookingService = new TourPackageBookingService(
            context,
            tenantContext,
            localTimeService,
            bookingAdapters?.ToArray() ?? Array.Empty<ITourPackageSourceBookingAdapter>(),
            actualReservationAdapters,
            auditService);
        var cancellationService = new TourPackageCancellationService(
            context,
            tenantContext,
            localTimeService,
            cancellationAdapters?.ToArray() ?? Array.Empty<ITourPackageSourceCancellationAdapter>(),
            auditService);

        return new TourPackageRescheduleService(
            context,
            tenantContext,
            localTimeService,
            reservationService,
            bookingService,
            cancellationService,
            auditService);
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

    private static async Task<SeededRescheduleContext> SeedAsync(
        AppDbContext context,
        TourPackageSourceType sourceType,
        TourPackageBindingMode bindingMode)
    {
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 4, 2, 8, 0, 0, TimeSpan.Zero);

        var tourId = Guid.NewGuid();
        var sourceScheduleId = Guid.NewGuid();
        var targetScheduleId = Guid.NewGuid();
        var packageId = Guid.NewGuid();
        var sourceReservationId = Guid.NewGuid();
        var sourceBookingId = Guid.NewGuid();
        var componentId = Guid.NewGuid();
        var optionId = Guid.NewGuid();
        Guid? sourceEntityId = bindingMode == TourPackageBindingMode.StaticReference ? Guid.NewGuid() : null;

        context.Tours.Add(new Tour
        {
            Id = tourId,
            TenantId = tenantId,
            Code = "TOUR-RSCH",
            Name = "Tour Reschedule",
            Slug = "tour-reschedule",
            Status = TourStatus.Active,
            CurrencyCode = "VND",
            DurationDays = 2,
            DurationNights = 1,
            IsActive = true,
            CreatedAt = now
        });

        context.TourSchedules.AddRange(
            new TourSchedule
            {
                Id = sourceScheduleId,
                TenantId = tenantId,
                TourId = tourId,
                Code = "SCH-OLD",
                Name = "Old Schedule",
                DepartureDate = new DateOnly(2026, 4, 10),
                ReturnDate = new DateOnly(2026, 4, 11),
                Status = TourScheduleStatus.Open,
                IsActive = true,
                CreatedAt = now
            },
            new TourSchedule
            {
                Id = targetScheduleId,
                TenantId = tenantId,
                TourId = tourId,
                Code = "SCH-NEW",
                Name = "New Schedule",
                DepartureDate = new DateOnly(2026, 4, 12),
                ReturnDate = new DateOnly(2026, 4, 13),
                Status = TourScheduleStatus.Open,
                IsActive = true,
                CreatedAt = now
            });

        var package = new TourPackage
        {
            Id = packageId,
            TenantId = tenantId,
            TourId = tourId,
            Code = "PKG-RSCH",
            Name = "Package Reschedule",
            Mode = TourPackageMode.Configurable,
            Status = TourPackageStatus.Active,
            CurrencyCode = "VND",
            IsDefault = true,
            HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
            IsActive = true,
            CreatedAt = now
        };

        package.Components.Add(new TourPackageComponent
        {
            Id = componentId,
            TenantId = tenantId,
            TourPackageId = packageId,
            Code = "COMP-RSCH",
            Name = "Component",
            ComponentType = TourPackageComponentType.Activity,
            SelectionMode = TourPackageSelectionMode.RequiredSingle,
            SortOrder = 1,
            IsActive = true,
            CreatedAt = now,
            Options =
            {
                new TourPackageComponentOption
                {
                    Id = optionId,
                    TenantId = tenantId,
                    TourPackageComponentId = componentId,
                    Code = "OPT-RSCH",
                    Name = "Option",
                    SourceType = sourceType,
                    BindingMode = bindingMode,
                    SourceEntityId = sourceEntityId,
                    PricingMode = TourPackagePricingMode.Override,
                    CurrencyCode = "VND",
                    PriceOverride = 1_000_000m,
                    DefaultQuantity = 1,
                    IsDefaultSelected = true,
                    IsActive = true,
                    CreatedAt = now
                }
            }
        });

        context.TourPackages.Add(package);

        context.TourPackageReservations.Add(new TourPackageReservation
        {
            Id = sourceReservationId,
            TenantId = tenantId,
            TourId = tourId,
            TourScheduleId = sourceScheduleId,
            TourPackageId = packageId,
            UserId = userId,
            Code = "TPR-OLD",
            HoldToken = "hold-old",
            Status = TourPackageReservationStatus.Confirmed,
            HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
            CurrencyCode = "VND",
            RequestedPax = 1,
            HeldCapacitySlots = 0,
            PackageSubtotalAmount = 1_000_000m,
            IsDeleted = false,
            CreatedAt = now
        });

        context.TourPackageBookings.Add(new TourPackageBooking
        {
            Id = sourceBookingId,
            TenantId = tenantId,
            TourId = tourId,
            TourScheduleId = sourceScheduleId,
            TourPackageId = packageId,
            TourPackageReservationId = sourceReservationId,
            UserId = userId,
            Code = "TPB-OLD",
            Status = TourPackageBookingStatus.Confirmed,
            HoldStrategy = TourPackageHoldStrategy.AllOrNothing,
            CurrencyCode = "VND",
            RequestedPax = 1,
            ConfirmedCapacitySlots = 0,
            PackageSubtotalAmount = 1_000_000m,
            ConfirmedAt = now,
            IsDeleted = false,
            CreatedAt = now,
            Items =
            {
                new TourPackageBookingItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    TourPackageReservationItemId = Guid.NewGuid(),
                    TourPackageComponentId = componentId,
                    TourPackageComponentOptionId = optionId,
                    ComponentType = TourPackageComponentType.Activity,
                    SourceType = sourceType,
                    SourceEntityId = sourceEntityId,
                    Status = TourPackageBookingItemStatus.Confirmed,
                    Quantity = 1,
                    CurrencyCode = "VND",
                    UnitPrice = 1_000_000m,
                    LineAmount = 1_000_000m,
                    SourceHoldToken = bindingMode == TourPackageBindingMode.StaticReference ? "source-hold" : null,
                    IsDeleted = false,
                    CreatedAt = now
                }
            }
        });

        await context.SaveChangesAsync();

        return new SeededRescheduleContext
        {
            UserId = userId,
            TourId = tourId,
            SourceBookingId = sourceBookingId,
            TargetScheduleId = targetScheduleId
        };
    }

    private sealed class TestReservationAdapter : ITourPackageSourceReservationAdapter
    {
        private readonly TourPackageSourceType _sourceType;

        public TestReservationAdapter(TourPackageSourceType sourceType)
        {
            _sourceType = sourceType;
        }

        public bool CanHandle(TourPackageSourceType sourceType)
            => sourceType == _sourceType;

        public Task<TourPackageSourceReservationHoldResult> HoldAsync(
            TourPackageSourceReservationHoldRequest request,
            CancellationToken ct = default)
        {
            return Task.FromResult(new TourPackageSourceReservationHoldResult
            {
                Status = TourPackageReservationHoldOutcomeStatus.Held,
                SourceHoldToken = $"hold-{request.Option.Id:N}",
                HoldExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
            });
        }

        public Task ReleaseAsync(
            TourPackageSourceReservationReleaseRequest request,
            CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class TestBookingAdapter : ITourPackageSourceBookingAdapter
    {
        private readonly TourPackageSourceType _sourceType;

        public TestBookingAdapter(TourPackageSourceType sourceType)
        {
            _sourceType = sourceType;
        }

        public bool CanHandle(TourPackageSourceType sourceType)
            => sourceType == _sourceType;

        public Task<TourPackageSourceBookingConfirmResult> ConfirmAsync(
            TourPackageSourceBookingConfirmRequest request,
            CancellationToken ct = default)
        {
            return Task.FromResult(new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Confirmed
            });
        }
    }

    private sealed class TestCancellationAdapter : ITourPackageSourceCancellationAdapter
    {
        private readonly TourPackageSourceType _sourceType;
        private readonly TourPackageSourceCancellationResult _result;

        public TestCancellationAdapter(
            TourPackageSourceType sourceType,
            TourPackageSourceCancellationResult result)
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

    private sealed class SeededRescheduleContext
    {
        public Guid UserId { get; init; }
        public Guid TourId { get; init; }
        public Guid SourceBookingId { get; init; }
        public Guid TargetScheduleId { get; init; }
    }
}
