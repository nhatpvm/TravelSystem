using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Catalog;
using TicketBooking.Domain.Fleet;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlnx/bus/options")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX}")]
public sealed class QlNxBusOptionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlNxBusOptionsController(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetOptions(CancellationToken ct = default)
    {
        EnsureTenantScope();

        var tenantId = _tenantContext.TenantId!.Value;

        var locations = await _db.Locations.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Type,
                x.Name,
                x.ShortName,
                x.Code,
                x.AddressLine,
                x.IsActive
            })
            .ToListAsync(ct);

        var locationById = locations.ToDictionary(x => x.Id);

        var stopPoints = await _db.BusStopPoints.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.LocationId,
                x.Type,
                x.Name,
                x.AddressLine,
                x.SortOrder,
                x.IsActive
            })
            .ToListAsync(ct);

        var providers = await _db.Providers.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.Type == ProviderType.Bus && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.Slug,
                x.SupportPhone,
                x.LogoUrl,
                x.IsActive
            })
            .ToListAsync(ct);

        var seatMaps = await _db.SeatMaps.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                !x.IsDeleted &&
                (x.VehicleType == VehicleType.Bus || x.VehicleType == VehicleType.TourBus))
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.VehicleType,
                x.TotalRows,
                x.TotalColumns,
                x.DeckCount,
                x.IsActive
            })
            .ToListAsync(ct);

        var seatMapById = seatMaps.ToDictionary(x => x.Id);

        var vehicles = await _db.Vehicles.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == tenantId &&
                !x.IsDeleted &&
                (x.VehicleType == VehicleType.Bus || x.VehicleType == VehicleType.TourBus))
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.ProviderId,
                x.VehicleModelId,
                x.SeatMapId,
                x.VehicleType,
                x.Code,
                x.Name,
                x.PlateNumber,
                x.SeatCapacity,
                x.Status,
                x.IsActive
            })
            .ToListAsync(ct);

        var providerById = providers.ToDictionary(x => x.Id);
        var stopPointById = stopPoints.ToDictionary(x => x.Id);

        var routes = await _db.BusRoutes.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.ProviderId,
                x.Code,
                x.Name,
                x.FromStopPointId,
                x.ToStopPointId,
                x.EstimatedMinutes,
                x.DistanceKm,
                x.IsActive
            })
            .ToListAsync(ct);

        return Ok(new
        {
            locations,
            stopPoints = stopPoints.Select(x => new
            {
                x.Id,
                x.LocationId,
                x.Type,
                x.Name,
                x.AddressLine,
                x.SortOrder,
                x.IsActive,
                location = locationById.GetValueOrDefault(x.LocationId)
            }),
            providers,
            seatMaps,
            vehicles = vehicles.Select(x => new
            {
                x.Id,
                x.ProviderId,
                x.VehicleModelId,
                x.SeatMapId,
                x.VehicleType,
                x.Code,
                x.Name,
                x.PlateNumber,
                x.SeatCapacity,
                x.Status,
                x.IsActive,
                provider = providerById.GetValueOrDefault(x.ProviderId),
                seatMap = x.SeatMapId.HasValue ? seatMapById.GetValueOrDefault(x.SeatMapId.Value) : null
            }),
            routes = routes.Select(x => new
            {
                x.Id,
                x.ProviderId,
                x.Code,
                x.Name,
                x.FromStopPointId,
                x.ToStopPointId,
                x.EstimatedMinutes,
                x.DistanceKm,
                x.IsActive,
                provider = providerById.GetValueOrDefault(x.ProviderId),
                fromStopPoint = stopPointById.GetValueOrDefault(x.FromStopPointId),
                toStopPoint = stopPointById.GetValueOrDefault(x.ToStopPointId)
            })
        });
    }

    private void EnsureTenantScope()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required.");
    }
}
