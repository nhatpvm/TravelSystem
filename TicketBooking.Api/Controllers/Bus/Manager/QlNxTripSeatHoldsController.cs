// FILE #286: TicketBooking.Api/Controllers/QlNxTripSeatHoldsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using TicketBooking.Api.Services.Bus;
using TicketBooking.Domain.Bus;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlnx/bus/seat-holds")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX}")]
public sealed class QlNxTripSeatHoldsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlNxTripSeatHoldsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public sealed class HoldSeatsRequest
    {
        public Guid TripId { get; set; }
        public Guid FromTripStopTimeId { get; set; }
        public Guid ToTripStopTimeId { get; set; }
        public List<Guid> SeatIds { get; set; } = new();
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
        EnsureTenantScope();

        if (req.TripId == Guid.Empty)
            return BadRequest(new { message = "TripId is required." });

        if (req.FromTripStopTimeId == Guid.Empty)
            return BadRequest(new { message = "FromTripStopTimeId is required." });

        if (req.ToTripStopTimeId == Guid.Empty)
            return BadRequest(new { message = "ToTripStopTimeId is required." });

        if (req.SeatIds is null || req.SeatIds.Count == 0)
            return BadRequest(new { message = "SeatIds is required." });

        var seatIds = req.SeatIds.Where(x => x != Guid.Empty).Distinct().ToList();
        if (seatIds.Count == 0)
            return BadRequest(new { message = "SeatIds is required." });

        if (seatIds.Count > 9)
            return BadRequest(new { message = "Maximum 9 seats per request." });


        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == req.TripId &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted, ct);

        if (trip is null)
            return BadRequest(new { message = "TripId is invalid for this tenant." });

        var fromSt = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == req.FromTripStopTimeId &&
                x.TripId == req.TripId &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted, ct);

        var toSt = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == req.ToTripStopTimeId &&
                x.TripId == req.TripId &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted, ct);

        if (fromSt is null || toSt is null)
            return BadRequest(new { message = "From/To TripStopTimeId is invalid for this trip." });

        if (fromSt.StopIndex >= toSt.StopIndex)
            return BadRequest(new { message = "FromStopIndex must be < ToStopIndex." });

        var vehicle = await _db.Vehicles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == trip.VehicleId &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted, ct);

        if (vehicle is null || vehicle.SeatMapId is null)
            return BadRequest(new { message = "Trip vehicle or seatmap not found." });

        var validSeatsCount = await _db.Seats.IgnoreQueryFilters()
            .CountAsync(x =>
                x.TenantId == _tenantContext.TenantId &&
                x.SeatMapId == vehicle.SeatMapId &&
                seatIds.Contains(x.Id) &&
                !x.IsDeleted &&
                x.IsActive, ct);

        if (validSeatsCount != seatIds.Count)
            return BadRequest(new { message = "One or more SeatIds are invalid for this trip seat map." });

        var holdMinutes = await _db.Tenants.IgnoreQueryFilters()
            .Where(x => x.Id == _tenantContext.TenantId && !x.IsDeleted)
            .Select(x => x.HoldMinutes)
            .FirstOrDefaultAsync(ct);

        if (holdMinutes <= 0)
            holdMinutes = 5;

        var now = DateTimeOffset.Now;
        var expiresAt = now.AddMinutes(holdMinutes);

        var userId = GetUserIdOrNull();
        if (!userId.HasValue)
            return Unauthorized(new { message = "UserId claim is required." });

        var clientToken = (req.ClientToken ?? "").Trim();
        var newFrom = fromSt.StopIndex;
        var newTo = toSt.StopIndex;

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        await ReleaseExpiredHoldsForTripAsync(req.TripId, now, ct);

        if (!string.IsNullOrWhiteSpace(clientToken))
        {
            var existingTokenQuery = _db.BusTripSeatHolds.IgnoreQueryFilters()
                .Where(x =>
                    x.TenantId == _tenantContext.TenantId &&
                    x.TripId == req.TripId &&
                    x.FromTripStopTimeId == req.FromTripStopTimeId &&
                    x.ToTripStopTimeId == req.ToTripStopTimeId &&
                    x.HoldToken == clientToken &&
                    !x.IsDeleted)
                .WhereActiveSeatOccupancy(now);

            if (!User.IsInRole(RoleNames.Admin))
                existingTokenQuery = existingTokenQuery.Where(x => x.UserId == userId.Value);

            var existingToken = await existingTokenQuery
                .Where(x => x.Status == SeatHoldStatus.Held)
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
                    HeldSeatIds = existingToken.Select(x => x.SeatId).Distinct().ToList()
                });
            }
        }

        var conflicting = await _db.BusTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == _tenantContext.TenantId &&
                x.TripId == req.TripId &&
                seatIds.Contains(x.SeatId) &&
                !x.IsDeleted)
            .WhereActiveSeatOccupancy(now)
            .WhereOverlappingSegment(newFrom, newTo)
            .Select(x => x.SeatId)
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

        var rows = seatIds.Select(seatId => new TripSeatHold
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId!.Value,
            TripId = req.TripId,
            SeatId = seatId,
            FromTripStopTimeId = req.FromTripStopTimeId,
            ToTripStopTimeId = req.ToTripStopTimeId,
            FromStopIndex = newFrom,
            ToStopIndex = newTo,
            Status = SeatHoldStatus.Held,
            UserId = userId,
            BookingId = null,
            HoldToken = token,
            HoldExpiresAt = expiresAt,
            IsDeleted = false,
            CreatedAt = now
        }).ToList();

        _db.BusTripSeatHolds.AddRange(rows);
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Ok(new HoldSeatsResponse
        {
            Ok = true,
            HoldToken = token,
            ExpiresAt = expiresAt,
            HeldCount = rows.Count,
            HeldSeatIds = rows.Select(x => x.SeatId).ToList()
        });
    }

    [HttpDelete("{holdToken}")]
    public async Task<IActionResult> ReleaseByToken(string holdToken, CancellationToken ct = default)
    {
        EnsureTenantScope();

        holdToken = (holdToken ?? "").Trim();
        if (string.IsNullOrWhiteSpace(holdToken))
            return BadRequest(new { message = "holdToken is required." });

        var now = DateTimeOffset.Now;
        if (!GetUserIdOrNull().HasValue)
            return Unauthorized(new { message = "UserId claim is required." });

        var query = _db.BusTripSeatHolds.IgnoreQueryFilters()
          .Where(x =>
              x.TenantId == _tenantContext.TenantId &&
              x.HoldToken == holdToken &&
              !x.IsDeleted &&
              x.Status == SeatHoldStatus.Held);

        var holds = await query.ToListAsync(ct);
        if (holds.Count == 0)
            return NotFound(new { message = "Hold token not found." });


        foreach (var h in holds)
        {
            h.Status = SeatHoldStatus.Cancelled;
            h.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, released = holds.Count });
    }

    [HttpGet("trip/{tripId:guid}")]
    public async Task<IActionResult> ListByTrip(
        Guid tripId,
        [FromQuery] bool includeExpired = false,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        if (tripId == Guid.Empty)
            return BadRequest(new { message = "tripId is required." });

        var tripExists = await _db.BusTrips.IgnoreQueryFilters()
            .AnyAsync(x => x.Id == tripId && x.TenantId == _tenantContext.TenantId, ct);

        if (!tripExists)
            return NotFound(new { message = "Trip not found in this tenant." });

        var now = DateTimeOffset.Now;

        var query = _db.BusTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == _tenantContext.TenantId &&
                x.TripId == tripId &&
                !x.IsDeleted);

        if (!includeExpired)
        {
            query = query.WhereActiveSeatOccupancy(now);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(500)
            .Select(x => new
            {
                x.Id,
                x.TripId,
                x.SeatId,
                x.FromTripStopTimeId,
                x.ToTripStopTimeId,
                x.FromStopIndex,
                x.ToStopIndex,
                x.Status,
                x.UserId,
                x.BookingId,
                x.HoldToken,
                x.HoldExpiresAt,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    private async Task ReleaseExpiredHoldsForTripAsync(Guid tripId, DateTimeOffset now, CancellationToken ct)
    {
        var expired = await _db.BusTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == _tenantContext.TenantId &&
                x.TripId == tripId &&
                !x.IsDeleted &&
                x.Status == SeatHoldStatus.Held &&
                x.HoldExpiresAt <= now)
            .ToListAsync(ct);

        if (expired.Count == 0)
            return;

        foreach (var h in expired)
        {
            h.Status = SeatHoldStatus.Expired;
            h.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
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
}
