using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class TourPackageAuditService
{
    private readonly AppDbContext _db;

    public TourPackageAuditService(AppDbContext db)
    {
        _db = db;
    }

    public void Track(TourPackageAuditWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.TenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(request));

        if (request.TourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.EventType))
            throw new ArgumentException("EventType is required.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.", nameof(request));

        var now = request.OccurredAt ?? DateTimeOffset.UtcNow;
        _db.TourPackageAuditEvents.Add(new TourPackageAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            TourId = request.TourId,
            TourScheduleId = NormalizeGuid(request.TourScheduleId),
            TourPackageId = NormalizeGuid(request.TourPackageId),
            TourPackageReservationId = NormalizeGuid(request.TourPackageReservationId),
            TourPackageBookingId = NormalizeGuid(request.TourPackageBookingId),
            TourPackageBookingItemId = NormalizeGuid(request.TourPackageBookingItemId),
            TourPackageCancellationId = NormalizeGuid(request.TourPackageCancellationId),
            TourPackageRefundId = NormalizeGuid(request.TourPackageRefundId),
            TourPackageRescheduleId = NormalizeGuid(request.TourPackageRescheduleId),
            ActorUserId = NormalizeGuid(request.ActorUserId),
            SourceType = request.SourceType,
            EventType = request.EventType.Trim(),
            Title = request.Title.Trim(),
            Status = NormalizeText(request.Status),
            Description = NormalizeText(request.Description),
            CurrencyCode = NormalizeText(request.CurrencyCode),
            Amount = request.Amount,
            Severity = request.Severity,
            IsSystemGenerated = request.IsSystemGenerated,
            SnapshotJson = NormalizeText(request.SnapshotJson),
            MetadataJson = NormalizeText(request.MetadataJson),
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = NormalizeGuid(request.ActorUserId),
            UpdatedAt = now,
            UpdatedByUserId = NormalizeGuid(request.ActorUserId)
        });
    }

    public async Task<TourPackageAuditOverviewView> GetOverviewAsync(
        Guid tenantId,
        Guid tourId,
        TourPackageAuditOverviewRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        var bookingFilter = BuildBookingRelationshipQuery(tenantId, tourId, request);
        var bookingIds = bookingFilter.Select(x => x.Id);
        var relatedReservationId = await ResolveReservationIdAsync(tenantId, tourId, request.BookingId, ct);

        var bookingsQuery = ApplyDateFilter(bookingFilter, request.From, request.To, x => x.CreatedAt);
        var bookings = await bookingsQuery
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.PackageSubtotalAmount
            })
            .ToListAsync(ct);

        var reservationsQuery = _db.TourPackageReservations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourId == tourId &&
                !x.IsDeleted);

        if (request.PackageId.HasValue)
            reservationsQuery = reservationsQuery.Where(x => x.TourPackageId == request.PackageId.Value);

        if (request.ScheduleId.HasValue)
            reservationsQuery = reservationsQuery.Where(x => x.TourScheduleId == request.ScheduleId.Value);

        if (request.UserId.HasValue)
            reservationsQuery = reservationsQuery.Where(x => x.UserId == request.UserId.Value);

        if (relatedReservationId.HasValue)
            reservationsQuery = reservationsQuery.Where(x => x.Id == relatedReservationId.Value);

        reservationsQuery = ApplyDateFilter(reservationsQuery, request.From, request.To, x => x.CreatedAt);
        var reservations = await reservationsQuery
            .Select(x => new
            {
                x.Status
            })
            .ToListAsync(ct);

        var cancellationsQuery = _db.TourPackageCancellations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourId == tourId &&
                !x.IsDeleted);

        if (request.PackageId.HasValue)
            cancellationsQuery = cancellationsQuery.Where(x => x.TourPackageId == request.PackageId.Value);

        if (request.ScheduleId.HasValue)
            cancellationsQuery = cancellationsQuery.Where(x => x.TourScheduleId == request.ScheduleId.Value);

        if (request.BookingId.HasValue)
            cancellationsQuery = cancellationsQuery.Where(x => x.TourPackageBookingId == request.BookingId.Value);
        else
            cancellationsQuery = cancellationsQuery.Where(x => bookingIds.Contains(x.TourPackageBookingId));

        cancellationsQuery = ApplyDateFilter(cancellationsQuery, request.From, request.To, x => x.CreatedAt);
        var cancellations = await cancellationsQuery
            .Select(x => new
            {
                x.Status,
                x.PenaltyAmount,
                x.RefundAmount
            })
            .ToListAsync(ct);

        var refundsQuery = _db.TourPackageRefunds
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TourPackageBooking != null &&
                x.TourPackageBooking.TenantId == tenantId &&
                x.TourPackageBooking.TourId == tourId &&
                !x.IsDeleted);

        if (request.PackageId.HasValue)
            refundsQuery = refundsQuery.Where(x => x.TourPackageBooking != null && x.TourPackageBooking.TourPackageId == request.PackageId.Value);

        if (request.ScheduleId.HasValue)
            refundsQuery = refundsQuery.Where(x => x.TourPackageBooking != null && x.TourPackageBooking.TourScheduleId == request.ScheduleId.Value);

        if (request.UserId.HasValue)
            refundsQuery = refundsQuery.Where(x => x.TourPackageBooking != null && x.TourPackageBooking.UserId == request.UserId.Value);

        if (request.BookingId.HasValue)
            refundsQuery = refundsQuery.Where(x => x.TourPackageBookingId == request.BookingId.Value);

        refundsQuery = ApplyDateFilter(refundsQuery, request.From, request.To, x => x.CreatedAt);
        var refunds = await refundsQuery
            .Select(x => new
            {
                x.Status,
                x.RefundAmount
            })
            .ToListAsync(ct);

        var reschedulesQuery = _db.TourPackageReschedules
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourId == tourId &&
                !x.IsDeleted);

        if (request.PackageId.HasValue)
            reschedulesQuery = reschedulesQuery.Where(x =>
                x.SourceTourPackageId == request.PackageId.Value ||
                x.TargetTourPackageId == request.PackageId.Value);

        if (request.ScheduleId.HasValue)
            reschedulesQuery = reschedulesQuery.Where(x =>
                x.SourceTourScheduleId == request.ScheduleId.Value ||
                x.TargetTourScheduleId == request.ScheduleId.Value);

        if (request.BookingId.HasValue)
            reschedulesQuery = reschedulesQuery.Where(x =>
                x.SourceTourPackageBookingId == request.BookingId.Value ||
                x.TargetTourPackageBookingId == request.BookingId.Value);
        else
            reschedulesQuery = reschedulesQuery.Where(x =>
                bookingIds.Contains(x.SourceTourPackageBookingId) ||
                (x.TargetTourPackageBookingId.HasValue && bookingIds.Contains(x.TargetTourPackageBookingId.Value)));

        reschedulesQuery = ApplyDateFilter(reschedulesQuery, request.From, request.To, x => x.CreatedAt);
        var reschedules = await reschedulesQuery
            .Select(x => new
            {
                x.Status
            })
            .ToListAsync(ct);

        var eventsQuery = _db.TourPackageAuditEvents
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourId == tourId &&
                !x.IsDeleted);

        if (request.PackageId.HasValue)
            eventsQuery = eventsQuery.Where(x => x.TourPackageId == request.PackageId.Value);

        if (request.ScheduleId.HasValue)
            eventsQuery = eventsQuery.Where(x => x.TourScheduleId == request.ScheduleId.Value);

        if (request.BookingId.HasValue)
            eventsQuery = eventsQuery.Where(x => x.TourPackageBookingId == request.BookingId.Value);

        eventsQuery = ApplyDateFilter(eventsQuery, request.From, request.To, x => x.CreatedAt);
        var eventAgg = await eventsQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Warning = g.Count(x => x.Severity == TourPackageAuditSeverity.Warning),
                Error = g.Count(x => x.Severity == TourPackageAuditSeverity.Error)
            })
            .FirstOrDefaultAsync(ct);

        var grossBookedAmount = bookings
            .Where(x => x.Status != TourPackageBookingStatus.Failed)
            .Sum(x => x.PackageSubtotalAmount);
        var activeBookingCount = bookings.Count(x => x.Status != TourPackageBookingStatus.Failed);

        return new TourPackageAuditOverviewView
        {
            From = request.From,
            To = request.To,
            BookingCount = bookings.Count,
            ConfirmedBookingCount = bookings.Count(x => x.Status == TourPackageBookingStatus.Confirmed),
            PartiallyConfirmedBookingCount = bookings.Count(x => x.Status == TourPackageBookingStatus.PartiallyConfirmed),
            PartiallyCancelledBookingCount = bookings.Count(x => x.Status == TourPackageBookingStatus.PartiallyCancelled),
            CancelledBookingCount = bookings.Count(x => x.Status == TourPackageBookingStatus.Cancelled),
            FailedBookingCount = bookings.Count(x => x.Status == TourPackageBookingStatus.Failed),
            HeldReservationCount = reservations.Count(x => x.Status == TourPackageReservationStatus.Held),
            PartiallyHeldReservationCount = reservations.Count(x => x.Status == TourPackageReservationStatus.PartiallyHeld),
            ExpiredReservationCount = reservations.Count(x => x.Status == TourPackageReservationStatus.Expired),
            ReleasedReservationCount = reservations.Count(x => x.Status == TourPackageReservationStatus.Released),
            CompletedCancellationCount = cancellations.Count(x => x.Status == TourPackageCancellationStatus.Completed),
            RejectedCancellationCount = cancellations.Count(x => x.Status == TourPackageCancellationStatus.Rejected),
            ReadyRefundCount = refunds.Count(x => x.Status == TourPackageRefundStatus.ReadyForProvider),
            ProcessingRefundCount = refunds.Count(x => x.Status == TourPackageRefundStatus.Processing),
            CompletedRefundCount = refunds.Count(x => x.Status == TourPackageRefundStatus.Completed),
            RejectedRefundCount = refunds.Count(x => x.Status == TourPackageRefundStatus.Rejected),
            FailedRefundCount = refunds.Count(x => x.Status == TourPackageRefundStatus.Failed),
            CompletedRescheduleCount = reschedules.Count(x => x.Status == TourPackageRescheduleStatus.Completed),
            AttentionRequiredRescheduleCount = reschedules.Count(x => x.Status == TourPackageRescheduleStatus.AttentionRequired),
            ReleasedRescheduleCount = reschedules.Count(x => x.Status == TourPackageRescheduleStatus.Released),
            FailedRescheduleCount = reschedules.Count(x => x.Status == TourPackageRescheduleStatus.Failed),
            GrossBookedAmount = grossBookedAmount,
            TotalPenaltyAmount = cancellations.Sum(x => x.PenaltyAmount),
            TotalRefundAmount = refunds.Sum(x => x.RefundAmount),
            NetAfterRefundAmount = grossBookedAmount - refunds.Sum(x => x.RefundAmount),
            AverageBookingValue = activeBookingCount == 0 ? 0m : grossBookedAmount / activeBookingCount,
            AuditEventCount = eventAgg?.Total ?? 0,
            WarningEventCount = eventAgg?.Warning ?? 0,
            ErrorEventCount = eventAgg?.Error ?? 0
        };
    }

    public async Task<List<TourPackageAuditSourceBreakdownItemView>> GetSourceBreakdownAsync(
        Guid tenantId,
        Guid tourId,
        TourPackageAuditSourceBreakdownRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        if (tourId == Guid.Empty)
            throw new ArgumentException("TourId is required.", nameof(tourId));

        var bookingFilter = BuildBookingRelationshipQuery(tenantId, tourId, request);
        var bookingIds = bookingFilter.Select(x => x.Id);

        var itemQuery = _db.TourPackageBookingItems
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TourPackageBooking != null &&
                x.TourPackageBooking.TenantId == tenantId &&
                x.TourPackageBooking.TourId == tourId &&
                !x.IsDeleted);

        if (request.BookingId.HasValue)
            itemQuery = itemQuery.Where(x => x.TourPackageBookingId == request.BookingId.Value);
        else
            itemQuery = itemQuery.Where(x => bookingIds.Contains(x.TourPackageBookingId));

        itemQuery = ApplyDateFilter(itemQuery, request.From, request.To, x => x.CreatedAt);

        var items = await itemQuery
            .Select(x => new
            {
                x.Id,
                x.SourceType,
                x.Status,
                x.LineAmount,
                LastActivityAt = x.UpdatedAt ?? x.CreatedAt
            })
            .ToListAsync(ct);

        var itemIds = items.Select(x => x.Id).ToList();

        var refunds = itemIds.Count == 0
            ? new List<TourPackageAuditRefundAggRow>()
            : await _db.TourPackageRefunds
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x =>
                    !x.IsDeleted &&
                    itemIds.Contains(x.TourPackageBookingItemId))
                .Select(x => new TourPackageAuditRefundAggRow
                {
                    BookingItemId = x.TourPackageBookingItemId,
                    Status = x.Status,
                    PenaltyAmount = x.PenaltyAmount,
                    RefundAmount = x.RefundAmount,
                    LastActivityAt = x.UpdatedAt ?? x.CreatedAt
                })
                .ToListAsync(ct);

        var refundByItemId = refunds
            .GroupBy(x => x.BookingItemId)
            .ToDictionary(
                x => x.Key,
                x => new
                {
                    Penalty = x.Sum(y => y.PenaltyAmount),
                    Refund = x.Sum(y => y.RefundAmount),
                    Completed = x.Count(y => y.Status == TourPackageRefundStatus.Completed),
                    Ready = x.Count(y => y.Status == TourPackageRefundStatus.ReadyForProvider),
                    Processing = x.Count(y => y.Status == TourPackageRefundStatus.Processing),
                    Rejected = x.Count(y => y.Status == TourPackageRefundStatus.Rejected),
                    Failed = x.Count(y => y.Status == TourPackageRefundStatus.Failed),
                    LastActivityAt = x.Max(y => y.LastActivityAt)
                });

        return items
            .GroupBy(x => x.SourceType)
            .Select(group =>
            {
                var lastActivityAt = group
                    .Select(x =>
                    {
                        refundByItemId.TryGetValue(x.Id, out var refundAgg);
                        return refundAgg?.LastActivityAt ?? x.LastActivityAt;
                    })
                    .DefaultIfEmpty()
                    .Max();

                return new TourPackageAuditSourceBreakdownItemView
                {
                    SourceType = group.Key,
                    ItemCount = group.Count(),
                    ConfirmedItemCount = group.Count(x => x.Status == TourPackageBookingItemStatus.Confirmed),
                    CancelledItemCount = group.Count(x => x.Status == TourPackageBookingItemStatus.Cancelled),
                    FailedItemCount = group.Count(x => x.Status == TourPackageBookingItemStatus.Failed),
                    RefundPendingItemCount = group.Count(x => x.Status == TourPackageBookingItemStatus.RefundPending),
                    RefundedItemCount = group.Count(x => x.Status == TourPackageBookingItemStatus.Refunded),
                    RefundRejectedItemCount = group.Count(x => x.Status == TourPackageBookingItemStatus.RefundRejected),
                    GrossLineAmount = group.Sum(x => x.LineAmount),
                    PenaltyAmount = group.Sum(x => refundByItemId.TryGetValue(x.Id, out var refundAgg) ? refundAgg.Penalty : 0m),
                    RefundAmount = group.Sum(x => refundByItemId.TryGetValue(x.Id, out var refundAgg) ? refundAgg.Refund : 0m),
                    ReadyRefundCount = group.Sum(x => refundByItemId.TryGetValue(x.Id, out var refundAgg) ? refundAgg.Ready : 0),
                    ProcessingRefundCount = group.Sum(x => refundByItemId.TryGetValue(x.Id, out var refundAgg) ? refundAgg.Processing : 0),
                    CompletedRefundCount = group.Sum(x => refundByItemId.TryGetValue(x.Id, out var refundAgg) ? refundAgg.Completed : 0),
                    RejectedRefundCount = group.Sum(x => refundByItemId.TryGetValue(x.Id, out var refundAgg) ? refundAgg.Rejected : 0),
                    FailedRefundCount = group.Sum(x => refundByItemId.TryGetValue(x.Id, out var refundAgg) ? refundAgg.Failed : 0),
                    LastActivityAt = lastActivityAt
                };
            })
            .OrderBy(x => x.SourceType)
            .ToList();
    }

    public async Task<TourPackageAuditEventPagedResponse> ListEventsAsync(
        Guid tenantId,
        Guid tourId,
        TourPackageAuditEventListRequest request,
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

        IQueryable<TourPackageAuditEvent> query = _db.TourPackageAuditEvents
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourId == tourId &&
                !x.IsDeleted);

        if (request.PackageId.HasValue)
            query = query.Where(x => x.TourPackageId == request.PackageId.Value);

        if (request.ScheduleId.HasValue)
            query = query.Where(x => x.TourScheduleId == request.ScheduleId.Value);

        if (request.BookingId.HasValue)
            query = query.Where(x => x.TourPackageBookingId == request.BookingId.Value);

        if (request.ReservationId.HasValue)
            query = query.Where(x => x.TourPackageReservationId == request.ReservationId.Value);

        if (request.CancellationId.HasValue)
            query = query.Where(x => x.TourPackageCancellationId == request.CancellationId.Value);

        if (request.RefundId.HasValue)
            query = query.Where(x => x.TourPackageRefundId == request.RefundId.Value);

        if (request.RescheduleId.HasValue)
            query = query.Where(x => x.TourPackageRescheduleId == request.RescheduleId.Value);

        if (request.ActorUserId.HasValue)
            query = query.Where(x => x.ActorUserId == request.ActorUserId.Value);

        if (request.SourceType.HasValue)
            query = query.Where(x => x.SourceType == request.SourceType.Value);

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            var eventType = request.EventType.Trim();
            query = query.Where(x => x.EventType == eventType);
        }

        if (request.Severity.HasValue)
            query = query.Where(x => x.Severity == request.Severity.Value);

        query = ApplyDateFilter(query, request.From, request.To, x => x.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TourPackageAuditEventView
            {
                Id = x.Id,
                TourId = x.TourId,
                ScheduleId = x.TourScheduleId,
                PackageId = x.TourPackageId,
                ReservationId = x.TourPackageReservationId,
                BookingId = x.TourPackageBookingId,
                BookingItemId = x.TourPackageBookingItemId,
                CancellationId = x.TourPackageCancellationId,
                RefundId = x.TourPackageRefundId,
                RescheduleId = x.TourPackageRescheduleId,
                ActorUserId = x.ActorUserId,
                SourceType = x.SourceType,
                EventType = x.EventType,
                Title = x.Title,
                Status = x.Status,
                Description = x.Description,
                CurrencyCode = x.CurrencyCode,
                Amount = x.Amount,
                Severity = x.Severity,
                IsSystemGenerated = x.IsSystemGenerated,
                SnapshotJson = x.SnapshotJson,
                MetadataJson = x.MetadataJson,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);

        return new TourPackageAuditEventPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }

    private IQueryable<TourPackageBooking> BuildBookingRelationshipQuery(
        Guid tenantId,
        Guid tourId,
        TourPackageAuditFilterRequest request)
    {
        IQueryable<TourPackageBooking> query = _db.TourPackageBookings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourId == tourId &&
                !x.IsDeleted);

        if (request.PackageId.HasValue)
            query = query.Where(x => x.TourPackageId == request.PackageId.Value);

        if (request.ScheduleId.HasValue)
            query = query.Where(x => x.TourScheduleId == request.ScheduleId.Value);

        if (request.UserId.HasValue)
            query = query.Where(x => x.UserId == request.UserId.Value);

        if (request.BookingId.HasValue)
            query = query.Where(x => x.Id == request.BookingId.Value);

        return query;
    }

    private async Task<Guid?> ResolveReservationIdAsync(
        Guid tenantId,
        Guid tourId,
        Guid? bookingId,
        CancellationToken ct)
    {
        if (!bookingId.HasValue || bookingId.Value == Guid.Empty)
            return null;

        return await _db.TourPackageBookings
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TourId == tourId &&
                x.Id == bookingId.Value &&
                !x.IsDeleted)
            .Select(x => (Guid?)x.TourPackageReservationId)
            .FirstOrDefaultAsync(ct);
    }

    private static IQueryable<T> ApplyDateFilter<T>(
        IQueryable<T> query,
        DateTimeOffset? from,
        DateTimeOffset? to,
        Expression<Func<T, DateTimeOffset>> selector)
        where T : class
    {
        var parameter = selector.Parameters[0];
        if (from.HasValue)
        {
            var body = Expression.GreaterThanOrEqual(selector.Body, Expression.Constant(from.Value));
            query = query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
        }

        if (to.HasValue)
        {
            var body = Expression.LessThanOrEqual(selector.Body, Expression.Constant(to.Value));
            query = query.Where(Expression.Lambda<Func<T, bool>>(body, parameter));
        }

        return query;
    }

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static Guid? NormalizeGuid(Guid? value)
        => value.HasValue && value.Value != Guid.Empty ? value.Value : null;
}

internal sealed class TourPackageAuditRefundAggRow
{
    public Guid BookingItemId { get; set; }
    public TourPackageRefundStatus Status { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public DateTimeOffset LastActivityAt { get; set; }
}

public sealed class TourPackageAuditWriteRequest
{
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public Guid? TourScheduleId { get; set; }
    public Guid? TourPackageId { get; set; }
    public Guid? TourPackageReservationId { get; set; }
    public Guid? TourPackageBookingId { get; set; }
    public Guid? TourPackageBookingItemId { get; set; }
    public Guid? TourPackageCancellationId { get; set; }
    public Guid? TourPackageRefundId { get; set; }
    public Guid? TourPackageRescheduleId { get; set; }
    public Guid? ActorUserId { get; set; }
    public TourPackageSourceType? SourceType { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Description { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? Amount { get; set; }
    public TourPackageAuditSeverity Severity { get; set; } = TourPackageAuditSeverity.Info;
    public bool IsSystemGenerated { get; set; } = true;
    public string? SnapshotJson { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset? OccurredAt { get; set; }
}

public abstract class TourPackageAuditFilterRequest
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public Guid? PackageId { get; set; }
    public Guid? ScheduleId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? UserId { get; set; }
}

public sealed class TourPackageAuditOverviewRequest : TourPackageAuditFilterRequest
{
}

public sealed class TourPackageAuditSourceBreakdownRequest : TourPackageAuditFilterRequest
{
}

public sealed class TourPackageAuditEventListRequest : TourPackageAuditFilterRequest
{
    public Guid? ReservationId { get; set; }
    public Guid? CancellationId { get; set; }
    public Guid? RefundId { get; set; }
    public Guid? RescheduleId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? EventType { get; set; }
    public TourPackageAuditSeverity? Severity { get; set; }
    public TourPackageSourceType? SourceType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed class TourPackageAuditOverviewView
{
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public int BookingCount { get; set; }
    public int ConfirmedBookingCount { get; set; }
    public int PartiallyConfirmedBookingCount { get; set; }
    public int PartiallyCancelledBookingCount { get; set; }
    public int CancelledBookingCount { get; set; }
    public int FailedBookingCount { get; set; }
    public int HeldReservationCount { get; set; }
    public int PartiallyHeldReservationCount { get; set; }
    public int ExpiredReservationCount { get; set; }
    public int ReleasedReservationCount { get; set; }
    public int CompletedCancellationCount { get; set; }
    public int RejectedCancellationCount { get; set; }
    public int ReadyRefundCount { get; set; }
    public int ProcessingRefundCount { get; set; }
    public int CompletedRefundCount { get; set; }
    public int RejectedRefundCount { get; set; }
    public int FailedRefundCount { get; set; }
    public int CompletedRescheduleCount { get; set; }
    public int AttentionRequiredRescheduleCount { get; set; }
    public int ReleasedRescheduleCount { get; set; }
    public int FailedRescheduleCount { get; set; }
    public decimal GrossBookedAmount { get; set; }
    public decimal TotalPenaltyAmount { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public decimal NetAfterRefundAmount { get; set; }
    public decimal AverageBookingValue { get; set; }
    public int AuditEventCount { get; set; }
    public int WarningEventCount { get; set; }
    public int ErrorEventCount { get; set; }
}

public sealed class TourPackageAuditSourceBreakdownItemView
{
    public TourPackageSourceType SourceType { get; set; }
    public int ItemCount { get; set; }
    public int ConfirmedItemCount { get; set; }
    public int CancelledItemCount { get; set; }
    public int FailedItemCount { get; set; }
    public int RefundPendingItemCount { get; set; }
    public int RefundedItemCount { get; set; }
    public int RefundRejectedItemCount { get; set; }
    public decimal GrossLineAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public int ReadyRefundCount { get; set; }
    public int ProcessingRefundCount { get; set; }
    public int CompletedRefundCount { get; set; }
    public int RejectedRefundCount { get; set; }
    public int FailedRefundCount { get; set; }
    public DateTimeOffset LastActivityAt { get; set; }
}

public sealed class TourPackageAuditEventPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TourPackageAuditEventView> Items { get; set; } = new();
}

public sealed class TourPackageAuditEventView
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public Guid? ScheduleId { get; set; }
    public Guid? PackageId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? BookingItemId { get; set; }
    public Guid? CancellationId { get; set; }
    public Guid? RefundId { get; set; }
    public Guid? RescheduleId { get; set; }
    public Guid? ActorUserId { get; set; }
    public TourPackageSourceType? SourceType { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Description { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? Amount { get; set; }
    public TourPackageAuditSeverity Severity { get; set; }
    public bool IsSystemGenerated { get; set; }
    public string? SnapshotJson { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
