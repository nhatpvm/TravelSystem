using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class TourPackageBookingOpsService
{
    private readonly AppDbContext _db;

    public TourPackageBookingOpsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TourPackageBookingOpsPagedResponse> ListAsync(
        Guid tenantId,
        Guid tourId,
        TourPackageBookingOpsListRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 50 : request.PageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        IQueryable<TourPackageBooking> query = _db.TourPackageBookings
            .IgnoreQueryFilters()
            .Include(x => x.TourSchedule)
            .Include(x => x.TourPackage)
            .Include(x => x.TourPackageReservation)
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourId == tourId &&
                (request.IncludeDeleted || !x.IsDeleted));

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        if (request.PackageId.HasValue && request.PackageId.Value != Guid.Empty)
            query = query.Where(x => x.TourPackageId == request.PackageId.Value);

        if (request.ScheduleId.HasValue && request.ScheduleId.Value != Guid.Empty)
            query = query.Where(x => x.TourScheduleId == request.ScheduleId.Value);

        if (request.UserId.HasValue && request.UserId.Value != Guid.Empty)
            query = query.Where(x => x.UserId == request.UserId.Value);

        if (request.CreatedFrom.HasValue)
            query = query.Where(x => x.CreatedAt >= request.CreatedFrom.Value);

        if (request.CreatedTo.HasValue)
            query = query.Where(x => x.CreatedAt <= request.CreatedTo.Value);

        if (request.ConfirmedFrom.HasValue)
            query = query.Where(x => x.ConfirmedAt.HasValue && x.ConfirmedAt.Value >= request.ConfirmedFrom.Value);

        if (request.ConfirmedTo.HasValue)
            query = query.Where(x => x.ConfirmedAt.HasValue && x.ConfirmedAt.Value <= request.ConfirmedTo.Value);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var q = request.Q.Trim();
            query = query.Where(x =>
                x.Code.Contains(q) ||
                (x.Notes != null && x.Notes.Contains(q)) ||
                (x.FailureReason != null && x.FailureReason.Contains(q)) ||
                (x.TourPackage != null && (x.TourPackage.Code.Contains(q) || x.TourPackage.Name.Contains(q))) ||
                (x.TourSchedule != null && (x.TourSchedule.Code.Contains(q) || (x.TourSchedule.Name != null && x.TourSchedule.Name.Contains(q)))) ||
                (x.TourPackageReservation != null && x.TourPackageReservation.Code.Contains(q)));
        }

        var total = await query.CountAsync(ct);
        var bookings = await query
            .AsNoTracking()
            .OrderByDescending(x => x.ConfirmedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var bookingIds = bookings.Select(x => x.Id).ToList();
        var cancellationAgg = await _db.TourPackageCancellations.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && bookingIds.Contains(x.TourPackageBookingId) && !x.IsDeleted)
            .GroupBy(x => x.TourPackageBookingId)
            .Select(g => new
            {
                BookingId = g.Key,
                Total = g.Count(),
                Completed = g.Count(x => x.Status == TourPackageCancellationStatus.Completed),
                Rejected = g.Count(x => x.Status == TourPackageCancellationStatus.Rejected),
                Penalty = g.Sum(x => x.PenaltyAmount),
                Refund = g.Sum(x => x.RefundAmount),
                LastActivityAt = g.Max(x => x.UpdatedAt ?? x.CreatedAt)
            })
            .ToDictionaryAsync(x => x.BookingId, ct);

        var refundAgg = await _db.TourPackageRefunds.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && bookingIds.Contains(x.TourPackageBookingId) && !x.IsDeleted)
            .GroupBy(x => x.TourPackageBookingId)
            .Select(g => new
            {
                BookingId = g.Key,
                Total = g.Count(),
                Ready = g.Count(x => x.Status == TourPackageRefundStatus.ReadyForProvider),
                Processing = g.Count(x => x.Status == TourPackageRefundStatus.Processing),
                Completed = g.Count(x => x.Status == TourPackageRefundStatus.Completed),
                Rejected = g.Count(x => x.Status == TourPackageRefundStatus.Rejected),
                Failed = g.Count(x => x.Status == TourPackageRefundStatus.Failed),
                LastActivityAt = g.Max(x => x.UpdatedAt ?? x.CreatedAt)
            })
            .ToDictionaryAsync(x => x.BookingId, ct);

        return new TourPackageBookingOpsPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = bookings.Select(x =>
            {
                cancellationAgg.TryGetValue(x.Id, out var cancellation);
                refundAgg.TryGetValue(x.Id, out var refund);
                var lastActivityAt = new[]
                {
                    x.UpdatedAt ?? x.CreatedAt,
                    cancellation?.LastActivityAt,
                    refund?.LastActivityAt
                }
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .DefaultIfEmpty(x.CreatedAt)
                .Max();

                return new TourPackageBookingOpsListItem
                {
                    Id = x.Id,
                    ReservationId = x.TourPackageReservationId,
                    ReservationCode = x.TourPackageReservation?.Code,
                    PackageId = x.TourPackageId,
                    PackageCode = x.TourPackage?.Code ?? string.Empty,
                    PackageName = x.TourPackage?.Name ?? string.Empty,
                    ScheduleId = x.TourScheduleId,
                    ScheduleCode = x.TourSchedule?.Code ?? string.Empty,
                    ScheduleName = x.TourSchedule?.Name,
                    UserId = x.UserId,
                    Code = x.Code,
                    Status = x.Status,
                    CurrencyCode = x.CurrencyCode,
                    RequestedPax = x.RequestedPax,
                    ConfirmedCapacitySlots = x.ConfirmedCapacitySlots,
                    PackageSubtotalAmount = x.PackageSubtotalAmount,
                    CancellationCount = cancellation?.Total ?? 0,
                    CompletedCancellationCount = cancellation?.Completed ?? 0,
                    RejectedCancellationCount = cancellation?.Rejected ?? 0,
                    RefundCount = refund?.Total ?? 0,
                    ReadyRefundCount = refund?.Ready ?? 0,
                    ProcessingRefundCount = refund?.Processing ?? 0,
                    CompletedRefundCount = refund?.Completed ?? 0,
                    RejectedRefundCount = refund?.Rejected ?? 0,
                    FailedRefundCount = refund?.Failed ?? 0,
                    TotalPenaltyAmount = cancellation?.Penalty ?? 0m,
                    TotalRefundAmount = cancellation?.Refund ?? 0m,
                    ConfirmedAt = x.ConfirmedAt,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    LastActivityAt = lastActivityAt,
                    IsDeleted = x.IsDeleted
                };
            }).ToList()
        };
    }

    public async Task<TourPackageBookingOpsDetailView> GetAsync(
        Guid tenantId,
        Guid tourId,
        Guid bookingId,
        CancellationToken ct = default)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        if (bookingId == Guid.Empty)
            throw new ArgumentException("BookingId is required.", nameof(bookingId));

        var booking = await _db.TourPackageBookings.IgnoreQueryFilters()
            .Include(x => x.TourSchedule)
            .Include(x => x.TourPackage)
            .Include(x => x.TourPackageReservation)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.TourId == tourId &&
                x.Id == bookingId &&
                !x.IsDeleted, ct);

        if (booking is null)
            throw new KeyNotFoundException("Tour package booking not found in current tenant.");

        var cancellations = await _db.TourPackageCancellations.IgnoreQueryFilters()
            .Include(x => x.Items)
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourPackageBookingId == bookingId &&
                !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);

        var refunds = await _db.TourPackageRefunds.IgnoreQueryFilters()
            .Include(x => x.Attempts)
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourPackageBookingId == bookingId &&
                !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);

        return new TourPackageBookingOpsDetailView
        {
            Booking = MapBookingDetail(booking),
            Reservation = booking.TourPackageReservation is null ? null : MapReservationSummary(booking.TourPackageReservation),
            Cancellations = cancellations.Select(MapCancellationSummary).ToList(),
            Refunds = refunds.Select(MapRefundSummary).ToList(),
            Timeline = BuildTimeline(booking, booking.TourPackageReservation, cancellations, refunds)
        };
    }

    public async Task<List<TourPackageBookingOpsTimelineEventView>> GetTimelineAsync(
        Guid tenantId,
        Guid tourId,
        Guid bookingId,
        CancellationToken ct = default)
    {
        var detail = await GetAsync(tenantId, tourId, bookingId, ct);
        return detail.Timeline;
    }

    private static TourPackageBookingOpsBookingDetailView MapBookingDetail(TourPackageBooking booking)
    {
        return new TourPackageBookingOpsBookingDetailView
        {
            Id = booking.Id,
            TourId = booking.TourId,
            ReservationId = booking.TourPackageReservationId,
            ReservationCode = booking.TourPackageReservation?.Code,
            ScheduleId = booking.TourScheduleId,
            ScheduleCode = booking.TourSchedule?.Code ?? string.Empty,
            ScheduleName = booking.TourSchedule?.Name,
            PackageId = booking.TourPackageId,
            PackageCode = booking.TourPackage?.Code ?? string.Empty,
            PackageName = booking.TourPackage?.Name ?? string.Empty,
            PackageMode = booking.TourPackage?.Mode ?? TourPackageMode.Fixed,
            Code = booking.Code,
            Status = booking.Status,
            HoldStrategy = booking.HoldStrategy,
            CurrencyCode = booking.CurrencyCode,
            UserId = booking.UserId,
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
                .Select(x => new TourPackageBookingOpsBookingItemDetailView
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
                    SnapshotJson = x.SnapshotJson,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToList()
        };
    }

    private static TourPackageBookingOpsReservationSummaryView MapReservationSummary(TourPackageReservation reservation)
    {
        return new TourPackageBookingOpsReservationSummaryView
        {
            Id = reservation.Id,
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
            CreatedAt = reservation.CreatedAt,
            UpdatedAt = reservation.UpdatedAt
        };
    }

    private static TourPackageBookingOpsCancellationSummaryView MapCancellationSummary(TourPackageCancellation cancellation)
    {
        return new TourPackageBookingOpsCancellationSummaryView
        {
            Id = cancellation.Id,
            Status = cancellation.Status,
            IsAdminOverride = cancellation.IsAdminOverride,
            RequestedByUserId = cancellation.RequestedByUserId,
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
            ItemCount = cancellation.Items.Count(x => !x.IsDeleted),
            CompletedAt = cancellation.CompletedAt,
            CreatedAt = cancellation.CreatedAt,
            UpdatedAt = cancellation.UpdatedAt
        };
    }

    private static TourPackageBookingOpsRefundSummaryView MapRefundSummary(TourPackageRefund refund)
    {
        return new TourPackageBookingOpsRefundSummaryView
        {
            Id = refund.Id,
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
            WebhookState = refund.WebhookState,
            LastProviderError = refund.LastProviderError,
            AttemptCount = refund.Attempts.Count(x => !x.IsDeleted),
            PreparedAt = refund.PreparedAt,
            CompletedAt = refund.CompletedAt,
            CreatedAt = refund.CreatedAt,
            UpdatedAt = refund.UpdatedAt
        };
    }

    private static List<TourPackageBookingOpsTimelineEventView> BuildTimeline(
        TourPackageBooking booking,
        TourPackageReservation? reservation,
        IReadOnlyCollection<TourPackageCancellation> cancellations,
        IReadOnlyCollection<TourPackageRefund> refunds)
    {
        var events = new List<TourPackageBookingOpsTimelineEventView>();

        if (reservation is not null)
        {
            events.Add(new TourPackageBookingOpsTimelineEventView
            {
                OccurredAt = reservation.CreatedAt,
                EventType = "reservation.created",
                Status = reservation.Status.ToString(),
                Title = "Package reservation created",
                Description = $"Reservation {reservation.Code} was created with status {reservation.Status}.",
                ReferenceId = reservation.Id
            });

            if (reservation.Status == TourPackageReservationStatus.Confirmed && reservation.UpdatedAt.HasValue)
            {
                events.Add(new TourPackageBookingOpsTimelineEventView
                {
                    OccurredAt = reservation.UpdatedAt.Value,
                    EventType = "reservation.confirmed",
                    Status = reservation.Status.ToString(),
                    Title = "Package reservation consumed",
                    Description = $"Reservation {reservation.Code} was consumed during booking confirmation.",
                    ReferenceId = reservation.Id
                });
            }
        }

        events.Add(new TourPackageBookingOpsTimelineEventView
        {
            OccurredAt = booking.CreatedAt,
            EventType = "booking.created",
            Status = booking.Status.ToString(),
            Title = "Package booking created",
            Description = $"Booking {booking.Code} was created.",
            ReferenceId = booking.Id
        });

        if (booking.ConfirmedAt.HasValue)
        {
            events.Add(new TourPackageBookingOpsTimelineEventView
            {
                OccurredAt = booking.ConfirmedAt.Value,
                EventType = "booking.confirmed",
                Status = booking.Status.ToString(),
                Title = "Package booking confirmed",
                Description = $"Booking {booking.Code} was confirmed with status {booking.Status}.",
                ReferenceId = booking.Id
            });
        }

        foreach (var cancellation in cancellations)
        {
            events.Add(new TourPackageBookingOpsTimelineEventView
            {
                OccurredAt = cancellation.CreatedAt,
                EventType = "cancellation.requested",
                Status = cancellation.Status.ToString(),
                Title = "Cancellation requested",
                Description = $"Cancellation request {cancellation.Id} was created for {cancellation.Items.Count(x => !x.IsDeleted)} item(s).",
                ReferenceId = cancellation.Id,
                Amount = cancellation.RefundAmount
            });

            if (cancellation.CompletedAt.HasValue)
            {
                events.Add(new TourPackageBookingOpsTimelineEventView
                {
                    OccurredAt = cancellation.CompletedAt.Value,
                    EventType = cancellation.Status == TourPackageCancellationStatus.Rejected
                        ? "cancellation.rejected"
                        : "cancellation.completed",
                    Status = cancellation.Status.ToString(),
                    Title = cancellation.Status == TourPackageCancellationStatus.Rejected
                        ? "Cancellation rejected"
                        : "Cancellation completed",
                    Description = cancellation.FailureReason
                        ?? $"Cancellation {cancellation.Id} finished with status {cancellation.Status}.",
                    ReferenceId = cancellation.Id,
                    Amount = cancellation.RefundAmount
                });
            }
        }

        foreach (var refund in refunds)
        {
            if (refund.PreparedAt.HasValue)
            {
                events.Add(new TourPackageBookingOpsTimelineEventView
                {
                    OccurredAt = refund.PreparedAt.Value,
                    EventType = "refund.prepared",
                    Status = refund.Status.ToString(),
                    Title = "Refund prepared",
                    Description = $"Refund {refund.Id} is in status {refund.Status}.",
                    ReferenceId = refund.Id,
                    BookingItemId = refund.TourPackageBookingItemId,
                    Amount = refund.RefundAmount
                });
            }

            if (refund.CompletedAt.HasValue)
            {
                events.Add(new TourPackageBookingOpsTimelineEventView
                {
                    OccurredAt = refund.CompletedAt.Value,
                    EventType = refund.Status == TourPackageRefundStatus.Rejected
                        ? "refund.rejected"
                        : "refund.completed",
                    Status = refund.Status.ToString(),
                    Title = refund.Status == TourPackageRefundStatus.Rejected
                        ? "Refund rejected"
                        : "Refund completed",
                    Description = refund.LastProviderError
                        ?? $"Refund {refund.Id} finished with status {refund.Status}.",
                    ReferenceId = refund.Id,
                    BookingItemId = refund.TourPackageBookingItemId,
                    Amount = refund.RefundAmount
                });
            }
        }

        return events
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.ReferenceId)
            .ToList();
    }
}

public sealed class TourPackageBookingOpsListRequest
{
    public string? Q { get; set; }
    public Guid? PackageId { get; set; }
    public Guid? ScheduleId { get; set; }
    public Guid? UserId { get; set; }
    public TourPackageBookingStatus? Status { get; set; }
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }
    public DateTimeOffset? ConfirmedFrom { get; set; }
    public DateTimeOffset? ConfirmedTo { get; set; }
    public bool IncludeDeleted { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed class TourPackageBookingOpsPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TourPackageBookingOpsListItem> Items { get; set; } = new();
}

public sealed class TourPackageBookingOpsListItem
{
    public Guid Id { get; set; }
    public Guid ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public Guid PackageId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public Guid ScheduleId { get; set; }
    public string ScheduleCode { get; set; } = string.Empty;
    public string? ScheduleName { get; set; }
    public Guid? UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public TourPackageBookingStatus Status { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int RequestedPax { get; set; }
    public int ConfirmedCapacitySlots { get; set; }
    public decimal PackageSubtotalAmount { get; set; }
    public int CancellationCount { get; set; }
    public int CompletedCancellationCount { get; set; }
    public int RejectedCancellationCount { get; set; }
    public int RefundCount { get; set; }
    public int ReadyRefundCount { get; set; }
    public int ProcessingRefundCount { get; set; }
    public int CompletedRefundCount { get; set; }
    public int RejectedRefundCount { get; set; }
    public int FailedRefundCount { get; set; }
    public decimal TotalPenaltyAmount { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset LastActivityAt { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class TourPackageBookingOpsDetailView
{
    public TourPackageBookingOpsBookingDetailView Booking { get; set; } = new();
    public TourPackageBookingOpsReservationSummaryView? Reservation { get; set; }
    public List<TourPackageBookingOpsCancellationSummaryView> Cancellations { get; set; } = new();
    public List<TourPackageBookingOpsRefundSummaryView> Refunds { get; set; } = new();
    public List<TourPackageBookingOpsTimelineEventView> Timeline { get; set; } = new();
}

public sealed class TourPackageBookingOpsBookingDetailView
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public Guid ReservationId { get; set; }
    public string? ReservationCode { get; set; }
    public Guid ScheduleId { get; set; }
    public string ScheduleCode { get; set; } = string.Empty;
    public string? ScheduleName { get; set; }
    public Guid PackageId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public TourPackageMode PackageMode { get; set; }
    public string Code { get; set; } = string.Empty;
    public TourPackageBookingStatus Status { get; set; }
    public TourPackageHoldStrategy HoldStrategy { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
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
    public List<TourPackageBookingOpsBookingItemDetailView> Items { get; set; } = new();
}

public sealed class TourPackageBookingOpsBookingItemDetailView
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
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public string? SourceHoldToken { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SnapshotJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class TourPackageBookingOpsReservationSummaryView
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string HoldToken { get; set; } = string.Empty;
    public TourPackageReservationStatus Status { get; set; }
    public TourPackageHoldStrategy HoldStrategy { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public int RequestedPax { get; set; }
    public int HeldCapacitySlots { get; set; }
    public decimal PackageSubtotalAmount { get; set; }
    public DateTimeOffset? HoldExpiresAt { get; set; }
    public string? Notes { get; set; }
    public string? FailureReason { get; set; }
    public string? SnapshotJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class TourPackageBookingOpsCancellationSummaryView
{
    public Guid Id { get; set; }
    public TourPackageCancellationStatus Status { get; set; }
    public bool IsAdminOverride { get; set; }
    public Guid? RequestedByUserId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
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
    public int ItemCount { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class TourPackageBookingOpsRefundSummaryView
{
    public Guid Id { get; set; }
    public Guid BookingItemId { get; set; }
    public Guid CancellationId { get; set; }
    public Guid CancellationItemId { get; set; }
    public TourPackageRefundStatus Status { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal GrossLineAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public string? Provider { get; set; }
    public string? ExternalReference { get; set; }
    public string? WebhookState { get; set; }
    public string? LastProviderError { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? PreparedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class TourPackageBookingOpsTimelineEventView
{
    public DateTimeOffset OccurredAt { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ReferenceId { get; set; }
    public Guid? BookingItemId { get; set; }
    public decimal? Amount { get; set; }
}
