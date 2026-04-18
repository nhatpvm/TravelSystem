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
public sealed class BusTripDetailsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BusTripDetailsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{tripId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDetail(
        Guid tripId,
        [FromQuery] Guid? fromTripStopTimeId = null,
        [FromQuery] Guid? toTripStopTimeId = null,
        CancellationToken ct = default)
    {
        if (tripId == Guid.Empty)
            return BadRequest(new { message = "tripId is required." });

        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && !x.IsDeleted, ct);

        if (trip is null)
            return NotFound(new { message = "Trip not found." });

        if (!trip.IsActive || trip.Status != TripStatus.Published)
            return BadRequest(new { message = "Trip is not available for booking." });

        var tenantId = trip.TenantId;
        var now = DateTimeOffset.Now;

        await ReleaseExpiredHoldsForTripAsync(tripId, now, ct);

        var tenant = await _db.Tenants.IgnoreQueryFilters()
            .Where(x => x.Id == tenantId && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Type
            })
            .FirstOrDefaultAsync(ct);

        var provider = await _db.Providers.IgnoreQueryFilters()
            .Where(x => x.Id == trip.ProviderId && x.TenantId == tenantId && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Slug,
                x.LogoUrl,
                x.CoverUrl,
                x.SupportPhone,
                x.SupportEmail,
                x.WebsiteUrl,
                x.Description,
                x.RatingAverage,
                x.RatingCount
            })
            .FirstOrDefaultAsync(ct);

        var vehicle = await _db.Vehicles.IgnoreQueryFilters()
            .Where(x => x.Id == trip.VehicleId && x.TenantId == tenantId && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.PlateNumber,
                x.SeatMapId,
                x.SeatCapacity
            })
            .FirstOrDefaultAsync(ct);

        var vehicleDetail = vehicle is null
            ? null
            : await _db.BusVehicleDetails.IgnoreQueryFilters()
                .Where(x => x.VehicleId == vehicle.Id && x.TenantId == tenantId && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.BusType,
                    x.AmenitiesJson
                })
                .FirstOrDefaultAsync(ct);

        var seatMap = vehicle?.SeatMapId is null
            ? null
            : await _db.SeatMaps.IgnoreQueryFilters()
                .Where(x => x.Id == vehicle.SeatMapId.Value && x.TenantId == tenantId && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.Name,
                    x.VehicleType,
                    x.TotalRows,
                    x.TotalColumns,
                    x.DeckCount
                })
                .FirstOrDefaultAsync(ct);

        var stopTimes = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .Where(x => x.TripId == tripId && x.TenantId == tenantId && !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.StopIndex)
            .Select(x => new
            {
                x.Id,
                x.StopPointId,
                x.StopIndex,
                x.ArriveAt,
                x.DepartAt,
                x.MinutesFromStart
            })
            .ToListAsync(ct);

        if (stopTimes.Count < 2)
            return BadRequest(new { message = "Trip stop times are not configured." });

        var stopPointIds = stopTimes.Select(x => x.StopPointId).Distinct().ToList();
        var stopPoints = await _db.BusStopPoints.IgnoreQueryFilters()
            .Where(x => stopPointIds.Contains(x.Id) && x.TenantId == tenantId && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.LocationId,
                x.Type,
                x.Name,
                x.AddressLine,
                x.Latitude,
                x.Longitude
            })
            .ToListAsync(ct);

        var stopPointById = stopPoints.ToDictionary(x => x.Id);

        var locationIds = stopPoints.Select(x => x.LocationId).Distinct().ToList();
        var locations = await _db.Locations.IgnoreQueryFilters()
            .Where(x => locationIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.ShortName,
                x.Code,
                x.AddressLine
            })
            .ToListAsync(ct);

        var locationById = locations.ToDictionary(x => x.Id);

        var tripStopTimeIds = stopTimes.Select(x => x.Id).ToList();
        var pickupLookup = await _db.BusTripStopPickupPoints.IgnoreQueryFilters()
            .Where(x => tripStopTimeIds.Contains(x.TripStopTimeId) && x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .Select(x => new
            {
                x.Id,
                x.TripStopTimeId,
                x.Name,
                x.AddressLine,
                x.IsDefault
            })
            .ToListAsync(ct);

        var dropoffLookup = await _db.BusTripStopDropoffPoints.IgnoreQueryFilters()
            .Where(x => tripStopTimeIds.Contains(x.TripStopTimeId) && x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .Select(x => new
            {
                x.Id,
                x.TripStopTimeId,
                x.Name,
                x.AddressLine,
                x.IsDefault
            })
            .ToListAsync(ct);

        var pickupByStop = pickupLookup.GroupBy(x => x.TripStopTimeId).ToDictionary(g => g.Key, g => g.ToList());
        var dropoffByStop = dropoffLookup.GroupBy(x => x.TripStopTimeId).ToDictionary(g => g.Key, g => g.ToList());

        var fromStop = fromTripStopTimeId.HasValue && fromTripStopTimeId.Value != Guid.Empty
            ? stopTimes.FirstOrDefault(x => x.Id == fromTripStopTimeId.Value)
            : stopTimes.FirstOrDefault();

        var toStop = toTripStopTimeId.HasValue && toTripStopTimeId.Value != Guid.Empty
            ? stopTimes.FirstOrDefault(x => x.Id == toTripStopTimeId.Value)
            : stopTimes.LastOrDefault();

        if (fromStop is null || toStop is null)
            return BadRequest(new { message = "fromTripStopTimeId/toTripStopTimeId is invalid for this trip." });

        if (fromStop.StopIndex >= toStop.StopIndex)
            return BadRequest(new { message = "FromStopIndex must be < ToStopIndex." });

        var segPrices = await _db.BusTripSegmentPrices.IgnoreQueryFilters()
            .Where(x => x.TripId == tripId && x.TenantId == tenantId && !x.IsDeleted && x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.FromTripStopTimeId,
                x.ToTripStopTimeId,
                x.FromStopIndex,
                x.ToStopIndex,
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice
            })
            .ToListAsync(ct);

        var segmentPrice = segPrices
            .FirstOrDefault(x => x.FromStopIndex == fromStop.StopIndex && x.ToStopIndex == toStop.StopIndex)
            ?? segPrices
                .Where(x => x.FromStopIndex <= fromStop.StopIndex && x.ToStopIndex >= toStop.StopIndex)
                .OrderBy(x => x.TotalPrice)
                .FirstOrDefault();

        var seatCapacity = vehicle?.SeatMapId is Guid seatMapId
            ? await _db.Seats.IgnoreQueryFilters()
                .CountAsync(x => x.SeatMapId == seatMapId && x.TenantId == tenantId && !x.IsDeleted && x.IsActive, ct)
            : vehicle?.SeatCapacity ?? 0;

        var occupiedSeatCount = await _db.BusTripSeatHolds.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TripId == tripId && !x.IsDeleted)
            .WhereActiveSeatOccupancy(now)
            .WhereOverlappingSegment(fromStop.StopIndex, toStop.StopIndex)
            .Select(x => x.SeatId)
            .Distinct()
            .CountAsync(ct);

        var stops = stopTimes.Select(stop =>
        {
            stopPointById.TryGetValue(stop.StopPointId, out var stopPoint);
            var location = stopPoint is null
                ? null
                : locationById.GetValueOrDefault(stopPoint.LocationId);

            return new
            {
                stop.Id,
                stop.StopIndex,
                stop.ArriveAt,
                stop.DepartAt,
                stop.MinutesFromStart,
                stopPoint = stopPoint is null
                    ? null
                    : new
                    {
                        stopPoint.Id,
                        stopPoint.Name,
                        stopPoint.Type,
                        stopPoint.AddressLine,
                        stopPoint.Latitude,
                        stopPoint.Longitude
                    },
                location,
                pickupPoints = pickupByStop.TryGetValue(stop.Id, out var pickups)
                    ? pickups.Cast<object>()
                    : Enumerable.Empty<object>(),
                dropoffPoints = dropoffByStop.TryGetValue(stop.Id, out var dropoffs)
                    ? dropoffs.Cast<object>()
                    : Enumerable.Empty<object>(),
                isSelectedOrigin = stop.Id == fromStop.Id,
                isSelectedDestination = stop.Id == toStop.Id,
                isInSelectedSegment = stop.StopIndex >= fromStop.StopIndex && stop.StopIndex <= toStop.StopIndex
            };
        }).ToList();

        return Ok(new
        {
            serverNow = now,
            trip = new
            {
                trip.Id,
                trip.Code,
                trip.Name,
                trip.Status,
                trip.DepartureAt,
                trip.ArrivalAt,
                trip.FareRulesJson,
                trip.BaggagePolicyJson,
                trip.BoardingPolicyJson,
                trip.Notes
            },
            tenant,
            provider,
            vehicle,
            vehicleDetail,
            seatMap,
            segment = new
            {
                fromTripStopTimeId = fromStop.Id,
                toTripStopTimeId = toStop.Id,
                fromStopIndex = fromStop.StopIndex,
                toStopIndex = toStop.StopIndex,
                departureAt = fromStop.DepartAt ?? fromStop.ArriveAt ?? trip.DepartureAt,
                arrivalAt = toStop.ArriveAt ?? toStop.DepartAt ?? trip.ArrivalAt,
                availableSeatCount = Math.Max(0, seatCapacity - occupiedSeatCount),
                price = segmentPrice?.TotalPrice,
                currency = segmentPrice?.CurrencyCode ?? "VND",
                priceDetail = segmentPrice is null
                    ? null
                    : new
                    {
                        segmentPrice.Id,
                        segmentPrice.BaseFare,
                        segmentPrice.TaxesFees,
                        segmentPrice.TotalPrice,
                        segmentPrice.CurrencyCode
                    }
            },
            stops
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
}
