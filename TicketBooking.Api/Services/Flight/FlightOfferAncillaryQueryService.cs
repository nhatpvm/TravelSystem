//FILE: TicketBooking.Api/Services/Flight/FlightOfferAncillaryQueryService.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Contracts.Flight;
using TicketBooking.Domain.Flight;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Services.Flight;

/// <summary>
/// Public/customer read-only ancillary query service for Flight.
/// Supports 2 modes:
/// - Scoped mode: when X-TenantId is already resolved by middleware.
/// - Public/global mode: when tenant is not supplied, resolve tenant automatically by OfferId.
/// </summary>
public sealed class FlightOfferAncillaryQueryService : IFlightOfferAncillaryQueryService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IFlightPublicTenantResolver _tenantResolver;

    public FlightOfferAncillaryQueryService(
        AppDbContext db,
        ITenantContext tenant,
        IFlightPublicTenantResolver tenantResolver)
    {
        _db = db;
        _tenant = tenant;
        _tenantResolver = tenantResolver;
    }

    public async Task<FlightOfferAncillaryResponse?> GetByOfferAsync(
        FlightOfferAncillaryRequest request,
        CancellationToken ct = default)
    {
        if (request.OfferId == Guid.Empty)
            throw new InvalidOperationException("offerId is required.");

        var tenantId = await ResolveTenantIdForOfferAsync(
            request.OfferId,
            request.IncludeExpired,
            ct);

        if (!tenantId.HasValue)
            return null;

        var now = DateTimeOffset.Now;

        IQueryable<Offer> offerQuery = _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId.Value &&
                x.Id == request.OfferId &&
                !x.IsDeleted);

        if (!request.IncludeExpired)
        {
            offerQuery = offerQuery.Where(x =>
                x.Status == OfferStatus.Active &&
                x.ExpiresAt > now);
        }

        var offerRow = await offerQuery
            .Join(
                _db.FlightFlights
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.TenantId == tenantId.Value && !x.IsDeleted),
                o => o.FlightId,
                f => f.Id,
                (o, f) => new { o, f })
            .Join(
                _db.FlightAirlines
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.TenantId == tenantId.Value && !x.IsDeleted),
                x => x.f.AirlineId,
                a => a.Id,
                (x, a) => new { x.o, x.f, a })
            .Join(
                _db.FlightFareClasses
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(x => x.TenantId == tenantId.Value && !x.IsDeleted),
                x => x.o.FareClassId,
                fc => fc.Id,
                (x, fc) => new { x.o, x.f, x.a, fc })
            .FirstOrDefaultAsync(ct);

        if (offerRow is null)
            return null;

        var currentSeatsAvailable = await FlightOfferSupport.ResolveCurrentSeatsAvailableAsync(
            _db,
            offerRow.o.TenantId,
            offerRow.o.FlightId,
            offerRow.o.FareClassId,
            now,
            ct);

        var segments = await _db.FlightOfferSegments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s =>
                s.TenantId == tenantId.Value &&
                s.OfferId == request.OfferId &&
                !s.IsDeleted)
            .OrderBy(s => s.SegmentIndex)
            .ToListAsync(ct);

        var airportIds = segments
            .SelectMany(s => new[] { s.FromAirportId, s.ToAirportId })
            .Concat(new[] { offerRow.f.FromAirportId, offerRow.f.ToAirportId })
            .Distinct()
            .ToList();

        var airportsMap = await _db.FlightAirports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId.Value &&
                airportIds.Contains(x.Id) &&
                !x.IsDeleted)
            .ToDictionaryAsync(
                x => x.Id,
                x => new FlightOfferAncillaryAirportDto
                {
                    Id = x.Id,
                    LocationId = x.LocationId,
                    Name = x.Name,
                    Code = x.Code,
                    IataCode = x.IataCode,
                    IcaoCode = x.IcaoCode,
                    TimeZone = x.TimeZone
                },
                ct);

        var route = BuildRouteInfo(offerRow.f, segments);

        IQueryable<AncillaryDefinition> ancillaryQuery = _db.FlightAncillaryDefinitions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TenantId == tenantId.Value &&
                x.AirlineId == offerRow.a.Id &&
                !x.IsDeleted);

        if (request.Type.HasValue)
            ancillaryQuery = ancillaryQuery.Where(x => x.Type == request.Type.Value);

        if (!request.IncludeInactive)
            ancillaryQuery = ancillaryQuery.Where(x => x.IsActive);

        var ancillaries = await ancillaryQuery
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Code)
            .Select(x => new FlightOfferAncillaryItemDto
            {
                Id = x.Id,
                AirlineId = x.AirlineId,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type.ToString(),
                CurrencyCode = x.CurrencyCode,
                Price = x.Price,
                RulesJson = x.RulesJson,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        var grouped = ancillaries
            .GroupBy(x => x.Type, StringComparer.OrdinalIgnoreCase)
            .Select(g => new FlightOfferAncillaryGroupSummaryDto
            {
                Type = g.Key,
                Count = g.Count(),
                ActiveCount = g.Count(x => x.IsActive)
            })
            .OrderBy(x => x.Type)
            .ToList();

        return new FlightOfferAncillaryResponse
        {
            Offer = new FlightOfferAncillaryOfferDto
            {
                Id = offerRow.o.Id,
                Status = offerRow.o.Status.ToString(),
                CurrencyCode = offerRow.o.CurrencyCode,
                BaseFare = offerRow.o.BaseFare,
                TaxesFees = offerRow.o.TaxesFees,
                TotalPrice = offerRow.o.TotalPrice,
                SeatsAvailable = currentSeatsAvailable,
                RequestedAt = offerRow.o.RequestedAt,
                ExpiresAt = offerRow.o.ExpiresAt,
                IsExpired = offerRow.o.ExpiresAt <= now,
                ConditionsJson = offerRow.o.ConditionsJson,
                MetadataJson = offerRow.o.MetadataJson
            },
            Airline = new FlightOfferAncillaryAirlineDto
            {
                Id = offerRow.a.Id,
                Code = offerRow.a.Code,
                Name = offerRow.a.Name,
                IataCode = offerRow.a.IataCode,
                IcaoCode = offerRow.a.IcaoCode,
                LogoUrl = offerRow.a.LogoUrl,
                WebsiteUrl = offerRow.a.WebsiteUrl,
                SupportPhone = offerRow.a.SupportPhone,
                SupportEmail = offerRow.a.SupportEmail
            },
            Flight = new FlightOfferAncillaryFlightDto
            {
                Id = offerRow.f.Id,
                FlightNumber = route.DisplayFlightNumber,
                DepartureAt = route.DepartureAt,
                ArrivalAt = route.ArrivalAt,
                Status = offerRow.f.Status.ToString(),
                From = airportsMap.TryGetValue(route.FromAirportId, out var fromAirport)
                    ? fromAirport
                    : new FlightOfferAncillaryAirportDto
                    {
                        Id = route.FromAirportId,
                        LocationId = Guid.Empty,
                        Name = string.Empty,
                        Code = string.Empty
                    },
                To = airportsMap.TryGetValue(route.ToAirportId, out var toAirport)
                    ? toAirport
                    : new FlightOfferAncillaryAirportDto
                    {
                        Id = route.ToAirportId,
                        LocationId = Guid.Empty,
                        Name = string.Empty,
                        Code = string.Empty
                    }
            },
            FareClass = new FlightOfferAncillaryFareClassDto
            {
                Id = offerRow.fc.Id,
                Code = offerRow.fc.Code,
                Name = offerRow.fc.Name,
                CabinClass = offerRow.fc.CabinClass.ToString(),
                IsRefundable = offerRow.fc.IsRefundable,
                IsChangeable = offerRow.fc.IsChangeable
            },
            Summary = new FlightOfferAncillarySummaryDto
            {
                Total = ancillaries.Count,
                Active = ancillaries.Count(x => x.IsActive),
                Inactive = ancillaries.Count(x => !x.IsActive),
                Groups = grouped
            },
            Items = ancillaries
        };
    }

    private async Task<Guid?> ResolveTenantIdForOfferAsync(
        Guid offerId,
        bool includeExpired,
        CancellationToken ct)
    {
        if (_tenant.HasTenant && _tenant.TenantId.HasValue && _tenant.TenantId.Value != Guid.Empty)
            return _tenant.TenantId.Value;

        return await _tenantResolver.ResolveTenantIdForOfferAsync(offerId, includeExpired, ct);
    }

    private static FlightRouteInfo BuildRouteInfo(
        TicketBooking.Domain.Flight.Flight flight,
        IReadOnlyList<OfferSegment> segments)
    {
        if (segments.Count == 0)
        {
            return new FlightRouteInfo(
                flight.FromAirportId,
                flight.ToAirportId,
                flight.DepartureAt,
                flight.ArrivalAt,
                FlightOfferSupport.BuildDisplayFlightNumber(new[] { flight.FlightNumber }, flight.FlightNumber));
        }

        var first = segments[0];
        var last = segments[^1];

        return new FlightRouteInfo(
            first.FromAirportId,
            last.ToAirportId,
            first.DepartureAt,
            last.ArrivalAt,
            FlightOfferSupport.BuildDisplayFlightNumber(segments.Select(x => x.FlightNumber), flight.FlightNumber));
    }

    private sealed record FlightRouteInfo(
        Guid FromAirportId,
        Guid ToAirportId,
        DateTimeOffset DepartureAt,
        DateTimeOffset ArrivalAt,
        string DisplayFlightNumber);
}
