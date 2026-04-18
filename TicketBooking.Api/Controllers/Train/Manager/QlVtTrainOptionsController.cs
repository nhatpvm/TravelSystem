using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Catalog;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlvt/train/options")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLVT}")]
public sealed class QlVtTrainOptionsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public QlVtTrainOptionsController(AppDbContext db, ITenantContext tenantContext)
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
                x.TrainStationCode,
                x.AddressLine,
                x.IsActive
            })
            .ToListAsync(ct);

        var locationById = locations.ToDictionary(x => x.Id);

        var stopPoints = await _db.TrainStopPoints.IgnoreQueryFilters()
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

        var stopPointById = stopPoints.ToDictionary(x => x.Id);

        var providers = await _db.Providers.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.Type == ProviderType.Train && !x.IsDeleted)
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

        var providerById = providers.ToDictionary(x => x.Id);

        var routes = await _db.TrainRoutes.IgnoreQueryFilters()
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

        var routeById = routes.ToDictionary(x => x.Id);

        var trips = await _db.TrainTrips.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderByDescending(x => x.DepartureAt)
            .Select(x => new
            {
                x.Id,
                x.ProviderId,
                x.RouteId,
                x.TrainNumber,
                x.Code,
                x.Name,
                x.Status,
                x.DepartureAt,
                x.ArrivalAt,
                x.IsActive
            })
            .ToListAsync(ct);

        var cars = await _db.TrainCars.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CarNumber)
            .Select(x => new
            {
                x.Id,
                x.TripId,
                x.CarNumber,
                x.CarType,
                x.CabinClass,
                x.SortOrder,
                x.IsActive
            })
            .ToListAsync(ct);

        var carIds = cars.Select(x => x.Id).ToList();
        var seatCounts = carIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _db.TrainCarSeats.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && carIds.Contains(x.CarId) && !x.IsDeleted)
                .GroupBy(x => x.CarId)
                .Select(g => new { CarId = g.Key, SeatCount = g.Count() })
                .ToDictionaryAsync(x => x.CarId, x => x.SeatCount, ct);

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
            }),
            trips = trips.Select(x => new
            {
                x.Id,
                x.ProviderId,
                x.RouteId,
                x.TrainNumber,
                x.Code,
                x.Name,
                x.Status,
                x.DepartureAt,
                x.ArrivalAt,
                x.IsActive,
                provider = providerById.GetValueOrDefault(x.ProviderId),
                route = routeById.GetValueOrDefault(x.RouteId)
            }),
            cars = cars.Select(x => new
            {
                x.Id,
                x.TripId,
                x.CarNumber,
                x.CarType,
                x.CabinClass,
                x.SortOrder,
                x.IsActive,
                seatCount = seatCounts.GetValueOrDefault(x.Id),
                trip = trips.FirstOrDefault(trip => trip.Id == x.TripId)
            })
        });
    }

    private void EnsureTenantScope()
    {
        if (!_tenantContext.HasTenant)
            throw new InvalidOperationException("X-TenantId is required.");
    }
}
