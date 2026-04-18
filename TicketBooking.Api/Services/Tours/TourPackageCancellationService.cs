using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Services.Tours;

public sealed class TourPackageCancellationService
{
    private const string DefaultRefundProvider = "SePay";

    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly TourLocalTimeService _tourLocalTimeService;
    private readonly IReadOnlyCollection<ITourPackageSourceCancellationAdapter> _cancellationAdapters;
    private readonly TourPackageAuditService? _auditService;

    public TourPackageCancellationService(
        AppDbContext db,
        ITenantContext tenantContext,
        TourLocalTimeService tourLocalTimeService,
        IEnumerable<ITourPackageSourceCancellationAdapter> cancellationAdapters,
        TourPackageAuditService? auditService = null)
    {
        _db = db;
        _tenantContext = tenantContext;
        _tourLocalTimeService = tourLocalTimeService;
        _cancellationAdapters = cancellationAdapters?.ToList()
            ?? throw new ArgumentNullException(nameof(cancellationAdapters));
        _auditService = auditService;
    }

    public async Task<TourPackageCancellationExecutionResult> CancelItemsAsync(
        Guid tourId,
        Guid bookingId,
        TourPackageCancellationCreateRequest request,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestedItemIds = NormalizeBookingItemIds(request.BookingItemIds);
        if (requestedItemIds.Count == 0)
            throw new ArgumentException("At least one BookingItemId is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.ReasonCode) && string.IsNullOrWhiteSpace(request.ReasonText))
            throw new ArgumentException("ReasonCode or ReasonText is required.", nameof(request));

        return await ExecuteCancellationAsync(
            tourId,
            bookingId,
            requestedItemIds,
            request.ReasonCode,
            request.ReasonText,
            request.OverrideNote,
            userId,
            isAdmin,
            allowRequiredForNonAdmin: false,
            ct);
    }

    public async Task<TourPackageCancellationExecutionResult> CancelRemainingAsync(
        Guid tourId,
        Guid bookingId,
        TourPackageBulkCancellationRequest request,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!isAdmin)
            throw new UnauthorizedAccessException("Only admin or manager can bulk-cancel a package booking.");

        if (string.IsNullOrWhiteSpace(request.ReasonCode) && string.IsNullOrWhiteSpace(request.ReasonText))
            throw new ArgumentException("ReasonCode or ReasonText is required.", nameof(request));

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var booking = await LoadBookingAsync(tourId, bookingId, userId, isAdmin, ct);
            if (booking is null)
                throw new KeyNotFoundException("Package booking not found.");

            _tenantContext.SetTenant(booking.TenantId);

            var requestedItemIds = booking.Items
                .Where(x => !x.IsDeleted && TourPackageCancellationSupport.IsBookingItemCancellable(x.Status))
                .Select(x => x.Id)
                .ToList();

            if (requestedItemIds.Count == 0)
                throw new InvalidOperationException("There are no active package booking items left to cancel.");

            return await ExecuteCancellationAsync(
                tourId,
                bookingId,
                requestedItemIds,
                request.ReasonCode,
                request.ReasonText,
                request.OverrideNote,
                userId,
                isAdmin,
                allowRequiredForNonAdmin: false,
                ct);
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    public async Task<TourPackageCancellationExecutionResult> CancelRemainingForRescheduleAsync(
        Guid tourId,
        Guid bookingId,
        TourPackageBulkCancellationRequest request,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ReasonCode) && string.IsNullOrWhiteSpace(request.ReasonText))
            throw new ArgumentException("ReasonCode or ReasonText is required.", nameof(request));

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var booking = await LoadBookingAsync(tourId, bookingId, userId, isAdmin, ct);
            if (booking is null)
                throw new KeyNotFoundException("Package booking not found.");

            _tenantContext.SetTenant(booking.TenantId);

            var requestedItemIds = booking.Items
                .Where(x => !x.IsDeleted && TourPackageCancellationSupport.IsBookingItemCancellable(x.Status))
                .Select(x => x.Id)
                .ToList();

            if (requestedItemIds.Count == 0)
                throw new InvalidOperationException("There are no active package booking items left to cancel.");

            return await ExecuteCancellationAsync(
                tourId,
                bookingId,
                requestedItemIds,
                request.ReasonCode,
                request.ReasonText,
                request.OverrideNote,
                userId,
                isAdmin,
                allowRequiredForNonAdmin: true,
                ct);
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    public async Task<TourPackageCancellationView> GetCancellationAsync(
        Guid tourId,
        Guid bookingId,
        Guid cancellationId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId is required.", nameof(bookingId));

        if (cancellationId == Guid.Empty)
            throw new ArgumentException("CancellationId is required.", nameof(cancellationId));

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var booking = await LoadBookingAsync(tourId, bookingId, userId, isAdmin, ct);
            if (booking is null)
                throw new KeyNotFoundException("Package booking not found.");

            _tenantContext.SetTenant(booking.TenantId);

            var cancellation = await LoadCancellationAsync(booking.Id, cancellationId, ct);
            if (cancellation is null)
                throw new KeyNotFoundException("Package cancellation not found.");

            return MapCancellation(cancellation);
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    public async Task<TourPackageRefundListView> ListRefundsAsync(
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

            _tenantContext.SetTenant(booking.TenantId);

            var refunds = await _db.TourPackageRefunds
                .IgnoreQueryFilters()
                .Include(x => x.Attempts)
                .Where(x =>
                    x.TourPackageBookingId == booking.Id &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .ToListAsync(ct);

            return new TourPackageRefundListView
            {
                Total = refunds.Count,
                Items = refunds.Select(MapRefund).ToList()
            };
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    public async Task<TourPackageRefundView> MarkRefundReadyAsync(
        Guid tourId,
        Guid bookingId,
        Guid refundId,
        TourPackageRefundStateChangeRequest request,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!isAdmin)
            throw new UnauthorizedAccessException("Only admin or manager can change refund state.");

        return await UpdateRefundStateAsync(
            tourId,
            bookingId,
            refundId,
            TourPackageRefundStatus.ReadyForProvider,
            request.Note,
            userId,
            isAdmin,
            ct);
    }

    public async Task<TourPackageRefundView> RejectRefundAsync(
        Guid tourId,
        Guid bookingId,
        Guid refundId,
        TourPackageRefundStateChangeRequest request,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!isAdmin)
            throw new UnauthorizedAccessException("Only admin or manager can change refund state.");

        return await UpdateRefundStateAsync(
            tourId,
            bookingId,
            refundId,
            TourPackageRefundStatus.Rejected,
            request.Note,
            userId,
            isAdmin,
            ct);
    }

    private async Task<TourPackageCancellationExecutionResult> ExecuteCancellationAsync(
        Guid tourId,
        Guid bookingId,
        IReadOnlyCollection<Guid> requestedItemIds,
        string? reasonCode,
        string? reasonText,
        string? overrideNote,
        Guid? userId,
        bool isAdmin,
        bool allowRequiredForNonAdmin,
        CancellationToken ct)
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

            _tenantContext.SetTenant(booking.TenantId);

            var existingCancellation = await FindExistingCancellationAsync(booking.Id, requestedItemIds, ct);
            if (existingCancellation is not null)
            {
                return new TourPackageCancellationExecutionResult
                {
                    Reused = true,
                    Cancellation = MapCancellation(existingCancellation)
                };
            }

            var targetItems = booking.Items
                .Where(x => !x.IsDeleted && requestedItemIds.Contains(x.Id))
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToList();

            if (targetItems.Count != requestedItemIds.Count)
                throw new InvalidOperationException("One or more package booking items do not belong to this booking.");

            if (targetItems.Any(x => !TourPackageCancellationSupport.IsBookingItemCancellable(x.Status)))
                throw new InvalidOperationException("One or more package booking items are not eligible for cancellation.");

            var package = await _db.TourPackages
                .IgnoreQueryFilters()
                .Include(x => x.Components)
                .ThenInclude(x => x.Options)
                .FirstOrDefaultAsync(x =>
                    x.Id == booking.TourPackageId &&
                    !x.IsDeleted, ct);

            if (package is null)
                throw new KeyNotFoundException("Tour package not found.");

            var componentById = package.Components.ToDictionary(x => x.Id);
            var optionById = package.Components
                .SelectMany(x => x.Options)
                .ToDictionary(x => x.Id);

            if (!isAdmin && !allowRequiredForNonAdmin)
            {
                var requiredItem = targetItems.FirstOrDefault(x =>
                {
                    if (!componentById.TryGetValue(x.TourPackageComponentId, out var component))
                        return true;

                    return !TourPackageCancellationSupport.IsOptionalSelectionMode(component.SelectionMode);
                });

                if (requiredItem is not null)
                    throw new InvalidOperationException("Customers may cancel optional package items only.");
            }

            var tour = await _db.Tours
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == booking.TourId &&
                    !x.IsDeleted, ct);

            if (tour is null)
                throw new KeyNotFoundException("Tour not found.");

            var schedule = await _db.TourSchedules
                .IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == booking.TourScheduleId &&
                    !x.IsDeleted, ct);

            if (schedule is null)
                throw new KeyNotFoundException("Tour schedule not found.");

            var now = await _tourLocalTimeService.ResolveCurrentTimeAsync(tour.PrimaryLocationId, ct);
            var policy = await _db.TourPolicies
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x =>
                    x.TourId == booking.TourId &&
                    x.Type == TourPolicyType.Cancellation &&
                    x.IsActive &&
                    !x.IsDeleted)
                .OrderByDescending(x => x.IsHighlighted)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            var cancellation = new TourPackageCancellation
            {
                Id = Guid.NewGuid(),
                TenantId = booking.TenantId,
                TourId = booking.TourId,
                TourScheduleId = booking.TourScheduleId,
                TourPackageId = booking.TourPackageId,
                TourPackageBookingId = booking.Id,
                RequestedByUserId = userId,
                Status = TourPackageCancellationStatus.Requested,
                IsAdminOverride = isAdmin && !string.IsNullOrWhiteSpace(overrideNote),
                CurrencyCode = booking.CurrencyCode,
                PolicyCode = policy?.Code,
                PolicyName = policy?.Name,
                ReasonCode = NormalizeText(reasonCode),
                ReasonText = NormalizeText(reasonText),
                OverrideNote = NormalizeText(overrideNote),
                BookingSnapshotJson = booking.SnapshotJson,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId,
                UpdatedAt = now,
                UpdatedByUserId = userId
            };

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            _db.TourPackageCancellations.Add(cancellation);
            await _db.SaveChangesAsync(ct);

            var acceptedCount = 0;
            var rejectionMessages = new List<string>();
            var completedItems = new List<TourPackageCancellationItem>();
            var createdRefunds = new List<TourPackageRefund>();

            foreach (var bookingItem in targetItems)
            {
                var matchedRule = TourPackageCancellationSupport.ResolveRule(policy?.PolicyJson, schedule.DepartureDate, now);
                var amounts = TourPackageCancellationSupport.CalculateAmounts(bookingItem.LineAmount, matchedRule);

                bookingItem.Status = TourPackageBookingItemStatus.CancellationPending;
                bookingItem.UpdatedAt = now;
                bookingItem.UpdatedByUserId = userId;

                var item = new TourPackageCancellationItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = booking.TenantId,
                    TourPackageCancellationId = cancellation.Id,
                    TourPackageBookingItemId = bookingItem.Id,
                    Status = TourPackageCancellationStatus.Requested,
                    CurrencyCode = bookingItem.CurrencyCode,
                    GrossLineAmount = amounts.GrossLineAmount,
                    PenaltyAmount = amounts.PenaltyAmount,
                    RefundAmount = amounts.RefundAmount,
                    PolicyRuleJson = matchedRule.RawJson,
                    BookingItemSnapshotJson = bookingItem.SnapshotJson,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedByUserId = userId,
                    UpdatedAt = now,
                    UpdatedByUserId = userId
                };

                _db.TourPackageCancellationItems.Add(item);
                await _db.SaveChangesAsync(ct);

                var option = optionById[bookingItem.TourPackageComponentOptionId];
                var sourceOutcome = await CancelSourceAsync(
                    tour,
                    schedule,
                    booking,
                    bookingItem,
                    option,
                    now,
                    userId,
                    isAdmin,
                    ct);

                var supplierRejected = sourceOutcome.Status == TourPackageSourceCancellationOutcomeStatus.Rejected;
                var allowOverride = isAdmin && !string.IsNullOrWhiteSpace(cancellation.OverrideNote);
                if (supplierRejected && !allowOverride)
                {
                    item.Status = TourPackageCancellationStatus.Rejected;
                    item.SupplierSnapshotJson = sourceOutcome.SnapshotJson;
                    item.SupplierNote = NormalizeText(sourceOutcome.Note);
                    item.FailureReason = NormalizeText(sourceOutcome.ErrorMessage);
                    item.UpdatedAt = now;
                    item.UpdatedByUserId = userId;

                    bookingItem.Status = TourPackageBookingItemStatus.Confirmed;
                    bookingItem.ErrorMessage = NormalizeText(sourceOutcome.ErrorMessage);
                    bookingItem.Notes = MergeText(bookingItem.Notes, sourceOutcome.Note);
                    bookingItem.UpdatedAt = now;
                    bookingItem.UpdatedByUserId = userId;

                    if (!string.IsNullOrWhiteSpace(sourceOutcome.ErrorMessage))
                        rejectionMessages.Add(sourceOutcome.ErrorMessage.Trim());

                    continue;
                }

                item.Status = TourPackageCancellationStatus.Completed;
                item.SupplierSnapshotJson = sourceOutcome.SnapshotJson;
                item.SupplierNote = allowOverride && supplierRejected
                    ? MergeText(sourceOutcome.Note, $"Admin override: {cancellation.OverrideNote}")
                    : NormalizeText(sourceOutcome.Note);
                item.FailureReason = allowOverride && supplierRejected
                    ? NormalizeText(sourceOutcome.ErrorMessage)
                    : null;
                item.Notes = allowOverride && supplierRejected
                    ? "Supplier rejection was overridden by admin."
                    : null;
                item.UpdatedAt = now;
                item.UpdatedByUserId = userId;

                bookingItem.ErrorMessage = null;
                bookingItem.Notes = MergeText(bookingItem.Notes, sourceOutcome.Note);
                bookingItem.Status = amounts.RefundAmount > 0m
                    ? TourPackageBookingItemStatus.RefundPending
                    : TourPackageBookingItemStatus.Cancelled;
                bookingItem.UpdatedAt = now;
                bookingItem.UpdatedByUserId = userId;

                var refund = CreateRefundEntity(cancellation, item, booking, bookingItem, matchedRule, amounts, now, userId);
                _db.TourPackageRefunds.Add(refund);
                completedItems.Add(item);
                createdRefunds.Add(refund);
                acceptedCount++;
            }

            cancellation.Status = acceptedCount > 0
                ? TourPackageCancellationStatus.Completed
                : TourPackageCancellationStatus.Rejected;
            cancellation.PenaltyAmount = completedItems.Sum(x => x.PenaltyAmount);
            cancellation.RefundAmount = completedItems.Sum(x => x.RefundAmount);
            cancellation.FailureReason = rejectionMessages.Count == 0 ? null : string.Join(" | ", rejectionMessages.Distinct(StringComparer.OrdinalIgnoreCase));
            cancellation.DecisionSnapshotJson = BuildDecisionSnapshotJson(cancellation, targetItems, booking.Items);
            cancellation.CompletedAt = acceptedCount > 0 ? now : null;
            cancellation.UpdatedAt = now;
            cancellation.UpdatedByUserId = userId;

            booking.Status = TourPackageCancellationSupport.CalculateBookingStatus(booking.Items.Where(x => !x.IsDeleted).ToList());
            booking.FailureReason = MergeText(booking.FailureReason, cancellation.FailureReason);
            booking.UpdatedAt = now;
            booking.UpdatedByUserId = userId;

            if (booking.Status == TourPackageBookingStatus.Cancelled && booking.ConfirmedCapacitySlots > 0)
            {
                var capacity = await _db.TourScheduleCapacities
                    .FirstOrDefaultAsync(x =>
                        x.TourScheduleId == booking.TourScheduleId &&
                        x.IsActive &&
                        !x.IsDeleted, ct);

                if (capacity is not null)
                {
                    capacity.SoldSlots = Math.Max(0, capacity.SoldSlots - booking.ConfirmedCapacitySlots);
                    if (capacity.Status == TourCapacityStatus.Full && capacity.AvailableSlots > 0)
                        capacity.Status = TourCapacityStatus.Open;
                }

                booking.ConfirmedCapacitySlots = 0;
            }

            _auditService?.Track(new TourPackageAuditWriteRequest
            {
                TenantId = cancellation.TenantId,
                TourId = cancellation.TourId,
                TourScheduleId = cancellation.TourScheduleId,
                TourPackageId = cancellation.TourPackageId,
                TourPackageBookingId = cancellation.TourPackageBookingId,
                TourPackageCancellationId = cancellation.Id,
                ActorUserId = userId,
                EventType = cancellation.Status == TourPackageCancellationStatus.Completed
                    ? "cancellation.completed"
                    : "cancellation.rejected",
                Title = cancellation.Status == TourPackageCancellationStatus.Completed
                    ? "Package cancellation completed"
                    : "Package cancellation rejected",
                Status = cancellation.Status.ToString(),
                Description = cancellation.Status == TourPackageCancellationStatus.Completed
                    ? $"Cancellation {cancellation.Id} completed for {completedItems.Count} package item(s)."
                    : cancellation.FailureReason ?? $"Cancellation {cancellation.Id} was rejected.",
                CurrencyCode = cancellation.CurrencyCode,
                Amount = cancellation.RefundAmount,
                Severity = cancellation.Status == TourPackageCancellationStatus.Completed
                    ? TourPackageAuditSeverity.Info
                    : TourPackageAuditSeverity.Warning,
                SnapshotJson = cancellation.DecisionSnapshotJson,
                OccurredAt = now
            });

            foreach (var refund in createdRefunds)
            {
                var bookingItem = targetItems.FirstOrDefault(x => x.Id == refund.TourPackageBookingItemId);
                _auditService?.Track(new TourPackageAuditWriteRequest
                {
                    TenantId = refund.TenantId,
                    TourId = booking.TourId,
                    TourScheduleId = booking.TourScheduleId,
                    TourPackageId = booking.TourPackageId,
                    TourPackageBookingId = refund.TourPackageBookingId,
                    TourPackageBookingItemId = refund.TourPackageBookingItemId,
                    TourPackageCancellationId = refund.TourPackageCancellationId,
                    TourPackageRefundId = refund.Id,
                    ActorUserId = userId,
                    SourceType = bookingItem?.SourceType,
                    EventType = refund.Status == TourPackageRefundStatus.ReadyForProvider
                        ? "refund.prepared"
                        : "refund.rejected",
                    Title = refund.Status == TourPackageRefundStatus.ReadyForProvider
                        ? "Refund prepared"
                        : "Refund closed without payout",
                    Status = refund.Status.ToString(),
                    Description = refund.Status == TourPackageRefundStatus.ReadyForProvider
                        ? $"Refund {refund.Id} is ready for provider handoff."
                        : refund.LastProviderError ?? "Refund is not payable under the current package policy.",
                    CurrencyCode = refund.CurrencyCode,
                    Amount = refund.RefundAmount,
                    Severity = refund.Status == TourPackageRefundStatus.ReadyForProvider
                        ? TourPackageAuditSeverity.Info
                        : TourPackageAuditSeverity.Warning,
                    SnapshotJson = refund.SnapshotJson,
                    OccurredAt = now
                });
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var created = await LoadCancellationAsync(booking.Id, cancellation.Id, ct)
                ?? throw new KeyNotFoundException("Package cancellation not found after creation.");

            return new TourPackageCancellationExecutionResult
            {
                Reused = false,
                Cancellation = MapCancellation(created)
            };
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    private async Task<TourPackageSourceCancellationResult> CancelSourceAsync(
        Tour tour,
        TourSchedule schedule,
        TourPackageBooking booking,
        TourPackageBookingItem bookingItem,
        TourPackageComponentOption option,
        DateTimeOffset now,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        if (option.BindingMode == TourPackageBindingMode.ManualFulfillment)
        {
            return new TourPackageSourceCancellationResult
            {
                Status = TourPackageSourceCancellationOutcomeStatus.Cancelled,
                Note = "Manual fulfillment package item was cancelled internally."
            };
        }

        var adapter = _cancellationAdapters.FirstOrDefault(x => x.CanHandle(bookingItem.SourceType));
        if (adapter is null)
        {
            return bookingItem.SourceType is TourPackageSourceType.Activity
                or TourPackageSourceType.InternalService
                or TourPackageSourceType.Other
                ? new TourPackageSourceCancellationResult
                {
                    Status = TourPackageSourceCancellationOutcomeStatus.Cancelled,
                    Note = "Package item does not require an external cancellation adapter."
                }
                : new TourPackageSourceCancellationResult
                {
                    Status = TourPackageSourceCancellationOutcomeStatus.Rejected,
                    ErrorMessage = $"No cancellation adapter is configured for source type '{bookingItem.SourceType}'."
                };
        }

        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var sourceTenantId = await ResolveSourceTenantIdAsync(bookingItem.SourceType, bookingItem.SourceEntityId, ct);
            if (sourceTenantId.HasValue)
                _tenantContext.SetTenant(sourceTenantId.Value);

            return await adapter.CancelAsync(new TourPackageSourceCancellationRequest
            {
                UserId = userId,
                IsAdmin = isAdmin,
                CurrentTime = now,
                Tour = tour,
                Schedule = schedule,
                Booking = booking,
                BookingItem = bookingItem
            }, ct);
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    private TourPackageRefund CreateRefundEntity(
        TourPackageCancellation cancellation,
        TourPackageCancellationItem cancellationItem,
        TourPackageBooking booking,
        TourPackageBookingItem bookingItem,
        TourPackageCancellationMatchedRule matchedRule,
        TourPackageCancellationAmountBreakdown amounts,
        DateTimeOffset now,
        Guid? userId)
    {
        var refundStatus = amounts.RefundAmount > 0m
            ? TourPackageRefundStatus.ReadyForProvider
            : TourPackageRefundStatus.Rejected;
        var externalReference = amounts.RefundAmount > 0m
            ? $"tour-refund-{Guid.NewGuid():N}"
            : null;
        var externalPayload = amounts.RefundAmount > 0m
            ? BuildRefundPayloadJson(cancellation, cancellationItem, booking, bookingItem, matchedRule, amounts)
            : null;
        var webhookState = amounts.RefundAmount > 0m ? "ready" : "not-applicable";
        var providerError = amounts.RefundAmount > 0m ? null : "Refund amount is zero under package cancellation policy.";
        var note = amounts.RefundAmount > 0m
            ? "Prepared for provider handoff."
            : "Cancellation completed with no refundable amount.";

        return new TourPackageRefund
        {
            Id = Guid.NewGuid(),
            TenantId = booking.TenantId,
            TourPackageBookingId = booking.Id,
            TourPackageBookingItemId = bookingItem.Id,
            TourPackageCancellationId = cancellation.Id,
            TourPackageCancellationItemId = cancellationItem.Id,
            Status = refundStatus,
            CurrencyCode = bookingItem.CurrencyCode,
            GrossLineAmount = amounts.GrossLineAmount,
            PenaltyAmount = amounts.PenaltyAmount,
            RefundAmount = amounts.RefundAmount,
            Provider = amounts.RefundAmount > 0m ? DefaultRefundProvider : null,
            ExternalReference = externalReference,
            ExternalPayloadJson = externalPayload,
            WebhookState = webhookState,
            LastProviderError = providerError,
            SnapshotJson = JsonSerializer.Serialize(new
            {
                bookingItemId = bookingItem.Id,
                matchedRule = matchedRule.RawJson,
                amounts.GrossLineAmount,
                amounts.PenaltyAmount,
                amounts.RefundAmount
            }),
            Notes = note,
            PreparedAt = now,
            CompletedAt = amounts.RefundAmount > 0m ? null : now,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId,
            Attempts =
            {
                new TourPackageRefundAttempt
                {
                    Id = Guid.NewGuid(),
                    TenantId = booking.TenantId,
                    Status = refundStatus,
                    Provider = amounts.RefundAmount > 0m ? DefaultRefundProvider : null,
                    ExternalReference = externalReference,
                    ExternalPayloadJson = externalPayload,
                    WebhookState = webhookState,
                    LastProviderError = providerError,
                    Notes = note,
                    AttemptedAt = now,
                    CompletedAt = amounts.RefundAmount > 0m ? null : now,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedByUserId = userId,
                    UpdatedAt = now,
                    UpdatedByUserId = userId
                }
            }
        };
    }

    private async Task<TourPackageRefundView> UpdateRefundStateAsync(
        Guid tourId,
        Guid bookingId,
        Guid refundId,
        TourPackageRefundStatus targetStatus,
        string? note,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        var originalTenantId = _tenantContext.TenantId;
        try
        {
            var booking = await LoadBookingAsync(tourId, bookingId, userId, isAdmin, ct);
            if (booking is null)
                throw new KeyNotFoundException("Package booking not found.");

            _tenantContext.SetTenant(booking.TenantId);

            var refund = await _db.TourPackageRefunds
                .Include(x => x.Attempts)
                .FirstOrDefaultAsync(x =>
                    x.Id == refundId &&
                    x.TourPackageBookingId == booking.Id &&
                    !x.IsDeleted, ct);

            if (refund is null)
                throw new KeyNotFoundException("Package refund not found.");

            var bookingItem = await _db.TourPackageBookingItems
                .FirstOrDefaultAsync(x =>
                    x.Id == refund.TourPackageBookingItemId &&
                    !x.IsDeleted, ct);

            if (bookingItem is null)
                throw new KeyNotFoundException("Package booking item not found.");

            var now = DateTimeOffset.UtcNow;
            refund.Status = targetStatus;
            refund.Notes = MergeText(refund.Notes, note);
            refund.WebhookState = targetStatus switch
            {
                TourPackageRefundStatus.ReadyForProvider => "ready",
                TourPackageRefundStatus.Rejected => "rejected",
                _ => refund.WebhookState
            };
            if (targetStatus == TourPackageRefundStatus.ReadyForProvider)
            {
                refund.Provider ??= DefaultRefundProvider;
                refund.LastProviderError = null;
            }

            refund.LastProviderError = targetStatus == TourPackageRefundStatus.Rejected
                ? NormalizeText(note) ?? refund.LastProviderError ?? "Refund was rejected by operator."
                : refund.LastProviderError;
            refund.PreparedAt ??= now;
            refund.CompletedAt = targetStatus == TourPackageRefundStatus.Rejected ? now : refund.CompletedAt;
            refund.UpdatedAt = now;
            refund.UpdatedByUserId = userId;

            bookingItem.Status = targetStatus switch
            {
                TourPackageRefundStatus.ReadyForProvider => TourPackageBookingItemStatus.RefundPending,
                TourPackageRefundStatus.Rejected => TourPackageBookingItemStatus.RefundRejected,
                TourPackageRefundStatus.Completed => TourPackageBookingItemStatus.Refunded,
                _ => bookingItem.Status
            };
            bookingItem.UpdatedAt = now;
            bookingItem.UpdatedByUserId = userId;

            refund.Attempts.Add(new TourPackageRefundAttempt
            {
                Id = Guid.NewGuid(),
                TenantId = refund.TenantId,
                TourPackageRefundId = refund.Id,
                Status = targetStatus,
                Provider = refund.Provider,
                ExternalReference = refund.ExternalReference,
                ExternalPayloadJson = refund.ExternalPayloadJson,
                WebhookState = refund.WebhookState,
                LastProviderError = refund.LastProviderError,
                Notes = NormalizeText(note),
                AttemptedAt = now,
                CompletedAt = targetStatus is TourPackageRefundStatus.Rejected or TourPackageRefundStatus.Completed ? now : null,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId,
                UpdatedAt = now,
                UpdatedByUserId = userId
            });

            _auditService?.Track(new TourPackageAuditWriteRequest
            {
                TenantId = refund.TenantId,
                TourId = booking.TourId,
                TourScheduleId = booking.TourScheduleId,
                TourPackageId = booking.TourPackageId,
                TourPackageBookingId = refund.TourPackageBookingId,
                TourPackageBookingItemId = refund.TourPackageBookingItemId,
                TourPackageCancellationId = refund.TourPackageCancellationId,
                TourPackageRefundId = refund.Id,
                ActorUserId = userId,
                SourceType = bookingItem.SourceType,
                EventType = targetStatus switch
                {
                    TourPackageRefundStatus.ReadyForProvider => "refund.ready",
                    TourPackageRefundStatus.Rejected => "refund.rejected",
                    TourPackageRefundStatus.Completed => "refund.completed",
                    TourPackageRefundStatus.Processing => "refund.processing",
                    _ => "refund.updated"
                },
                Title = targetStatus switch
                {
                    TourPackageRefundStatus.ReadyForProvider => "Refund marked ready",
                    TourPackageRefundStatus.Rejected => "Refund rejected",
                    TourPackageRefundStatus.Completed => "Refund completed",
                    TourPackageRefundStatus.Processing => "Refund processing",
                    _ => "Refund updated"
                },
                Status = targetStatus.ToString(),
                Description = NormalizeText(note) ?? $"Refund {refund.Id} moved to status {targetStatus}.",
                CurrencyCode = refund.CurrencyCode,
                Amount = refund.RefundAmount,
                Severity = targetStatus is TourPackageRefundStatus.Rejected or TourPackageRefundStatus.Failed
                    ? TourPackageAuditSeverity.Warning
                    : TourPackageAuditSeverity.Info,
                SnapshotJson = refund.SnapshotJson,
                OccurredAt = now
            });

            await _db.SaveChangesAsync(ct);
            return MapRefund(refund);
        }
        finally
        {
            _tenantContext.SetTenant(originalTenantId);
        }
    }

    private async Task<TourPackageCancellation?> FindExistingCancellationAsync(
        Guid bookingId,
        IReadOnlyCollection<Guid> requestedItemIds,
        CancellationToken ct)
    {
        var normalizedRequested = requestedItemIds
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var candidates = await _db.TourPackageCancellations
            .IgnoreQueryFilters()
            .Include(x => x.Items)
            .Include(x => x.Refunds)
            .ThenInclude(x => x.Attempts)
            .Where(x =>
                x.TourPackageBookingId == bookingId &&
                !x.IsDeleted &&
                x.Status == TourPackageCancellationStatus.Completed)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);

        foreach (var candidate in candidates)
        {
            var itemIds = candidate.Items
                .Where(x => !x.IsDeleted)
                .Select(x => x.TourPackageBookingItemId)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (itemIds.SequenceEqual(normalizedRequested))
                return candidate;
        }

        return null;
    }

    private async Task<TourPackageBooking?> LoadBookingAsync(
        Guid tourId,
        Guid bookingId,
        Guid? userId,
        bool isAdmin,
        CancellationToken ct)
    {
        var booking = await _db.TourPackageBookings
            .IgnoreQueryFilters()
            .Include(x => x.TourPackage)
            .Include(x => x.TourSchedule)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.Id == bookingId &&
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

    private async Task<TourPackageCancellation?> LoadCancellationAsync(
        Guid bookingId,
        Guid cancellationId,
        CancellationToken ct)
    {
        return await _db.TourPackageCancellations
            .IgnoreQueryFilters()
            .Include(x => x.Items)
            .Include(x => x.Refunds)
            .ThenInclude(x => x.Attempts)
            .FirstOrDefaultAsync(x =>
                x.Id == cancellationId &&
                x.TourPackageBookingId == bookingId &&
                !x.IsDeleted, ct);
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

    private static List<Guid> NormalizeBookingItemIds(IReadOnlyCollection<Guid>? bookingItemIds)
    {
        if (bookingItemIds is null || bookingItemIds.Count == 0)
            return new List<Guid>();

        return bookingItemIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    private static string BuildDecisionSnapshotJson(
        TourPackageCancellation cancellation,
        IReadOnlyCollection<TourPackageBookingItem> requestedItems,
        IEnumerable<TourPackageBookingItem> allItems)
    {
        return JsonSerializer.Serialize(new
        {
            cancellationId = cancellation.Id,
            cancellation.Status,
            cancellation.PolicyCode,
            cancellation.PolicyName,
            cancellation.ReasonCode,
            cancellation.ReasonText,
            cancellation.OverrideNote,
            requestedItems = requestedItems.Select(x => new
            {
                x.Id,
                x.TourPackageComponentId,
                x.TourPackageComponentOptionId,
                x.ComponentType,
                x.SourceType,
                x.SourceEntityId,
                x.Status,
                x.Quantity,
                x.CurrencyCode,
                x.UnitPrice,
                x.LineAmount
            }).ToList(),
            finalBookingStatus = TourPackageCancellationSupport.CalculateBookingStatus(allItems.Where(x => !x.IsDeleted).ToList())
        });
    }

    private static string BuildRefundPayloadJson(
        TourPackageCancellation cancellation,
        TourPackageCancellationItem cancellationItem,
        TourPackageBooking booking,
        TourPackageBookingItem bookingItem,
        TourPackageCancellationMatchedRule matchedRule,
        TourPackageCancellationAmountBreakdown amounts)
    {
        return JsonSerializer.Serialize(new
        {
            provider = DefaultRefundProvider,
            bookingId = booking.Id,
            bookingCode = booking.Code,
            bookingItemId = bookingItem.Id,
            cancellationId = cancellation.Id,
            cancellationItemId = cancellationItem.Id,
            currencyCode = bookingItem.CurrencyCode,
            grossLineAmount = amounts.GrossLineAmount,
            penaltyAmount = amounts.PenaltyAmount,
            refundAmount = amounts.RefundAmount,
            matchedRule = matchedRule.RawJson,
            reasonCode = cancellation.ReasonCode,
            reasonText = cancellation.ReasonText
        });
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

    private static TourPackageCancellationView MapCancellation(TourPackageCancellation cancellation)
    {
        return new TourPackageCancellationView
        {
            Id = cancellation.Id,
            BookingId = cancellation.TourPackageBookingId,
            Status = cancellation.Status,
            IsAdminOverride = cancellation.IsAdminOverride,
            CurrencyCode = cancellation.CurrencyCode,
            PenaltyAmount = cancellation.PenaltyAmount,
            RefundAmount = cancellation.RefundAmount,
            PolicyCode = cancellation.PolicyCode,
            PolicyName = cancellation.PolicyName,
            ReasonCode = cancellation.ReasonCode,
            ReasonText = cancellation.ReasonText,
            OverrideNote = cancellation.OverrideNote,
            FailureReason = cancellation.FailureReason,
            BookingSnapshotJson = cancellation.BookingSnapshotJson,
            DecisionSnapshotJson = cancellation.DecisionSnapshotJson,
            Notes = cancellation.Notes,
            CompletedAt = cancellation.CompletedAt,
            CreatedAt = cancellation.CreatedAt,
            UpdatedAt = cancellation.UpdatedAt,
            Items = cancellation.Items
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Select(x =>
                {
                    var refund = cancellation.Refunds.FirstOrDefault(r => !r.IsDeleted && r.TourPackageCancellationItemId == x.Id);
                    return new TourPackageCancellationItemView
                    {
                        Id = x.Id,
                        BookingItemId = x.TourPackageBookingItemId,
                        Status = x.Status,
                        CurrencyCode = x.CurrencyCode,
                        GrossLineAmount = x.GrossLineAmount,
                        PenaltyAmount = x.PenaltyAmount,
                        RefundAmount = x.RefundAmount,
                        PolicyRuleJson = x.PolicyRuleJson,
                        BookingItemSnapshotJson = x.BookingItemSnapshotJson,
                        SupplierSnapshotJson = x.SupplierSnapshotJson,
                        SupplierNote = x.SupplierNote,
                        FailureReason = x.FailureReason,
                        Notes = x.Notes,
                        Refund = refund is null ? null : MapRefund(refund)
                    };
                })
                .ToList()
        };
    }

    private static TourPackageRefundView MapRefund(TourPackageRefund refund)
    {
        return new TourPackageRefundView
        {
            Id = refund.Id,
            BookingId = refund.TourPackageBookingId,
            BookingItemId = refund.TourPackageBookingItemId,
            CancellationId = refund.TourPackageCancellationId,
            CancellationItemId = refund.TourPackageCancellationItemId,
            Status = refund.Status,
            CurrencyCode = refund.CurrencyCode,
            GrossLineAmount = refund.GrossLineAmount,
            PenaltyAmount = refund.PenaltyAmount,
            RefundAmount = refund.RefundAmount,
            Provider = refund.Provider,
            ExternalReference = refund.ExternalReference,
            ExternalPayloadJson = refund.ExternalPayloadJson,
            WebhookState = refund.WebhookState,
            LastProviderError = refund.LastProviderError,
            SnapshotJson = refund.SnapshotJson,
            Notes = refund.Notes,
            PreparedAt = refund.PreparedAt,
            CompletedAt = refund.CompletedAt,
            CreatedAt = refund.CreatedAt,
            UpdatedAt = refund.UpdatedAt,
            Attempts = refund.Attempts
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .Select(x => new TourPackageRefundAttemptView
                {
                    Id = x.Id,
                    RefundId = x.TourPackageRefundId,
                    Status = x.Status,
                    Provider = x.Provider,
                    ExternalReference = x.ExternalReference,
                    ExternalPayloadJson = x.ExternalPayloadJson,
                    ResponsePayloadJson = x.ResponsePayloadJson,
                    WebhookState = x.WebhookState,
                    LastProviderError = x.LastProviderError,
                    Notes = x.Notes,
                    AttemptedAt = x.AttemptedAt,
                    CompletedAt = x.CompletedAt,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToList()
        };
    }
}

public sealed class TourPackageCancellationCreateRequest
{
    public List<Guid> BookingItemIds { get; set; } = new();
    public string? ReasonCode { get; set; }
    public string? ReasonText { get; set; }
    public string? OverrideNote { get; set; }
}

public sealed class TourPackageBulkCancellationRequest
{
    public string? ReasonCode { get; set; }
    public string? ReasonText { get; set; }
    public string? OverrideNote { get; set; }
}

public sealed class TourPackageRefundStateChangeRequest
{
    public string? Note { get; set; }
}

public sealed class TourPackageCancellationExecutionResult
{
    public bool Reused { get; set; }
    public TourPackageCancellationView Cancellation { get; set; } = new();
}

public sealed class TourPackageCancellationView
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public TourPackageCancellationStatus Status { get; set; }
    public bool IsAdminOverride { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal PenaltyAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string? PolicyCode { get; set; }
    public string? PolicyName { get; set; }
    public string? ReasonCode { get; set; }
    public string? ReasonText { get; set; }
    public string? OverrideNote { get; set; }
    public string? FailureReason { get; set; }
    public string? BookingSnapshotJson { get; set; }
    public string? DecisionSnapshotJson { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<TourPackageCancellationItemView> Items { get; set; } = new();
}

public sealed class TourPackageCancellationItemView
{
    public Guid Id { get; set; }
    public Guid BookingItemId { get; set; }
    public TourPackageCancellationStatus Status { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal GrossLineAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string? PolicyRuleJson { get; set; }
    public string? BookingItemSnapshotJson { get; set; }
    public string? SupplierSnapshotJson { get; set; }
    public string? SupplierNote { get; set; }
    public string? FailureReason { get; set; }
    public string? Notes { get; set; }
    public TourPackageRefundView? Refund { get; set; }
}

public sealed class TourPackageRefundListView
{
    public int Total { get; set; }
    public List<TourPackageRefundView> Items { get; set; } = new();
}

public sealed class TourPackageRefundView
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public Guid BookingItemId { get; set; }
    public Guid CancellationId { get; set; }
    public Guid CancellationItemId { get; set; }
    public TourPackageRefundStatus Status { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal GrossLineAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string? Provider { get; set; }
    public string? ExternalReference { get; set; }
    public string? ExternalPayloadJson { get; set; }
    public string? WebhookState { get; set; }
    public string? LastProviderError { get; set; }
    public string? SnapshotJson { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? PreparedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<TourPackageRefundAttemptView> Attempts { get; set; } = new();
}

public sealed class TourPackageRefundAttemptView
{
    public Guid Id { get; set; }
    public Guid RefundId { get; set; }
    public TourPackageRefundStatus Status { get; set; }
    public string? Provider { get; set; }
    public string? ExternalReference { get; set; }
    public string? ExternalPayloadJson { get; set; }
    public string? ResponsePayloadJson { get; set; }
    public string? WebhookState { get; set; }
    public string? LastProviderError { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? AttemptedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
