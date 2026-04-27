using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Train;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlvt/train/seat-blocks")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLVT}")]
public sealed class QlVtTrainSeatBlocksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlVtTrainSeatBlocksController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public sealed class CreateSeatBlockRequest
    {
        public Guid TripId { get; set; }
        public Guid FromTripStopTimeId { get; set; }
        public Guid ToTripStopTimeId { get; set; }
        public List<Guid> TrainCarSeatIds { get; set; } = new();
        public TrainSeatBlockReason Reason { get; set; } = TrainSeatBlockReason.Maintenance;
        public string? ReasonText { get; set; }
        public string? Note { get; set; }
        public DateTimeOffset? StartsAt { get; set; }
        public DateTimeOffset? EndsAt { get; set; }
    }

    [HttpGet("trip/{tripId:guid}")]
    public async Task<IActionResult> ListByTrip(
        Guid tripId,
        [FromQuery] bool includeReleased = false,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        var tenantId = _tenantContext.TenantId!.Value;
        var tripExists = await _db.TrainTrips.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == tripId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (!tripExists)
            return NotFound(new { message = "TrainTrip not found in this tenant." });

        var query = _db.TrainSeatBlocks.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TripId == tripId && !x.IsDeleted);

        if (!includeReleased)
            query = query.Where(x => x.Status == TrainSeatBlockStatus.Active);

        var blocks = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

        var seatIds = blocks.Select(x => x.TrainCarSeatId).Distinct().ToList();
        var seats = seatIds.Count == 0
            ? new Dictionary<Guid, SeatLookup>()
            : await _db.TrainCarSeats.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && seatIds.Contains(x.Id))
                .Join(
                    _db.TrainCars.IgnoreQueryFilters().Where(c => c.TenantId == tenantId),
                    seat => seat.CarId,
                    car => car.Id,
                    (seat, car) => new SeatLookup
                    {
                        Id = seat.Id,
                        SeatNumber = seat.SeatNumber,
                        SeatClass = seat.SeatClass,
                        CarId = car.Id,
                        CarNumber = car.CarNumber
                    })
                .ToDictionaryAsync(x => x.Id, ct);

        var items = blocks.Select(x =>
        {
            seats.TryGetValue(x.TrainCarSeatId, out var seat);
            return new
            {
                x.Id,
                x.TripId,
                x.TrainCarSeatId,
                seat?.SeatNumber,
                seat?.SeatClass,
                seat?.CarId,
                seat?.CarNumber,
                x.FromTripStopTimeId,
                x.ToTripStopTimeId,
                x.FromStopIndex,
                x.ToStopIndex,
                x.Reason,
                x.Status,
                x.ReasonText,
                x.Note,
                x.StartsAt,
                x.EndsAt,
                x.ReleasedAt,
                x.CreatedAt,
                x.UpdatedAt
            };
        }).ToList();

        return Ok(new { items });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSeatBlockRequest req, CancellationToken ct = default)
    {
        EnsureTenantScope();
        if (req.TripId == Guid.Empty) return BadRequest(new { message = "TripId is required." });
        if (req.FromTripStopTimeId == Guid.Empty) return BadRequest(new { message = "FromTripStopTimeId is required." });
        if (req.ToTripStopTimeId == Guid.Empty) return BadRequest(new { message = "ToTripStopTimeId is required." });

        var seatIds = (req.TrainCarSeatIds ?? new List<Guid>())
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (seatIds.Count == 0)
            return BadRequest(new { message = "TrainCarSeatIds is required." });

        var tenantId = _tenantContext.TenantId!.Value;
        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == req.TripId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (trip is null)
            return NotFound(new { message = "TrainTrip not found in this tenant." });

        var fromSt = await _db.TrainTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == req.FromTripStopTimeId && x.TripId == req.TripId && x.TenantId == tenantId && !x.IsDeleted, ct);
        var toSt = await _db.TrainTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == req.ToTripStopTimeId && x.TripId == req.TripId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (fromSt is null || toSt is null)
            return BadRequest(new { message = "From/To TripStopTimeId is invalid for this trip." });

        if (fromSt.StopIndex >= toSt.StopIndex)
            return BadRequest(new { message = "FromStopIndex must be < ToStopIndex." });

        var validSeatCount = await _db.TrainCarSeats.IgnoreQueryFilters()
            .Join(
                _db.TrainCars.IgnoreQueryFilters().Where(c => c.TenantId == tenantId && c.TripId == req.TripId && !c.IsDeleted),
                seat => seat.CarId,
                car => car.Id,
                (seat, car) => seat)
            .CountAsync(x => x.TenantId == tenantId && seatIds.Contains(x.Id) && !x.IsDeleted, ct);

        if (validSeatCount != seatIds.Count)
            return BadRequest(new { message = "One or more TrainCarSeatIds are invalid for this trip." });

        var now = DateTimeOffset.Now;
        var occupied = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TripId == req.TripId &&
                seatIds.Contains(x.TrainCarSeatId) &&
                !x.IsDeleted)
            .WhereActiveSeatOccupancy(now)
            .WhereOverlappingSegment(fromSt.StopIndex, toSt.StopIndex)
            .Select(x => x.TrainCarSeatId)
            .Distinct()
            .ToListAsync(ct);

        if (occupied.Count > 0)
            return Conflict(new { message = "Cannot block seats that are already held or booked.", occupiedSeatIds = occupied });

        var alreadyBlocked = await _db.TrainSeatBlocks.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TripId == req.TripId &&
                seatIds.Contains(x.TrainCarSeatId))
            .WhereActiveSeatBlocks()
            .WhereOverlappingSegment(fromSt.StopIndex, toSt.StopIndex)
            .Select(x => x.TrainCarSeatId)
            .Distinct()
            .ToListAsync(ct);

        if (alreadyBlocked.Count > 0)
            return Conflict(new { message = "One or more seats are already blocked on this segment.", blockedSeatIds = alreadyBlocked });

        var actorId = GetUserIdOrNull();
        var rows = seatIds.Select(seatId => new TrainSeatBlock
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TripId = req.TripId,
            TrainCarSeatId = seatId,
            FromTripStopTimeId = req.FromTripStopTimeId,
            ToTripStopTimeId = req.ToTripStopTimeId,
            FromStopIndex = fromSt.StopIndex,
            ToStopIndex = toSt.StopIndex,
            Reason = req.Reason,
            Status = TrainSeatBlockStatus.Active,
            ReasonText = TrimOrNull(req.ReasonText, 200),
            Note = TrimOrNull(req.Note, null),
            StartsAt = req.StartsAt,
            EndsAt = req.EndsAt,
            CreatedAt = now,
            CreatedByUserId = actorId
        }).ToList();

        _db.TrainSeatBlocks.AddRange(rows);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, blockedCount = rows.Count, itemIds = rows.Select(x => x.Id).ToList() });
    }

    [HttpPost("{id:guid}/release")]
    public async Task<IActionResult> Release(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var tenantId = _tenantContext.TenantId!.Value;
        var entity = await _db.TrainSeatBlocks.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (entity is null)
            return NotFound(new { message = "Seat block not found in this tenant." });

        var now = DateTimeOffset.Now;
        entity.Status = TrainSeatBlockStatus.Released;
        entity.ReleasedAt = now;
        entity.ReleasedByUserId = GetUserIdOrNull();
        entity.UpdatedAt = now;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        EnsureTenantScope();

        var tenantId = _tenantContext.TenantId!.Value;
        var entity = await _db.TrainSeatBlocks
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Seat block not found in this tenant." });

        _db.TrainSeatBlocks.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private void EnsureTenantScope()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required.");
    }

    private Guid? GetUserIdOrNull()
    {
        var sub = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
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

    private sealed class SeatLookup
    {
        public Guid Id { get; init; }
        public Guid CarId { get; init; }
        public string CarNumber { get; init; } = "";
        public string SeatNumber { get; init; } = "";
        public string? SeatClass { get; init; }
    }
}
