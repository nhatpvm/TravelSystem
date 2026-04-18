using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Services.Tours;

public sealed class TourPackageBookingService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly TourLocalTimeService _tourLocalTimeService;
    private readonly IReadOnlyCollection<ITourPackageSourceBookingAdapter> _bookingAdapters;
    private readonly IReadOnlyCollection<ITourPackageSourceReservationAdapter> _reservationAdapters;
    private readonly TourPackageAuditService? _auditService;

    public TourPackageBookingService(
        AppDbContext db,
        ITenantContext tenantContext,
        TourLocalTimeService tourLocalTimeService,
        IEnumerable<ITourPackageSourceBookingAdapter> bookingAdapters,
        IEnumerable<ITourPackageSourceReservationAdapter> reservationAdapters,
        TourPackageAuditService? auditService = null)
    {
        _db = db;
        _tenantContext = tenantContext;
        _tourLocalTimeService = tourLocalTimeService;
        _bookingAdapters = bookingAdapters?.ToList()
            ?? throw new ArgumentNullException(nameof(bookingAdapters));
        _reservationAdapters = reservationAdapters?.ToList()
            ?? throw new ArgumentNullException(nameof(reservationAdapters));
        _auditService = auditService;
    }

    public async Task<TourPackageBookingConfirmServiceResult> ConfirmAsync(
        Guid tourId,
        TourPackageBookingConfirmRequest request,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        if (request.ReservationId == Guid.Empty)
            throw new ArgumentException("ReservationId is required.", nameof(request));

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var reservation = await LoadReservationAsync(tourId, request.ReservationId, userId, isAdmin, ct);
            if (reservation is null)
                throw new KeyNotFoundException("Package reservation not found.");

            _tenantContext.SetTenant(reservation.TenantId);

            var existing = await LoadBookingByReservationIdAsync(tourId, reservation.Id, userId, isAdmin, ct);
            if (existing is not null)
            {
                return new TourPackageBookingConfirmServiceResult
                {
                    Reused = true,
                    Booking = MapBooking(existing)
                };
            }

            if (!IsReservationConfirmable(reservation))
                throw new InvalidOperationException("Package reservation is not eligible for confirmation.");

            var tour = await _db.Tours.IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == reservation.TourId &&
                    !x.IsDeleted, ct);

            if (tour is null)
                throw new KeyNotFoundException("Tour not found.");

            var currentTime = await _tourLocalTimeService.ResolveCurrentTimeAsync(tour.PrimaryLocationId, ct);

            if (reservation.HoldExpiresAt.HasValue && reservation.HoldExpiresAt.Value <= currentTime)
            {
                await ExpireReservationAsync(reservation, userId, isAdmin, ct);
                throw new InvalidOperationException("Package reservation has expired and can no longer be confirmed.");
            }

            var schedule = await _db.TourSchedules.IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == reservation.TourScheduleId &&
                    !x.IsDeleted, ct);

            if (schedule is null)
                throw new KeyNotFoundException("Tour schedule not found.");

            var package = await _db.TourPackages.IgnoreQueryFilters()
                .Include(x => x.Components)
                .ThenInclude(x => x.Options)
                .FirstOrDefaultAsync(x =>
                    x.Id == reservation.TourPackageId &&
                    !x.IsDeleted, ct);

            if (package is null)
                throw new KeyNotFoundException("Tour package not found.");

            var componentById = package.Components.ToDictionary(x => x.Id);
            var optionById = package.Components
                .SelectMany(x => x.Options)
                .ToDictionary(x => x.Id);

            var confirmedCapacitySlots = reservation.HeldCapacitySlots;
            var booking = CreateBookingEntity(reservation, request, userId, currentTime);
            var bookingItemsByReservationItemId = booking.Items.ToDictionary(x => x.TourPackageReservationItemId);

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            _db.TourPackageBookings.Add(booking);
            await _db.SaveChangesAsync(ct);

            var outcomes = new List<TourPackageSourceBookingConfirmResult>();
            foreach (var reservationItem in reservation.Items
                         .Where(x => !x.IsDeleted)
                         .OrderBy(x => x.CreatedAt)
                         .ThenBy(x => x.Id))
            {
                var bookingItem = bookingItemsByReservationItemId[reservationItem.Id];
                var component = componentById[reservationItem.TourPackageComponentId];
                var option = optionById[reservationItem.TourPackageComponentOptionId];

                var outcome = await ConfirmBookingItemAsync(
                    tour,
                    schedule,
                    package,
                    reservation,
                    reservationItem,
                    booking,
                    bookingItem,
                    component,
                    option,
                    userId,
                    isAdmin,
                    ct);

                ApplyOutcomeToBookingItem(bookingItem, outcome);
                ApplyOutcomeToReservationItem(reservationItem, outcome, userId, currentTime);

                if (outcome.Status == TourPackageBookingConfirmOutcomeStatus.Failed)
                {
                    await ReleaseFailedItemSourceHoldAsync(
                        reservation,
                        reservationItem,
                        userId,
                        isAdmin,
                        ct);
                }

                outcomes.Add(outcome);
            }

            var finalStatus = TourPackageBookingSupport.CalculateBookingStatus(outcomes, reservation.HoldStrategy);
            if (finalStatus == TourPackageBookingStatus.Failed)
            {
                await tx.RollbackAsync(ct);
                _db.ChangeTracker.Clear();

                throw new InvalidOperationException(
                    outcomes.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))?.ErrorMessage
                    ?? "Package booking confirmation failed.");
            }

            var capacity = await _db.TourScheduleCapacities
                .FirstOrDefaultAsync(x =>
                    x.TourScheduleId == reservation.TourScheduleId &&
                    x.IsActive &&
                    !x.IsDeleted, ct);

            if (capacity is not null && confirmedCapacitySlots > 0)
            {
                capacity.HeldSlots = Math.Max(0, capacity.HeldSlots - confirmedCapacitySlots);
                capacity.SoldSlots += confirmedCapacitySlots;

                if (capacity.AutoCloseWhenFull && capacity.AvailableSlots <= 0)
                    capacity.Status = TourCapacityStatus.Full;
            }

            booking.Status = finalStatus;
            booking.ConfirmedCapacitySlots = confirmedCapacitySlots;
            booking.ConfirmedAt = currentTime;
            booking.FailureReason = BuildFailureReason(outcomes);
            booking.SnapshotJson = BuildBookingSnapshotJson(tour, schedule, package, reservation, booking, booking.Items);

            reservation.Status = TourPackageReservationStatus.Confirmed;
            reservation.HeldCapacitySlots = 0;
            reservation.HoldExpiresAt = currentTime;
            reservation.FailureReason = booking.FailureReason;
            reservation.UpdatedAt = currentTime;
            reservation.UpdatedByUserId = userId;

            _auditService?.Track(new TourPackageAuditWriteRequest
            {
                TenantId = booking.TenantId,
                TourId = booking.TourId,
                TourScheduleId = booking.TourScheduleId,
                TourPackageId = booking.TourPackageId,
                TourPackageReservationId = reservation.Id,
                TourPackageBookingId = booking.Id,
                ActorUserId = userId,
                EventType = finalStatus == TourPackageBookingStatus.PartiallyConfirmed
                    ? "booking.partially-confirmed"
                    : "booking.confirmed",
                Title = finalStatus == TourPackageBookingStatus.PartiallyConfirmed
                    ? "Package booking partially confirmed"
                    : "Package booking confirmed",
                Status = booking.Status.ToString(),
                Description = finalStatus == TourPackageBookingStatus.PartiallyConfirmed
                    ? $"Booking {booking.Code} confirmed only part of the package services."
                    : $"Booking {booking.Code} was confirmed successfully.",
                CurrencyCode = booking.CurrencyCode,
                Amount = booking.PackageSubtotalAmount,
                Severity = finalStatus == TourPackageBookingStatus.PartiallyConfirmed
                    ? TourPackageAuditSeverity.Warning
                    : TourPackageAuditSeverity.Info,
                SnapshotJson = booking.SnapshotJson,
                OccurredAt = currentTime
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new TourPackageBookingConfirmServiceResult
            {
                Reused = false,
                Booking = MapBooking(
                    booking,
                    schedule.Code,
                    schedule.Name,
                    package.Code,
                    package.Name,
                    package.Mode)
            };
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    public async Task<TourPackageBookingView> GetAsync(
        Guid tourId,
        Guid bookingId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId is required.", nameof(bookingId));

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var booking = await LoadBookingAsync(tourId, bookingId, userId, isAdmin, ct);
            if (booking is null)
                throw new KeyNotFoundException("Package booking not found.");

            return MapBooking(booking);
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    private async Task<TourPackageSourceBookingConfirmResult> ConfirmBookingItemAsync(
        Tour tour,
        TourSchedule schedule,
        TourPackage package,
        TourPackageReservation reservation,
        TourPackageReservationItem reservationItem,
        TourPackageBooking booking,
        TourPackageBookingItem bookingItem,
        TourPackageComponent component,
        TourPackageComponentOption option,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        if (reservationItem.Status == TourPackageReservationItemStatus.Failed)
        {
            return new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Failed,
                ErrorMessage = reservationItem.ErrorMessage ?? "Package reservation item failed during hold phase."
            };
        }

        if (reservationItem.Status is TourPackageReservationItemStatus.Released or TourPackageReservationItemStatus.Expired)
        {
            return new TourPackageSourceBookingConfirmResult
            {
                Status = TourPackageBookingConfirmOutcomeStatus.Failed,
                ErrorMessage = "Package reservation item is no longer active for confirmation."
            };
        }

        if (option.BindingMode == TourPackageBindingMode.ManualFulfillment)
        {
            return CreateConfirmedOutcome(
                reservationItem,
                $"Package option '{option.Code}' is fulfilled manually and does not require external confirmation.");
        }

        var adapter = _bookingAdapters.FirstOrDefault(x => x.CanHandle(reservationItem.SourceType));
        if (adapter is null)
        {
            return reservationItem.SourceType is TourPackageSourceType.Activity
                or TourPackageSourceType.InternalService
                or TourPackageSourceType.Other
                ? CreateConfirmedOutcome(reservationItem, "This package item does not require an external booking adapter.")
                : new TourPackageSourceBookingConfirmResult
                {
                    Status = TourPackageBookingConfirmOutcomeStatus.Failed,
                    ErrorMessage = $"No booking adapter is configured for source type '{reservationItem.SourceType}'."
                };
        }

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var sourceTenantId = await ResolveSourceTenantIdAsync(reservationItem.SourceType, reservationItem.SourceEntityId, ct);
            if (sourceTenantId.HasValue)
                _tenantContext.SetTenant(sourceTenantId.Value);

            return await adapter.ConfirmAsync(new TourPackageSourceBookingConfirmRequest
            {
                UserId = userId,
                IsAdmin = isAdmin,
                Tour = tour,
                Schedule = schedule,
                Package = package,
                Reservation = reservation,
                ReservationItem = reservationItem,
                Booking = booking,
                BookingItem = bookingItem
            }, ct);
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    private TourPackageBooking CreateBookingEntity(
        TourPackageReservation reservation,
        TourPackageBookingConfirmRequest request,
        Guid? userId,
        DateTimeOffset now)
    {
        var booking = new TourPackageBooking
        {
            Id = Guid.NewGuid(),
            TenantId = reservation.TenantId,
            TourId = reservation.TourId,
            TourScheduleId = reservation.TourScheduleId,
            TourPackageId = reservation.TourPackageId,
            TourPackageReservationId = reservation.Id,
            UserId = reservation.UserId ?? userId,
            Code = $"TPB-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            Status = TourPackageBookingStatus.Pending,
            HoldStrategy = reservation.HoldStrategy,
            CurrencyCode = reservation.CurrencyCode,
            RequestedPax = reservation.RequestedPax,
            ConfirmedCapacitySlots = reservation.HeldCapacitySlots,
            PackageSubtotalAmount = reservation.PackageSubtotalAmount,
            Notes = MergeText(reservation.Notes, request.Notes),
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        foreach (var item in reservation.Items.Where(x => !x.IsDeleted))
        {
            booking.Items.Add(new TourPackageBookingItem
            {
                Id = Guid.NewGuid(),
                TenantId = reservation.TenantId,
                TourPackageBookingId = booking.Id,
                TourPackageReservationItemId = item.Id,
                TourPackageComponentId = item.TourPackageComponentId,
                TourPackageComponentOptionId = item.TourPackageComponentOptionId,
                ComponentType = item.ComponentType,
                SourceType = item.SourceType,
                SourceEntityId = item.SourceEntityId,
                Status = TourPackageBookingItemStatus.Pending,
                Quantity = item.Quantity,
                CurrencyCode = item.CurrencyCode,
                UnitPrice = item.UnitPrice,
                LineAmount = item.LineAmount,
                SourceHoldToken = item.SourceHoldToken,
                SnapshotJson = item.SnapshotJson,
                Notes = item.Notes,
                ErrorMessage = item.ErrorMessage,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId,
                UpdatedAt = now,
                UpdatedByUserId = userId
            });
        }

        return booking;
    }

    private static void ApplyOutcomeToBookingItem(
        TourPackageBookingItem item,
        TourPackageSourceBookingConfirmResult outcome)
    {
        item.Status = outcome.Status == TourPackageBookingConfirmOutcomeStatus.Confirmed
            ? TourPackageBookingItemStatus.Confirmed
            : TourPackageBookingItemStatus.Failed;
        item.ErrorMessage = string.IsNullOrWhiteSpace(outcome.ErrorMessage) ? null : outcome.ErrorMessage.Trim();
        item.Notes = MergeText(item.Notes, outcome.Note);
        item.SnapshotJson = !string.IsNullOrWhiteSpace(outcome.SnapshotJson)
            ? outcome.SnapshotJson
            : item.SnapshotJson;
    }

    private static void ApplyOutcomeToReservationItem(
        TourPackageReservationItem item,
        TourPackageSourceBookingConfirmResult outcome,
        Guid? userId,
        DateTimeOffset now)
    {
        item.Status = outcome.Status == TourPackageBookingConfirmOutcomeStatus.Confirmed
            ? TourPackageReservationItemStatus.Confirmed
            : TourPackageReservationItemStatus.Failed;
        item.ErrorMessage = string.IsNullOrWhiteSpace(outcome.ErrorMessage) ? null : outcome.ErrorMessage.Trim();
        item.Notes = MergeText(item.Notes, outcome.Note);
        item.HoldExpiresAt = outcome.Status == TourPackageBookingConfirmOutcomeStatus.Confirmed
            ? now
            : item.HoldExpiresAt;
        item.UpdatedAt = now;
        item.UpdatedByUserId = userId;
    }

    private async Task ReleaseFailedItemSourceHoldAsync(
        TourPackageReservation reservation,
        TourPackageReservationItem item,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        if (item.Status != TourPackageReservationItemStatus.Failed ||
            string.IsNullOrWhiteSpace(item.SourceHoldToken))
            return;

        var adapter = _reservationAdapters.FirstOrDefault(x => x.CanHandle(item.SourceType));
        if (adapter is null)
            return;

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var sourceTenantId = await ResolveSourceTenantIdAsync(item.SourceType, item.SourceEntityId, ct);
            if (sourceTenantId.HasValue)
                _tenantContext.SetTenant(sourceTenantId.Value);

            await adapter.ReleaseAsync(new TourPackageSourceReservationReleaseRequest
            {
                UserId = userId,
                IsAdmin = isAdmin,
                Reservation = reservation,
                Item = item
            }, ct);
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    private async Task<TourPackageReservation?> LoadReservationAsync(
        Guid tourId,
        Guid reservationId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        var reservation = await _db.TourPackageReservations.IgnoreQueryFilters()
            .Include(x => x.TourPackage)
            .Include(x => x.TourSchedule)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == reservationId &&
                x.TourId == tourId &&
                !x.IsDeleted, ct);

        if (reservation is null)
            return null;

        if (!isAdmin && userId.HasValue && reservation.UserId.HasValue && reservation.UserId.Value != userId.Value)
            return null;

        if (!isAdmin && reservation.UserId.HasValue == false)
            return null;

        return reservation;
    }

    private async Task<TourPackageBooking?> LoadBookingAsync(
        Guid tourId,
        Guid bookingId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        var booking = await _db.TourPackageBookings.IgnoreQueryFilters()
            .Include(x => x.TourPackage)
            .Include(x => x.TourSchedule)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == bookingId &&
                x.TourId == tourId &&
                !x.IsDeleted, ct);

        if (booking is null)
            return null;

        _tenantContext.SetTenant(booking.TenantId);

        if (!isAdmin && userId.HasValue && booking.UserId.HasValue && booking.UserId.Value != userId.Value)
            return null;

        if (!isAdmin && booking.UserId.HasValue == false)
            return null;

        return booking;
    }

    private async Task<TourPackageBooking?> LoadBookingByReservationIdAsync(
        Guid tourId,
        Guid reservationId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        var booking = await _db.TourPackageBookings.IgnoreQueryFilters()
            .Include(x => x.TourPackage)
            .Include(x => x.TourSchedule)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.TourId == tourId &&
                x.TourPackageReservationId == reservationId &&
                !x.IsDeleted, ct);

        if (booking is null)
            return null;

        _tenantContext.SetTenant(booking.TenantId);

        if (!isAdmin && userId.HasValue && booking.UserId.HasValue && booking.UserId.Value != userId.Value)
            return null;

        if (!isAdmin && booking.UserId.HasValue == false)
            return null;

        return booking;
    }

    private async Task ExpireReservationAsync(
        TourPackageReservation reservation,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        foreach (var item in reservation.Items.Where(x => !x.IsDeleted))
        {
            var adapter = _reservationAdapters.FirstOrDefault(x => x.CanHandle(item.SourceType));
            if (adapter is not null)
            {
                await adapter.ReleaseAsync(new TourPackageSourceReservationReleaseRequest
                {
                    UserId = userId,
                    IsAdmin = isAdmin,
                    Reservation = reservation,
                    Item = item
                }, ct);
            }

            if (item.Status != TourPackageReservationItemStatus.Failed)
                item.Status = TourPackageReservationItemStatus.Expired;
        }

        var capacity = await _db.TourScheduleCapacities
            .FirstOrDefaultAsync(x =>
                x.TourScheduleId == reservation.TourScheduleId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (capacity is not null && reservation.HeldCapacitySlots > 0)
            capacity.HeldSlots = Math.Max(0, capacity.HeldSlots - reservation.HeldCapacitySlots);

        reservation.Status = TourPackageReservationStatus.Expired;
        reservation.HeldCapacitySlots = 0;
        reservation.HoldExpiresAt = DateTimeOffset.UtcNow;
        reservation.UpdatedAt = DateTimeOffset.UtcNow;
        reservation.UpdatedByUserId = userId;

        _auditService?.Track(new TourPackageAuditWriteRequest
        {
            TenantId = reservation.TenantId,
            TourId = reservation.TourId,
            TourScheduleId = reservation.TourScheduleId,
            TourPackageId = reservation.TourPackageId,
            TourPackageReservationId = reservation.Id,
            ActorUserId = userId,
            EventType = "reservation.expired",
            Title = "Package reservation expired",
            Status = reservation.Status.ToString(),
            Description = $"Reservation {reservation.Code} expired before booking confirmation.",
            CurrencyCode = reservation.CurrencyCode,
            Amount = reservation.PackageSubtotalAmount,
            Severity = TourPackageAuditSeverity.Warning,
            SnapshotJson = reservation.SnapshotJson,
            OccurredAt = reservation.UpdatedAt
        });

        await _db.SaveChangesAsync(ct);
    }

    private async Task<Guid?> ResolveSourceTenantIdAsync(
        TourPackageSourceType sourceType,
        Guid? sourceEntityId,
        CancellationToken ct)
    {
        if (!sourceEntityId.HasValue || sourceEntityId.Value == Guid.Empty)
            return null;

        return sourceType switch
        {
            TourPackageSourceType.Flight => await _db.FlightOffers.IgnoreQueryFilters()
                .Where(x => x.Id == sourceEntityId.Value && !x.IsDeleted)
                .Select(x => (Guid?)x.TenantId)
                .FirstOrDefaultAsync(ct),
            TourPackageSourceType.Hotel => await _db.RatePlanRoomTypes.IgnoreQueryFilters()
                .Where(x => x.Id == sourceEntityId.Value && !x.IsDeleted)
                .Select(x => (Guid?)x.TenantId)
                .FirstOrDefaultAsync(ct),
            TourPackageSourceType.Bus => await _db.BusTripSegmentPrices.IgnoreQueryFilters()
                .Where(x => x.Id == sourceEntityId.Value && !x.IsDeleted)
                .Select(x => (Guid?)x.TenantId)
                .FirstOrDefaultAsync(ct),
            TourPackageSourceType.Train => await _db.TrainTripSegmentPrices.IgnoreQueryFilters()
                .Where(x => x.Id == sourceEntityId.Value && !x.IsDeleted)
                .Select(x => (Guid?)x.TenantId)
                .FirstOrDefaultAsync(ct),
            _ => null
        };
    }

    private static string? BuildFailureReason(IReadOnlyCollection<TourPackageSourceBookingConfirmResult> outcomes)
    {
        var messages = outcomes
            .Where(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))
            .Select(x => x.ErrorMessage!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return messages.Count == 0 ? null : string.Join(" | ", messages);
    }

    private static TourPackageSourceBookingConfirmResult CreateConfirmedOutcome(
        TourPackageReservationItem reservationItem,
        string note)
    {
        return new TourPackageSourceBookingConfirmResult
        {
            Status = TourPackageBookingConfirmOutcomeStatus.Confirmed,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                reservationItem.TourPackageComponentId,
                reservationItem.TourPackageComponentOptionId,
                reservationItem.ComponentType,
                reservationItem.SourceType,
                reservationItem.SourceEntityId,
                reservationItem.Quantity,
                reservationItem.UnitPrice,
                reservationItem.LineAmount
            }),
            Note = note
        };
    }

    private static string BuildBookingSnapshotJson(
        Tour tour,
        TourSchedule schedule,
        TourPackage package,
        TourPackageReservation reservation,
        TourPackageBooking booking,
        IEnumerable<TourPackageBookingItem> items)
    {
        return JsonSerializer.Serialize(new
        {
            tourId = tour.Id,
            tourCode = tour.Code,
            tourName = tour.Name,
            scheduleId = schedule.Id,
            scheduleCode = schedule.Code,
            scheduleName = schedule.Name,
            packageId = package.Id,
            packageCode = package.Code,
            packageName = package.Name,
            packageMode = package.Mode,
            reservationId = reservation.Id,
            reservationCode = reservation.Code,
            bookingId = booking.Id,
            bookingCode = booking.Code,
            items = items.Select(x => new
            {
                x.Id,
                x.TourPackageReservationItemId,
                x.TourPackageComponentId,
                x.TourPackageComponentOptionId,
                x.ComponentType,
                x.SourceType,
                x.SourceEntityId,
                x.Quantity,
                x.CurrencyCode,
                x.UnitPrice,
                x.LineAmount,
                x.Status,
                x.SourceHoldToken
            }).ToList()
        });
    }

    private static TourPackageBookingView MapBooking(
        TourPackageBooking booking,
        string? scheduleCode = null,
        string? scheduleName = null,
        string? packageCode = null,
        string? packageName = null,
        TourPackageMode? packageMode = null)
    {
        return new TourPackageBookingView
        {
            Id = booking.Id,
            TourId = booking.TourId,
            ScheduleId = booking.TourScheduleId,
            ScheduleCode = scheduleCode ?? booking.TourSchedule?.Code ?? string.Empty,
            ScheduleName = scheduleName ?? booking.TourSchedule?.Name,
            PackageId = booking.TourPackageId,
            PackageCode = packageCode ?? booking.TourPackage?.Code ?? string.Empty,
            PackageName = packageName ?? booking.TourPackage?.Name ?? string.Empty,
            PackageMode = packageMode ?? booking.TourPackage?.Mode ?? TourPackageMode.Fixed,
            ReservationId = booking.TourPackageReservationId,
            Code = booking.Code,
            Status = booking.Status,
            HoldStrategy = booking.HoldStrategy,
            CurrencyCode = booking.CurrencyCode,
            RequestedPax = booking.RequestedPax,
            ConfirmedCapacitySlots = booking.ConfirmedCapacitySlots,
            PackageSubtotalAmount = booking.PackageSubtotalAmount,
            ConfirmedAt = booking.ConfirmedAt,
            Notes = booking.Notes,
            FailureReason = booking.FailureReason,
            SnapshotJson = booking.SnapshotJson,
            IsDeleted = booking.IsDeleted,
            CreatedAt = booking.CreatedAt,
            UpdatedAt = booking.UpdatedAt,
            Items = booking.Items
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Select(x => new TourPackageBookingItemView
                {
                    Id = x.Id,
                    ReservationItemId = x.TourPackageReservationItemId,
                    ComponentId = x.TourPackageComponentId,
                    OptionId = x.TourPackageComponentOptionId,
                    ComponentType = x.ComponentType,
                    SourceType = x.SourceType,
                    SourceEntityId = x.SourceEntityId,
                    Status = x.Status,
                    Quantity = x.Quantity,
                    CurrencyCode = x.CurrencyCode,
                    UnitPrice = x.UnitPrice,
                    LineAmount = x.LineAmount,
                    SourceHoldToken = x.SourceHoldToken,
                    Notes = x.Notes,
                    ErrorMessage = x.ErrorMessage,
                    SnapshotJson = x.SnapshotJson
                })
                .ToList()
        };
    }

    private static bool IsReservationConfirmable(TourPackageReservation reservation)
        => reservation.Status == TourPackageReservationStatus.Held
           || reservation.Status == TourPackageReservationStatus.PartiallyHeld;

    private static string? MergeText(string? current, string? next)
    {
        var normalizedCurrent = string.IsNullOrWhiteSpace(current) ? null : current.Trim();
        var normalizedNext = string.IsNullOrWhiteSpace(next) ? null : next.Trim();

        if (normalizedCurrent is null)
            return normalizedNext;

        if (normalizedNext is null || string.Equals(normalizedCurrent, normalizedNext, StringComparison.OrdinalIgnoreCase))
            return normalizedCurrent;

        return $"{normalizedCurrent} | {normalizedNext}";
    }
}

public sealed class TourPackageBookingConfirmRequest
{
    public Guid ReservationId { get; set; }
    public string? Notes { get; set; }
}

public sealed class TourPackageBookingConfirmServiceResult
{
    public bool Reused { get; set; }
    public TourPackageBookingView Booking { get; set; } = new();
}

public sealed class TourPackageBookingView
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public Guid ScheduleId { get; set; }
    public string ScheduleCode { get; set; } = "";
    public string? ScheduleName { get; set; }
    public Guid PackageId { get; set; }
    public string PackageCode { get; set; } = "";
    public string PackageName { get; set; } = "";
    public TourPackageMode PackageMode { get; set; }
    public Guid ReservationId { get; set; }
    public string Code { get; set; } = "";
    public TourPackageBookingStatus Status { get; set; }
    public TourPackageHoldStrategy HoldStrategy { get; set; }
    public string CurrencyCode { get; set; } = "";
    public int RequestedPax { get; set; }
    public int ConfirmedCapacitySlots { get; set; }
    public decimal PackageSubtotalAmount { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? Notes { get; set; }
    public string? FailureReason { get; set; }
    public string? SnapshotJson { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<TourPackageBookingItemView> Items { get; set; } = new();
}

public sealed class TourPackageBookingItemView
{
    public Guid Id { get; set; }
    public Guid ReservationItemId { get; set; }
    public Guid ComponentId { get; set; }
    public Guid OptionId { get; set; }
    public TourPackageComponentType ComponentType { get; set; }
    public TourPackageSourceType SourceType { get; set; }
    public Guid? SourceEntityId { get; set; }
    public TourPackageBookingItemStatus Status { get; set; }
    public int Quantity { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public string? SourceHoldToken { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SnapshotJson { get; set; }
}
