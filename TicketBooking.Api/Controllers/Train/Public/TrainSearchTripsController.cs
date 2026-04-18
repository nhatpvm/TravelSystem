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
[Route("api/v{version:apiVersion}/train/search")]
[Authorize]
public sealed class TrainSearchTripsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    private sealed class CarRow
    {
        public Guid Id { get; init; }
        public Guid TripId { get; init; }
        public string CarNumber { get; init; } = "";
        public TrainCarType CarType { get; init; }
        public string? CabinClass { get; init; }
        public int SortOrder { get; init; }
    }

    private sealed class SeatRow
    {
        public Guid Id { get; init; }
        public Guid CarId { get; init; }
        public decimal? PriceModifier { get; init; }
        public bool IsActive { get; init; }
    }

    public TrainSearchTripsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet("locations")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchLocations(
        [FromQuery] string? q = null,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var keyword = q?.Trim();

        var query =
            from stopPoint in _db.TrainStopPoints.IgnoreQueryFilters()
            join location in _db.Locations.IgnoreQueryFilters() on stopPoint.LocationId equals location.Id
            where !stopPoint.IsDeleted
                  && stopPoint.IsActive
                  && !location.IsDeleted
                  && location.IsActive
            select new
            {
                stopPoint.LocationId,
                location.Name,
                location.ShortName,
                location.Code,
                location.Type,
                location.ProvinceId,
                location.DistrictId,
                location.WardId,
                location.AddressLine,
                location.TrainStationCode
            };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                (x.ShortName != null && x.ShortName.Contains(keyword)) ||
                (x.Code != null && x.Code.Contains(keyword)) ||
                (x.TrainStationCode != null && x.TrainStationCode.Contains(keyword)) ||
                (x.AddressLine != null && x.AddressLine.Contains(keyword)));
        }

        var items = await query
            .GroupBy(x => new
            {
                x.LocationId,
                x.Name,
                x.ShortName,
                x.Code,
                x.Type,
                x.ProvinceId,
                x.DistrictId,
                x.WardId,
                x.AddressLine,
                x.TrainStationCode
            })
            .OrderBy(g => g.Key.Name)
            .Take(limit)
            .Select(g => new
            {
                id = g.Key.LocationId,
                name = g.Key.Name,
                shortName = g.Key.ShortName,
                code = g.Key.Code,
                type = g.Key.Type,
                provinceId = g.Key.ProvinceId,
                districtId = g.Key.DistrictId,
                wardId = g.Key.WardId,
                addressLine = g.Key.AddressLine,
                trainStationCode = g.Key.TrainStationCode,
                stopPointCount = g.Count()
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpGet("trips")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchTrips(
        [FromQuery] Guid fromLocationId,
        [FromQuery] Guid toLocationId,
        [FromQuery] DateOnly departDate,
        [FromQuery] int passengers = 1,
        CancellationToken ct = default)
    {
        if (fromLocationId == Guid.Empty) return BadRequest(new { message = "fromLocationId is required." });
        if (toLocationId == Guid.Empty) return BadRequest(new { message = "toLocationId is required." });
        if (fromLocationId == toLocationId) return BadRequest(new { message = "fromLocationId must be different from toLocationId." });
        if (passengers <= 0 || passengers > 9) return BadRequest(new { message = "passengers must be 1..9" });

        var now = DateTimeOffset.Now;
        var start = new DateTimeOffset(departDate.Year, departDate.Month, departDate.Day, 0, 0, 0, TimeSpan.FromHours(7));
        var end = start.AddDays(1);

        var fromStopPointIds = await _db.TrainStopPoints.IgnoreQueryFilters()
            .Where(x => x.LocationId == fromLocationId && !x.IsDeleted && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var toStopPointIds = await _db.TrainStopPoints.IgnoreQueryFilters()
            .Where(x => x.LocationId == toLocationId && !x.IsDeleted && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (fromStopPointIds.Count == 0 || toStopPointIds.Count == 0)
            return Ok(new { items = Array.Empty<object>() });

        var candidateTrips = await _db.TrainTrips.IgnoreQueryFilters()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == TrainTripStatus.Published &&
                x.ArrivalAt >= start &&
                x.DepartureAt < end)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.ProviderId,
                x.RouteId,
                x.TrainNumber,
                x.Code,
                x.Name,
                x.DepartureAt,
                x.ArrivalAt
            })
            .ToListAsync(ct);

        if (candidateTrips.Count == 0)
            return Ok(new { items = Array.Empty<object>() });

        var tripIds = candidateTrips.Select(x => x.Id).ToList();

        await ReleaseExpiredHoldsForTripsAsync(tripIds, now, ct);

        var stopTimes = await _db.TrainTripStopTimes.IgnoreQueryFilters()
            .Where(x =>
                tripIds.Contains(x.TripId) &&
                !x.IsDeleted &&
                x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.TripId,
                x.StopPointId,
                x.StopIndex,
                x.ArriveAt,
                x.DepartAt
            })
            .ToListAsync(ct);

        var stByTrip = stopTimes
            .GroupBy(x => x.TripId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var providerIds = candidateTrips.Select(x => x.ProviderId).Distinct().ToList();
        var tenantIds = candidateTrips.Select(x => x.TenantId).Distinct().ToList();

        var providers = await _db.Providers.IgnoreQueryFilters()
            .Where(x => providerIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Slug,
                x.LogoUrl
            })
            .ToListAsync(ct);

        var providerById = providers.ToDictionary(x => x.Id);

        var tenants = await _db.Tenants.IgnoreQueryFilters()
            .Where(x => tenantIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Type
            })
            .ToListAsync(ct);

        var tenantById = tenants.ToDictionary(x => x.Id);

        var cars = await _db.TrainCars.IgnoreQueryFilters()
            .Where(x =>
                tripIds.Contains(x.TripId) &&
                !x.IsDeleted &&
                x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CarNumber)
            .Select(x => new CarRow
            {
                Id = x.Id,
                TripId = x.TripId,
                CarNumber = x.CarNumber,
                CarType = x.CarType,
                CabinClass = x.CabinClass,
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct);

        var carIds = cars.Select(x => x.Id).ToList();
        var seatRows = carIds.Count == 0
            ? new List<SeatRow>()
            : await _db.TrainCarSeats.IgnoreQueryFilters()
                .Where(x =>
                    carIds.Contains(x.CarId) &&
                    !x.IsDeleted)
                .Select(x => new SeatRow
                {
                    Id = x.Id,
                    CarId = x.CarId,
                    PriceModifier = x.PriceModifier,
                    IsActive = x.IsActive
                })
                .ToListAsync(ct);

        var carsByTrip = cars.GroupBy(x => x.TripId).ToDictionary(g => g.Key, g => g.ToList());
        var seatsByCar = seatRows.GroupBy(x => x.CarId).ToDictionary(g => g.Key, g => g.ToList());

        var seatCounts = await _db.TrainCars.IgnoreQueryFilters()
            .Where(c => tripIds.Contains(c.TripId) && !c.IsDeleted && c.IsActive)
            .Join(
                _db.TrainCarSeats.IgnoreQueryFilters().Where(s => !s.IsDeleted && s.IsActive),
                c => c.Id,
                s => s.CarId,
                (c, s) => new { c.TripId, SeatId = s.Id })
            .GroupBy(x => x.TripId)
            .Select(g => new { TripId = g.Key, SeatCount = g.Count() })
            .ToListAsync(ct);

        var capacityByTrip = seatCounts.ToDictionary(x => x.TripId, x => x.SeatCount);

        var occupancies = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                tripIds.Contains(x.TripId) &&
                !x.IsDeleted)
            .WhereActiveSeatOccupancy(now)
            .Select(x => new
            {
                x.TripId,
                x.TrainCarSeatId,
                x.FromStopIndex,
                x.ToStopIndex,
                x.Status
            })
            .ToListAsync(ct);

        var occupanciesByTrip = occupancies.GroupBy(x => x.TripId).ToDictionary(g => g.Key, g => g.ToList());

        var segPrices = await _db.TrainTripSegmentPrices.IgnoreQueryFilters()
            .Where(x =>
                tripIds.Contains(x.TripId) &&
                !x.IsDeleted &&
                x.IsActive)
            .Select(x => new
            {
                x.TripId,
                x.FromStopIndex,
                x.ToStopIndex,
                x.CurrencyCode,
                x.TotalPrice
            })
            .ToListAsync(ct);

        var segByTrip = segPrices.GroupBy(x => x.TripId).ToDictionary(g => g.Key, g => g.ToList());

        var items = new List<(DateTimeOffset DepartureAt, object Item)>();

        foreach (var t in candidateTrips)
        {
            if (!stByTrip.TryGetValue(t.Id, out var stList))
                continue;

            var fromCandidates = stList.Where(x => fromStopPointIds.Contains(x.StopPointId)).OrderBy(x => x.StopIndex).ToList();
            var toCandidates = stList.Where(x => toStopPointIds.Contains(x.StopPointId)).OrderBy(x => x.StopIndex).ToList();

            if (fromCandidates.Count == 0 || toCandidates.Count == 0)
                continue;

            var fromPick = fromCandidates.First();
            var toPick = toCandidates.FirstOrDefault(x => x.StopIndex > fromPick.StopIndex);
            if (toPick is null)
                continue;

            var boardingDepartureAt = fromPick.DepartAt ?? fromPick.ArriveAt ?? t.DepartureAt;
            if (boardingDepartureAt < start || boardingDepartureAt >= end)
                continue;

            var segmentArrivalAt = toPick.ArriveAt ?? toPick.DepartAt ?? t.ArrivalAt;
            var fromIndex = fromPick.StopIndex;
            var toIndex = toPick.StopIndex;

            var capacity = capacityByTrip.TryGetValue(t.Id, out var cap) ? cap : 0;

            var heldSeatCount = 0;
            var occupiedSeatCount = 0;
            if (occupanciesByTrip.TryGetValue(t.Id, out var tripOccupancies) && tripOccupancies.Count > 0)
            {
                var overlapping = tripOccupancies
                    .Where(h => h.FromStopIndex < toIndex && fromIndex < h.ToStopIndex)
                    .ToList();

                heldSeatCount = overlapping
                    .Where(h => h.Status == TrainSeatHoldStatus.Held)
                    .Select(h => h.TrainCarSeatId)
                    .Distinct()
                    .Count();

                occupiedSeatCount = overlapping
                    .Select(h => h.TrainCarSeatId)
                    .Distinct()
                    .Count();
            }

            var available = Math.Max(0, capacity - occupiedSeatCount);

            decimal? price = null;
            string? currency = null;

            if (segByTrip.TryGetValue(t.Id, out var segList))
            {
                var exact = segList.FirstOrDefault(s => s.FromStopIndex == fromIndex && s.ToStopIndex == toIndex);
                if (exact is not null)
                {
                    price = exact.TotalPrice;
                    currency = exact.CurrencyCode;
                }
                else
                {
                    var cover = segList
                        .Where(s => s.FromStopIndex <= fromIndex && s.ToStopIndex >= toIndex)
                        .OrderBy(s => s.TotalPrice)
                        .FirstOrDefault();

                    if (cover is not null)
                    {
                        price = cover.TotalPrice;
                        currency = cover.CurrencyCode;
                    }
                }
            }

            providerById.TryGetValue(t.ProviderId, out var provider);
            tenantById.TryGetValue(t.TenantId, out var tenant);

            var overlappingSeatIds = occupanciesByTrip.TryGetValue(t.Id, out var tripOccupanciesForCars)
                ? tripOccupanciesForCars
                    .Where(h => h.FromStopIndex < toIndex && fromIndex < h.ToStopIndex)
                    .Select(h => h.TrainCarSeatId)
                    .Distinct()
                    .ToHashSet()
                : new HashSet<Guid>();

            var carOptions = carsByTrip.TryGetValue(t.Id, out var tripCars)
                ? tripCars
                    .Select(car =>
                    {
                        if (!seatsByCar.TryGetValue(car.Id, out var carSeats))
                        {
                            carSeats = new List<SeatRow>();
                        }

                        var activeSeats = carSeats.Where(seat => seat.IsActive).ToList();
                        var availableSeatCount = activeSeats.Count(seat => !overlappingSeatIds.Contains(seat.Id));
                        var minModifier = activeSeats.Count == 0
                            ? 0m
                            : activeSeats.Min(seat => seat.PriceModifier ?? 0m);

                        return new
                        {
                            car.Id,
                            car.CarNumber,
                            car.CarType,
                            car.CabinClass,
                            car.SortOrder,
                            seatCount = activeSeats.Count,
                            availableSeatCount,
                            price = price.HasValue ? price.Value + minModifier : (decimal?)null,
                            currency
                        };
                    })
                    .Where(item => item.seatCount > 0)
                    .Take(4)
                    .Cast<object>()
                    .ToList()
                : new List<object>();

            items.Add((boardingDepartureAt, new
            {
                tripId = t.Id,
                t.TrainNumber,
                t.Code,
                t.Name,
                tenant = tenant is null ? null : new { tenant.Id, tenant.Code, tenant.Name, tenant.Type },
                provider = provider is null ? null : new { provider.Id, provider.Name, provider.Slug, provider.LogoUrl },
                departureAt = boardingDepartureAt,
                arrivalAt = segmentArrivalAt,
                segment = new
                {
                    fromTripStopTimeId = fromPick.Id,
                    toTripStopTimeId = toPick.Id,
                    fromStopIndex = fromIndex,
                    toStopIndex = toIndex
                },
                capacitySeatCount = capacity,
                heldSeatCount,
                occupiedSeatCount,
                availableSeatCount = available,
                canBook = available >= passengers,
                price,
                currency,
                carOptions
            }));
        }

        return Ok(new { items = items.OrderBy(x => x.DepartureAt).Select(x => x.Item).ToList() });
    }

    private async Task ReleaseExpiredHoldsForTripsAsync(List<Guid> tripIds, DateTimeOffset now, CancellationToken ct)
    {
        if (tripIds.Count == 0) return;

        var expired = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                tripIds.Contains(x.TripId) &&
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
}
