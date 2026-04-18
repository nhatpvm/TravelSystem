//FILE: TicketBooking.Api/Controllers/QLVMM/QLVMMFlightSeatMapsController.cs
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
[Route("api/v{version:apiVersion}/qlvmm/flight/seat-maps")]
[Authorize(Roles = "Admin,QLVMM")]
public sealed class QLVMMFlightSeatMapsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QLVMMFlightSeatMapsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? aircraftModelId,
        [FromQuery] CabinClass? cabinClass,
        [FromQuery] Guid? flightId,
        [FromQuery] Guid? offerId,
        [FromQuery] string? q,
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

        Guid? resolvedAircraftModelId = aircraftModelId;
        CabinClass? resolvedCabinClass = cabinClass;

        if (flightId.HasValue && flightId.Value != Guid.Empty)
        {
            var flightInfo = await _db.FlightFlights
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == flightId.Value && !x.IsDeleted)
                .Join(
                    _db.FlightAircrafts.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted),
                    f => f.AircraftId,
                    a => a.Id,
                    (f, a) => new
                    {
                        f.Id,
                        a.AircraftModelId
                    })
                .FirstOrDefaultAsync(ct);

            if (flightInfo is null)
                return NotFound(new { message = "Flight not found." });

            resolvedAircraftModelId = flightInfo.AircraftModelId;
        }

        if (offerId.HasValue && offerId.Value != Guid.Empty)
        {
            var offerInfo = await _db.FlightOffers
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.Id == offerId.Value && !x.IsDeleted)
                .Join(
                    _db.FlightFlights.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted),
                    o => o.FlightId,
                    f => f.Id,
                    (o, f) => new { o, f })
                .Join(
                    _db.FlightAircrafts.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted),
                    x => x.f.AircraftId,
                    a => a.Id,
                    (x, a) => new { x.o, x.f, a })
                .Join(
                    _db.FlightFareClasses.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted),
                    x => x.o.FareClassId,
                    fc => fc.Id,
                    (x, fc) => new
                    {
                        x.o.Id,
                        AircraftModelId = x.a.AircraftModelId,
                        fc.CabinClass
                    })
                .FirstOrDefaultAsync(ct);

            if (offerInfo is null)
                return NotFound(new { message = "Offer not found." });

            resolvedAircraftModelId = offerInfo.AircraftModelId;
            resolvedCabinClass = offerInfo.CabinClass;
        }

        IQueryable<CabinSeatMap> query = _db.FlightCabinSeatMaps.AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (resolvedAircraftModelId.HasValue && resolvedAircraftModelId.Value != Guid.Empty)
            query = query.Where(x => x.AircraftModelId == resolvedAircraftModelId.Value);

        if (resolvedCabinClass.HasValue)
            query = query.Where(x => x.CabinClass == resolvedCabinClass.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var uq = q.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.Code.ToUpper().Contains(uq) ||
                x.Name.ToUpper().Contains(uq) ||
                (x.LayoutVersion != null && x.LayoutVersion.ToUpper().Contains(uq)) ||
                (x.SeatLabelScheme != null && x.SeatLabelScheme.ToUpper().Contains(uq)));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(x => x.AircraftModelId)
            .ThenBy(x => x.CabinClass)
            .ThenBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        var seatMapIds = rows.Select(x => x.Id).ToList();
        var aircraftModelIds = rows.Select(x => x.AircraftModelId).Distinct().ToList();

        var seatCounts = await _db.FlightCabinSeats
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && seatMapIds.Contains(x.CabinSeatMapId))
            .GroupBy(x => x.CabinSeatMapId)
            .Select(g => new
            {
                CabinSeatMapId = g.Key,
                TotalSeats = g.Count(),
                ActiveSeats = g.Count(x => !x.IsDeleted && x.IsActive),
                DeletedSeats = g.Count(x => x.IsDeleted)
            })
            .ToDictionaryAsync(x => x.CabinSeatMapId, ct);

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
                x.IsActive,
                x.IsDeleted
            })
            .ToDictionaryAsync(x => x.Id, ct);

        var items = rows.Select(x =>
        {
            aircraftModels.TryGetValue(x.AircraftModelId, out var model);
            seatCounts.TryGetValue(x.Id, out var counts);

            return new
            {
                x.Id,
                x.Code,
                x.Name,
                CabinClass = x.CabinClass.ToString(),
                x.TotalRows,
                x.TotalColumns,
                x.DeckCount,
                x.LayoutVersion,
                x.SeatLabelScheme,
                x.IsActive,
                x.IsDeleted,
                aircraftModel = model is null ? null : new
                {
                    model.Id,
                    model.Code,
                    model.Manufacturer,
                    model.Model,
                    model.TypicalSeatCapacity,
                    model.IsActive,
                    model.IsDeleted
                },
                seatSummary = new
                {
                    totalSeats = counts?.TotalSeats ?? 0,
                    activeSeats = counts?.ActiveSeats ?? 0,
                    deletedSeats = counts?.DeletedSeats ?? 0
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
        [FromQuery] int seatPage = 1,
        [FromQuery] int seatPageSize = 200,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        var tenantId = _tenant.TenantId.Value;

        seatPage = seatPage < 1 ? 1 : seatPage;
        seatPageSize = seatPageSize < 1 ? 200 : (seatPageSize > 1000 ? 1000 : seatPageSize);

        IQueryable<CabinSeatMap> seatMapQuery = _db.FlightCabinSeatMaps.AsNoTracking();
        if (includeDeleted)
            seatMapQuery = seatMapQuery.IgnoreQueryFilters();

        var seatMap = await seatMapQuery
            .Where(x => x.TenantId == tenantId && x.Id == id)
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
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (seatMap is null)
            return NotFound(new { message = "Seat map not found." });

        var aircraftModel = await _db.FlightAircraftModels
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == seatMap.AircraftModelId)
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

        IQueryable<CabinSeat> seatsQuery = _db.FlightCabinSeats.AsNoTracking();
        if (includeDeleted)
            seatsQuery = seatsQuery.IgnoreQueryFilters();

        seatsQuery = seatsQuery.Where(x => x.TenantId == tenantId && x.CabinSeatMapId == seatMap.Id);

        var totalSeats = await seatsQuery.CountAsync(ct);

        var seats = await seatsQuery
            .OrderBy(x => x.DeckIndex)
            .ThenBy(x => x.RowIndex)
            .ThenBy(x => x.ColumnIndex)
            .ThenBy(x => x.SeatNumber)
            .Skip((seatPage - 1) * seatPageSize)
            .Take(seatPageSize)
            .Select(x => new
            {
                x.Id,
                x.SeatNumber,
                x.RowIndex,
                x.ColumnIndex,
                x.DeckIndex,
                x.IsWindow,
                x.IsAisle,
                x.SeatType,
                x.SeatClass,
                x.PriceModifier,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        var deckSummaries = await _db.FlightCabinSeats
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.CabinSeatMapId == seatMap.Id)
            .GroupBy(x => x.DeckIndex)
            .Select(g => new
            {
                DeckIndex = g.Key,
                TotalSeats = g.Count(),
                ActiveSeats = g.Count(x => !x.IsDeleted && x.IsActive)
            })
            .OrderBy(x => x.DeckIndex)
            .ToListAsync(ct);

        return Ok(new
        {
            seatMap = new
            {
                seatMap.Id,
                seatMap.Code,
                seatMap.Name,
                CabinClass = seatMap.CabinClass.ToString(),
                seatMap.TotalRows,
                seatMap.TotalColumns,
                seatMap.DeckCount,
                seatMap.LayoutVersion,
                seatMap.SeatLabelScheme,
                seatMap.IsActive,
                seatMap.IsDeleted,
                seatMap.CreatedAt,
                seatMap.UpdatedAt
            },
            aircraftModel,
            deckSummaries,
            seats = new
            {
                page = seatPage,
                pageSize = seatPageSize,
                total = totalSeats,
                items = seats
            }
        });
    }

    [HttpGet("by-offer/{offerId:guid}")]
    public async Task<IActionResult> GetByOffer(
        [FromRoute] Guid offerId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || _tenant.TenantId is null || _tenant.TenantId == Guid.Empty)
            return BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

        if (offerId == Guid.Empty)
            return BadRequest(new { message = "offerId is required." });

        var tenantId = _tenant.TenantId.Value;

        var offerInfo = await _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Id == offerId && !x.IsDeleted)
            .Join(
                _db.FlightFlights.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted),
                o => o.FlightId,
                f => f.Id,
                (o, f) => new { o, f })
            .Join(
                _db.FlightAircrafts.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted),
                x => x.f.AircraftId,
                a => a.Id,
                (x, a) => new { x.o, x.f, a })
            .Join(
                _db.FlightFareClasses.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted),
                x => x.o.FareClassId,
                fc => fc.Id,
                (x, fc) => new
                {
                    x.o.Id,
                    OfferStatus = x.o.Status,
                    x.o.ExpiresAt,
                    x.o.SeatsAvailable,
                    FlightId = x.f.Id,
                    x.f.FlightNumber,
                    x.f.DepartureAt,
                    x.f.ArrivalAt,
                    AircraftModelId = x.a.AircraftModelId,
                    fc.CabinClass,
                    FareClassId = fc.Id,
                    FareClassCode = fc.Code,
                    FareClassName = fc.Name
                })
            .FirstOrDefaultAsync(ct);

        if (offerInfo is null)
            return NotFound(new { message = "Offer not found." });

        IQueryable<CabinSeatMap> seatMapQuery = _db.FlightCabinSeatMaps.AsNoTracking();
        if (includeDeleted)
            seatMapQuery = seatMapQuery.IgnoreQueryFilters();

        var seatMap = await seatMapQuery
            .Where(x =>
                x.TenantId == tenantId &&
                x.AircraftModelId == offerInfo.AircraftModelId &&
                x.CabinClass == offerInfo.CabinClass)
            .OrderBy(x => x.Code)
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
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (seatMap is null)
            return NotFound(new { message = "No seat map matched this offer." });

        var seatCount = await _db.FlightCabinSeats
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.CabinSeatMapId == seatMap.Id && !x.IsDeleted)
            .CountAsync(ct);

        return Ok(new
        {
            offer = new
            {
                offerInfo.Id,
                Status = offerInfo.OfferStatus.ToString(),
                offerInfo.ExpiresAt,
                offerInfo.SeatsAvailable
            },
            flight = new
            {
                offerInfo.FlightId,
                offerInfo.FlightNumber,
                offerInfo.DepartureAt,
                offerInfo.ArrivalAt
            },
            fareClass = new
            {
                offerInfo.FareClassId,
                offerInfo.FareClassCode,
                offerInfo.FareClassName,
                CabinClass = offerInfo.CabinClass.ToString()
            },
            seatMap = new
            {
                seatMap.Id,
                seatMap.Code,
                seatMap.Name,
                CabinClass = seatMap.CabinClass.ToString(),
                seatMap.TotalRows,
                seatMap.TotalColumns,
                seatMap.DeckCount,
                seatMap.LayoutVersion,
                seatMap.SeatLabelScheme,
                seatMap.IsActive,
                seatMap.IsDeleted,
                totalSeats = seatCount
            }
        });
    }
}
