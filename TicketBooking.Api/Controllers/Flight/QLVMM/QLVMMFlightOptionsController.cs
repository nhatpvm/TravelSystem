using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Catalog;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.QLVMM;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlvmm/flight/options")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class QLVMMFlightOptionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QLVMMFlightOptionsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetOptions(CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;

        var locations = await _db.Locations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                !x.IsDeleted &&
                (x.Type == LocationType.Airport ||
                 x.AirportIataCode != null ||
                 x.AirportIcaoCode != null))
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Type,
                x.Name,
                x.ShortName,
                x.Code,
                x.AirportIataCode,
                x.AirportIcaoCode,
                x.TimeZone,
                x.AddressLine,
                x.Latitude,
                x.Longitude,
                x.IsActive
            })
            .ToListAsync(ct);

        var locationById = locations.ToDictionary(x => x.Id);

        var airlines = await _db.FlightAirlines
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.Name)
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
                x.IsActive
            })
            .ToListAsync(ct);

        var airlineById = airlines.ToDictionary(x => x.Id);

        var airports = await _db.FlightAirports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.Name)
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
                x.IsActive
            })
            .ToListAsync(ct);

        var airportById = airports.ToDictionary(x => x.Id);

        var aircraftModels = await _db.FlightAircraftModels
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.Manufacturer)
            .ThenBy(x => x.Model)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Manufacturer,
                x.Model,
                x.TypicalSeatCapacity,
                x.MetadataJson,
                x.IsActive
            })
            .ToListAsync(ct);

        var aircraftModelById = aircraftModels.ToDictionary(x => x.Id);

        var aircrafts = await _db.FlightAircrafts
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.AircraftModelId,
                x.AirlineId,
                x.Code,
                x.Registration,
                x.Name,
                x.IsActive
            })
            .ToListAsync(ct);

        var aircraftById = aircrafts.ToDictionary(x => x.Id);

        var fareClasses = await _db.FlightFareClasses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.AirlineId)
            .ThenBy(x => x.CabinClass)
            .ThenBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.Code,
                x.Name,
                x.CabinClass,
                x.IsRefundable,
                x.IsChangeable,
                x.IsActive
            })
            .ToListAsync(ct);

        var fareClassById = fareClasses.ToDictionary(x => x.Id);

        var fareRules = await _db.FlightFareRules
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.FareClassId,
                x.IsActive,
                x.RulesJson,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        var flights = await _db.FlightFlights
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderByDescending(x => x.DepartureAt)
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
                x.Notes
            })
            .ToListAsync(ct);

        var flightById = flights.ToDictionary(x => x.Id);

        var offers = await _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderByDescending(x => x.RequestedAt)
            .ThenByDescending(x => x.CreatedAt)
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
                x.ExpiresAt
            })
            .ToListAsync(ct);

        var seatMaps = await _db.FlightCabinSeatMaps
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.AircraftModelId)
            .ThenBy(x => x.CabinClass)
            .ThenBy(x => x.Code)
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
                x.LayoutVersion,
                x.SeatLabelScheme,
                x.IsActive
            })
            .ToListAsync(ct);

        var seatMapIds = seatMaps.Select(x => x.Id).ToList();

        var seatCounts = seatMapIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _db.FlightCabinSeats
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && seatMapIds.Contains(x.CabinSeatMapId) && !x.IsDeleted)
                .GroupBy(x => x.CabinSeatMapId)
                .Select(g => new
                {
                    CabinSeatMapId = g.Key,
                    SeatCount = g.Count()
                })
                .ToDictionaryAsync(x => x.CabinSeatMapId, x => x.SeatCount, ct);

        var ancillaries = await _db.FlightAncillaryDefinitions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.AirlineId)
            .ThenBy(x => x.Type)
            .ThenBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.Code,
                x.Name,
                x.Type,
                x.CurrencyCode,
                x.Price,
                x.IsActive
            })
            .ToListAsync(ct);

        return Ok(new
        {
            locations,
            airlines,
            airports = airports.Select(x => new
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
                location = locationById.GetValueOrDefault(x.LocationId)
            }),
            aircraftModels,
            aircrafts = aircrafts.Select(x => new
            {
                x.Id,
                x.AircraftModelId,
                x.AirlineId,
                x.Code,
                x.Registration,
                x.Name,
                x.IsActive,
                airline = airlineById.GetValueOrDefault(x.AirlineId),
                aircraftModel = aircraftModelById.GetValueOrDefault(x.AircraftModelId)
            }),
            fareClasses = fareClasses.Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.Code,
                x.Name,
                CabinClass = x.CabinClass.ToString(),
                x.IsRefundable,
                x.IsChangeable,
                x.IsActive,
                airline = airlineById.GetValueOrDefault(x.AirlineId)
            }),
            fareRules = fareRules.Select(x => new
            {
                x.Id,
                x.FareClassId,
                x.IsActive,
                x.RulesJson,
                x.CreatedAt,
                x.UpdatedAt,
                fareClass = fareClassById.GetValueOrDefault(x.FareClassId)
            }),
            flights = flights.Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.AircraftId,
                x.FromAirportId,
                x.ToAirportId,
                x.FlightNumber,
                x.DepartureAt,
                x.ArrivalAt,
                Status = x.Status.ToString(),
                x.IsActive,
                x.Notes,
                airline = airlineById.GetValueOrDefault(x.AirlineId),
                aircraft = aircraftById.GetValueOrDefault(x.AircraftId),
                fromAirport = airportById.GetValueOrDefault(x.FromAirportId),
                toAirport = airportById.GetValueOrDefault(x.ToAirportId)
            }),
            offers = offers.Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.FlightId,
                x.FareClassId,
                Status = x.Status.ToString(),
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice,
                x.SeatsAvailable,
                x.RequestedAt,
                x.ExpiresAt,
                isExpired = x.ExpiresAt <= now,
                airline = airlineById.GetValueOrDefault(x.AirlineId),
                flight = flightById.GetValueOrDefault(x.FlightId),
                fareClass = fareClassById.GetValueOrDefault(x.FareClassId)
            }),
            seatMaps = seatMaps.Select(x => new
            {
                x.Id,
                x.AircraftModelId,
                CabinClass = x.CabinClass.ToString(),
                x.Code,
                x.Name,
                x.TotalRows,
                x.TotalColumns,
                x.DeckCount,
                x.LayoutVersion,
                x.SeatLabelScheme,
                x.IsActive,
                seatCount = seatCounts.GetValueOrDefault(x.Id),
                aircraftModel = aircraftModelById.GetValueOrDefault(x.AircraftModelId)
            }),
            ancillaries = ancillaries.Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.Code,
                x.Name,
                Type = x.Type.ToString(),
                x.CurrencyCode,
                x.Price,
                x.IsActive,
                airline = airlineById.GetValueOrDefault(x.AirlineId)
            })
        });
    }
}
