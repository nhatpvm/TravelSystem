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
[Route("api/v{version:apiVersion}/qlvt/train/boarding")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLVT}")]
public sealed class QlVtTrainBoardingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlVtTrainBoardingController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public sealed class ScanRequest
    {
        public string Code { get; set; } = "";
        public Guid? TripId { get; set; }
        public string? PlatformCode { get; set; }
        public string? GateCode { get; set; }
        public string? DeviceCode { get; set; }
        public string? Note { get; set; }
    }

    public sealed class RejectRequest
    {
        public string? Reason { get; set; }
    }

    [HttpGet("trip/{tripId:guid}")]
    public async Task<IActionResult> ListByTrip(Guid tripId, [FromQuery] TrainTicketCheckInStatus? status = null, CancellationToken ct = default)
    {
        EnsureTenantScope();
        var tenantId = _tenantContext.TenantId!.Value;
        await EnsureTripExistsAsync(tenantId, tripId, ct);

        var query = _db.TrainTicketCheckIns.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TripId == tripId && !x.IsDeleted);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var items = await query
            .OrderBy(x => x.CarNumber)
            .ThenBy(x => x.SeatNumber)
            .ThenBy(x => x.PassengerName)
            .Take(1000)
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpPost("scan")]
    public async Task<IActionResult> Scan([FromBody] ScanRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        var tenantId = _tenantContext.TenantId!.Value;
        var rows = await EnsureCheckInsAsync(tenantId, req.Code, req.TripId, ct);
        return Ok(new { items = rows });
    }

    [HttpPost("{id:guid}/check-in")]
    public async Task<IActionResult> CheckIn(Guid id, [FromBody] ScanRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        var entity = await GetCheckInAsync(id, ct);
        if (entity.Status == TrainTicketCheckInStatus.Boarded)
            return Conflict(new { message = "Ticket is already boarded." });

        var now = DateTimeOffset.Now;
        entity.Status = TrainTicketCheckInStatus.CheckedIn;
        entity.CheckedInAt = now;
        entity.CheckedInByUserId = GetUserIdOrNull();
        entity.PlatformCode = TrimOrNull(req.PlatformCode, 30);
        entity.GateCode = TrimOrNull(req.GateCode, 30);
        entity.DeviceCode = TrimOrNull(req.DeviceCode, 80);
        entity.Note = TrimOrNull(req.Note, null);
        entity.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpPost("{id:guid}/board")]
    public async Task<IActionResult> Board(Guid id, [FromBody] ScanRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        var entity = await GetCheckInAsync(id, ct);
        if (entity.Status == TrainTicketCheckInStatus.Rejected || entity.Status == TrainTicketCheckInStatus.Cancelled)
            return Conflict(new { message = "Ticket cannot be boarded from current status." });

        var now = DateTimeOffset.Now;
        entity.Status = TrainTicketCheckInStatus.Boarded;
        entity.CheckedInAt ??= now;
        entity.CheckedInByUserId ??= GetUserIdOrNull();
        entity.BoardedAt = now;
        entity.BoardedByUserId = GetUserIdOrNull();
        entity.PlatformCode = TrimOrNull(req.PlatformCode, 30) ?? entity.PlatformCode;
        entity.GateCode = TrimOrNull(req.GateCode, 30) ?? entity.GateCode;
        entity.DeviceCode = TrimOrNull(req.DeviceCode, 80) ?? entity.DeviceCode;
        entity.Note = TrimOrNull(req.Note, null) ?? entity.Note;
        entity.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        var entity = await GetCheckInAsync(id, ct);
        if (entity.Status == TrainTicketCheckInStatus.Boarded)
            return Conflict(new { message = "Boarded ticket cannot be rejected." });

        var now = DateTimeOffset.Now;
        entity.Status = TrainTicketCheckInStatus.Rejected;
        entity.RejectedAt = now;
        entity.RejectReason = TrimOrNull(req.Reason, 300);
        entity.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);
        return Ok(entity);
    }

    private async Task<List<TrainTicketCheckIn>> EnsureCheckInsAsync(Guid tenantId, string code, Guid? tripId, CancellationToken ct)
    {
        var normalized = (code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw new InvalidOperationException("Ticket code or order code is required.");

        var ticket = await _db.CustomerTickets.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                !x.IsDeleted &&
                x.Status == CustomerTicketStatus.Issued &&
                (x.TicketCode == normalized || x.OrderId.ToString() == normalized), ct);

        CustomerOrder? order = null;
        if (ticket is not null)
        {
            order = await _db.CustomerOrders.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == ticket.OrderId && x.TenantId == tenantId && !x.IsDeleted, ct);
        }
        else
        {
            order = await _db.CustomerOrders.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId &&
                    !x.IsDeleted &&
                    x.ProductType == CustomerProductType.Train &&
                    x.OrderCode == normalized, ct);

            if (order is not null)
            {
                ticket = await _db.CustomerTickets.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(x => x.OrderId == order.Id && x.TenantId == tenantId && !x.IsDeleted, ct);
            }
        }

        if (order is null || ticket is null)
            throw new KeyNotFoundException("Issued train ticket not found.");

        if (order.ProductType != CustomerProductType.Train || order.SourceEntityId is null)
            throw new InvalidOperationException("Ticket is not a train ticket.");

        if (tripId.HasValue && tripId.Value != Guid.Empty && order.SourceEntityId.Value != tripId.Value)
            throw new InvalidOperationException("Ticket does not belong to selected trip.");

        var existing = await _db.TrainTicketCheckIns.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TicketId == ticket.Id && !x.IsDeleted)
            .ToListAsync(ct);

        if (existing.Count > 0)
            return existing;

        var metadata = DeserializeTrainMetadata(order.MetadataJson);
        var seatIds = metadata?.TrainCarSeatIds ?? new List<Guid>();
        var seats = seatIds.Count == 0
            ? new List<SeatProjection>()
            : await _db.TrainCarSeats.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && seatIds.Contains(x.Id) && !x.IsDeleted)
                .Join(
                    _db.TrainCars.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && !x.IsDeleted),
                    seat => seat.CarId,
                    car => car.Id,
                    (seat, car) => new SeatProjection
                    {
                        SeatId = seat.Id,
                        SeatNumber = seat.SeatNumber,
                        CarNumber = car.CarNumber
                    })
                .ToListAsync(ct);

        if (seats.Count == 0)
        {
            seats.Add(new SeatProjection { SeatNumber = "", CarNumber = "" });
        }

        var now = DateTimeOffset.Now;
        var rows = seats.Select((seat, index) =>
        {
            var passenger = metadata?.Passengers.ElementAtOrDefault(index) ?? metadata?.Passengers.FirstOrDefault();
            return new TrainTicketCheckIn
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TripId = order.SourceEntityId.Value,
                OrderId = order.Id,
                TicketId = ticket.Id,
                TrainCarSeatId = seat.SeatId == Guid.Empty ? null : seat.SeatId,
                TicketCode = ticket.TicketCode,
                Status = TrainTicketCheckInStatus.Pending,
                CarNumber = seat.CarNumber,
                SeatNumber = seat.SeatNumber,
                PassengerName = passenger?.FullName ?? order.ContactFullName,
                DocumentNumber = passenger?.IdNumber ?? passenger?.PassportNumber,
                CreatedAt = now
            };
        }).ToList();

        _db.TrainTicketCheckIns.AddRange(rows);
        await _db.SaveChangesAsync(ct);
        return rows;
    }

    private async Task<TrainTicketCheckIn> GetCheckInAsync(Guid id, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId!.Value;
        return await _db.TrainTicketCheckIns.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Check-in row not found.");
    }

    private async Task EnsureTripExistsAsync(Guid tenantId, Guid tripId, CancellationToken ct)
    {
        var exists = await _db.TrainTrips.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == tripId && x.TenantId == tenantId && !x.IsDeleted, ct);
        if (!exists)
            throw new KeyNotFoundException("TrainTrip not found in this tenant.");
    }

    private static TrainOrderMetadataForOps? DeserializeTrainMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<TrainOrderMetadataForOps>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
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
        if (string.IsNullOrWhiteSpace(trimmed))
            return null;
        return maxLength.HasValue && trimmed.Length > maxLength.Value ? trimmed[..maxLength.Value] : trimmed;
    }

    private sealed class SeatProjection
    {
        public Guid SeatId { get; init; }
        public string SeatNumber { get; init; } = "";
        public string CarNumber { get; init; } = "";
    }

    private sealed class TrainOrderMetadataForOps
    {
        public List<Guid> TrainCarSeatIds { get; set; } = new();
        public List<CustomerPassengerInput> Passengers { get; set; } = new();
    }
}
