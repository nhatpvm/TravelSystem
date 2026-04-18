using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Application.Services.Tours;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Services.Tours;

public sealed class TourPackageReservationService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly TourBookabilityService _bookabilityService;
    private readonly TourPackageQuoteBuilder _packageQuoteBuilder;
    private readonly TourPackageSourceQuoteResolver _packageSourceQuoteResolver;
    private readonly TourLocalTimeService _tourLocalTimeService;
    private readonly IReadOnlyCollection<ITourPackageSourceReservationAdapter> _reservationAdapters;
    private readonly TourPackageAuditService? _auditService;

    public TourPackageReservationService(
        AppDbContext db,
        ITenantContext tenantContext,
        TourBookabilityService bookabilityService,
        TourPackageQuoteBuilder packageQuoteBuilder,
        TourPackageSourceQuoteResolver packageSourceQuoteResolver,
        TourLocalTimeService tourLocalTimeService,
        IEnumerable<ITourPackageSourceReservationAdapter> reservationAdapters,
        TourPackageAuditService? auditService = null)
    {
        _db = db;
        _tenantContext = tenantContext;
        _bookabilityService = bookabilityService;
        _packageQuoteBuilder = packageQuoteBuilder;
        _packageSourceQuoteResolver = packageSourceQuoteResolver;
        _tourLocalTimeService = tourLocalTimeService;
        _reservationAdapters = reservationAdapters?.ToList()
            ?? throw new ArgumentNullException(nameof(reservationAdapters));
        _auditService = auditService;
    }

    public async Task<TourPackageReservationHoldServiceResult> HoldAsync(
        Guid tourId,
        TourPackageReservationCreateRequest request,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        if (request.ScheduleId == Guid.Empty)
            throw new ArgumentException("ScheduleId is required.", nameof(request));

        if (request.TotalPax <= 0)
            throw new ArgumentException("TotalPax must be greater than 0.", nameof(request));

        ValidateSelectedOptions(request.SelectedPackageOptions);

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var tour = await _db.Tours.IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == tourId &&
                    x.IsActive &&
                    !x.IsDeleted &&
                    x.Status == TourStatus.Active, ct);

            if (tour is null)
                throw new KeyNotFoundException("Tour not found.");

            _tenantContext.SetTenant(tour.TenantId);

            var currentTime = await _tourLocalTimeService.ResolveCurrentTimeAsync(tour.PrimaryLocationId, ct);
            await ExpireReservationsAsync(tour.TenantId, tourId, request.ScheduleId, currentTime, userId, isAdmin, ct);

            var schedule = await _db.TourSchedules
                .FirstOrDefaultAsync(x =>
                    x.Id == request.ScheduleId &&
                    x.TourId == tourId &&
                    x.IsActive &&
                    !x.IsDeleted, ct);

            if (schedule is null)
                throw new KeyNotFoundException("Tour schedule not found.");

            var capacity = await _db.TourScheduleCapacities
                .FirstOrDefaultAsync(x =>
                    x.TourScheduleId == schedule.Id &&
                    x.IsActive &&
                    !x.IsDeleted, ct);

            var bookability = _bookabilityService.EvaluateSchedule(new TourBookabilityRequest
            {
                Tour = tour,
                Schedule = schedule,
                Capacity = capacity,
                RequestedPax = request.TotalPax,
                Now = currentTime
            });

            if (!bookability.CanBook)
                throw new InvalidOperationException(bookability.Reason);

            if (bookability.IsWaitlist)
                throw new InvalidOperationException("Package reservation does not support waitlist schedules yet.");

            var holdToken = string.IsNullOrWhiteSpace(request.ClientToken)
                ? $"pkg-res-{Guid.NewGuid():N}"
                : request.ClientToken.Trim();

            if (holdToken.Length > 100)
                throw new ArgumentException("ClientToken max length is 100.", nameof(request));

            var existing = await TryGetActiveReservationByTokenAsync(
                tour.TenantId,
                tourId,
                schedule.Id,
                holdToken,
                userId,
                isAdmin,
                currentTime,
                ct);

            if (existing is not null)
            {
                return new TourPackageReservationHoldServiceResult
                {
                    Reused = true,
                    Reservation = MapReservation(existing)
                };
            }

            var package = await ResolvePackageAsync(tour.Id, request.PackageId, request.SelectedPackageOptions.Count > 0, ct);
            if (package is null)
                throw new InvalidOperationException("Tour package not found or not available for reservation.");

            var totalNights = schedule.ReturnDate.DayNumber - schedule.DepartureDate.DayNumber;
            if (totalNights < 0)
                totalNights = 0;

            if (totalNights == 0 && tour.DurationNights > 0)
                totalNights = tour.DurationNights;

            var selectedOptions = request.SelectedPackageOptions
                .Select(x => new TourPackageQuoteSelectedOptionInput
                {
                    OptionId = x.OptionId,
                    Quantity = x.Quantity
                })
                .ToList();

            var sourceResolution = await _packageSourceQuoteResolver.ResolveAsync(new TourPackageSourceQuoteResolverRequest
            {
                Tour = tour,
                Schedule = schedule,
                Package = package,
                TotalPax = request.TotalPax,
                TotalNights = totalNights,
                SelectedOptions = selectedOptions
            }, ct);

            var packageQuote = _packageQuoteBuilder.Build(new TourPackageQuoteBuildRequest
            {
                TourId = tour.Id,
                ScheduleId = schedule.Id,
                TotalPax = request.TotalPax,
                TotalNights = totalNights,
                ExpectedCurrency = package.CurrencyCode,
                IncludeDefaultOptions = request.IncludeDefaultPackageOptions,
                Package = package,
                SelectedOptions = selectedOptions,
                SourceQuotes = sourceResolution.SourceQuotes
            }) ?? throw new InvalidOperationException("Package quote could not be resolved for reservation.");

            foreach (var note in sourceResolution.Notes.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                var normalized = note.Trim();
                if (!packageQuote.Notes.Contains(normalized, StringComparer.OrdinalIgnoreCase))
                    packageQuote.Notes.Add(normalized);
            }

            var holdMinutes = await ResolveHoldMinutesAsync(tour.TenantId, ct);
            var reservation = CreateReservationEntity(
                tour,
                schedule,
                package,
                packageQuote,
                request,
                holdToken,
                userId,
                capacity is null ? 0 : request.TotalPax,
                holdMinutes,
                currentTime);

            var linesByOptionId = packageQuote.Lines.ToDictionary(x => x.OptionId);
            var componentById = package.Components.ToDictionary(x => x.Id);
            var optionById = package.Components
                .SelectMany(x => x.Options)
                .ToDictionary(x => x.Id);

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            if (capacity is not null)
                capacity.HeldSlots += request.TotalPax;

            _db.TourPackageReservations.Add(reservation);
            await _db.SaveChangesAsync(ct);

            var outcomes = new List<TourPackageSourceReservationHoldResult>();
            foreach (var item in reservation.Items.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id))
            {
                if (!linesByOptionId.TryGetValue(item.TourPackageComponentOptionId, out var line))
                    throw new InvalidOperationException("Package reservation line could not be matched back to package quote.");

                var component = componentById[item.TourPackageComponentId];
                var option = optionById[item.TourPackageComponentOptionId];

                TourPackageSourceReservationHoldResult outcome;
                if (package.HoldStrategy == TourPackageHoldStrategy.None)
                {
                    outcome = CreateValidatedOutcome(
                        line,
                        $"Package hold strategy is {TourPackageHoldStrategy.None}; external source hold was skipped.");
                }
                else
                {
                    outcome = await HoldReservationItemAsync(
                        reservation,
                        item,
                        line,
                        tour,
                        schedule,
                        package,
                        component,
                        option,
                        totalNights,
                        userId,
                        ct);
                }

                ApplyOutcomeToItem(item, line, outcome);
                outcomes.Add(outcome);
            }

            var finalStatus = TourPackageReservationSupport.CalculateReservationStatus(outcomes, package.HoldStrategy);
            if (finalStatus == TourPackageReservationStatus.Failed)
            {
                if (capacity is not null)
                    capacity.HeldSlots = Math.Max(0, capacity.HeldSlots - request.TotalPax);

                await tx.RollbackAsync(ct);
                _db.ChangeTracker.Clear();

                throw new InvalidOperationException(
                    outcomes.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))?.ErrorMessage
                    ?? "Package reservation could not hold any source services.");
            }

            reservation.Status = finalStatus;
            reservation.HoldExpiresAt = TourPackageReservationSupport.ResolveReservationExpiry(currentTime, outcomes, holdMinutes);
            reservation.SnapshotJson = BuildReservationSnapshotJson(tour, schedule, package, packageQuote, reservation.Items);
            reservation.FailureReason = BuildFailureReason(outcomes);

            _auditService?.Track(new TourPackageAuditWriteRequest
            {
                TenantId = reservation.TenantId,
                TourId = reservation.TourId,
                TourScheduleId = reservation.TourScheduleId,
                TourPackageId = reservation.TourPackageId,
                TourPackageReservationId = reservation.Id,
                ActorUserId = userId,
                EventType = finalStatus == TourPackageReservationStatus.PartiallyHeld
                    ? "reservation.partially-held"
                    : "reservation.held",
                Title = finalStatus == TourPackageReservationStatus.PartiallyHeld
                    ? "Package reservation partially held"
                    : "Package reservation held",
                Status = reservation.Status.ToString(),
                Description = finalStatus == TourPackageReservationStatus.PartiallyHeld
                    ? $"Reservation {reservation.Code} held only part of the requested package services."
                    : $"Reservation {reservation.Code} held package services successfully.",
                CurrencyCode = reservation.CurrencyCode,
                Amount = reservation.PackageSubtotalAmount,
                Severity = finalStatus == TourPackageReservationStatus.PartiallyHeld
                    ? TourPackageAuditSeverity.Warning
                    : TourPackageAuditSeverity.Info,
                SnapshotJson = reservation.SnapshotJson,
                OccurredAt = currentTime
            });

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new TourPackageReservationHoldServiceResult
            {
                Reused = false,
                Reservation = MapReservation(
                    reservation,
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

    public async Task<TourPackageReservationView> GetAsync(
        Guid tourId,
        Guid reservationId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        if (reservationId == Guid.Empty)
            throw new ArgumentException("ReservationId is required.", nameof(reservationId));

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var reservation = await LoadReservationAsync(tourId, reservationId, userId, isAdmin, ct);
            if (reservation is null)
                throw new KeyNotFoundException("Package reservation not found.");

            if (IsReservationActive(reservation) &&
                reservation.HoldExpiresAt.HasValue &&
                reservation.HoldExpiresAt.Value <= DateTimeOffset.UtcNow)
            {
                await ReleaseReservationAsync(
                    reservation,
                    TourPackageReservationStatus.Expired,
                    TourPackageReservationItemStatus.Expired,
                    userId,
                    isAdmin,
                    ct);

                reservation = await LoadReservationAsync(tourId, reservationId, userId, isAdmin, ct);
                if (reservation is null)
                    throw new KeyNotFoundException("Package reservation not found.");
            }

            return MapReservation(reservation);
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    public async Task<TourPackageReservationView> ReleaseAsync(
        Guid tourId,
        Guid reservationId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        if (reservationId == Guid.Empty)
            throw new ArgumentException("ReservationId is required.", nameof(reservationId));

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var reservation = await LoadReservationAsync(tourId, reservationId, userId, isAdmin, ct);
            if (reservation is null)
                throw new KeyNotFoundException("Package reservation not found.");

            if (!IsReservationActive(reservation))
                return MapReservation(reservation);

            await ReleaseReservationAsync(
                reservation,
                TourPackageReservationStatus.Released,
                TourPackageReservationItemStatus.Released,
                userId,
                isAdmin,
                ct);

            var updated = await LoadReservationAsync(tourId, reservationId, userId, isAdmin, ct);
            if (updated is null)
                throw new KeyNotFoundException("Package reservation not found.");

            return MapReservation(updated);
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    private async Task<TourPackageSourceReservationHoldResult> HoldReservationItemAsync(
        TourPackageReservation reservation,
        TourPackageReservationItem item,
        TourQuoteBuildPackageLineInput line,
        Tour tour,
        TourSchedule schedule,
        TourPackage package,
        TourPackageComponent component,
        TourPackageComponentOption option,
        int totalNights,
        Guid? userId,
        CancellationToken ct)
    {
        if (option.BindingMode == TourPackageBindingMode.ManualFulfillment)
        {
            return CreateValidatedOutcome(
                line,
                "This package option is fulfilled manually and does not create an external hold.");
        }

        var adapter = _reservationAdapters.FirstOrDefault(x => x.CanHandle(option.SourceType));
        if (adapter is null)
        {
            return option.SourceType is TourPackageSourceType.Activity
                or TourPackageSourceType.InternalService
                or TourPackageSourceType.Other
                ? CreateValidatedOutcome(line, "This package option does not require an external hold adapter.")
                : new TourPackageSourceReservationHoldResult
                {
                    Status = TourPackageReservationHoldOutcomeStatus.Failed,
                    ErrorMessage = $"No reservation adapter is configured for source type '{option.SourceType}'."
                };
        }

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var sourceTenantId = await ResolveSourceTenantIdAsync(option.SourceType, line.BoundSourceEntityId, ct);
            if (sourceTenantId.HasValue)
                _tenantContext.SetTenant(sourceTenantId.Value);

            return await adapter.HoldAsync(new TourPackageSourceReservationHoldRequest
            {
                ReservationId = reservation.Id,
                ReservationToken = reservation.HoldToken,
                UserId = userId,
                Tour = tour,
                Schedule = schedule,
                Package = package,
                Component = component,
                Option = option,
                Line = line,
                TotalNights = totalNights
            }, ct);
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    private async Task<TourPackage?> ResolvePackageAsync(
        Guid tourId,
        Guid? requestedPackageId,
        bool selectedOptionsProvided,
        CancellationToken ct)
    {
        IQueryable<TourPackage> query = _db.TourPackages
            .Where(x =>
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == TourPackageStatus.Active);

        query = requestedPackageId.HasValue && requestedPackageId.Value != Guid.Empty
            ? query.Where(x => x.Id == requestedPackageId.Value)
            : query.Where(x => x.IsDefault);

        var package = await query
            .Include(x => x.Components)
            .ThenInclude(x => x.Options)
            .ThenInclude(x => x.ScheduleOverrides)
            .FirstOrDefaultAsync(ct);

        if (package is null && selectedOptionsProvided && (!requestedPackageId.HasValue || requestedPackageId.Value == Guid.Empty))
            throw new InvalidOperationException("SelectedPackageOptions require an active default package.");

        return package;
    }

    private TourPackageReservation CreateReservationEntity(
        Tour tour,
        TourSchedule schedule,
        TourPackage package,
        TourPackageQuoteBuildResult packageQuote,
        TourPackageReservationCreateRequest request,
        string holdToken,
        Guid? userId,
        int heldCapacitySlots,
        int holdMinutes,
        DateTimeOffset now)
    {
        var optionSourceTypes = package.Components
            .SelectMany(x => x.Options)
            .ToDictionary(x => x.Id, x => x.SourceType);

        var reservation = new TourPackageReservation
        {
            Id = Guid.NewGuid(),
            TenantId = tour.TenantId,
            TourId = tour.Id,
            TourScheduleId = schedule.Id,
            TourPackageId = package.Id,
            UserId = userId,
            Code = $"TPR-{Guid.NewGuid():N}".Substring(0, 16).ToUpperInvariant(),
            HoldToken = holdToken,
            Status = TourPackageReservationStatus.Pending,
            HoldStrategy = package.HoldStrategy,
            CurrencyCode = packageQuote.CurrencyCode,
            RequestedPax = request.TotalPax,
            HeldCapacitySlots = heldCapacitySlots,
            PackageSubtotalAmount = packageQuote.Lines.Sum(x => x.UnitPrice * x.Quantity),
            HoldExpiresAt = now.AddMinutes(holdMinutes),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        foreach (var line in packageQuote.Lines)
        {
            reservation.Items.Add(new TourPackageReservationItem
            {
                Id = Guid.NewGuid(),
                TenantId = tour.TenantId,
                TourPackageReservationId = reservation.Id,
                TourPackageComponentId = line.ComponentId,
                TourPackageComponentOptionId = line.OptionId,
                ComponentType = line.ComponentType,
                SourceType = optionSourceTypes[line.OptionId],
                SourceEntityId = line.BoundSourceEntityId,
                Status = TourPackageReservationItemStatus.Pending,
                Quantity = line.Quantity,
                CurrencyCode = line.CurrencyCode,
                UnitPrice = line.UnitPrice,
                LineAmount = line.UnitPrice * line.Quantity,
                Notes = line.Note,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId,
                UpdatedAt = now,
                UpdatedByUserId = userId,
                SnapshotJson = JsonSerializer.Serialize(new
                {
                    line.ComponentId,
                    line.ComponentCode,
                    line.ComponentName,
                    line.ComponentType,
                    line.OptionId,
                    line.Code,
                    line.Name,
                    line.BoundSourceEntityId,
                    line.Quantity,
                    line.UnitPrice,
                    line.UnitOriginalPrice,
                    line.PricingMode,
                    line.IsRequired,
                    line.IsDefaultSelected,
                    line.Note
                })
            });
        }

        reservation.SnapshotJson = BuildReservationSnapshotJson(tour, schedule, package, packageQuote, reservation.Items);
        return reservation;
    }

    private static void ApplyOutcomeToItem(
        TourPackageReservationItem item,
        TourQuoteBuildPackageLineInput line,
        TourPackageSourceReservationHoldResult outcome)
    {
        item.Status = outcome.Status switch
        {
            TourPackageReservationHoldOutcomeStatus.Held => TourPackageReservationItemStatus.Held,
            TourPackageReservationHoldOutcomeStatus.Validated => TourPackageReservationItemStatus.Validated,
            _ => TourPackageReservationItemStatus.Failed
        };
        item.SourceEntityId = line.BoundSourceEntityId ?? item.SourceEntityId;
        item.SourceHoldToken = outcome.SourceHoldToken;
        item.HoldExpiresAt = outcome.HoldExpiresAt;
        item.ErrorMessage = string.IsNullOrWhiteSpace(outcome.ErrorMessage) ? null : outcome.ErrorMessage.Trim();
        item.Notes = MergeText(item.Notes, outcome.Note);
        item.SnapshotJson = !string.IsNullOrWhiteSpace(outcome.SnapshotJson)
            ? outcome.SnapshotJson
            : item.SnapshotJson;
    }

    private async Task<TourPackageReservation?> TryGetActiveReservationByTokenAsync(
        Guid tenantId,
        Guid tourId,
        Guid scheduleId,
        string holdToken,
        Guid? userId,
        bool isAdmin,
        DateTimeOffset now,
        CancellationToken ct)
    {
        IQueryable<TourPackageReservation> query = _db.TourPackageReservations
            .Include(x => x.TourPackage)
            .Include(x => x.TourSchedule)
            .Include(x => x.Items)
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourId == tourId &&
                x.TourScheduleId == scheduleId &&
                x.HoldToken == holdToken &&
                !x.IsDeleted &&
                (x.Status == TourPackageReservationStatus.Held || x.Status == TourPackageReservationStatus.PartiallyHeld) &&
                x.HoldExpiresAt.HasValue &&
                x.HoldExpiresAt.Value > now);

        if (!isAdmin && userId.HasValue)
            query = query.Where(x => x.UserId == userId);

        return await query.FirstOrDefaultAsync(ct);
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

        _tenantContext.SetTenant(reservation.TenantId);

        if (!isAdmin && userId.HasValue && reservation.UserId.HasValue && reservation.UserId.Value != userId.Value)
            return null;

        if (!isAdmin && reservation.UserId.HasValue == false)
            return null;

        return reservation;
    }

    private async Task ReleaseReservationAsync(
        TourPackageReservation reservation,
        TourPackageReservationStatus targetStatus,
        TourPackageReservationItemStatus itemStatus,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

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
                item.Status = itemStatus;
        }

        var capacity = await _db.TourScheduleCapacities
            .FirstOrDefaultAsync(x =>
                x.TourScheduleId == reservation.TourScheduleId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (capacity is not null && reservation.HeldCapacitySlots > 0)
            capacity.HeldSlots = Math.Max(0, capacity.HeldSlots - reservation.HeldCapacitySlots);

        reservation.Status = targetStatus;
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
            EventType = targetStatus == TourPackageReservationStatus.Expired
                ? "reservation.expired"
                : "reservation.released",
            Title = targetStatus == TourPackageReservationStatus.Expired
                ? "Package reservation expired"
                : "Package reservation released",
            Status = targetStatus.ToString(),
            Description = targetStatus == TourPackageReservationStatus.Expired
                ? $"Reservation {reservation.Code} expired before confirmation."
                : $"Reservation {reservation.Code} was released.",
            CurrencyCode = reservation.CurrencyCode,
            Amount = reservation.PackageSubtotalAmount,
            Severity = targetStatus == TourPackageReservationStatus.Expired
                ? TourPackageAuditSeverity.Warning
                : TourPackageAuditSeverity.Info,
            SnapshotJson = reservation.SnapshotJson,
            OccurredAt = reservation.UpdatedAt
        });

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task ExpireReservationsAsync(
        Guid tenantId,
        Guid tourId,
        Guid scheduleId,
        DateTimeOffset now,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        var expiredReservations = await _db.TourPackageReservations
            .Include(x => x.Items)
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourId == tourId &&
                x.TourScheduleId == scheduleId &&
                !x.IsDeleted &&
                (x.Status == TourPackageReservationStatus.Held || x.Status == TourPackageReservationStatus.PartiallyHeld) &&
                x.HoldExpiresAt.HasValue &&
                x.HoldExpiresAt.Value <= now)
            .ToListAsync(ct);

        foreach (var reservation in expiredReservations)
            await ReleaseReservationAsync(reservation, TourPackageReservationStatus.Expired, TourPackageReservationItemStatus.Expired, userId, isAdmin, ct);
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

    private async Task<int> ResolveHoldMinutesAsync(Guid tenantId, CancellationToken ct)
    {
        var holdMinutes = await _db.Tenants.IgnoreQueryFilters()
            .Where(x => x.Id == tenantId && !x.IsDeleted)
            .Select(x => x.HoldMinutes)
            .FirstOrDefaultAsync(ct);

        return holdMinutes > 0 ? holdMinutes : 5;
    }

    private static string? BuildFailureReason(IReadOnlyCollection<TourPackageSourceReservationHoldResult> outcomes)
    {
        var messages = outcomes
            .Where(x => !string.IsNullOrWhiteSpace(x.ErrorMessage))
            .Select(x => x.ErrorMessage!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return messages.Count == 0 ? null : string.Join(" | ", messages);
    }

    private static TourPackageSourceReservationHoldResult CreateValidatedOutcome(
        TourQuoteBuildPackageLineInput line,
        string note)
    {
        return new TourPackageSourceReservationHoldResult
        {
            Status = TourPackageReservationHoldOutcomeStatus.Validated,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                line.ComponentId,
                line.ComponentCode,
                line.ComponentName,
                line.ComponentType,
                line.OptionId,
                line.Code,
                line.Name,
                line.BoundSourceEntityId,
                line.Quantity,
                line.UnitPrice
            }),
            Note = note
        };
    }

    private static string BuildReservationSnapshotJson(
        Tour tour,
        TourSchedule schedule,
        TourPackage package,
        TourPackageQuoteBuildResult packageQuote,
        IEnumerable<TourPackageReservationItem> items)
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
            notes = packageQuote.Notes,
            items = items.Select(x => new
            {
                x.Id,
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
                x.SourceHoldToken,
                x.HoldExpiresAt
            }).ToList()
        });
    }

    private static TourPackageReservationView MapReservation(
        TourPackageReservation reservation,
        string? scheduleCode = null,
        string? scheduleName = null,
        string? packageCode = null,
        string? packageName = null,
        TourPackageMode? packageMode = null)
    {
        return new TourPackageReservationView
        {
            Id = reservation.Id,
            TourId = reservation.TourId,
            ScheduleId = reservation.TourScheduleId,
            ScheduleCode = scheduleCode ?? reservation.TourSchedule?.Code ?? string.Empty,
            ScheduleName = scheduleName ?? reservation.TourSchedule?.Name,
            PackageId = reservation.TourPackageId,
            PackageCode = packageCode ?? reservation.TourPackage?.Code ?? string.Empty,
            PackageName = packageName ?? reservation.TourPackage?.Name ?? string.Empty,
            PackageMode = packageMode ?? reservation.TourPackage?.Mode ?? TourPackageMode.Fixed,
            Code = reservation.Code,
            HoldToken = reservation.HoldToken,
            Status = reservation.Status,
            HoldStrategy = reservation.HoldStrategy,
            CurrencyCode = reservation.CurrencyCode,
            RequestedPax = reservation.RequestedPax,
            HeldCapacitySlots = reservation.HeldCapacitySlots,
            PackageSubtotalAmount = reservation.PackageSubtotalAmount,
            HoldExpiresAt = reservation.HoldExpiresAt,
            Notes = reservation.Notes,
            FailureReason = reservation.FailureReason,
            SnapshotJson = reservation.SnapshotJson,
            IsDeleted = reservation.IsDeleted,
            CreatedAt = reservation.CreatedAt,
            UpdatedAt = reservation.UpdatedAt,
            Items = reservation.Items
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Select(x => new TourPackageReservationItemView
                {
                    Id = x.Id,
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
                    HoldExpiresAt = x.HoldExpiresAt,
                    Notes = x.Notes,
                    ErrorMessage = x.ErrorMessage,
                    SnapshotJson = x.SnapshotJson
                })
                .ToList()
        };
    }

    private static bool IsReservationActive(TourPackageReservation reservation)
        => reservation.Status == TourPackageReservationStatus.Held
           || reservation.Status == TourPackageReservationStatus.PartiallyHeld;

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

public sealed class TourPackageReservationCreateRequest
{
    public Guid ScheduleId { get; set; }
    public Guid? PackageId { get; set; }
    public int TotalPax { get; set; }
    public bool IncludeDefaultPackageOptions { get; set; } = true;
    public List<TourPackageReservationSelectedOptionRequest> SelectedPackageOptions { get; set; } = new();
    public string? ClientToken { get; set; }
    public string? Notes { get; set; }
}

public sealed class TourPackageReservationSelectedOptionRequest
{
    public Guid OptionId { get; set; }
    public int? Quantity { get; set; }
}

public sealed class TourPackageReservationHoldServiceResult
{
    public bool Reused { get; set; }
    public TourPackageReservationView Reservation { get; set; } = new();
}

public sealed class TourPackageReservationView
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
    public string Code { get; set; } = "";
    public string HoldToken { get; set; } = "";
    public TourPackageReservationStatus Status { get; set; }
    public TourPackageHoldStrategy HoldStrategy { get; set; }
    public string CurrencyCode { get; set; } = "";
    public int RequestedPax { get; set; }
    public int HeldCapacitySlots { get; set; }
    public decimal PackageSubtotalAmount { get; set; }
    public DateTimeOffset? HoldExpiresAt { get; set; }
    public string? Notes { get; set; }
    public string? FailureReason { get; set; }
    public string? SnapshotJson { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<TourPackageReservationItemView> Items { get; set; } = new();
}

public sealed class TourPackageReservationItemView
{
    public Guid Id { get; set; }
    public Guid ComponentId { get; set; }
    public Guid OptionId { get; set; }
    public TourPackageComponentType ComponentType { get; set; }
    public TourPackageSourceType SourceType { get; set; }
    public Guid? SourceEntityId { get; set; }
    public TourPackageReservationItemStatus Status { get; set; }
    public int Quantity { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public string? SourceHoldToken { get; set; }
    public DateTimeOffset? HoldExpiresAt { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SnapshotJson { get; set; }
}
