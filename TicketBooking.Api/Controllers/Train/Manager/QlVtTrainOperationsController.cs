using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Commerce;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlvt/train")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLVT}")]
public sealed class QlVtTrainOperationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlVtTrainOperationsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public sealed class CreateOperationalEventRequest
    {
        public Guid TripId { get; set; }
        public TrainOperationalEventType Type { get; set; } = TrainOperationalEventType.Other;
        public TrainOperationalEventStatus Status { get; set; } = TrainOperationalEventStatus.Published;
        public DateTimeOffset? NewDepartureAt { get; set; }
        public DateTimeOffset? NewArrivalAt { get; set; }
        public string? NewPlatformCode { get; set; }
        public string? NewTrackCode { get; set; }
        public string ReasonCode { get; set; } = "";
        public string? ReasonText { get; set; }
        public string? InternalNote { get; set; }
    }

    public sealed class RescheduleTripRequest
    {
        public DateTimeOffset NewDepartureAt { get; set; }
        public DateTimeOffset NewArrivalAt { get; set; }
        public string ReasonCode { get; set; } = "RESCHEDULE";
        public string? ReasonText { get; set; }
        public string? InternalNote { get; set; }
        public bool ShiftStopTimes { get; set; } = true;
    }

    public sealed class CancelTripRequest
    {
        public string ReasonCode { get; set; } = "CANCELLED";
        public string? ReasonText { get; set; }
        public string? InternalNote { get; set; }
    }

    [HttpGet("operational-events")]
    public async Task<IActionResult> ListEvents(
        [FromQuery] Guid? tripId = null,
        [FromQuery] TrainOperationalEventStatus? status = null,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        var tenantId = _tenantContext.TenantId!.Value;
        var query = _db.TrainOperationalEvents.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (tripId.HasValue && tripId.Value != Guid.Empty)
            query = query.Where(x => x.TripId == tripId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(300)
            .Select(x => new
            {
                x.Id,
                x.TripId,
                x.Type,
                x.Status,
                x.OldDepartureAt,
                x.NewDepartureAt,
                x.OldArrivalAt,
                x.NewArrivalAt,
                x.OldPlatformCode,
                x.NewPlatformCode,
                x.OldTrackCode,
                x.NewTrackCode,
                x.ReasonCode,
                x.ReasonText,
                x.InternalNote,
                x.PublishedAt,
                x.NotifiedAt,
                x.ResolvedAt,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpPost("operational-events")]
    public async Task<IActionResult> CreateEvent([FromBody] CreateOperationalEventRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var tenantId = _tenantContext.TenantId!.Value;
        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == req.TripId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (trip is null)
            return NotFound(new { message = "TrainTrip not found in this tenant." });

        var now = DateTimeOffset.Now;
        var entity = new TrainOperationalEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TripId = trip.Id,
            Type = req.Type,
            Status = req.Status,
            OldDepartureAt = trip.DepartureAt,
            NewDepartureAt = req.NewDepartureAt,
            OldArrivalAt = trip.ArrivalAt,
            NewArrivalAt = req.NewArrivalAt,
            NewPlatformCode = TrimOrNull(req.NewPlatformCode, 30),
            NewTrackCode = TrimOrNull(req.NewTrackCode, 30),
            ReasonCode = TrimOrDefault(req.ReasonCode, "OPS", 50),
            ReasonText = TrimOrNull(req.ReasonText, 300),
            InternalNote = TrimOrNull(req.InternalNote, null),
            PublishedAt = req.Status is TrainOperationalEventStatus.Published or TrainOperationalEventStatus.Notified ? now : null,
            CreatedAt = now
        };

        _db.TrainOperationalEvents.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpPost("trips/{tripId:guid}/reschedule")]
    public async Task<IActionResult> RescheduleTrip(Guid tripId, [FromBody] RescheduleTripRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();

        if (req.NewArrivalAt < req.NewDepartureAt)
            return BadRequest(new { message = "NewArrivalAt must be >= NewDepartureAt." });

        var tenantId = _tenantContext.TenantId!.Value;
        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (trip is null)
            return NotFound(new { message = "TrainTrip not found in this tenant." });

        if (trip.Status == TrainTripStatus.Cancelled)
            return Conflict(new { message = "Cancelled trip cannot be rescheduled." });

        var now = DateTimeOffset.Now;
        var oldDeparture = trip.DepartureAt;
        var oldArrival = trip.ArrivalAt;
        var delta = req.NewDepartureAt - oldDeparture;

        trip.DepartureAt = req.NewDepartureAt;
        trip.ArrivalAt = req.NewArrivalAt;
        trip.UpdatedAt = now;

        if (req.ShiftStopTimes && delta != TimeSpan.Zero)
        {
            var stopTimes = await _db.TrainTripStopTimes.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TripId == tripId && !x.IsDeleted)
                .ToListAsync(ct);

            foreach (var stopTime in stopTimes)
            {
                if (stopTime.ArriveAt.HasValue)
                    stopTime.ArriveAt = stopTime.ArriveAt.Value.Add(delta);
                if (stopTime.DepartAt.HasValue)
                    stopTime.DepartAt = stopTime.DepartAt.Value.Add(delta);
                stopTime.UpdatedAt = now;
            }
        }

        var opEvent = new TrainOperationalEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TripId = tripId,
            Type = TrainOperationalEventType.Reschedule,
            Status = TrainOperationalEventStatus.Published,
            OldDepartureAt = oldDeparture,
            NewDepartureAt = req.NewDepartureAt,
            OldArrivalAt = oldArrival,
            NewArrivalAt = req.NewArrivalAt,
            ReasonCode = TrimOrDefault(req.ReasonCode, "RESCHEDULE", 50),
            ReasonText = TrimOrNull(req.ReasonText, 300),
            InternalNote = TrimOrNull(req.InternalNote, null),
            PublishedAt = now,
            CreatedAt = now
        };

        _db.TrainOperationalEvents.Add(opEvent);
        var notifiedCount = await NotifyAffectedCustomersAsync(trip, "train.rescheduled", "Lịch tàu đã thay đổi", "Chuyến tàu của bạn đã được cập nhật giờ chạy.", opEvent.Id, ct);
        opEvent.NotifiedAt = notifiedCount > 0 ? now : null;
        if (notifiedCount > 0)
            opEvent.Status = TrainOperationalEventStatus.Notified;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true, eventId = opEvent.Id, notifiedCount });
    }

    [HttpPost("trips/{tripId:guid}/cancel")]
    public async Task<IActionResult> CancelTrip(Guid tripId, [FromBody] CancelTripRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var tenantId = _tenantContext.TenantId!.Value;
        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (trip is null)
            return NotFound(new { message = "TrainTrip not found in this tenant." });

        var now = DateTimeOffset.Now;
        trip.Status = TrainTripStatus.Cancelled;
        trip.IsActive = false;
        trip.UpdatedAt = now;

        var activeHolds = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TripId == tripId &&
                !x.IsDeleted &&
                x.Status == TrainSeatHoldStatus.Held)
            .ToListAsync(ct);

        foreach (var hold in activeHolds)
        {
            hold.Status = TrainSeatHoldStatus.Cancelled;
            hold.UpdatedAt = now;
        }

        var opEvent = new TrainOperationalEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TripId = tripId,
            Type = TrainOperationalEventType.CancelTrip,
            Status = TrainOperationalEventStatus.Published,
            OldDepartureAt = trip.DepartureAt,
            OldArrivalAt = trip.ArrivalAt,
            ReasonCode = TrimOrDefault(req.ReasonCode, "CANCELLED", 50),
            ReasonText = TrimOrNull(req.ReasonText, 300),
            InternalNote = TrimOrNull(req.InternalNote, null),
            PublishedAt = now,
            CreatedAt = now
        };

        _db.TrainOperationalEvents.Add(opEvent);
        var notifiedCount = await NotifyAffectedCustomersAsync(trip, "train.cancelled", "Chuyến tàu đã bị hủy", "Chuyến tàu của bạn đã bị hủy. Vui lòng theo dõi yêu cầu hoàn/đổi vé.", opEvent.Id, ct);
        var refundMarkedCount = await MarkAffectedOrdersForRefundAsync(trip.Id, tenantId, now, ct);

        opEvent.NotifiedAt = notifiedCount > 0 ? now : null;
        if (notifiedCount > 0)
            opEvent.Status = TrainOperationalEventStatus.Notified;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true, eventId = opEvent.Id, releasedHolds = activeHolds.Count, notifiedCount, refundMarkedCount });
    }

    [HttpPost("operational-events/{id:guid}/resolve")]
    public async Task<IActionResult> ResolveEvent(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var tenantId = _tenantContext.TenantId!.Value;
        var entity = await _db.TrainOperationalEvents.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (entity is null)
            return NotFound(new { message = "Operational event not found in this tenant." });

        var now = DateTimeOffset.Now;
        entity.Status = TrainOperationalEventStatus.Resolved;
        entity.ResolvedAt = now;
        entity.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<int> NotifyAffectedCustomersAsync(
        TrainTrip trip,
        string category,
        string title,
        string body,
        Guid eventId,
        CancellationToken ct)
    {
        var orders = await _db.CustomerOrders.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == trip.TenantId &&
                x.ProductType == CustomerProductType.Train &&
                x.SourceEntityId == trip.Id &&
                !x.IsDeleted &&
                x.Status != CustomerOrderStatus.Cancelled &&
                x.Status != CustomerOrderStatus.Expired &&
                x.Status != CustomerOrderStatus.Failed)
            .Select(x => new { x.Id, x.UserId })
            .ToListAsync(ct);

        if (orders.Count == 0)
            return 0;

        var now = DateTimeOffset.Now;
        var notifications = orders.Select(order => new CustomerNotification
        {
            Id = Guid.NewGuid(),
            UserId = order.UserId,
            TenantId = trip.TenantId,
            Status = CustomerNotificationStatus.Unread,
            Category = category,
            Title = title,
            Body = body,
            ReferenceType = "TrainOperationalEvent",
            ReferenceId = eventId,
            MetadataJson = $"{{\"tripId\":\"{trip.Id}\",\"orderId\":\"{order.Id}\"}}",
            CreatedAt = now
        }).ToList();

        _db.CustomerNotifications.AddRange(notifications);
        return notifications.Count;
    }

    private async Task<int> MarkAffectedOrdersForRefundAsync(Guid tripId, Guid tenantId, DateTimeOffset now, CancellationToken ct)
    {
        var orders = await _db.CustomerOrders.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.ProductType == CustomerProductType.Train &&
                x.SourceEntityId == tripId &&
                !x.IsDeleted &&
                x.PaymentStatus == CustomerPaymentStatus.Paid &&
                x.RefundStatus == CustomerRefundStatus.None)
            .ToListAsync(ct);

        foreach (var order in orders)
        {
            order.Status = CustomerOrderStatus.RefundRequested;
            order.RefundStatus = CustomerRefundStatus.Requested;
            order.UpdatedAt = now;
        }

        return orders.Count;
    }

    private void EnsureTenantScope()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required.");
    }

    private static string TrimOrDefault(string? value, string fallback, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            trimmed = fallback;
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private static string? TrimOrNull(string? value, int? maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;
        return maxLength.HasValue && trimmed.Length > maxLength.Value
            ? trimmed[..maxLength.Value]
            : trimmed;
    }
}
