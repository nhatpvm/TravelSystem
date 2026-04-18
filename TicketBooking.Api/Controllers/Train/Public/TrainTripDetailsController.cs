using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Train;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/train/trips")]
public sealed class TrainTripDetailsController : ControllerBase
{
    private readonly AppDbContext _db;

    private sealed class SeatSummaryRow
    {
        public Guid Id { get; init; }
        public Guid CarId { get; init; }
        public string SeatNumber { get; init; } = "";
        public TrainSeatType SeatType { get; init; }
        public string? SeatClass { get; init; }
        public decimal? PriceModifier { get; init; }
    }

    public TrainTripDetailsController(AppDbContext db)
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

        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == tripId && !x.IsDeleted, ct);

        if (trip is null)
            return NotFound(new { message = "TrainTrip not found." });

        if (!trip.IsActive || trip.Status != TrainTripStatus.Published)
            return BadRequest(new { message = "Trip is not available for booking." });

        var tenantId = trip.TenantId;
        var now = DateTimeOffset.Now;

        await ReleaseExpiredHoldsForTripAsync(tripId, tenantId, now, ct);

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

        var stopTimes = await _db.TrainTripStopTimes.IgnoreQueryFilters()
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
        var stopPoints = await _db.TrainStopPoints.IgnoreQueryFilters()
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
                x.AddressLine,
                x.TrainStationCode
            })
            .ToListAsync(ct);

        var locationById = locations.ToDictionary(x => x.Id);

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

        var segmentDepartureAt = fromStop.DepartAt ?? fromStop.ArriveAt ?? trip.DepartureAt;
        var segmentArrivalAt = toStop.ArriveAt ?? toStop.DepartAt ?? trip.ArrivalAt;

        var segmentPrices = await _db.TrainTripSegmentPrices.IgnoreQueryFilters()
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

        var segmentPrice = segmentPrices
            .FirstOrDefault(x => x.FromStopIndex == fromStop.StopIndex && x.ToStopIndex == toStop.StopIndex)
            ?? segmentPrices
                .Where(x => x.FromStopIndex <= fromStop.StopIndex && x.ToStopIndex >= toStop.StopIndex)
                .OrderBy(x => x.TotalPrice)
                .FirstOrDefault();

        var cars = await _db.TrainCars.IgnoreQueryFilters()
            .Where(x => x.TripId == tripId && x.TenantId == tenantId && !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CarNumber)
            .Select(x => new
            {
                x.Id,
                x.CarNumber,
                x.CarType,
                x.CabinClass,
                x.SortOrder
            })
            .ToListAsync(ct);

        var carIds = cars.Select(x => x.Id).ToList();
        var seats = carIds.Count == 0
            ? new List<SeatSummaryRow>()
            : await _db.TrainCarSeats.IgnoreQueryFilters()
                .Where(x =>
                    x.TenantId == tenantId &&
                    carIds.Contains(x.CarId) &&
                    !x.IsDeleted &&
                    x.IsActive)
                .Select(x => new SeatSummaryRow
                {
                    Id = x.Id,
                    CarId = x.CarId,
                    SeatNumber = x.SeatNumber,
                    SeatType = x.SeatType,
                    SeatClass = x.SeatClass,
                    PriceModifier = x.PriceModifier
                })
                .ToListAsync(ct);

        var occupiedSeatIds = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TripId == tripId && !x.IsDeleted)
            .WhereActiveSeatOccupancy(now)
            .WhereOverlappingSegment(fromStop.StopIndex, toStop.StopIndex)
            .Select(x => x.TrainCarSeatId)
            .Distinct()
            .ToListAsync(ct);

        var occupiedSeatSet = occupiedSeatIds.ToHashSet();
        var seatsByCar = seats.GroupBy(x => x.CarId).ToDictionary(g => g.Key, g => g.ToList());
        var seatCapacity = seats.Count;
        var occupiedSeatCount = occupiedSeatSet.Count;
        var availableSeatCount = Math.Max(0, seatCapacity - occupiedSeatCount);

        var carItems = cars.Select(car =>
        {
            if (!seatsByCar.TryGetValue(car.Id, out var carSeats))
            {
                carSeats = new List<SeatSummaryRow>();
            }

            var availableCount = carSeats.Count(seat => !occupiedSeatSet.Contains(seat.Id));

            return new
            {
                car.Id,
                car.CarNumber,
                car.CarType,
                car.CabinClass,
                car.SortOrder,
                seatCount = carSeats.Count,
                availableSeatCount = availableCount,
                sampleSeats = carSeats
                    .Take(6)
                    .Select(seat => new
                    {
                        seat.Id,
                        seat.SeatNumber,
                        seat.SeatType,
                        seat.SeatClass,
                        seat.PriceModifier
                    })
                    .ToList()
            };
        }).ToList();

        var stops = stopTimes.Select(stop =>
        {
            stopPointById.TryGetValue(stop.StopPointId, out var stopPoint);
            var location = stopPoint is null ? null : locationById.GetValueOrDefault(stopPoint.LocationId);

            return new
            {
                stop.Id,
                stop.StopIndex,
                stop.ArriveAt,
                stop.DepartAt,
                stop.MinutesFromStart,
                isSelectedOrigin = stop.Id == fromStop.Id,
                isSelectedDestination = stop.Id == toStop.Id,
                stopPoint,
                location
            };
        }).ToList();

        return Ok(new
        {
            trip = new
            {
                trip.Id,
                trip.TrainNumber,
                trip.Code,
                trip.Name,
                trip.Status,
                trip.DepartureAt,
                trip.ArrivalAt,
                trip.FareRulesJson,
                trip.BaggagePolicyJson,
                trip.BoardingPolicyJson
            },
            tenant,
            provider,
            segment = new
            {
                fromTripStopTimeId = fromStop.Id,
                toTripStopTimeId = toStop.Id,
                fromStopIndex = fromStop.StopIndex,
                toStopIndex = toStop.StopIndex,
                departureAt = segmentDepartureAt,
                arrivalAt = segmentArrivalAt,
                durationMinutes = Math.Max(0, (int)Math.Round((segmentArrivalAt - segmentDepartureAt).TotalMinutes)),
                availableSeatCount,
                occupiedSeatCount,
                price = segmentPrice?.TotalPrice,
                currency = segmentPrice?.CurrencyCode ?? "VND"
            },
            seatSummary = new
            {
                seatCapacity,
                occupiedSeatCount,
                availableSeatCount
            },
            cars = carItems,
            stops
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

        if (expired.Count == 0)
            return;

        foreach (var hold in expired)
        {
            hold.Status = TrainSeatHoldStatus.Expired;
            hold.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }
}
