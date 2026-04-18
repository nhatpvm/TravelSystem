//FILE: TicketBooking.Api/Controllers/QLVMM/QLVMMFlightAncillariesController.cs
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
[Route("api/v{version:apiVersion}/qlvmm/flight/ancillaries")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class QLVMMFlightAncillariesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QLVMMFlightAncillariesController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? airlineId,
        [FromQuery] Guid? offerId,
        [FromQuery] AncillaryType? type,
        [FromQuery] string? q,
        [FromQuery] bool includeInactive = true,
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

        Guid? resolvedAirlineId = airlineId;

        if (offerId.HasValue && offerId.Value != Guid.Empty)
        {
            var offer = await _db.FlightOffers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == offerId.Value && !x.IsDeleted)
                .Select(x => new
                {
                    x.Id,
                    x.AirlineId,
                    x.FlightId,
                    x.FareClassId,
                    x.Status,
                    x.ExpiresAt,
                    x.SeatsAvailable
                })
                .FirstOrDefaultAsync(ct);

            if (offer is null)
                return NotFound(new { message = "Offer not found." });

            resolvedAirlineId = offer.AirlineId;
        }

        IQueryable<AncillaryDefinition> query = _db.FlightAncillaryDefinitions.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (resolvedAirlineId.HasValue && resolvedAirlineId.Value != Guid.Empty)
            query = query.Where(x => x.AirlineId == resolvedAirlineId.Value);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var uq = q.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.Code.ToUpper().Contains(uq) ||
                x.Name.ToUpper().Contains(uq) ||
                x.CurrencyCode.ToUpper().Contains(uq) ||
                (x.RulesJson != null && x.RulesJson.ToUpper().Contains(uq)));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(x => x.AirlineId)
            .ThenBy(x => x.Type)
            .ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.Code,
                x.Name,
                x.Type,
                x.CurrencyCode,
                x.Price,
                x.RulesJson,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        var airlineIds = rows.Select(x => x.AirlineId).Distinct().ToList();

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

        var items = rows.Select(x =>
        {
            airlines.TryGetValue(x.AirlineId, out var airline);

            return new
            {
                x.Id,
                x.Code,
                x.Name,
                Type = x.Type.ToString(),
                x.CurrencyCode,
                x.Price,
                x.RulesJson,
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
                    airline.IsActive,
                    airline.IsDeleted
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

        IQueryable<AncillaryDefinition> query = _db.FlightAncillaryDefinitions.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var item = await query
            .Where(x => x.TenantId == tenantId && x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.Code,
                x.Name,
                x.Type,
                x.CurrencyCode,
                x.Price,
                x.RulesJson,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (item is null)
            return NotFound(new { message = "Ancillary not found." });

        var airline = await _db.FlightAirlines
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == item.AirlineId)
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

        return Ok(new
        {
            item = new
            {
                item.Id,
                item.Code,
                item.Name,
                Type = item.Type.ToString(),
                item.CurrencyCode,
                item.Price,
                item.RulesJson,
                item.IsActive,
                item.IsDeleted,
                item.CreatedAt,
                item.UpdatedAt
            },
            airline
        });
    }

    [HttpGet("by-offer/{offerId:guid}")]
    public async Task<IActionResult> GetByOffer(
        [FromRoute] Guid offerId,
        [FromQuery] AncillaryType? type,
        [FromQuery] bool includeInactive = true,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        if (offerId == Guid.Empty)
            return BadRequest(new { message = "offerId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;

        var offer = await _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == offerId && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.AirlineId,
                x.FlightId,
                x.FareClassId,
                x.Status,
                x.CurrencyCode,
                x.TotalPrice,
                x.SeatsAvailable,
                x.RequestedAt,
                x.ExpiresAt,
                x.ConditionsJson,
                x.MetadataJson
            })
            .FirstOrDefaultAsync(ct);

        if (offer is null)
            return NotFound(new { message = "Offer not found." });

        var flight = await _db.FlightFlights
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == offer.FlightId)
            .Select(x => new
            {
                x.Id,
                x.FlightNumber,
                x.DepartureAt,
                x.ArrivalAt,
                x.FromAirportId,
                x.ToAirportId,
                x.Status,
                x.IsActive,
                x.IsDeleted
            })
            .FirstOrDefaultAsync(ct);

        var fareClass = await _db.FlightFareClasses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == offer.FareClassId)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.CabinClass,
                x.IsRefundable,
                x.IsChangeable,
                x.IsActive,
                x.IsDeleted
            })
            .FirstOrDefaultAsync(ct);

        IQueryable<AncillaryDefinition> ancillaryQuery = _db.FlightAncillaryDefinitions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId &&
                x.AirlineId == offer.AirlineId &&
                !x.IsDeleted);

        if (type.HasValue)
            ancillaryQuery = ancillaryQuery.Where(x => x.Type == type.Value);

        if (!includeInactive)
            ancillaryQuery = ancillaryQuery.Where(x => x.IsActive);

        var ancillaries = await ancillaryQuery
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Type,
                x.CurrencyCode,
                x.Price,
                x.RulesJson,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

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
                x.IsActive,
                x.IsDeleted
            })
            .FirstOrDefaultAsync(ct);

        return Ok(new
        {
            offer = new
            {
                offer.Id,
                Status = offer.Status.ToString(),
                offer.CurrencyCode,
                offer.TotalPrice,
                offer.SeatsAvailable,
                offer.RequestedAt,
                offer.ExpiresAt,
                isExpired = offer.ExpiresAt <= now
            },
            airline,
            flight = flight is null ? null : new
            {
                flight.Id,
                flight.FlightNumber,
                flight.DepartureAt,
                flight.ArrivalAt,
                Status = flight.Status.ToString(),
                flight.IsActive,
                flight.IsDeleted
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
            ancillarySummary = new
            {
                total = ancillaries.Count,
                active = ancillaries.Count(x => x.IsActive && !x.IsDeleted)
            },
            items = ancillaries.Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                Type = x.Type.ToString(),
                x.CurrencyCode,
                x.Price,
                x.RulesJson,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            }).ToList()
        });
    }
}
