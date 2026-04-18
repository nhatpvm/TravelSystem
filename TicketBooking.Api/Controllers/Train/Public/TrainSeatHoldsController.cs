using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TicketBooking.Api.Services.Train;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/train/seat-holds")]
[Authorize]
public sealed class TrainSeatHoldsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public TrainSeatHoldsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public sealed class HoldSeatsRequest
    {
        public Guid TripId { get; set; }
        public Guid FromTripStopTimeId { get; set; }
        public Guid ToTripStopTimeId { get; set; }
        public List<Guid> TrainCarSeatIds { get; set; } = new();
        public string? ClientToken { get; set; }
    }

    public sealed class HoldSeatsResponse
    {
        public bool Ok { get; set; }
        public string HoldToken { get; set; } = "";
        public DateTimeOffset ExpiresAt { get; set; }
        public int HeldCount { get; set; }
        public List<Guid> HeldSeatIds { get; set; } = new();
    }

    [HttpPost]
    public async Task<IActionResult> HoldSeats([FromBody] HoldSeatsRequest req, CancellationToken ct = default)
    {
        if (req.TripId == Guid.Empty) return BadRequest(new { message = "TripId is required." });
        if (req.FromTripStopTimeId == Guid.Empty) return BadRequest(new { message = "FromTripStopTimeId is required." });
        if (req.ToTripStopTimeId == Guid.Empty) return BadRequest(new { message = "ToTripStopTimeId is required." });

        if (req.TrainCarSeatIds is null || req.TrainCarSeatIds.Count == 0)
            return BadRequest(new { message = "TrainCarSeatIds is required." });

        var seatIds = req.TrainCarSeatIds.Where(x => x != Guid.Empty).Distinct().ToList();
        if (seatIds.Count == 0) return BadRequest(new { message = "TrainCarSeatIds is required." });
        if (seatIds.Count > 9) return BadRequest(new { message = "Maximum 9 seats per request." });

        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == req.TripId && !x.IsDeleted, ct);

        if (trip is null)
            return BadRequest(new { message = "TripId is invalid." });

        var tenantId = trip.TenantId;

        if (!trip.IsActive || trip.Status != TrainTripStatus.Published)
            return BadRequest(new { message = "Trip is not available for booking." });

        var fromSt = await _db.TrainTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == req.FromTripStopTimeId && x.TripId == req.TripId && x.TenantId == tenantId && !x.IsDeleted, ct);

        var toSt = await _db.TrainTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == req.ToTripStopTimeId && x.TripId == req.TripId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (fromSt is null || toSt is null)
            return BadRequest(new { message = "From/To TripStopTimeId is invalid for this trip." });

        if (fromSt.StopIndex >= toSt.StopIndex)
            return BadRequest(new { message = "FromStopIndex must be < ToStopIndex." });

        var carIds = await _db.TrainCars.IgnoreQueryFilters()
            .Where(c => c.TenantId == tenantId && c.TripId == req.TripId && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (carIds.Count == 0)
            return BadRequest(new { message = "Trip has no TrainCars yet." });

        var validSeatCount = await _db.TrainCarSeats.IgnoreQueryFilters()
            .CountAsync(s =>
                s.TenantId == tenantId &&
                carIds.Contains(s.CarId) &&
                seatIds.Contains(s.Id) &&
                !s.IsDeleted &&
                s.IsActive, ct);

        if (validSeatCount != seatIds.Count)
            return BadRequest(new { message = "One or more TrainCarSeatIds are invalid for this trip cars." });

        var holdMinutes = await _db.Tenants.IgnoreQueryFilters()
            .Where(x => x.Id == tenantId && !x.IsDeleted)
            .Select(x => x.HoldMinutes)
            .FirstOrDefaultAsync(ct);

        if (holdMinutes <= 0) holdMinutes = 5;

        var userId = GetUserIdOrNull();
        if (!userId.HasValue)
            return Unauthorized(new { message = "UserId claim is required." });

        var now = DateTimeOffset.Now;
        var expiresAt = now.AddMinutes(holdMinutes);
        var clientToken = (req.ClientToken ?? "").Trim();
        var newFrom = fromSt.StopIndex;
        var newTo = toSt.StopIndex;

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        await ReleaseExpiredHoldsForTripAsync(req.TripId, tenantId, now, ct);

        if (!string.IsNullOrWhiteSpace(clientToken))
        {
            var existingTokenQuery = _db.TrainTripSeatHolds.IgnoreQueryFilters()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.TripId == req.TripId &&
                    x.FromTripStopTimeId == req.FromTripStopTimeId &&
                    x.ToTripStopTimeId == req.ToTripStopTimeId &&
                    x.HoldToken == clientToken &&
                    !x.IsDeleted)
                .WhereActiveSeatOccupancy(now);

            if (!User.IsInRole(RoleNames.Admin))
                existingTokenQuery = existingTokenQuery.Where(x => x.UserId == userId.Value);

            var existingToken = await existingTokenQuery
                .Where(x => x.Status == TrainSeatHoldStatus.Held)
                .ToListAsync(ct);

            if (existingToken.Count > 0)
            {
                await tx.CommitAsync(ct);

                return Ok(new HoldSeatsResponse
                {
                    Ok = true,
                    HoldToken = clientToken,
                    ExpiresAt = existingToken.Min(x => x.HoldExpiresAt),
                    HeldCount = existingToken.Count,
                    HeldSeatIds = existingToken.Select(x => x.TrainCarSeatId).Distinct().ToList()
                });
            }
        }

        var conflicting = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TripId == req.TripId &&
                seatIds.Contains(x.TrainCarSeatId) &&
                !x.IsDeleted)
            .WhereActiveSeatOccupancy(now)
            .WhereOverlappingSegment(newFrom, newTo)
            .Select(x => x.TrainCarSeatId)
            .Distinct()
            .ToListAsync(ct);

        if (conflicting.Count > 0)
        {
            await tx.CommitAsync(ct);

            return Conflict(new
            {
                message = "Some seats are already occupied.",
                conflictingSeatIds = conflicting
            });
        }

        var token = !string.IsNullOrWhiteSpace(clientToken) ? clientToken : Guid.NewGuid().ToString("N");

        var rows = seatIds.Select(seatId => new TrainTripSeatHold
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TripId = req.TripId,
            TrainCarSeatId = seatId,
            FromTripStopTimeId = req.FromTripStopTimeId,
            ToTripStopTimeId = req.ToTripStopTimeId,
            FromStopIndex = newFrom,
            ToStopIndex = newTo,
            Status = TrainSeatHoldStatus.Held,
            UserId = userId,
            BookingId = null,
            HoldToken = token,
            HoldExpiresAt = expiresAt,
            IsDeleted = false,
            CreatedAt = now
        }).ToList();

        _db.TrainTripSeatHolds.AddRange(rows);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Ok(new HoldSeatsResponse
        {
            Ok = true,
            HoldToken = token,
            ExpiresAt = expiresAt,
            HeldCount = rows.Count,
            HeldSeatIds = rows.Select(x => x.TrainCarSeatId).ToList()
        });
    }

    [HttpDelete("{holdToken}")]
    public async Task<IActionResult> ReleaseByToken(string holdToken, CancellationToken ct = default)
    {
        holdToken = (holdToken ?? "").Trim();
        if (string.IsNullOrWhiteSpace(holdToken))
            return BadRequest(new { message = "holdToken is required." });

        var now = DateTimeOffset.Now;
        var userId = GetUserIdOrNull();
        var isAdmin = User.IsInRole(RoleNames.Admin);

        if (!isAdmin && !userId.HasValue)
            return Unauthorized(new { message = "UserId claim is required." });

        var probeQuery = _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x => x.HoldToken == holdToken && !x.IsDeleted);

        if (!isAdmin)
            probeQuery = probeQuery.Where(x => x.UserId == userId);

        var probe = await probeQuery
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (probe is null)
            return NotFound(new { message = "Hold token not found." });

        var holds = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == probe.TenantId &&
                x.TripId == probe.TripId &&
                x.HoldToken == holdToken &&
                !x.IsDeleted &&
                x.Status == TrainSeatHoldStatus.Held)
            .ToListAsync(ct);

        if (!isAdmin)
            holds = holds.Where(x => x.UserId == userId).ToList();

        if (holds.Count == 0)
            return NotFound(new { message = "Hold token not found." });

        foreach (var h in holds)
        {
            h.Status = TrainSeatHoldStatus.Cancelled;
            h.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, released = holds.Count });
    }

    [HttpGet("trip/{tripId:guid}")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> ListByTrip(Guid tripId, [FromQuery] bool includeExpired = false, CancellationToken ct = default)
    {
        if (tripId == Guid.Empty) return BadRequest(new { message = "tripId is required." });

        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && !x.IsDeleted, ct);

        if (trip is null)
            return NotFound(new { message = "TrainTrip not found." });

        var tenantId = trip.TenantId;
        var now = DateTimeOffset.Now;

        var query = _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TripId == tripId && !x.IsDeleted);

        if (!includeExpired)
            query = query.WhereActiveSeatOccupancy(now);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(500)
            .Select(x => new
            {
                x.Id,
                x.TripId,
                x.TrainCarSeatId,
                x.FromStopIndex,
                x.ToStopIndex,
                x.Status,
                x.UserId,
                x.HoldToken,
                x.HoldExpiresAt,
                x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    private async Task ReleaseExpiredHoldsForTripAsync(Guid tripId, Guid tenantId, DateTimeOffset now, CancellationToken ct)
    {
        var expired = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TripId == tripId &&
                !x.IsDeleted &&
                x.Status == TrainSeatHoldStatus.Held &&
                x.HoldExpiresAt <= now)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        foreach (var h in expired)
        {
            h.Status = TrainSeatHoldStatus.Expired;
            h.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    private Guid? GetUserIdOrNull()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(sub, out var id)) return id;
        return null;
    }
}
