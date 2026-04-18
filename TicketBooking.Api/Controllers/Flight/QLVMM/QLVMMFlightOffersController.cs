//FILE: TicketBooking.Api/Controllers/QLVMM/QLVMMFlightOffersController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Flight;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.QLVMM;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlvmm/flight/offers")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class QLVMMFlightOffersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QLVMMFlightOffersController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? flightId,
        [FromQuery] Guid? airlineId,
        [FromQuery] Guid? fareClassId,
        [FromQuery] OfferStatus? status,
        [FromQuery] DateOnly? departDate,
        [FromQuery] string? q,
        [FromQuery] bool includeExpired = true,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);

        IQueryable<Offer> offerQuery = _db.FlightOffers.AsNoTracking();
        if (includeDeleted)
            offerQuery = offerQuery.IgnoreQueryFilters();

        offerQuery = offerQuery.Where(x => x.TenantId == tenantId);

        if (flightId.HasValue && flightId.Value != Guid.Empty)
            offerQuery = offerQuery.Where(x => x.FlightId == flightId.Value);

        if (airlineId.HasValue && airlineId.Value != Guid.Empty)
            offerQuery = offerQuery.Where(x => x.AirlineId == airlineId.Value);

        if (fareClassId.HasValue && fareClassId.Value != Guid.Empty)
            offerQuery = offerQuery.Where(x => x.FareClassId == fareClassId.Value);

        if (status.HasValue)
            offerQuery = offerQuery.Where(x => x.Status == status.Value);

        if (!includeExpired)
            offerQuery = offerQuery.Where(x => x.ExpiresAt > now);

        IQueryable<TicketBooking.Domain.Flight.Flight> flightBaseQuery = _db.FlightFlights.AsNoTracking();
        if (includeDeleted)
            flightBaseQuery = flightBaseQuery.IgnoreQueryFilters();

        flightBaseQuery = flightBaseQuery.Where(x => x.TenantId == tenantId);

        if (departDate.HasValue)
        {
            var start = new DateTimeOffset(
                departDate.Value.Year,
                departDate.Value.Month,
                departDate.Value.Day,
                0, 0, 0,
                TimeSpan.FromHours(7));
            var end = start.AddDays(1);

            var flightIdsByDate = flightBaseQuery
                .Where(x => x.DepartureAt >= start && x.DepartureAt < end)
                .Select(x => x.Id);

            offerQuery = offerQuery.Where(x => flightIdsByDate.Contains(x.FlightId));
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var uq = q.Trim().ToUpperInvariant();

            var matchedFlightIds = flightBaseQuery
                .Where(x => x.FlightNumber.ToUpper().Contains(uq) || (x.Notes != null && x.Notes.ToUpper().Contains(uq)))
                .Select(x => x.Id);

            var matchedAirlineIds = _db.FlightAirlines
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    (x.Code.ToUpper().Contains(uq) ||
                     x.Name.ToUpper().Contains(uq) ||
                     (x.IataCode != null && x.IataCode.ToUpper().Contains(uq)) ||
                     (x.IcaoCode != null && x.IcaoCode.ToUpper().Contains(uq))))
                .Select(x => x.Id);

            var matchedFareClassIds = _db.FlightFareClasses
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    (x.Code.ToUpper().Contains(uq) ||
                     x.Name.ToUpper().Contains(uq)))
                .Select(x => x.Id);

            offerQuery = offerQuery.Where(x =>
                matchedFlightIds.Contains(x.FlightId) ||
                matchedAirlineIds.Contains(x.AirlineId) ||
                matchedFareClassIds.Contains(x.FareClassId) ||
                x.CurrencyCode.ToUpper().Contains(uq));
        }

        var total = await offerQuery.CountAsync(ct);

        var rows = await offerQuery
            .OrderBy(x => x.ExpiresAt <= now ? 1 : 0)
            .ThenByDescending(x => x.RequestedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.FlightId,
                x.FareClassId,
                x.Status,
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice,
                x.SeatsAvailable,
                x.RequestedAt,
                x.ExpiresAt,
                x.ConditionsJson,
                x.MetadataJson,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        var airlineIds = rows.Select(x => x.AirlineId).Distinct().ToList();
        var flightIds = rows.Select(x => x.FlightId).Distinct().ToList();
        var fareClassIds = rows.Select(x => x.FareClassId).Distinct().ToList();
        var offerIds = rows.Select(x => x.Id).ToList();

        var airlines = await _db.FlightAirlines
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && airlineIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.IataCode,
                x.IcaoCode,
                x.LogoUrl,
                x.IsActive,
                x.IsDeleted
            })
            .ToDictionaryAsync(x => x.Id, ct);

        var flights = await _db.FlightFlights
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && flightIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.AircraftId,
                x.FromAirportId,
                x.ToAirportId,
                x.FlightNumber,
                x.DepartureAt,
                x.ArrivalAt,
                x.Status,
                x.IsActive,
                x.IsDeleted
            })
            .ToListAsync(ct);

        var airportIds = flights
            .SelectMany(x => new[] { x.FromAirportId, x.ToAirportId })
            .Distinct()
            .ToList();

        var airports = await _db.FlightAirports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && airportIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.LocationId,
                x.Code,
                x.Name,
                x.IataCode,
                x.IcaoCode,
                x.TimeZone,
                x.IsActive,
                x.IsDeleted
            })
            .ToDictionaryAsync(x => x.Id, ct);

        var flightsMap = flights.ToDictionary(x => x.Id);

        var fareClasses = await _db.FlightFareClasses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && fareClassIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.Code,
                x.Name,
                x.CabinClass,
                x.IsRefundable,
                x.IsChangeable,
                x.IsActive,
                x.IsDeleted
            })
            .ToDictionaryAsync(x => x.Id, ct);

        var segmentCounts = await _db.FlightOfferSegments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && offerIds.Contains(x.OfferId) && !x.IsDeleted)
            .GroupBy(x => x.OfferId)
            .Select(g => new
            {
                OfferId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.OfferId, x => x.Count, ct);

        var taxFeeSummaries = await _db.FlightOfferTaxFeeLines
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && offerIds.Contains(x.OfferId) && !x.IsDeleted)
            .GroupBy(x => x.OfferId)
            .Select(g => new
            {
                OfferId = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.Amount)
            })
            .ToDictionaryAsync(x => x.OfferId, ct);

        var items = rows.Select(x =>
        {
            airlines.TryGetValue(x.AirlineId, out var airline);
            flightsMap.TryGetValue(x.FlightId, out var flight);
            fareClasses.TryGetValue(x.FareClassId, out var fareClass);

            var fromAirport = flight is not null && airports.TryGetValue(flight.FromAirportId, out var fromAp) ? fromAp : null;
            var toAirport = flight is not null && airports.TryGetValue(flight.ToAirportId, out var toAp) ? toAp : null;

            segmentCounts.TryGetValue(x.Id, out var segmentsCount);
            taxFeeSummaries.TryGetValue(x.Id, out var taxFeeSummary);

            return new
            {
                x.Id,
                Status = x.Status.ToString(),
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice,
                x.SeatsAvailable,
                x.RequestedAt,
                x.ExpiresAt,
                isExpired = x.ExpiresAt <= now,
                x.IsDeleted,
                airline = airline is null ? null : new
                {
                    airline.Id,
                    airline.Code,
                    airline.Name,
                    airline.IataCode,
                    airline.IcaoCode,
                    airline.LogoUrl,
                    airline.IsActive,
                    airline.IsDeleted
                },
                flight = flight is null ? null : new
                {
                    flight.Id,
                    flight.FlightNumber,
                    flight.DepartureAt,
                    flight.ArrivalAt,
                    Status = flight.Status.ToString(),
                    flight.IsActive,
                    flight.IsDeleted,
                    fromAirport = fromAirport is null ? null : new
                    {
                        fromAirport.Id,
                        fromAirport.LocationId,
                        fromAirport.Code,
                        fromAirport.Name,
                        fromAirport.IataCode,
                        fromAirport.IcaoCode,
                        fromAirport.TimeZone
                    },
                    toAirport = toAirport is null ? null : new
                    {
                        toAirport.Id,
                        toAirport.LocationId,
                        toAirport.Code,
                        toAirport.Name,
                        toAirport.IataCode,
                        toAirport.IcaoCode,
                        toAirport.TimeZone
                    }
                },
                fareClass = fareClass is null ? null : new
                {
                    fareClass.Id,
                    fareClass.Code,
                    fareClass.Name,
                    CabinClass = fareClass.CabinClass.ToString(),
                    fareClass.IsRefundable,
                    fareClass.IsChangeable,
                    fareClass.IsActive,
                    fareClass.IsDeleted
                },
                segmentSummary = new
                {
                    count = segmentsCount
                },
                taxFeeSummary = new
                {
                    count = taxFeeSummary?.Count ?? 0,
                    totalAmount = taxFeeSummary?.TotalAmount ?? 0m
                },
                x.CreatedAt,
                x.UpdatedAt
            };
        }).ToList();

        return Ok(new
        {
            page,
            pageSize,
            total,
            items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromQuery] bool includeExpired = true,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;

        IQueryable<Offer> offerQuery = _db.FlightOffers.AsNoTracking();
        if (includeDeleted)
            offerQuery = offerQuery.IgnoreQueryFilters();

        offerQuery = offerQuery.Where(x => x.TenantId == tenantId && x.Id == id);

        if (!includeExpired)
            offerQuery = offerQuery.Where(x => x.ExpiresAt > now);

        var offer = await offerQuery
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.FlightId,
                x.FareClassId,
                x.Status,
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice,
                x.SeatsAvailable,
                x.RequestedAt,
                x.ExpiresAt,
                x.ConditionsJson,
                x.MetadataJson,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (offer is null)
            return NotFound(new { message = "Offer not found." });

        var airline = await _db.FlightAirlines
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == offer.AirlineId)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.IataCode,
                x.IcaoCode,
                x.LogoUrl,
                x.WebsiteUrl,
                x.SupportPhone,
                x.SupportEmail,
                x.IsActive,
                x.IsDeleted
            })
            .FirstOrDefaultAsync(ct);

        var flight = await _db.FlightFlights
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == offer.FlightId)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.AircraftId,
                x.FromAirportId,
                x.ToAirportId,
                x.FlightNumber,
                x.DepartureAt,
                x.ArrivalAt,
                x.Status,
                x.IsActive,
                x.IsDeleted,
                x.Notes
            })
            .FirstOrDefaultAsync(ct);

        var fareClass = await _db.FlightFareClasses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == offer.FareClassId)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.Code,
                x.Name,
                x.CabinClass,
                x.IsRefundable,
                x.IsChangeable,
                x.IsActive,
                x.IsDeleted
            })
            .FirstOrDefaultAsync(ct);

        var fareRule = fareClass is null
            ? null
            : await _db.FlightFareRules
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.FareClassId == fareClass.Id)
                .Select(x => new
                {
                    x.Id,
                    x.FareClassId,
                    x.IsActive,
                    x.RulesJson,
                    x.IsDeleted,
                    x.CreatedAt,
                    x.UpdatedAt
                })
                .FirstOrDefaultAsync(ct);

        var airportIds = flight is null
            ? new List<Guid>()
            : new List<Guid> { flight.FromAirportId, flight.ToAirportId };

        var segmentRows = await _db.FlightOfferSegments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.OfferId == offer.Id && !x.IsDeleted)
            .OrderBy(x => x.SegmentIndex)
            .Select(x => new
            {
                x.Id,
                x.SegmentIndex,
                x.FlightId,
                x.AirlineId,
                x.FareClassId,
                x.CabinSeatMapId,
                x.FromAirportId,
                x.ToAirportId,
                x.DepartureAt,
                x.ArrivalAt,
                x.FlightNumber,
                x.CabinClass,
                x.BaggagePolicyJson,
                x.FareRulesJson,
                x.MetadataJson,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        airportIds.AddRange(segmentRows.SelectMany(x => new[] { x.FromAirportId, x.ToAirportId }));
        airportIds = airportIds.Distinct().ToList();

        var airports = await _db.FlightAirports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && airportIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.LocationId,
                x.Code,
                x.Name,
                x.IataCode,
                x.IcaoCode,
                x.TimeZone,
                x.Latitude,
                x.Longitude,
                x.IsActive,
                x.IsDeleted
            })
            .ToDictionaryAsync(x => x.Id, ct);

        var cabinSeatMapIds = segmentRows
            .Where(x => x.CabinSeatMapId.HasValue && x.CabinSeatMapId.Value != Guid.Empty)
            .Select(x => x.CabinSeatMapId!.Value)
            .Distinct()
            .ToList();

        var cabinSeatMaps = await _db.FlightCabinSeatMaps
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && cabinSeatMapIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.AircraftModelId,
                x.CabinClass,
                x.Code,
                x.Name,
                x.TotalRows,
                x.TotalColumns,
                x.DeckCount,
                x.SeatLabelScheme,
                x.IsActive,
                x.IsDeleted
            })
            .ToDictionaryAsync(x => x.Id, ct);

        var taxFeeLines = await _db.FlightOfferTaxFeeLines
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.OfferId == offer.Id && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.SortOrder,
                LineType = x.LineType.ToString(),
                x.Code,
                x.Name,
                x.CurrencyCode,
                x.Amount,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        var detail = new
        {
            offer = new
            {
                offer.Id,
                Status = offer.Status.ToString(),
                offer.CurrencyCode,
                offer.BaseFare,
                offer.TaxesFees,
                offer.TotalPrice,
                offer.SeatsAvailable,
                offer.RequestedAt,
                offer.ExpiresAt,
                isExpired = offer.ExpiresAt <= now,
                offer.ConditionsJson,
                offer.MetadataJson,
                offer.IsDeleted,
                offer.CreatedAt,
                offer.UpdatedAt
            },
            airline = airline is null ? null : new
            {
                airline.Id,
                airline.Code,
                airline.Name,
                airline.IataCode,
                airline.IcaoCode,
                airline.LogoUrl,
                airline.WebsiteUrl,
                airline.SupportPhone,
                airline.SupportEmail,
                airline.IsActive,
                airline.IsDeleted
            },
            flight = flight is null ? null : new
            {
                flight.Id,
                flight.FlightNumber,
                flight.DepartureAt,
                flight.ArrivalAt,
                Status = flight.Status.ToString(),
                flight.IsActive,
                flight.IsDeleted,
                flight.Notes,
                fromAirport = airports.TryGetValue(flight.FromAirportId, out var fromAirport)
                    ? new
                    {
                        fromAirport.Id,
                        fromAirport.LocationId,
                        fromAirport.Code,
                        fromAirport.Name,
                        fromAirport.IataCode,
                        fromAirport.IcaoCode,
                        fromAirport.TimeZone,
                        fromAirport.Latitude,
                        fromAirport.Longitude,
                        fromAirport.IsActive,
                        fromAirport.IsDeleted
                    }
                    : null,
                toAirport = airports.TryGetValue(flight.ToAirportId, out var toAirport)
                    ? new
                    {
                        toAirport.Id,
                        toAirport.LocationId,
                        toAirport.Code,
                        toAirport.Name,
                        toAirport.IataCode,
                        toAirport.IcaoCode,
                        toAirport.TimeZone,
                        toAirport.Latitude,
                        toAirport.Longitude,
                        toAirport.IsActive,
                        toAirport.IsDeleted
                    }
                    : null
            },
            fareClass = fareClass is null ? null : new
            {
                fareClass.Id,
                fareClass.Code,
                fareClass.Name,
                CabinClass = fareClass.CabinClass.ToString(),
                fareClass.IsRefundable,
                fareClass.IsChangeable,
                fareClass.IsActive,
                fareClass.IsDeleted
            },
            fareRule,
            segments = segmentRows.Select(x =>
            {
                var fromAirport = airports.TryGetValue(x.FromAirportId, out var fromAp) ? fromAp : null;
                var toAirport = airports.TryGetValue(x.ToAirportId, out var toAp) ? toAp : null;
                var cabinSeatMap = x.CabinSeatMapId.HasValue && cabinSeatMaps.TryGetValue(x.CabinSeatMapId.Value, out var sm)
                    ? sm
                    : null;

                return new
                {
                    x.Id,
                    x.SegmentIndex,
                    x.FlightId,
                    x.AirlineId,
                    x.FareClassId,
                    x.CabinSeatMapId,
                    x.DepartureAt,
                    x.ArrivalAt,
                    x.FlightNumber,
                    CabinClass = x.CabinClass.HasValue ? x.CabinClass.Value.ToString() : null,
                    fromAirport = fromAirport is null ? null : new
                    {
                        fromAirport.Id,
                        fromAirport.LocationId,
                        fromAirport.Code,
                        fromAirport.Name,
                        fromAirport.IataCode,
                        fromAirport.IcaoCode,
                        fromAirport.TimeZone
                    },
                    toAirport = toAirport is null ? null : new
                    {
                        toAirport.Id,
                        toAirport.LocationId,
                        toAirport.Code,
                        toAirport.Name,
                        toAirport.IataCode,
                        toAirport.IcaoCode,
                        toAirport.TimeZone
                    },
                    cabinSeatMap = cabinSeatMap is null ? null : new
                    {
                        cabinSeatMap.Id,
                        cabinSeatMap.Code,
                        cabinSeatMap.Name,
                        CabinClass = cabinSeatMap.CabinClass.ToString(),
                        cabinSeatMap.TotalRows,
                        cabinSeatMap.TotalColumns,
                        cabinSeatMap.DeckCount,
                        cabinSeatMap.SeatLabelScheme,
                        cabinSeatMap.IsActive,
                        cabinSeatMap.IsDeleted
                    },
                    x.BaggagePolicyJson,
                    x.FareRulesJson,
                    x.MetadataJson,
                    x.CreatedAt,
                    x.UpdatedAt
                };
            }).ToList(),
            taxFeeLines
        };

        return Ok(detail);
    }
}
