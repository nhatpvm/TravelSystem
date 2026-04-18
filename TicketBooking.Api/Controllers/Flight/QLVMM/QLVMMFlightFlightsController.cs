//FILE: TicketBooking.Api/Controllers/QLVMM/QLVMMFlightFlightsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FlightEntity = TicketBooking.Domain.Flight.Flight;
using TicketBooking.Domain.Flight;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.QLVMM;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlvmm/flight/flights")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class QLVMMFlightFlightsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QLVMMFlightFlightsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] Guid? airlineId,
        [FromQuery] Guid? aircraftId,
        [FromQuery] Guid? fromAirportId,
        [FromQuery] Guid? toAirportId,
        [FromQuery] DateOnly? date,
        [FromQuery] FlightStatus? status,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : (pageSize > 100 ? 100 : pageSize);

        IQueryable<FlightEntity> query = _db.FlightFlights.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (airlineId.HasValue && airlineId.Value != Guid.Empty)
            query = query.Where(x => x.AirlineId == airlineId.Value);

        if (aircraftId.HasValue && aircraftId.Value != Guid.Empty)
            query = query.Where(x => x.AircraftId == aircraftId.Value);

        if (fromAirportId.HasValue && fromAirportId.Value != Guid.Empty)
            query = query.Where(x => x.FromAirportId == fromAirportId.Value);

        if (toAirportId.HasValue && toAirportId.Value != Guid.Empty)
            query = query.Where(x => x.ToAirportId == toAirportId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (date.HasValue)
        {
            var start = new DateTimeOffset(
                date.Value.Year,
                date.Value.Month,
                date.Value.Day,
                0, 0, 0,
                TimeSpan.FromHours(7));

            var end = start.AddDays(1);

            query = query.Where(x => x.DepartureAt >= start && x.DepartureAt < end);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var uq = q.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.FlightNumber.ToUpper().Contains(uq) ||
                (x.Notes != null && x.Notes.ToUpper().Contains(uq)));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(x => x.DepartureAt)
            .ThenBy(x => x.FlightNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        var airlineIds = rows.Select(x => x.AirlineId).Distinct().ToList();
        var aircraftIds = rows.Select(x => x.AircraftId).Distinct().ToList();
        var airportIds = rows.SelectMany(x => new[] { x.FromAirportId, x.ToAirportId }).Distinct().ToList();
        var flightIds = rows.Select(x => x.Id).ToList();

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
                x.IsDeleted
            })
            .ToDictionaryAsync(x => x.Id, ct);

        var aircrafts = await _db.FlightAircrafts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && aircraftIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.AircraftModelId,
                x.AirlineId,
                x.Code,
                x.Registration,
                x.Name,
                x.IsDeleted
            })
            .ToListAsync(ct);

        var aircraftModelIds = aircrafts.Select(x => x.AircraftModelId).Distinct().ToList();

        var aircraftModels = await _db.FlightAircraftModels
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && aircraftModelIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Manufacturer,
                x.Model,
                x.TypicalSeatCapacity,
                x.IsDeleted
            })
            .ToDictionaryAsync(x => x.Id, ct);

        var aircraftMap = aircrafts.ToDictionary(x => x.Id);

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
                x.IsDeleted
            })
            .ToDictionaryAsync(x => x.Id, ct);

        var offerCounts = await _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && flightIds.Contains(x.FlightId) && !x.IsDeleted)
            .GroupBy(x => x.FlightId)
            .Select(g => new
            {
                FlightId = g.Key,
                TotalOffers = g.Count(),
                ActiveOffers = g.Count(x => x.Status == OfferStatus.Active && x.ExpiresAt > DateTimeOffset.Now)
            })
            .ToDictionaryAsync(x => x.FlightId, ct);

        var items = rows.Select(x =>
        {
            airlines.TryGetValue(x.AirlineId, out var airline);
            aircraftMap.TryGetValue(x.AircraftId, out var aircraft);
            aircraftModels.TryGetValue(aircraft?.AircraftModelId ?? Guid.Empty, out var aircraftModel);
            airports.TryGetValue(x.FromAirportId, out var fromAirport);
            airports.TryGetValue(x.ToAirportId, out var toAirport);
            offerCounts.TryGetValue(x.Id, out var offerCount);

            return new
            {
                x.Id,
                x.FlightNumber,
                x.DepartureAt,
                x.ArrivalAt,
                Status = x.Status.ToString(),
                x.IsActive,
                x.IsDeleted,
                airline = airline is null ? null : new
                {
                    airline.Id,
                    airline.Code,
                    airline.Name,
                    airline.IataCode,
                    airline.IcaoCode,
                    airline.LogoUrl,
                    airline.IsDeleted
                },
                aircraft = aircraft is null ? null : new
                {
                    aircraft.Id,
                    aircraft.Code,
                    aircraft.Registration,
                    aircraft.Name,
                    aircraft.IsDeleted,
                    aircraftModel = aircraftModel is null ? null : new
                    {
                        aircraftModel.Id,
                        aircraftModel.Code,
                        aircraftModel.Manufacturer,
                        aircraftModel.Model,
                        aircraftModel.TypicalSeatCapacity,
                        aircraftModel.IsDeleted
                    }
                },
                fromAirport = fromAirport is null ? null : new
                {
                    fromAirport.Id,
                    fromAirport.LocationId,
                    fromAirport.Code,
                    fromAirport.Name,
                    fromAirport.IataCode,
                    fromAirport.IcaoCode,
                    fromAirport.TimeZone,
                    fromAirport.IsDeleted
                },
                toAirport = toAirport is null ? null : new
                {
                    toAirport.Id,
                    toAirport.LocationId,
                    toAirport.Code,
                    toAirport.Name,
                    toAirport.IataCode,
                    toAirport.IcaoCode,
                    toAirport.TimeZone,
                    toAirport.IsDeleted
                },
                offerSummary = new
                {
                    totalOffers = offerCount?.TotalOffers ?? 0,
                    activeOffers = offerCount?.ActiveOffers ?? 0
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
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        IQueryable<FlightEntity> flightQuery = _db.FlightFlights.AsNoTracking();
        if (includeDeleted)
            flightQuery = flightQuery.IgnoreQueryFilters();

        var flight = await flightQuery
            .Where(x => x.TenantId == tenantId && x.Id == id)
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
                x.Notes,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (flight is null)
            return NotFound(new { message = "Flight not found." });

        var airline = await _db.FlightAirlines
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == flight.AirlineId)
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

        var aircraft = await _db.FlightAircrafts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == flight.AircraftId)
            .Select(x => new
            {
                x.Id,
                x.AircraftModelId,
                x.AirlineId,
                x.Code,
                x.Registration,
                x.Name,
                x.IsActive,
                x.IsDeleted
            })
            .FirstOrDefaultAsync(ct);

        var aircraftModel = aircraft is null
            ? null
            : await _db.FlightAircraftModels
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == aircraft.AircraftModelId)
                .Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.Manufacturer,
                    x.Model,
                    x.TypicalSeatCapacity,
                    x.MetadataJson,
                    x.IsActive,
                    x.IsDeleted
                })
                .FirstOrDefaultAsync(ct);

        var fromAirport = await _db.FlightAirports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == flight.FromAirportId)
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
            .FirstOrDefaultAsync(ct);

        var toAirport = await _db.FlightAirports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == flight.ToAirportId)
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
            .FirstOrDefaultAsync(ct);

        var fareClasses = await _db.FlightFareClasses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.AirlineId == flight.AirlineId && !x.IsDeleted)
            .OrderBy(x => x.CabinClass)
            .ThenBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                CabinClass = x.CabinClass.ToString(),
                x.IsRefundable,
                x.IsChangeable,
                x.IsActive
            })
            .ToListAsync(ct);

        var cabinSeatMaps = aircraftModel is null
            ? new List<object>()
            : await _db.FlightCabinSeatMaps
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x =>
                    x.TenantId == tenantId &&
                    x.AircraftModelId == aircraftModel.Id &&
                    !x.IsDeleted)
                .OrderBy(x => x.CabinClass)
                .ThenBy(x => x.Code)
                .Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.Name,
                    CabinClass = x.CabinClass.ToString(),
                    x.TotalRows,
                    x.TotalColumns,
                    x.DeckCount,
                    x.SeatLabelScheme,
                    x.IsActive,
                    x.IsDeleted
                })
                .Cast<object>()
                .ToListAsync(ct);

        var offers = await _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.FlightId == flight.Id && !x.IsDeleted)
            .OrderByDescending(x => x.RequestedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                Status = x.Status.ToString(),
                x.FareClassId,
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice,
                x.SeatsAvailable,
                x.RequestedAt,
                x.ExpiresAt
            })
            .ToListAsync(ct);

        return Ok(new
        {
            trip = new
            {
                flight.Id,
                flight.FlightNumber,
                flight.DepartureAt,
                flight.ArrivalAt,
                Status = flight.Status.ToString(),
                flight.IsActive,
                flight.IsDeleted,
                flight.Notes,
                flight.CreatedAt,
                flight.UpdatedAt
            },
            airline,
            aircraft = aircraft is null ? null : new
            {
                aircraft.Id,
                aircraft.Code,
                aircraft.Registration,
                aircraft.Name,
                aircraft.IsActive,
                aircraft.IsDeleted,
                aircraftModel
            },
            fromAirport,
            toAirport,
            fareClasses,
            cabinSeatMaps,
            offers
        });
    }
}
