using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Services.Tours;

public sealed class TourPackageRescheduleService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly TourLocalTimeService _tourLocalTimeService;
    private readonly TourPackageReservationService _reservationService;
    private readonly TourPackageBookingService _bookingService;
    private readonly TourPackageCancellationService _cancellationService;
    private readonly TourPackageAuditService? _auditService;

    public TourPackageRescheduleService(
        AppDbContext db,
        ITenantContext tenantContext,
        TourLocalTimeService tourLocalTimeService,
        TourPackageReservationService reservationService,
        TourPackageBookingService bookingService,
        TourPackageCancellationService cancellationService,
        TourPackageAuditService? auditService = null)
    {
        _db = db;
        _tenantContext = tenantContext;
        _tourLocalTimeService = tourLocalTimeService;
        _reservationService = reservationService;
        _bookingService = bookingService;
        _cancellationService = cancellationService;
        _auditService = auditService;
    }

    public async Task<TourPackageRescheduleExecutionResult> HoldAsync(
        Guid tourId,
        Guid sourceBookingId,
        TourPackageRescheduleHoldRequest request,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        if (sourceBookingId == Guid.Empty)
            throw new ArgumentException("SourceBookingId is required.", nameof(sourceBookingId));

        if (request.TargetScheduleId == Guid.Empty)
            throw new ArgumentException("TargetScheduleId is required.", nameof(request));

        if (request.TotalPax.HasValue && request.TotalPax.Value <= 0)
            throw new ArgumentException("TotalPax must be greater than 0 when provided.", nameof(request));

        ValidateSelectedOptions(request.SelectedPackageOptions);

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var sourceBooking = await LoadSourceBookingAsync(tourId, sourceBookingId, userId, isAdmin, ct);
            if (sourceBooking is null)
                throw new KeyNotFoundException("Source package booking not found.");

            if (!TourPackageRescheduleSupport.IsBookingEligibleForReschedule(sourceBooking))
                throw new InvalidOperationException("Only confirmed package bookings can be rescheduled.");

            if (sourceBooking.TourScheduleId == request.TargetScheduleId)
                throw new InvalidOperationException("Target schedule must be different from the source schedule.");

            _tenantContext.SetTenant(sourceBooking.TenantId);

            var clientToken = string.IsNullOrWhiteSpace(request.ClientToken)
                ? $"pkg-rsch-{Guid.NewGuid():N}"
                : request.ClientToken.Trim();

            if (clientToken.Length > 100)
                throw new ArgumentException("ClientToken max length is 100.", nameof(request));

            var existing = await LoadExistingByClientTokenAsync(sourceBooking.TenantId, sourceBooking.Id, clientToken, ct);
            if (existing is not null)
            {
                return new TourPackageRescheduleExecutionResult
                {
                    Reused = true,
                    Reschedule = MapReschedule(existing)
                };
            }

            var activeOther = await _db.TourPackageReschedules
                .IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.TenantId == sourceBooking.TenantId &&
                    x.SourceTourPackageBookingId == sourceBooking.Id &&
                    !x.IsDeleted &&
                    x.ClientToken != clientToken &&
                    (x.Status == TourPackageRescheduleStatus.Held || x.Status == TourPackageRescheduleStatus.Confirming), ct);

            if (activeOther)
                throw new InvalidOperationException("This package booking already has an active reschedule in progress.");

            var selectedOptions = request.SelectedPackageOptions.Count > 0
                ? request.SelectedPackageOptions
                    .Select(x => new TourPackageReservationSelectedOptionRequest
                    {
                        OptionId = x.OptionId,
                        Quantity = x.Quantity
                    })
                    .ToList()
                : request.CopyExistingSelections
                    ? TourPackageRescheduleSupport.BuildSelectedOptionsFromBooking(sourceBooking.Items.ToList())
                    : new List<TourPackageReservationSelectedOptionRequest>();

            var reservationRequest = new TourPackageReservationCreateRequest
            {
                ScheduleId = request.TargetScheduleId,
                PackageId = request.TargetPackageId.HasValue && request.TargetPackageId.Value != Guid.Empty
                    ? request.TargetPackageId
                    : sourceBooking.TourPackageId,
                TotalPax = request.TotalPax ?? sourceBooking.RequestedPax,
                IncludeDefaultPackageOptions = request.SelectedPackageOptions.Count > 0
                    ? request.IncludeDefaultPackageOptions
                    : !request.CopyExistingSelections && request.IncludeDefaultPackageOptions,
                SelectedPackageOptions = selectedOptions,
                ClientToken = clientToken,
                Notes = request.Notes
            };

            var reservationResult = await _reservationService.HoldAsync(
                tourId,
                reservationRequest,
                userId,
                isAdmin,
                ct);

            var now = await ResolveCurrentTimeAsync(sourceBooking.Tour, ct);
            var reschedule = new TourPackageReschedule
            {
                Id = Guid.NewGuid(),
                TenantId = sourceBooking.TenantId,
                TourId = sourceBooking.TourId,
                SourceTourPackageBookingId = sourceBooking.Id,
                SourceTourPackageReservationId = sourceBooking.TourPackageReservationId,
                SourceTourScheduleId = sourceBooking.TourScheduleId,
                SourceTourPackageId = sourceBooking.TourPackageId,
                TargetTourScheduleId = reservationResult.Reservation.ScheduleId,
                TargetTourPackageId = reservationResult.Reservation.PackageId,
                TargetTourPackageReservationId = reservationResult.Reservation.Id,
                RequestedByUserId = userId,
                Code = $"TPRS-{Guid.NewGuid():N}".Substring(0, 17).ToUpperInvariant(),
                ClientToken = clientToken,
                Status = TourPackageRescheduleStatus.Held,
                HoldStrategy = reservationResult.Reservation.HoldStrategy,
                CurrencyCode = reservationResult.Reservation.CurrencyCode,
                RequestedPax = reservationResult.Reservation.RequestedPax,
                SourcePackageSubtotalAmount = sourceBooking.PackageSubtotalAmount,
                TargetPackageSubtotalAmount = reservationResult.Reservation.PackageSubtotalAmount,
                PriceDifferenceAmount = reservationResult.Reservation.PackageSubtotalAmount - sourceBooking.PackageSubtotalAmount,
                HoldExpiresAt = reservationResult.Reservation.HoldExpiresAt,
                ReasonCode = NormalizeText(request.ReasonCode),
                ReasonText = NormalizeText(request.ReasonText),
                OverrideNote = NormalizeText(request.OverrideNote),
                Notes = NormalizeText(request.Notes),
                SnapshotJson = TourPackageRescheduleSupport.BuildSnapshotJson(
                    sourceBooking,
                    new TourPackageReservation
                    {
                        Id = reservationResult.Reservation.Id,
                        Code = reservationResult.Reservation.Code,
                        TourScheduleId = reservationResult.Reservation.ScheduleId,
                        TourPackageId = reservationResult.Reservation.PackageId,
                        PackageSubtotalAmount = reservationResult.Reservation.PackageSubtotalAmount
                    },
                    selectedOptions),
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId,
                UpdatedAt = now,
                UpdatedByUserId = userId
            };

            _db.TourPackageReschedules.Add(reschedule);

            _auditService?.Track(new TourPackageAuditWriteRequest
            {
                TenantId = reschedule.TenantId,
                TourId = reschedule.TourId,
                TourScheduleId = reschedule.TargetTourScheduleId,
                TourPackageId = reschedule.TargetTourPackageId,
                TourPackageReservationId = reschedule.TargetTourPackageReservationId,
                TourPackageBookingId = reschedule.SourceTourPackageBookingId,
                TourPackageRescheduleId = reschedule.Id,
                ActorUserId = userId,
                EventType = "reschedule.held",
                Title = "Package reschedule held",
                Status = reschedule.Status.ToString(),
                Description = $"Reschedule {reschedule.Code} held a replacement reservation for schedule {reservationResult.Reservation.ScheduleCode}.",
                CurrencyCode = reschedule.CurrencyCode,
                Amount = reschedule.PriceDifferenceAmount,
                Severity = TourPackageAuditSeverity.Info,
                SnapshotJson = reschedule.SnapshotJson,
                OccurredAt = now
            });
            await _db.SaveChangesAsync(ct);

            var created = await LoadRescheduleAsync(tourId, sourceBookingId, reschedule.Id, userId, isAdmin, ct)
                ?? throw new KeyNotFoundException("Package reschedule not found after creation.");

            return new TourPackageRescheduleExecutionResult
            {
                Reused = reservationResult.Reused,
                Reschedule = MapReschedule(created)
            };
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    public async Task<TourPackageRescheduleExecutionResult> ConfirmAsync(
        Guid tourId,
        Guid sourceBookingId,
        Guid rescheduleId,
        TourPackageRescheduleConfirmRequest request,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        if (sourceBookingId == Guid.Empty)
            throw new ArgumentException("SourceBookingId is required.", nameof(sourceBookingId));

        if (rescheduleId == Guid.Empty)
            throw new ArgumentException("RescheduleId is required.", nameof(rescheduleId));

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var reschedule = await LoadRescheduleAsync(tourId, sourceBookingId, rescheduleId, userId, isAdmin, ct);
            if (reschedule is null)
                throw new KeyNotFoundException("Package reschedule not found.");

            _tenantContext.SetTenant(reschedule.TenantId);

            if (reschedule.TargetTourPackageBookingId.HasValue && reschedule.TargetTourPackageBookingId.Value != Guid.Empty)
            {
                return new TourPackageRescheduleExecutionResult
                {
                    Reused = true,
                    Reschedule = MapReschedule(reschedule)
                };
            }

            if (reschedule.Status != TourPackageRescheduleStatus.Held &&
                reschedule.Status != TourPackageRescheduleStatus.Confirming)
            {
                throw new InvalidOperationException("Package reschedule is not eligible for reconfirm.");
            }

            var now = await ResolveCurrentTimeAsync(reschedule.SourceTourPackageBooking?.Tour, ct);
            reschedule.Status = TourPackageRescheduleStatus.Confirming;
            reschedule.UpdatedAt = now;
            reschedule.UpdatedByUserId = userId;
            reschedule.Notes = MergeText(reschedule.Notes, request.Notes);
            await _db.SaveChangesAsync(ct);

            TourPackageBookingConfirmServiceResult bookingResult;
            try
            {
                bookingResult = await _bookingService.ConfirmAsync(
                    tourId,
                    new TourPackageBookingConfirmRequest
                    {
                        ReservationId = reschedule.TargetTourPackageReservationId
                            ?? throw new InvalidOperationException("Reschedule does not have a target reservation."),
                        Notes = request.Notes
                    },
                    userId,
                    isAdmin,
                    ct);
            }
            catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
            {
                reschedule.Status = TourPackageRescheduleStatus.Failed;
                reschedule.FailureReason = NormalizeText(ex.Message);
                reschedule.UpdatedAt = DateTimeOffset.UtcNow;
                reschedule.UpdatedByUserId = userId;
                await _db.SaveChangesAsync(ct);
                throw;
            }

            reschedule.TargetTourPackageBookingId = bookingResult.Booking.Id;
            reschedule.TargetPackageSubtotalAmount = bookingResult.Booking.PackageSubtotalAmount;
            reschedule.PriceDifferenceAmount = bookingResult.Booking.PackageSubtotalAmount - reschedule.SourcePackageSubtotalAmount;
            reschedule.ConfirmedAt = bookingResult.Booking.ConfirmedAt;
            reschedule.HoldExpiresAt = null;

            TourPackageCancellationView? sourceCancellation = null;
            try
            {
                var cancellationResult = await _cancellationService.CancelRemainingForRescheduleAsync(
                    tourId,
                    sourceBookingId,
                    new TourPackageBulkCancellationRequest
                    {
                        ReasonCode = reschedule.ReasonCode ?? "reschedule",
                        ReasonText = reschedule.ReasonText ?? $"Rescheduled via {reschedule.Code}.",
                        OverrideNote = isAdmin ? reschedule.OverrideNote : null
                    },
                    userId,
                    isAdmin,
                    ct);

                sourceCancellation = cancellationResult.Cancellation;
                reschedule.SourceTourPackageCancellationId = sourceCancellation.Id;

                var refreshedSourceBooking = await _db.TourPackageBookings
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Id == sourceBookingId &&
                        x.TourId == tourId &&
                        !x.IsDeleted, ct);

                reschedule.Status = refreshedSourceBooking?.Status == TourPackageBookingStatus.Cancelled
                    ? TourPackageRescheduleStatus.Completed
                    : TourPackageRescheduleStatus.AttentionRequired;

                reschedule.FailureReason = reschedule.Status == TourPackageRescheduleStatus.AttentionRequired
                    ? NormalizeText(sourceCancellation.FailureReason)
                        ?? "The replacement booking was confirmed, but the source booking still has active items."
                    : null;
            }
            catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException)
            {
                reschedule.Status = TourPackageRescheduleStatus.AttentionRequired;
                reschedule.FailureReason = NormalizeText(ex.Message);
            }

            reschedule.ResolutionSnapshotJson = TourPackageRescheduleSupport.BuildResolutionSnapshotJson(
                reschedule,
                new TourPackageBooking
                {
                    Id = bookingResult.Booking.Id,
                    Code = bookingResult.Booking.Code,
                    Status = bookingResult.Booking.Status
                },
                sourceCancellation);
            reschedule.UpdatedAt = DateTimeOffset.UtcNow;
            reschedule.UpdatedByUserId = userId;

            _auditService?.Track(new TourPackageAuditWriteRequest
            {
                TenantId = reschedule.TenantId,
                TourId = reschedule.TourId,
                TourScheduleId = reschedule.TargetTourScheduleId,
                TourPackageId = reschedule.TargetTourPackageId,
                TourPackageReservationId = reschedule.TargetTourPackageReservationId,
                TourPackageBookingId = reschedule.TargetTourPackageBookingId ?? reschedule.SourceTourPackageBookingId,
                TourPackageCancellationId = reschedule.SourceTourPackageCancellationId,
                TourPackageRescheduleId = reschedule.Id,
                ActorUserId = userId,
                EventType = reschedule.Status == TourPackageRescheduleStatus.Completed
                    ? "reschedule.completed"
                    : "reschedule.attention-required",
                Title = reschedule.Status == TourPackageRescheduleStatus.Completed
                    ? "Package reschedule completed"
                    : "Package reschedule requires attention",
                Status = reschedule.Status.ToString(),
                Description = reschedule.Status == TourPackageRescheduleStatus.Completed
                    ? $"Reschedule {reschedule.Code} completed and replacement booking {bookingResult.Booking.Code} is active."
                    : reschedule.FailureReason ?? $"Reschedule {reschedule.Code} needs operator follow-up.",
                CurrencyCode = reschedule.CurrencyCode,
                Amount = reschedule.PriceDifferenceAmount,
                Severity = reschedule.Status == TourPackageRescheduleStatus.Completed
                    ? TourPackageAuditSeverity.Info
                    : TourPackageAuditSeverity.Warning,
                SnapshotJson = reschedule.ResolutionSnapshotJson,
                OccurredAt = reschedule.UpdatedAt
            });
            await _db.SaveChangesAsync(ct);

            var updated = await LoadRescheduleAsync(tourId, sourceBookingId, rescheduleId, userId, isAdmin, ct)
                ?? throw new KeyNotFoundException("Package reschedule not found after confirmation.");

            return new TourPackageRescheduleExecutionResult
            {
                Reused = bookingResult.Reused,
                Reschedule = MapReschedule(updated)
            };
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    public async Task<TourPackageRescheduleView> ReleaseAsync(
        Guid tourId,
        Guid sourceBookingId,
        Guid rescheduleId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        if (sourceBookingId == Guid.Empty)
            throw new ArgumentException("SourceBookingId is required.", nameof(sourceBookingId));

        if (rescheduleId == Guid.Empty)
            throw new ArgumentException("RescheduleId is required.", nameof(rescheduleId));

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var reschedule = await LoadRescheduleAsync(tourId, sourceBookingId, rescheduleId, userId, isAdmin, ct);
            if (reschedule is null)
                throw new KeyNotFoundException("Package reschedule not found.");

            _tenantContext.SetTenant(reschedule.TenantId);

            if (reschedule.Status == TourPackageRescheduleStatus.Released)
                return MapReschedule(reschedule);

            if (!TourPackageRescheduleSupport.CanRelease(reschedule.Status))
                throw new InvalidOperationException("Only held reschedules can be released.");

            if (!reschedule.TargetTourPackageReservationId.HasValue || reschedule.TargetTourPackageReservationId.Value == Guid.Empty)
                throw new InvalidOperationException("Reschedule does not have an active target reservation.");

            var releasedReservation = await _reservationService.ReleaseAsync(
                tourId,
                reschedule.TargetTourPackageReservationId.Value,
                userId,
                isAdmin,
                ct);

            var now = DateTimeOffset.UtcNow;
            reschedule.Status = TourPackageRescheduleStatus.Released;
            reschedule.HoldExpiresAt = releasedReservation.HoldExpiresAt;
            reschedule.FailureReason = null;
            reschedule.ResolutionSnapshotJson = JsonSerializer.Serialize(new
            {
                releasedReservationId = releasedReservation.Id,
                releasedReservationCode = releasedReservation.Code,
                releasedReservationStatus = releasedReservation.Status
            });
            reschedule.UpdatedAt = now;
            reschedule.UpdatedByUserId = userId;

            _auditService?.Track(new TourPackageAuditWriteRequest
            {
                TenantId = reschedule.TenantId,
                TourId = reschedule.TourId,
                TourScheduleId = reschedule.TargetTourScheduleId,
                TourPackageId = reschedule.TargetTourPackageId,
                TourPackageReservationId = reschedule.TargetTourPackageReservationId,
                TourPackageBookingId = reschedule.SourceTourPackageBookingId,
                TourPackageRescheduleId = reschedule.Id,
                ActorUserId = userId,
                EventType = "reschedule.released",
                Title = "Package reschedule released",
                Status = reschedule.Status.ToString(),
                Description = $"Reschedule {reschedule.Code} released the replacement reservation.",
                CurrencyCode = reschedule.CurrencyCode,
                Amount = reschedule.PriceDifferenceAmount,
                Severity = TourPackageAuditSeverity.Info,
                SnapshotJson = reschedule.ResolutionSnapshotJson,
                OccurredAt = now
            });
            await _db.SaveChangesAsync(ct);

            var updated = await LoadRescheduleAsync(tourId, sourceBookingId, rescheduleId, userId, isAdmin, ct)
                ?? throw new KeyNotFoundException("Package reschedule not found after release.");

            return MapReschedule(updated);
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    public async Task<List<TourPackageRescheduleView>> ListAsync(
        Guid tourId,
        Guid sourceBookingId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        if (sourceBookingId == Guid.Empty)
            throw new ArgumentException("SourceBookingId is required.", nameof(sourceBookingId));

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var sourceBooking = await LoadSourceBookingAsync(tourId, sourceBookingId, userId, isAdmin, ct);
            if (sourceBooking is null)
                throw new KeyNotFoundException("Source package booking not found.");

            _tenantContext.SetTenant(sourceBooking.TenantId);

            var items = await _db.TourPackageReschedules
                .IgnoreQueryFilters()
                .Include(x => x.SourceTourSchedule)
                .Include(x => x.SourceTourPackage)
                .Include(x => x.TargetTourSchedule)
                .Include(x => x.TargetTourPackage)
                .Include(x => x.TargetTourPackageReservation)
                .Include(x => x.TargetTourPackageBooking)
                .Include(x => x.SourceTourPackageCancellation)
                .Where(x =>
                    x.TourId == tourId &&
                    x.SourceTourPackageBookingId == sourceBookingId &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);

            return items.Select(MapReschedule).ToList();
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    public async Task<TourPackageRescheduleView> GetAsync(
        Guid tourId,
        Guid sourceBookingId,
        Guid rescheduleId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        if (sourceBookingId == Guid.Empty)
            throw new ArgumentException("SourceBookingId is required.", nameof(sourceBookingId));

        if (rescheduleId == Guid.Empty)
            throw new ArgumentException("RescheduleId is required.", nameof(rescheduleId));

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var reschedule = await LoadRescheduleAsync(tourId, sourceBookingId, rescheduleId, userId, isAdmin, ct);
            if (reschedule is null)
                throw new KeyNotFoundException("Package reschedule not found.");

            _tenantContext.SetTenant(reschedule.TenantId);

            if (reschedule.Status == TourPackageRescheduleStatus.Held &&
                reschedule.TargetTourPackageReservationId.HasValue)
            {
                var reservation = await _reservationService.GetAsync(
                    tourId,
                    reschedule.TargetTourPackageReservationId.Value,
                    userId,
                    isAdmin,
                    ct);

                if (reservation.Status == TourPackageReservationStatus.Expired)
                {
                    reschedule.Status = TourPackageRescheduleStatus.Failed;
                    reschedule.FailureReason = "Target reservation expired before reconfirm.";
                    reschedule.HoldExpiresAt = reservation.HoldExpiresAt;
                    reschedule.UpdatedAt = DateTimeOffset.UtcNow;
                    reschedule.UpdatedByUserId = userId;
                    await _db.SaveChangesAsync(ct);
                }
                else if (reservation.Status == TourPackageReservationStatus.Released)
                {
                    reschedule.Status = TourPackageRescheduleStatus.Released;
                    reschedule.HoldExpiresAt = reservation.HoldExpiresAt;
                    reschedule.UpdatedAt = DateTimeOffset.UtcNow;
                    reschedule.UpdatedByUserId = userId;
                    await _db.SaveChangesAsync(ct);
                }
            }

            var updated = await LoadRescheduleAsync(tourId, sourceBookingId, rescheduleId, userId, isAdmin, ct)
                ?? throw new KeyNotFoundException("Package reschedule not found.");

            return MapReschedule(updated);
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    private async Task<TourPackageBooking?> LoadSourceBookingAsync(
        Guid tourId,
        Guid sourceBookingId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        var booking = await _db.TourPackageBookings
            .IgnoreQueryFilters()
            .Include(x => x.Tour)
            .Include(x => x.TourSchedule)
            .Include(x => x.TourPackage)
            .Include(x => x.TourPackageReservation)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == sourceBookingId &&
                x.TourId == tourId &&
                !x.IsDeleted, ct);

        if (booking is null)
            return null;

        if (!isAdmin && userId.HasValue && booking.UserId.HasValue && booking.UserId.Value != userId.Value)
            return null;

        if (!isAdmin && booking.UserId.HasValue == false)
            return null;

        return booking;
    }

    private async Task<TourPackageReschedule?> LoadExistingByClientTokenAsync(
        Guid tenantId,
        Guid sourceBookingId,
        string clientToken,
        CancellationToken ct)
    {
        return await _db.TourPackageReschedules
            .IgnoreQueryFilters()
            .Include(x => x.SourceTourSchedule)
            .Include(x => x.SourceTourPackage)
            .Include(x => x.TargetTourSchedule)
            .Include(x => x.TargetTourPackage)
            .Include(x => x.TargetTourPackageReservation)
            .Include(x => x.TargetTourPackageBooking)
            .Include(x => x.SourceTourPackageCancellation)
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.SourceTourPackageBookingId == sourceBookingId &&
                x.ClientToken == clientToken &&
                !x.IsDeleted, ct);
    }

    private async Task<TourPackageReschedule?> LoadRescheduleAsync(
        Guid tourId,
        Guid sourceBookingId,
        Guid rescheduleId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        var reschedule = await _db.TourPackageReschedules
            .IgnoreQueryFilters()
            .Include(x => x.SourceTourPackageBooking)
            .ThenInclude(x => x!.Tour)
            .Include(x => x.SourceTourPackageBooking)
            .ThenInclude(x => x!.TourSchedule)
            .Include(x => x.SourceTourPackageBooking)
            .ThenInclude(x => x!.TourPackage)
            .Include(x => x.SourceTourPackageBooking)
            .ThenInclude(x => x!.TourPackageReservation)
            .Include(x => x.SourceTourSchedule)
            .Include(x => x.SourceTourPackage)
            .Include(x => x.TargetTourSchedule)
            .Include(x => x.TargetTourPackage)
            .Include(x => x.TargetTourPackageReservation)
            .Include(x => x.TargetTourPackageBooking)
            .Include(x => x.SourceTourPackageCancellation)
            .FirstOrDefaultAsync(x =>
                x.Id == rescheduleId &&
                x.TourId == tourId &&
                x.SourceTourPackageBookingId == sourceBookingId &&
                !x.IsDeleted, ct);

        if (reschedule is null)
            return null;

        var sourceBooking = reschedule.SourceTourPackageBooking;
        if (sourceBooking is null)
            return null;

        if (!isAdmin && userId.HasValue && sourceBooking.UserId.HasValue && sourceBooking.UserId.Value != userId.Value)
            return null;

        if (!isAdmin && sourceBooking.UserId.HasValue == false)
            return null;

        return reschedule;
    }

    private async Task<DateTimeOffset> ResolveCurrentTimeAsync(Tour? tour, CancellationToken ct)
    {
        if (tour is null)
            return DateTimeOffset.UtcNow;

        return await _tourLocalTimeService.ResolveCurrentTimeAsync(tour.PrimaryLocationId, ct);
    }

    private static TourPackageRescheduleView MapReschedule(TourPackageReschedule entity)
    {
        return new TourPackageRescheduleView
        {
            Id = entity.Id,
            TourId = entity.TourId,
            SourceBookingId = entity.SourceTourPackageBookingId,
            SourceReservationId = entity.SourceTourPackageReservationId,
            SourceScheduleId = entity.SourceTourScheduleId,
            SourceScheduleCode = entity.SourceTourSchedule?.Code ?? entity.SourceTourPackageBooking?.TourSchedule?.Code ?? string.Empty,
            SourceScheduleName = entity.SourceTourSchedule?.Name ?? entity.SourceTourPackageBooking?.TourSchedule?.Name,
            SourcePackageId = entity.SourceTourPackageId,
            SourcePackageCode = entity.SourceTourPackage?.Code ?? entity.SourceTourPackageBooking?.TourPackage?.Code ?? string.Empty,
            SourcePackageName = entity.SourceTourPackage?.Name ?? entity.SourceTourPackageBooking?.TourPackage?.Name ?? string.Empty,
            TargetScheduleId = entity.TargetTourScheduleId,
            TargetScheduleCode = entity.TargetTourSchedule?.Code ?? string.Empty,
            TargetScheduleName = entity.TargetTourSchedule?.Name,
            TargetPackageId = entity.TargetTourPackageId,
            TargetPackageCode = entity.TargetTourPackage?.Code ?? string.Empty,
            TargetPackageName = entity.TargetTourPackage?.Name ?? string.Empty,
            TargetReservationId = entity.TargetTourPackageReservationId,
            TargetBookingId = entity.TargetTourPackageBookingId,
            SourceCancellationId = entity.SourceTourPackageCancellationId,
            Code = entity.Code,
            ClientToken = entity.ClientToken,
            Status = entity.Status,
            HoldStrategy = entity.HoldStrategy,
            CurrencyCode = entity.CurrencyCode,
            RequestedPax = entity.RequestedPax,
            SourcePackageSubtotalAmount = entity.SourcePackageSubtotalAmount,
            TargetPackageSubtotalAmount = entity.TargetPackageSubtotalAmount,
            PriceDifferenceAmount = entity.PriceDifferenceAmount,
            HoldExpiresAt = entity.HoldExpiresAt,
            ConfirmedAt = entity.ConfirmedAt,
            ReasonCode = entity.ReasonCode,
            ReasonText = entity.ReasonText,
            OverrideNote = entity.OverrideNote,
            FailureReason = entity.FailureReason,
            SnapshotJson = entity.SnapshotJson,
            ResolutionSnapshotJson = entity.ResolutionSnapshotJson,
            Notes = entity.Notes,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            TargetReservation = entity.TargetTourPackageReservation is null
                ? null
                : new TourPackageRescheduleReservationSummaryView
                {
                    Id = entity.TargetTourPackageReservation.Id,
                    Code = entity.TargetTourPackageReservation.Code,
                    Status = entity.TargetTourPackageReservation.Status,
                    HoldToken = entity.TargetTourPackageReservation.HoldToken,
                    HoldExpiresAt = entity.TargetTourPackageReservation.HoldExpiresAt,
                    PackageSubtotalAmount = entity.TargetTourPackageReservation.PackageSubtotalAmount
                },
            TargetBooking = entity.TargetTourPackageBooking is null
                ? null
                : new TourPackageRescheduleBookingSummaryView
                {
                    Id = entity.TargetTourPackageBooking.Id,
                    Code = entity.TargetTourPackageBooking.Code,
                    Status = entity.TargetTourPackageBooking.Status,
                    ConfirmedAt = entity.TargetTourPackageBooking.ConfirmedAt,
                    PackageSubtotalAmount = entity.TargetTourPackageBooking.PackageSubtotalAmount
                },
            SourceCancellation = entity.SourceTourPackageCancellation is null
                ? null
                : new TourPackageRescheduleCancellationSummaryView
                {
                    Id = entity.SourceTourPackageCancellation.Id,
                    Status = entity.SourceTourPackageCancellation.Status,
                    RefundAmount = entity.SourceTourPackageCancellation.RefundAmount,
                    FailureReason = entity.SourceTourPackageCancellation.FailureReason,
                    CompletedAt = entity.SourceTourPackageCancellation.CompletedAt
                }
        };
    }

    private static void ValidateSelectedOptions(IReadOnlyCollection<TourPackageReservationSelectedOptionRequest> selectedOptions)
    {
        if (selectedOptions.Any(x => x.OptionId == Guid.Empty))
            throw new ArgumentException("Each SelectedPackageOption.OptionId is required.");

        if (selectedOptions.Any(x => x.Quantity.HasValue && x.Quantity.Value <= 0))
            throw new ArgumentException("Each SelectedPackageOption.Quantity must be greater than 0 when provided.");

        var duplicateOptionIds = selectedOptions
            .GroupBy(x => x.OptionId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateOptionIds.Count > 0)
            throw new ArgumentException("Duplicate package OptionId values are not allowed.");
    }

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? MergeText(string? current, string? next)
    {
        var normalizedCurrent = NormalizeText(current);
        var normalizedNext = NormalizeText(next);

        if (normalizedCurrent is null)
            return normalizedNext;

        if (normalizedNext is null || string.Equals(normalizedCurrent, normalizedNext, StringComparison.OrdinalIgnoreCase))
            return normalizedCurrent;

        return $"{normalizedCurrent} | {normalizedNext}";
    }
}

public sealed class TourPackageRescheduleHoldRequest
{
    public Guid TargetScheduleId { get; set; }
    public Guid? TargetPackageId { get; set; }
    public int? TotalPax { get; set; }
    public bool IncludeDefaultPackageOptions { get; set; }
    public bool CopyExistingSelections { get; set; } = true;
    public List<TourPackageReservationSelectedOptionRequest> SelectedPackageOptions { get; set; } = new();
    public string? ClientToken { get; set; }
    public string? ReasonCode { get; set; }
    public string? ReasonText { get; set; }
    public string? OverrideNote { get; set; }
    public string? Notes { get; set; }
}

public sealed class TourPackageRescheduleConfirmRequest
{
    public string? Notes { get; set; }
}

public sealed class TourPackageRescheduleExecutionResult
{
    public bool Reused { get; set; }
    public TourPackageRescheduleView Reschedule { get; set; } = new();
}

public sealed class TourPackageRescheduleView
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public Guid SourceBookingId { get; set; }
    public Guid SourceReservationId { get; set; }
    public Guid SourceScheduleId { get; set; }
    public string SourceScheduleCode { get; set; } = string.Empty;
    public string? SourceScheduleName { get; set; }
    public Guid SourcePackageId { get; set; }
    public string SourcePackageCode { get; set; } = string.Empty;
    public string SourcePackageName { get; set; } = string.Empty;
    public Guid TargetScheduleId { get; set; }
    public string TargetScheduleCode { get; set; } = string.Empty;
    public string? TargetScheduleName { get; set; }
    public Guid TargetPackageId { get; set; }
    public string TargetPackageCode { get; set; } = string.Empty;
    public string TargetPackageName { get; set; } = string.Empty;
    public Guid? TargetReservationId { get; set; }
    public Guid? TargetBookingId { get; set; }
    public Guid? SourceCancellationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string ClientToken { get; set; } = string.Empty;
    public TourPackageRescheduleStatus Status { get; set; }
    public TourPackageHoldStrategy HoldStrategy { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int RequestedPax { get; set; }
    public decimal SourcePackageSubtotalAmount { get; set; }
    public decimal? TargetPackageSubtotalAmount { get; set; }
    public decimal? PriceDifferenceAmount { get; set; }
    public DateTimeOffset? HoldExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public string? ReasonCode { get; set; }
    public string? ReasonText { get; set; }
    public string? OverrideNote { get; set; }
    public string? FailureReason { get; set; }
    public string? SnapshotJson { get; set; }
    public string? ResolutionSnapshotJson { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public TourPackageRescheduleReservationSummaryView? TargetReservation { get; set; }
    public TourPackageRescheduleBookingSummaryView? TargetBooking { get; set; }
    public TourPackageRescheduleCancellationSummaryView? SourceCancellation { get; set; }
}

public sealed class TourPackageRescheduleReservationSummaryView
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public TourPackageReservationStatus Status { get; set; }
    public string HoldToken { get; set; } = string.Empty;
    public DateTimeOffset? HoldExpiresAt { get; set; }
    public decimal PackageSubtotalAmount { get; set; }
}

public sealed class TourPackageRescheduleBookingSummaryView
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public TourPackageBookingStatus Status { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public decimal PackageSubtotalAmount { get; set; }
}

public sealed class TourPackageRescheduleCancellationSummaryView
{
    public Guid Id { get; set; }
    public TourPackageCancellationStatus Status { get; set; }
    public decimal RefundAmount { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
