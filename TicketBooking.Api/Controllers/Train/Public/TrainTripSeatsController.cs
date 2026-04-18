using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Train;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/train/trips")]
[Authorize]
public sealed class TrainTripSeatsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public TrainTripSeatsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    private sealed class CarRow
    {
        public Guid Id { get; init; }
        public string CarNumber { get; init; } = "";
        public TrainCarType CarType { get; init; }
        public string? CabinClass { get; init; }
        public int SortOrder { get; init; }
        public bool IsActive { get; init; }
    }

    private sealed class SeatRow
    {
        public Guid Id { get; init; }
        public Guid CarId { get; init; }
        public string SeatNumber { get; init; } = "";
        public TrainSeatType SeatType { get; init; }
        public string? CompartmentCode { get; init; }
        public int? CompartmentIndex { get; init; }
        public int RowIndex { get; init; }
        public int ColumnIndex { get; init; }
        public bool IsWindow { get; init; }
        public bool IsAisle { get; init; }
        public string? SeatClass { get; init; }
        public decimal? PriceModifier { get; init; }
        public bool IsActive { get; init; }
    }

    private sealed class OccupancyInfo
    {
        public Guid SeatId { get; init; }
        public Guid? UserId { get; init; }
        public string HoldToken { get; init; } = "";
        public DateTimeOffset HoldExpiresAt { get; init; }
        public TrainSeatHoldStatus Status { get; init; }
    }

    [HttpGet("{tripId:guid}/seats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSeats(
        Guid tripId,
        [FromQuery] Guid fromTripStopTimeId,
        [FromQuery] Guid toTripStopTimeId,
        CancellationToken ct = default)
    {
        if (tripId == Guid.Empty) return BadRequest(new { message = "tripId is required." });
        if (fromTripStopTimeId == Guid.Empty) return BadRequest(new { message = "fromTripStopTimeId is required." });
        if (toTripStopTimeId == Guid.Empty) return BadRequest(new { message = "toTripStopTimeId is required." });

        var now = DateTimeOffset.Now;

        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && !x.IsDeleted, ct);

        if (trip is null)
            return NotFound(new { message = "TrainTrip not found." });

        var tenantId = trip.TenantId;

        if (!trip.IsActive || trip.Status != TrainTripStatus.Published)
            return BadRequest(new { message = "Trip is not available for booking." });

        var fromSt = await _db.TrainTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == fromTripStopTimeId && x.TripId == tripId && x.TenantId == tenantId && !x.IsDeleted, ct);

        var toSt = await _db.TrainTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == toTripStopTimeId && x.TripId == tripId && x.TenantId == tenantId && !x.IsDeleted, ct);

        if (fromSt is null || toSt is null)
            return BadRequest(new { message = "fromTripStopTimeId/toTripStopTimeId is invalid for this trip." });

        if (fromSt.StopIndex >= toSt.StopIndex)
            return BadRequest(new { message = "FromStopIndex must be < ToStopIndex." });

        await ReleaseExpiredHoldsForTripAsync(tripId, tenantId, now, ct);

        var fromIndex = fromSt.StopIndex;
        var toIndex = toSt.StopIndex;
        var myUserId = GetUserIdOrNull();

        var cars = await _db.TrainCars.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TripId == tripId && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CarNumber)
            .Select(x => new CarRow
            {
                Id = x.Id,
                CarNumber = x.CarNumber,
                CarType = x.CarType,
                CabinClass = x.CabinClass,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

        if (cars.Count == 0)
        {
            return Ok(new
            {
                tripId,
                serverNow = now,
                segment = new
                {
                    fromTripStopTimeId,
                    toTripStopTimeId,
                    fromStopIndex = fromIndex,
                    toStopIndex = toIndex
                },
                cars = Array.Empty<object>()
            });
        }

        var carIds = cars.Select(x => x.Id).ToList();

        var seats = await _db.TrainCarSeats.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && carIds.Contains(x.CarId) && !x.IsDeleted)
            .OrderBy(x => x.CarId)
            .ThenBy(x => x.CompartmentIndex)
            .ThenBy(x => x.RowIndex)
            .ThenBy(x => x.ColumnIndex)
            .Select(x => new SeatRow
            {
                Id = x.Id,
                CarId = x.CarId,
                SeatNumber = x.SeatNumber,
                SeatType = x.SeatType,
                CompartmentCode = x.CompartmentCode,
                CompartmentIndex = x.CompartmentIndex,
                RowIndex = x.RowIndex,
                ColumnIndex = x.ColumnIndex,
                IsWindow = x.IsWindow,
                IsAisle = x.IsAisle,
                SeatClass = x.SeatClass,
                PriceModifier = x.PriceModifier,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

        var occupancies = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                x.TripId == tripId &&
                !x.IsDeleted)
            .WhereActiveSeatOccupancy(now)
            .WhereOverlappingSegment(fromIndex, toIndex)
            .Select(x => new OccupancyInfo
            {
                SeatId = x.TrainCarSeatId,
                UserId = x.UserId,
                HoldToken = x.HoldToken,
                HoldExpiresAt = x.HoldExpiresAt,
                Status = x.Status
            })
            .ToListAsync(ct);

        var occupancyBySeat = occupancies
            .GroupBy(x => x.SeatId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => GetOccupancyPriority(x, myUserId)).ThenBy(x => x.HoldExpiresAt).First());

        var seatsByCar = seats
            .GroupBy(s => s.CarId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SeatRow>)g.ToList());

        var carItems = cars.Select(c =>
        {
            if (!seatsByCar.TryGetValue(c.Id, out var carSeats))
                carSeats = Array.Empty<SeatRow>();

            var seatItems = carSeats.Select(seat =>
            {
                var status = seat.IsActive ? "available" : "inactive";
                string? holdToken = null;
                DateTimeOffset? holdExpiresAt = null;

                if (seat.IsActive && occupancyBySeat.TryGetValue(seat.Id, out var occupancy))
                {
                    if (occupancy.Status == TrainSeatHoldStatus.Confirmed)
                    {
                        status = "booked";
                    }
                    else
                    {
                        status = myUserId.HasValue && occupancy.UserId.HasValue && occupancy.UserId.Value == myUserId.Value
                            ? "held_by_me"
                            : "held";

                        holdToken = occupancy.HoldToken;
                        holdExpiresAt = occupancy.HoldExpiresAt;
                    }
                }

                return new
                {
                    seat.Id,
                    seat.SeatNumber,
                    seat.SeatType,
                    seat.CompartmentCode,
                    seat.CompartmentIndex,
                    seat.RowIndex,
                    seat.ColumnIndex,
                    seat.IsWindow,
                    seat.IsAisle,
                    seat.SeatClass,
                    seat.PriceModifier,
                    seat.IsActive,
                    status,
                    holdToken,
                    holdExpiresAt
                };
            }).ToList();

            return new
            {
                c.Id,
                c.CarNumber,
                c.CarType,
                c.CabinClass,
                c.SortOrder,
                c.IsActive,
                seats = seatItems
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
                fromStopIndex = fromIndex,
                toStopIndex = toIndex
            },
            cars = carItems
        });
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
        var sub = User.FindFirst("sub")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static int GetOccupancyPriority(OccupancyInfo occupancy, Guid? myUserId)
    {
        if (occupancy.Status == TrainSeatHoldStatus.Held &&
            myUserId.HasValue &&
            occupancy.UserId.HasValue &&
            occupancy.UserId.Value == myUserId.Value)
        {
            return 0;
        }

        return occupancy.Status == TrainSeatHoldStatus.Held ? 1 : 2;
    }
}
