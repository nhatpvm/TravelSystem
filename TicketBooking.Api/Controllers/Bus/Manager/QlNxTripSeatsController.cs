// FILE #285: TicketBooking.Api/Controllers/QlNxTripSeatsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Bus;
using TicketBooking.Domain.Bus;
using TicketBooking.Domain.Fleet;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlnx/bus/trips")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX}")]
public sealed class QlNxTripSeatsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlNxTripSeatsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Get seat map + seat availability for a trip segment (FromStopIndex -> ToStopIndex).
    /// Manager/Admin version scoped to current tenant.
    /// Lazy expiry: releases expired holds for this trip before calculating.
    /// </summary>
    [HttpGet("{tripId:guid}/seats")]
    public async Task<IActionResult> GetSeats(
        Guid tripId,
        [FromQuery] Guid fromTripStopTimeId,
        [FromQuery] Guid toTripStopTimeId,
        CancellationToken ct = default)
    {
        EnsureTenantScope();

        if (tripId == Guid.Empty)
            return BadRequest(new { message = "tripId is required." });

        if (fromTripStopTimeId == Guid.Empty)
            return BadRequest(new { message = "fromTripStopTimeId is required." });

        if (toTripStopTimeId == Guid.Empty)
            return BadRequest(new { message = "toTripStopTimeId is required." });

        var now = DateTimeOffset.Now;

        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == tripId &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted, ct);

        if (trip is null)
            return NotFound(new { message = "Trip not found in this tenant." });

        var fromSt = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == fromTripStopTimeId &&
                x.TripId == tripId &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted, ct);

        var toSt = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == toTripStopTimeId &&
                x.TripId == tripId &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted, ct);

        if (fromSt is null || toSt is null)
            return BadRequest(new { message = "fromTripStopTimeId/toTripStopTimeId is invalid for this trip." });

        if (fromSt.StopIndex >= toSt.StopIndex)
            return BadRequest(new { message = "FromStopIndex must be < ToStopIndex." });

        var vehicle = await _db.Vehicles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == trip.VehicleId &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted, ct);

        if (vehicle is null || vehicle.SeatMapId is null)
            return BadRequest(new { message = "Trip vehicle/seatmap not found." });

        var seatMap = await _db.SeatMaps.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.Id == vehicle.SeatMapId &&
                x.TenantId == _tenantContext.TenantId &&
                !x.IsDeleted, ct);

        if (seatMap is null)
            return BadRequest(new { message = "SeatMap not found." });

        await ReleaseExpiredHoldsForTripAsync(tripId, now, ct);

        var newFrom = fromSt.StopIndex;
        var newTo = toSt.StopIndex;

        var holds = await _db.BusTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == _tenantContext.TenantId &&
                x.TripId == tripId &&
                !x.IsDeleted)
            .WhereActiveSeatOccupancy(now)
            .WhereOverlappingSegment(newFrom, newTo)
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
            .Where(x =>
                x.TenantId == _tenantContext.TenantId &&
                x.SeatMapId == seatMap.Id &&
                !x.IsDeleted)
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

        var items = seats.Select(s =>
        {
            var status = s.IsActive ? "available" : "inactive";
            string? holdToken = null;
            DateTimeOffset? holdExpiresAt = null;

            if (s.IsActive && occupanciesBySeat.TryGetValue(s.Id, out var occupancies))
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
                s.Id,
                s.SeatNumber,
                s.RowIndex,
                s.ColumnIndex,
                s.DeckIndex,
                s.SeatType,
                s.SeatClass,
                s.IsAisle,
                s.IsWindow,
                s.PriceModifier,
                s.IsActive,
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
                fromStopIndex = newFrom,
                toStopIndex = newTo
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
