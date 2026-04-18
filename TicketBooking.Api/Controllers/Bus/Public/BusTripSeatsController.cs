using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Bus;
using TicketBooking.Domain.Bus;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/bus/trips")]
public sealed class BusTripSeatsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BusTripSeatsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Public seat map + seat availability for a trip segment.
    /// Seller tenant is resolved from the trip itself.
    /// </summary>
    [HttpGet("{tripId:guid}/seats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSeats(
        Guid tripId,
        [FromQuery] Guid fromTripStopTimeId,
        [FromQuery] Guid toTripStopTimeId,
        CancellationToken ct = default)
    {
        if (tripId == Guid.Empty)
            return BadRequest(new { message = "tripId is required." });

        if (fromTripStopTimeId == Guid.Empty)
            return BadRequest(new { message = "fromTripStopTimeId is required." });

        if (toTripStopTimeId == Guid.Empty)
            return BadRequest(new { message = "toTripStopTimeId is required." });

        var now = DateTimeOffset.Now;
        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && !x.IsDeleted, ct);

        if (trip is null)
            return NotFound(new { message = "Trip not found." });

        if (!trip.IsActive || trip.Status != TripStatus.Published)
            return BadRequest(new { message = "Trip is not available for booking." });

        var tenantId = trip.TenantId;

        var fromStop = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == fromTripStopTimeId && x.TripId == tripId && x.TenantId == tenantId && !x.IsDeleted, ct);

        var toStop = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == toTripStopTimeId && x.TripId == tripId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (fromStop is null || toStop is null)
            return BadRequest(new { message = "fromTripStopTimeId/toTripStopTimeId is invalid for this trip." });

        if (fromStop.StopIndex >= toStop.StopIndex)
            return BadRequest(new { message = "FromStopIndex must be < ToStopIndex." });

        var vehicle = await _db.Vehicles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == trip.VehicleId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (vehicle is null || vehicle.SeatMapId is null)
            return BadRequest(new { message = "Trip vehicle/seatmap not found." });

        var seatMap = await _db.SeatMaps.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == vehicle.SeatMapId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (seatMap is null)
            return BadRequest(new { message = "SeatMap not found." });

        await ReleaseExpiredHoldsForTripAsync(tripId, now, ct);

        var holds = await _db.BusTripSeatHolds.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TripId == tripId && !x.IsDeleted)
            .WhereActiveSeatOccupancy(now)
            .WhereOverlappingSegment(fromStop.StopIndex, toStop.StopIndex)
            .Select(x => new
            {
                x.SeatId,
                x.UserId,
                x.Status,
                x.HoldToken,
                x.HoldExpiresAt
            })
            .ToListAsync(ct);

        var myUserId = GetUserIdOrNull();
        var occupanciesBySeat = holds
            .GroupBy(x => x.SeatId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var seats = await _db.Seats.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.SeatMapId == seatMap.Id && !x.IsDeleted)
            .OrderBy(x => x.DeckIndex)
            .ThenBy(x => x.RowIndex)
            .ThenBy(x => x.ColumnIndex)
            .Select(x => new
            {
                x.Id,
                x.SeatNumber,
                x.RowIndex,
                x.ColumnIndex,
                x.DeckIndex,
                x.SeatType,
                x.SeatClass,
                x.IsAisle,
                x.IsWindow,
                x.PriceModifier,
                x.IsActive
            })
            .ToListAsync(ct);

        var items = seats.Select(seat =>
        {
            var status = seat.IsActive ? "available" : "inactive";
            string? holdToken = null;
            DateTimeOffset? holdExpiresAt = null;

            if (seat.IsActive && occupanciesBySeat.TryGetValue(seat.Id, out var occupancies))
            {
                var heldByMe = occupancies
                    .Where(x => x.Status == SeatHoldStatus.Held && myUserId.HasValue && x.UserId == myUserId.Value)
                    .OrderBy(x => x.HoldExpiresAt)
                    .FirstOrDefault();

                if (heldByMe is not null)
                {
                    status = "held_by_me";
                    holdToken = heldByMe.HoldToken;
                    holdExpiresAt = heldByMe.HoldExpiresAt;
                }
                else
                {
                    var heldByOther = occupancies
                        .Where(x => x.Status == SeatHoldStatus.Held)
                        .OrderBy(x => x.HoldExpiresAt)
                        .FirstOrDefault();

                    if (heldByOther is not null)
                    {
                        status = "held";
                        holdToken = heldByOther.HoldToken;
                        holdExpiresAt = heldByOther.HoldExpiresAt;
                    }
                    else
                    {
                        status = "booked";
                    }
                }
            }

            return new
            {
                seat.Id,
                seat.SeatNumber,
                seat.RowIndex,
                seat.ColumnIndex,
                seat.DeckIndex,
                seat.SeatType,
                seat.SeatClass,
                seat.IsAisle,
                seat.IsWindow,
                seat.PriceModifier,
                seat.IsActive,
                status,
                holdToken,
                holdExpiresAt
            };
        }).ToList();

        return Ok(new
        {
            tripId,
            serverNow = now,
            segment = new
            {
                fromTripStopTimeId,
                toTripStopTimeId,
                fromStopIndex = fromStop.StopIndex,
                toStopIndex = toStop.StopIndex
            },
            seatMap = new
            {
                seatMap.Id,
                seatMap.VehicleType,
                seatMap.Code,
                seatMap.Name,
                seatMap.TotalRows,
                seatMap.TotalColumns,
                seatMap.DeckCount
            },
            seats = items
        });
    }

    private async Task ReleaseExpiredHoldsForTripAsync(Guid tripId, DateTimeOffset now, CancellationToken ct)
    {
        var expired = await _db.BusTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TripId == tripId &&
                !x.IsDeleted &&
                x.Status == SeatHoldStatus.Held &&
                x.HoldExpiresAt <= now)
            .ToListAsync(ct);

        if (expired.Count == 0)
            return;

        foreach (var hold in expired)
        {
            hold.Status = SeatHoldStatus.Expired;
            hold.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    private Guid? GetUserIdOrNull()
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
