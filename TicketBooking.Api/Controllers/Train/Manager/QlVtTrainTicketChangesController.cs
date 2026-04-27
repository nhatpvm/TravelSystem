using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TicketBooking.Api.Services.Commerce;
using TicketBooking.Domain.Commerce;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlvt/train/ticket-changes")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLVT}")]
public sealed class QlVtTrainTicketChangesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlVtTrainTicketChangesController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public sealed class CreateChangeRequest
    {
        public string OrderCode { get; set; } = "";
        public string NewHoldToken { get; set; } = "";
        public decimal ChangeFeeAmount { get; set; }
        public string? ReasonText { get; set; }
    }

    public sealed class ReviewRequest
    {
        public string? StaffNote { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] TrainTicketChangeStatus? status = null, CancellationToken ct = default)
    {
        EnsureTenantScope();
        var tenantId = _tenantContext.TenantId!.Value;
        var query = _db.TrainTicketChangeRequests.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(300)
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateChangeRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        var tenantId = _tenantContext.TenantId!.Value;
        var orderCode = (req.OrderCode ?? string.Empty).Trim();
        var newHoldToken = (req.NewHoldToken ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(orderCode)) return BadRequest(new { message = "OrderCode is required." });
        if (string.IsNullOrWhiteSpace(newHoldToken)) return BadRequest(new { message = "NewHoldToken is required." });

        var order = await _db.CustomerOrders.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.OrderCode == orderCode &&
                x.ProductType == CustomerProductType.Train &&
                !x.IsDeleted, ct);

        if (order is null)
            return NotFound(new { message = "Train order not found in this tenant." });

        if (order.SourceEntityId is null)
            return BadRequest(new { message = "Original train trip is missing." });

        if (order.TicketStatus != CustomerTicketStatus.Issued)
            return Conflict(new { message = "Only issued train tickets can be exchanged." });

        var newHolds = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.HoldToken == newHoldToken &&
                !x.IsDeleted &&
                x.Status == TrainSeatHoldStatus.Held)
            .ToListAsync(ct);

        if (newHolds.Count == 0)
            return BadRequest(new { message = "NewHoldToken does not contain active train holds." });

        if (newHolds.Any(x => x.HoldExpiresAt <= DateTimeOffset.Now))
            return Conflict(new { message = "New hold has expired." });

        var newTripId = newHolds[0].TripId;
        if (newHolds.Any(x => x.TripId != newTripId))
            return BadRequest(new { message = "NewHoldToken spans multiple trips." });

        var newAmount = await CalculateHeldAmountAsync(tenantId, newHolds, ct);
        var fareDifference = newAmount - order.PayableAmount;
        var payableDifference = Math.Max(0m, fareDifference + req.ChangeFeeAmount);
        var now = DateTimeOffset.Now;

        var entity = new TrainTicketChangeRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OriginalOrderId = order.Id,
            OriginalTripId = order.SourceEntityId.Value,
            NewTripId = newTripId,
            NewHoldToken = newHoldToken,
            Status = payableDifference > 0 ? TrainTicketChangeStatus.PendingPayment : TrainTicketChangeStatus.Quoted,
            CurrencyCode = order.CurrencyCode,
            OriginalAmount = order.PayableAmount,
            NewAmount = newAmount,
            ChangeFeeAmount = req.ChangeFeeAmount,
            FareDifferenceAmount = fareDifference,
            PayableDifferenceAmount = payableDifference,
            ReasonText = TrimOrNull(req.ReasonText, 300),
            QuotedAt = now,
            CreatedAt = now
        };

        _db.TrainTicketChangeRequests.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ReviewRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        var tenantId = _tenantContext.TenantId!.Value;
        var entity = await _db.TrainTicketChangeRequests.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (entity is null)
            return NotFound(new { message = "Ticket change request not found." });

        if (entity.PayableDifferenceAmount > 0 && entity.Status == TrainTicketChangeStatus.PendingPayment)
            return Conflict(new { message = "This change requires payment before approval." });

        var order = await _db.CustomerOrders.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == entity.OriginalOrderId && x.TenantId == tenantId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Original order not found.");

        var oldHolds = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.BookingId == order.Id &&
                x.TripId == entity.OriginalTripId &&
                !x.IsDeleted &&
                x.Status == TrainSeatHoldStatus.Confirmed)
            .ToListAsync(ct);

        var newHolds = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.HoldToken == entity.NewHoldToken &&
                x.TripId == entity.NewTripId &&
                !x.IsDeleted &&
                x.Status == TrainSeatHoldStatus.Held)
            .ToListAsync(ct);

        if (newHolds.Count == 0)
            return Conflict(new { message = "New hold is no longer available." });

        var newSeatIds = newHolds.Select(x => x.TrainCarSeatId).Distinct().ToList();
        var seatNumbers = await _db.TrainCarSeats.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && newSeatIds.Contains(x.Id) && !x.IsDeleted)
            .OrderBy(x => x.SeatNumber)
            .Select(x => x.SeatNumber)
            .ToListAsync(ct);

        var now = DateTimeOffset.Now;
        foreach (var hold in oldHolds)
        {
            hold.Status = TrainSeatHoldStatus.Cancelled;
            hold.UpdatedAt = now;
        }

        foreach (var hold in newHolds)
        {
            hold.Status = TrainSeatHoldStatus.Confirmed;
            hold.BookingId = order.Id;
            hold.UpdatedAt = now;
        }

        order.SourceEntityId = entity.NewTripId;
        order.MetadataJson = BuildUpdatedTrainMetadata(order.MetadataJson, entity.NewHoldToken, newHolds, seatNumbers);
        order.UpdatedAt = now;

        entity.Status = TrainTicketChangeStatus.Approved;
        entity.ApprovedAt = now;
        entity.ApprovedByUserId = GetUserIdOrNull();
        entity.StaffNote = TrimOrNull(req.StaffNote, null);
        entity.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpPost("{id:guid}/mark-paid")]
    public async Task<IActionResult> MarkPaid(Guid id, [FromBody] ReviewRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        var tenantId = _tenantContext.TenantId!.Value;
        var entity = await _db.TrainTicketChangeRequests.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (entity is null)
            return NotFound(new { message = "Ticket change request not found." });

        if (entity.Status != TrainTicketChangeStatus.PendingPayment)
            return Conflict(new { message = "Only pending payment change requests can be marked as paid." });

        if (entity.PayableDifferenceAmount <= 0)
            return Conflict(new { message = "This change request does not require an additional payment." });

        var note = TrimOrNull(req.StaffNote, null);
        entity.Status = TrainTicketChangeStatus.Quoted;
        entity.StaffNote = string.IsNullOrWhiteSpace(note)
            ? "Đã ghi nhận thanh toán chênh lệch đổi vé."
            : note;
        entity.UpdatedAt = DateTimeOffset.Now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        var tenantId = _tenantContext.TenantId!.Value;
        var entity = await _db.TrainTicketChangeRequests.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (entity is null)
            return NotFound(new { message = "Ticket change request not found." });

        var now = DateTimeOffset.Now;
        entity.Status = TrainTicketChangeStatus.Rejected;
        entity.RejectedAt = now;
        entity.RejectedByUserId = GetUserIdOrNull();
        entity.StaffNote = TrimOrNull(req.StaffNote, null);
        entity.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    private async Task<decimal> CalculateHeldAmountAsync(Guid tenantId, List<TrainTripSeatHold> holds, CancellationToken ct)
    {
        var first = holds[0];
        var now = DateTimeOffset.Now;
        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.Id == first.TripId && !x.IsDeleted)
            .Select(x => new { x.Id, x.RouteId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Train trip not found.");

        var segment = await _db.TrainTripSegmentPrices.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TripId == first.TripId &&
                x.FromTripStopTimeId == first.FromTripStopTimeId &&
                x.ToTripStopTimeId == first.ToTripStopTimeId &&
                x.IsActive &&
                !x.IsDeleted)
            .Select(x => (decimal?)x.TotalPrice)
            .FirstOrDefaultAsync(ct);

        if (!segment.HasValue)
            throw new InvalidOperationException("Chuyến tàu chưa có giá cho chặng đã chọn.");

        var seatIds = holds.Select(x => x.TrainCarSeatId).Distinct().ToList();
        var seats = await _db.TrainCarSeats.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && seatIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.SeatNumber,
                x.SeatType,
                x.SeatClass,
                x.PriceModifier
            })
            .ToListAsync(ct);

        var fareClasses = await _db.TrainFareClasses.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
            .Select(x => new TrainFareClassPrice
            {
                Id = x.Id,
                Code = x.Code,
                SeatType = x.SeatType,
                DefaultModifier = x.DefaultModifier
            })
            .ToListAsync(ct);

        var fareClassIds = fareClasses.Select(x => x.Id).ToList();
        var fareRules = fareClassIds.Count == 0
            ? new List<TrainFareRulePrice>()
            : await _db.TrainFareRules.IgnoreQueryFilters()
                .Where(x =>
                    x.TenantId == tenantId &&
                    fareClassIds.Contains(x.FareClassId) &&
                    x.FromStopIndex == first.FromStopIndex &&
                    x.ToStopIndex == first.ToStopIndex &&
                    x.IsActive &&
                    !x.IsDeleted &&
                    (!x.EffectiveFrom.HasValue || x.EffectiveFrom <= now) &&
                    (!x.EffectiveTo.HasValue || x.EffectiveTo >= now) &&
                    (x.TripId == trip.Id || x.RouteId == trip.RouteId))
                .Select(x => new TrainFareRulePrice
                {
                    FareClassId = x.FareClassId,
                    TripId = x.TripId,
                    TotalPrice = x.TotalPrice
                })
                .ToListAsync(ct);

        return seats.Sum(x =>
        {
            var fareClass = ResolveTrainFareClass(fareClasses, x.SeatClass, x.SeatType);
            var fareRule = fareClass is null
                ? null
                : fareRules
                    .Where(rule => rule.FareClassId == fareClass.Id)
                    .OrderByDescending(rule => rule.TripId == trip.Id)
                    .FirstOrDefault();

            return fareRule?.TotalPrice ?? segment.Value + (x.PriceModifier ?? fareClass?.DefaultModifier ?? 0m);
        });
    }

    private static string BuildUpdatedTrainMetadata(
        string? oldJson,
        string holdToken,
        List<TrainTripSeatHold> newHolds,
        List<string> seatNumbers)
    {
        var metadata = DeserializeTrainMetadata(oldJson) ?? new TrainChangeMetadata();
        metadata.TripId = newHolds[0].TripId;
        metadata.HoldToken = holdToken;
        metadata.FromTripStopTimeId = newHolds[0].FromTripStopTimeId;
        metadata.ToTripStopTimeId = newHolds[0].ToTripStopTimeId;
        metadata.TrainCarSeatIds = newHolds.Select(x => x.TrainCarSeatId).Distinct().ToList();
        metadata.SeatNumbers = seatNumbers;
        return JsonSerializer.Serialize(metadata);
    }

    private static TrainChangeMetadata? DeserializeTrainMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<TrainChangeMetadata>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void EnsureTenantScope()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required.");
    }

    private Guid? GetUserIdOrNull()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static string? TrimOrNull(string? value, int? maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return null;
        return maxLength.HasValue && trimmed.Length > maxLength.Value ? trimmed[..maxLength.Value] : trimmed;
    }

    private static TrainFareClassPrice? ResolveTrainFareClass(
        List<TrainFareClassPrice> fareClasses,
        string? seatClass,
        TrainSeatType seatType)
    {
        return fareClasses.FirstOrDefault(x => !string.IsNullOrWhiteSpace(seatClass) &&
                                               string.Equals(x.Code, seatClass.Trim(), StringComparison.OrdinalIgnoreCase)) ??
               fareClasses.FirstOrDefault(x => x.SeatType == seatType);
    }

    private sealed class TrainChangeMetadata
    {
        public Guid TripId { get; set; }
        public string HoldToken { get; set; } = "";
        public Guid FromTripStopTimeId { get; set; }
        public Guid ToTripStopTimeId { get; set; }
        public List<Guid> TrainCarSeatIds { get; set; } = new();
        public List<string> SeatNumbers { get; set; } = new();
        public List<CustomerPassengerInput> Passengers { get; set; } = new();
    }

    private sealed class TrainFareClassPrice
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public TrainSeatType SeatType { get; set; }
        public decimal DefaultModifier { get; set; }
    }

    private sealed class TrainFareRulePrice
    {
        public Guid FareClassId { get; set; }
        public Guid? TripId { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
