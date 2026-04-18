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
[Route("api/v{version:apiVersion}/bus/search")]
public sealed class BusSearchTripsController : ControllerBase
{
    private readonly AppDbContext _db;

    private sealed class SearchTripItem
    {
        public Guid TripId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public object? Tenant { get; set; }
        public object? Provider { get; set; }
        public object? Vehicle { get; set; }
        public DateTimeOffset DepartureAt { get; set; }
        public DateTimeOffset ArrivalAt { get; set; }
        public object? Segment { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public int AvailableSeatCount { get; set; }
        public bool CanBook { get; set; }
    }

    public BusSearchTripsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Public location lookup for bus marketplace search.
    /// Returns distinct catalog locations that are referenced by active tenant stop points.
    /// </summary>
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
            from stopPoint in _db.BusStopPoints.IgnoreQueryFilters()
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
                location.AddressLine
            };

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                (x.ShortName != null && x.ShortName.Contains(keyword)) ||
                (x.Code != null && x.Code.Contains(keyword)) ||
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
                x.AddressLine
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
                stopPointCount = g.Count()
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    /// <summary>
    /// Public marketplace bus trip search across all active bus tenants.
    /// Inputs are LocationId (catalog.Locations) for from/to; date is local (+07).
    /// Returns trips with segment, operator, price and availability.
    /// </summary>
    [HttpGet("trips")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchTrips(
        [FromQuery] Guid fromLocationId,
        [FromQuery] Guid toLocationId,
        [FromQuery] DateOnly departDate,
        [FromQuery] int passengers = 1,
        CancellationToken ct = default)
    {
        if (fromLocationId == Guid.Empty)
            return BadRequest(new { message = "fromLocationId is required." });

        if (toLocationId == Guid.Empty)
            return BadRequest(new { message = "toLocationId is required." });

        if (fromLocationId == toLocationId)
            return BadRequest(new { message = "fromLocationId must be different from toLocationId." });

        if (passengers <= 0 || passengers > 9)
            return BadRequest(new { message = "passengers must be 1..9." });

        var start = new DateTimeOffset(departDate.Year, departDate.Month, departDate.Day, 0, 0, 0, TimeSpan.FromHours(7));
        var end = start.AddDays(1);
        var now = DateTimeOffset.Now;

        var fromStopPointIds = await _db.BusStopPoints.IgnoreQueryFilters()
            .Where(x => x.LocationId == fromLocationId && !x.IsDeleted && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(ct);

        var toStopPointIds = await _db.BusStopPoints.IgnoreQueryFilters()
            .Where(x => x.LocationId == toLocationId && !x.IsDeleted && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (fromStopPointIds.Count == 0 || toStopPointIds.Count == 0)
            return Ok(new { items = Array.Empty<object>() });

        var candidateTrips = await _db.BusTrips.IgnoreQueryFilters()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == TripStatus.Published &&
                x.ArrivalAt >= start &&
                x.DepartureAt < end)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.ProviderId,
                x.RouteId,
                x.VehicleId,
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

        var stopTimes = await _db.BusTripStopTimes.IgnoreQueryFilters()
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

        var stByTrip = stopTimes.GroupBy(x => x.TripId).ToDictionary(g => g.Key, g => g.ToList());

        var vehicleIds = candidateTrips.Select(x => x.VehicleId).Distinct().ToList();
        var vehicles = await _db.Vehicles.IgnoreQueryFilters()
            .Where(x => vehicleIds.Contains(x.Id) && !x.IsDeleted && x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.SeatMapId,
                x.SeatCapacity,
                x.Code,
                x.Name,
                x.PlateNumber
            })
            .ToListAsync(ct);

        var vehicleById = vehicles.ToDictionary(x => x.Id);
        var seatMapIds = vehicles
            .Where(x => x.SeatMapId.HasValue)
            .Select(x => x.SeatMapId!.Value)
            .Distinct()
            .ToList();

        var activeSeatCounts = seatMapIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _db.Seats.IgnoreQueryFilters()
                .Where(x => seatMapIds.Contains(x.SeatMapId) && !x.IsDeleted && x.IsActive)
                .GroupBy(x => x.SeatMapId)
                .Select(g => new { SeatMapId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SeatMapId, x => x.Count, ct);

        var providerIds = candidateTrips.Select(x => x.ProviderId).Distinct().ToList();
        var providers = await _db.Providers.IgnoreQueryFilters()
            .Where(x => providerIds.Contains(x.Id) && !x.IsDeleted && x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.Name,
                x.Slug,
                x.LogoUrl,
                x.SupportPhone,
                x.RatingAverage,
                x.RatingCount
            })
            .ToListAsync(ct);

        var providerById = providers.ToDictionary(x => x.Id);

        var tenantIds = candidateTrips.Select(x => x.TenantId).Distinct().ToList();
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

        var holds = await _db.BusTripSeatHolds.IgnoreQueryFilters()
            .Where(x => tripIds.Contains(x.TripId) && !x.IsDeleted)
            .WhereActiveSeatOccupancy(now)
            .Select(x => new
            {
                x.TripId,
                x.SeatId,
                x.FromStopIndex,
                x.ToStopIndex
            })
            .ToListAsync(ct);

        var holdsByTrip = holds.GroupBy(x => x.TripId).ToDictionary(g => g.Key, g => g.ToList());

        var segPrices = await _db.BusTripSegmentPrices.IgnoreQueryFilters()
            .Where(x => tripIds.Contains(x.TripId) && !x.IsDeleted && x.IsActive)
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

        var items = new List<SearchTripItem>();

        foreach (var trip in candidateTrips)
        {
            if (!stByTrip.TryGetValue(trip.Id, out var stopList))
                continue;

            var fromCandidates = stopList.Where(x => fromStopPointIds.Contains(x.StopPointId)).OrderBy(x => x.StopIndex).ToList();
            var toCandidates = stopList.Where(x => toStopPointIds.Contains(x.StopPointId)).OrderBy(x => x.StopIndex).ToList();

            if (fromCandidates.Count == 0 || toCandidates.Count == 0)
                continue;

            var fromPick = fromCandidates.First();
            var toPick = toCandidates.FirstOrDefault(x => x.StopIndex > fromPick.StopIndex);
            if (toPick is null)
                continue;

            var fromIndex = fromPick.StopIndex;
            var toIndex = toPick.StopIndex;
            var boardingDepartureAt = fromPick.DepartAt ?? fromPick.ArriveAt ?? trip.DepartureAt;
            if (boardingDepartureAt < start || boardingDepartureAt >= end)
                continue;

            var segmentArrivalAt = toPick.ArriveAt ?? toPick.DepartAt ?? trip.ArrivalAt;

            var capacity = 0;
            if (vehicleById.TryGetValue(trip.VehicleId, out var vehicle))
            {
                if (vehicle.SeatMapId.HasValue && activeSeatCounts.TryGetValue(vehicle.SeatMapId.Value, out var seatCount))
                    capacity = seatCount;
                else
                    capacity = vehicle.SeatCapacity;
            }

            var occupiedSeatCount = 0;
            if (holdsByTrip.TryGetValue(trip.Id, out var tripHolds) && tripHolds.Count > 0)
            {
                occupiedSeatCount = tripHolds
                    .Where(h => h.FromStopIndex < toIndex && fromIndex < h.ToStopIndex)
                    .Select(h => h.SeatId)
                    .Distinct()
                    .Count();
            }

            var available = Math.Max(0, capacity - occupiedSeatCount);

            decimal? price = null;
            string? currency = null;

            if (segByTrip.TryGetValue(trip.Id, out var segmentList))
            {
                var exactMatch = segmentList.FirstOrDefault(x => x.FromStopIndex == fromIndex && x.ToStopIndex == toIndex);
                if (exactMatch is not null)
                {
                    price = exactMatch.TotalPrice;
                    currency = exactMatch.CurrencyCode;
                }
                else
                {
                    var cover = segmentList
                        .Where(x => x.FromStopIndex <= fromIndex && x.ToStopIndex >= toIndex)
                        .OrderBy(x => x.TotalPrice)
                        .FirstOrDefault();

                    if (cover is not null)
                    {
                        price = cover.TotalPrice;
                        currency = cover.CurrencyCode;
                    }
                }
            }

            var provider = providerById.TryGetValue(trip.ProviderId, out var providerValue) ? providerValue : null;
            var tenant = tenantById.TryGetValue(trip.TenantId, out var tenantValue) ? tenantValue : null;

            items.Add(new SearchTripItem
            {
                TripId = trip.Id,
                Code = trip.Code,
                Name = trip.Name,
                Tenant = tenant is null
                    ? null
                    : new
                    {
                        tenant.Id,
                        tenant.Code,
                        tenant.Name,
                        tenant.Type
                    },
                Provider = provider is null
                    ? null
                    : new
                    {
                        provider.Id,
                        provider.Name,
                        provider.Slug,
                        provider.LogoUrl,
                        provider.SupportPhone,
                        provider.RatingAverage,
                        provider.RatingCount
                    },
                Vehicle = vehicleById.TryGetValue(trip.VehicleId, out var vehicleValue)
                    ? new
                    {
                        vehicleValue.Id,
                        vehicleValue.Code,
                        vehicleValue.Name,
                        vehicleValue.PlateNumber,
                        vehicleValue.SeatMapId,
                        vehicleValue.SeatCapacity
                    }
                    : null,
                DepartureAt = boardingDepartureAt,
                ArrivalAt = segmentArrivalAt,
                Segment = new
                {
                    fromTripStopTimeId = fromPick.Id,
                    toTripStopTimeId = toPick.Id,
                    fromStopIndex = fromIndex,
                    toStopIndex = toIndex
                },
                Price = price,
                Currency = currency,
                AvailableSeatCount = available,
                CanBook = available >= passengers
            });
        }

        var orderedItems = items
            .OrderBy(x => x.DepartureAt)
            .ThenBy(x => x.Price ?? decimal.MaxValue)
            .ToList();

        return Ok(new { items = orderedItems });
    }

    private async Task ReleaseExpiredHoldsForTripsAsync(List<Guid> tripIds, DateTimeOffset now, CancellationToken ct)
    {
        if (tripIds.Count == 0)
            return;

        var expired = await _db.BusTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                tripIds.Contains(x.TripId) &&
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
