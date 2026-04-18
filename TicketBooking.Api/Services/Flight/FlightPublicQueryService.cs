using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Contracts.Flight;
using TicketBooking.Domain.Flight;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Services.Flight;

/// <summary>
/// Public/customer read-only queries for Flight.
/// Supports 2 modes:
/// - Scoped mode: when X-TenantId is already resolved by middleware.
/// - Public/global mode: when tenant is not supplied, resolve tenant(s) automatically.
/// </summary>
public sealed class FlightPublicQueryService : IFlightPublicQueryService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IFlightPublicTenantResolver _tenantResolver;

    public FlightPublicQueryService(
        AppDbContext db,
        ITenantContext tenant,
        IFlightPublicTenantResolver tenantResolver)
    {
        _db = db;
        _tenant = tenant;
        _tenantResolver = tenantResolver;
    }

    public async Task<FlightSearchResponse> SearchAsync(
        FlightSearchRequest request,
        CancellationToken ct = default)
    {
        var from = NormalizeCode(request.From);
        var to = NormalizeCode(request.To);

        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to))
        {
            return new FlightSearchResponse
            {
                Count = 0,
                Message = "Query 'from' and 'to' are required."
            };
        }

        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
        {
            return new FlightSearchResponse
            {
                Count = 0,
                Message = "'from' and 'to' must be different."
            };
        }

        var tenantIds = await ResolveTenantIdsForSearchAsync(from, to, ct);
        if (tenantIds.Count == 0)
        {
            return new FlightSearchResponse
            {
                Count = 0,
                Message = "No airports matched."
            };
        }

        var now = DateTimeOffset.Now;

        var airportBaseQuery = _db.FlightAirports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                tenantIds.Contains(x.TenantId) &&
                !x.IsDeleted &&
                x.IsActive);

        var fromAirportIds = await airportBaseQuery
            .Where(x =>
                x.Code.ToUpper() == from ||
                (x.IataCode != null && x.IataCode.ToUpper() == from))
            .Select(x => x.Id)
            .Distinct()
            .ToListAsync(ct);

        var toAirportIds = await airportBaseQuery
            .Where(x =>
                x.Code.ToUpper() == to ||
                (x.IataCode != null && x.IataCode.ToUpper() == to))
            .Select(x => x.Id)
            .Distinct()
            .ToListAsync(ct);

        if (fromAirportIds.Count == 0 || toAirportIds.Count == 0)
        {
            return new FlightSearchResponse
            {
                Count = 0,
                Message = "No airports matched."
            };
        }

        var offerRows = await _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o =>
                tenantIds.Contains(o.TenantId) &&
                !o.IsDeleted &&
                o.Status == OfferStatus.Active &&
                o.ExpiresAt > now)
            .Join(
                _db.FlightFlights
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(f =>
                        tenantIds.Contains(f.TenantId) &&
                        !f.IsDeleted &&
                        f.IsActive &&
                        f.Status == FlightStatus.Published),
                o => o.FlightId,
                f => f.Id,
                (o, f) => new { o, f })
            .Join(
                _db.FlightAirlines
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(a =>
                        tenantIds.Contains(a.TenantId) &&
                        !a.IsDeleted),
                x => x.f.AirlineId,
                a => a.Id,
                (x, a) => new { x.o, x.f, a })
            .Join(
                _db.FlightFareClasses
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(fc =>
                        tenantIds.Contains(fc.TenantId) &&
                        !fc.IsDeleted),
                x => x.o.FareClassId,
                fc => fc.Id,
                (x, fc) => new { x.o, x.f, x.a, fc })
            .ToListAsync(ct);

        if (offerRows.Count == 0)
        {
            return new FlightSearchResponse
            {
                Count = 0,
                Message = "No offers matched."
            };
        }

        var canonicalOffers = offerRows
            .GroupBy(x => new { x.o.TenantId, x.o.FlightId, x.o.FareClassId })
            .Select(g => g
                .OrderByDescending(x => x.o.RequestedAt)
                .ThenByDescending(x => x.o.CreatedAt)
                .ThenByDescending(x => x.o.Id)
                .First())
            .ToList();

        var offerIds = canonicalOffers.Select(x => x.o.Id).Distinct().ToList();

        var segments = await _db.FlightOfferSegments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s =>
                tenantIds.Contains(s.TenantId) &&
                offerIds.Contains(s.OfferId) &&
                !s.IsDeleted)
            .OrderBy(s => s.OfferId)
            .ThenBy(s => s.SegmentIndex)
            .ToListAsync(ct);

        var taxFeeLines = await _db.FlightOfferTaxFeeLines
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l =>
                tenantIds.Contains(l.TenantId) &&
                offerIds.Contains(l.OfferId) &&
                !l.IsDeleted)
            .OrderBy(l => l.OfferId)
            .ThenBy(l => l.SortOrder)
            .ToListAsync(ct);

        var segmentMap = segments
            .GroupBy(x => x.OfferId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<OfferSegment>)g
                    .OrderBy(x => x.SegmentIndex)
                    .ToList());

        var airportIds = segments
            .SelectMany(s => new[] { s.FromAirportId, s.ToAirportId })
            .Concat(canonicalOffers.SelectMany(x => new[] { x.f.FromAirportId, x.f.ToAirportId }))
            .Distinct()
            .ToList();

        var airportsMap = await _db.FlightAirports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(a =>
                tenantIds.Contains(a.TenantId) &&
                airportIds.Contains(a.Id) &&
                !a.IsDeleted)
            .ToDictionaryAsync(
                a => a.Id,
                a => new FlightAirportLiteDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Code = a.Code,
                    IataCode = a.IataCode,
                    IcaoCode = a.IcaoCode,
                    TimeZone = a.TimeZone
                },
                ct);

        var airportTimeZones = airportsMap.ToDictionary(x => x.Key, x => x.Value.TimeZone);

        var items = canonicalOffers.Select(x =>
        {
            segmentMap.TryGetValue(x.o.Id, out var offerSegments);
            offerSegments ??= Array.Empty<OfferSegment>();

            var route = BuildRouteInfo(x.f, offerSegments, airportTimeZones);
            if (!fromAirportIds.Contains(route.FromAirportId) ||
                !toAirportIds.Contains(route.ToAirportId) ||
                FlightOfferSupport.ResolveLocalDate(route.DepartureAt, route.FromAirportTimeZone) != request.Date ||
                x.o.SeatsAvailable <= 0)
            {
                return null;
            }

            var segs = offerSegments.Count > 0
                ? offerSegments.Select(s => new FlightOfferSegmentSummaryDto
                {
                    SegmentIndex = s.SegmentIndex,
                    From = airportsMap.TryGetValue(s.FromAirportId, out var fromAirport)
                        ? fromAirport
                        : new FlightAirportLiteDto
                        {
                            Id = s.FromAirportId,
                            Name = string.Empty,
                            Code = string.Empty
                        },
                    To = airportsMap.TryGetValue(s.ToAirportId, out var toAirport)
                        ? toAirport
                        : new FlightAirportLiteDto
                        {
                            Id = s.ToAirportId,
                            Name = string.Empty,
                            Code = string.Empty
                        },
                    FlightNumber = s.FlightNumber,
                    DepartureAt = s.DepartureAt,
                    ArrivalAt = s.ArrivalAt
                })
                .ToList()
                : new List<FlightOfferSegmentSummaryDto>
                {
                    new()
                    {
                        SegmentIndex = 0,
                        From = airportsMap.TryGetValue(x.f.FromAirportId, out var fromAirport)
                            ? fromAirport
                            : new FlightAirportLiteDto
                            {
                                Id = x.f.FromAirportId,
                                Name = string.Empty,
                                Code = string.Empty
                            },
                        To = airportsMap.TryGetValue(x.f.ToAirportId, out var toAirport)
                            ? toAirport
                            : new FlightAirportLiteDto
                            {
                                Id = x.f.ToAirportId,
                                Name = string.Empty,
                                Code = string.Empty
                            },
                        FlightNumber = x.f.FlightNumber,
                        DepartureAt = x.f.DepartureAt,
                        ArrivalAt = x.f.ArrivalAt
                    }
                };

            var lines = taxFeeLines
                .Where(l => l.OfferId == x.o.Id)
                .Select(l => new FlightOfferTaxFeeLineSummaryDto
                {
                    SortOrder = l.SortOrder,
                    LineType = l.LineType.ToString(),
                    Code = l.Code,
                    Name = l.Name,
                    CurrencyCode = l.CurrencyCode,
                    Amount = l.Amount
                })
                .ToList();

            return new FlightSearchItemDto
            {
                OfferId = x.o.Id,
                Status = x.o.Status.ToString(),
                CurrencyCode = x.o.CurrencyCode,
                BaseFare = x.o.BaseFare,
                TaxesFees = x.o.TaxesFees,
                TotalPrice = x.o.TotalPrice,
                SeatsAvailable = x.o.SeatsAvailable,
                RequestedAt = x.o.RequestedAt,
                ExpiresAt = x.o.ExpiresAt,
                Airline = new FlightAirlineSummaryDto
                {
                    Id = x.a.Id,
                    Code = x.a.Code,
                    Name = x.a.Name,
                    IataCode = x.a.IataCode,
                    IcaoCode = x.a.IcaoCode,
                    LogoUrl = x.a.LogoUrl
                },
                Flight = new FlightSummaryDto
                {
                    Id = x.f.Id,
                    FlightNumber = route.DisplayFlightNumber,
                    DepartureAt = route.DepartureAt,
                    ArrivalAt = route.ArrivalAt
                },
                FareClass = new FlightFareClassSummaryDto
                {
                    Id = x.fc.Id,
                    Code = x.fc.Code,
                    Name = x.fc.Name,
                    CabinClass = x.fc.CabinClass.ToString(),
                    IsRefundable = x.fc.IsRefundable,
                    IsChangeable = x.fc.IsChangeable
                },
                Segments = segs,
                TaxFeeLines = lines
            };
        })
            .Where(x => x is not null)
            .OrderBy(x => x!.Flight.DepartureAt)
            .ThenBy(x => x!.TotalPrice)
            .Cast<FlightSearchItemDto>()
            .ToList();

        return new FlightSearchResponse
        {
            Count = items.Count,
            Items = items
        };
    }

    public async Task<FlightAirportLookupResponse> LookupAirportsAsync(
        FlightAirportLookupRequest request,
        CancellationToken ct = default)
    {
        var q = (request.Q ?? string.Empty).Trim();
        var limit = request.Limit < 1 ? 50 : (request.Limit > 100 ? 100 : request.Limit);

        var tenantIds = await ResolveTenantIdsForAirportLookupAsync(q, ct);
        if (tenantIds.Count == 0)
        {
            return new FlightAirportLookupResponse
            {
                Count = 0,
                Items = new List<FlightAirportLookupItemDto>()
            };
        }

        var query = _db.FlightAirports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                tenantIds.Contains(x.TenantId) &&
                !x.IsDeleted &&
                x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var uq = q.ToUpperInvariant();
            query = query.Where(x =>
                x.Code.ToUpper().Contains(uq) ||
                (x.IataCode != null && x.IataCode.ToUpper().Contains(uq)) ||
                (x.IcaoCode != null && x.IcaoCode.ToUpper().Contains(uq)) ||
                x.Name.ToUpper().Contains(uq));
        }

        var rawItems = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.IataCode)
            .ThenBy(x => x.Code)
            .Take(limit * 3)
            .Select(x => new FlightAirportLookupItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                IataCode = x.IataCode,
                IcaoCode = x.IcaoCode,
                TimeZone = x.TimeZone
            })
            .ToListAsync(ct);

        var items = rawItems
            .GroupBy(x => $"{(x.IataCode ?? x.Code).ToUpperInvariant()}|{x.Name.ToUpperInvariant()}")
            .Select(g => g.First())
            .Take(limit)
            .ToList();

        return new FlightAirportLookupResponse
        {
            Count = items.Count,
            Items = items
        };
    }

    public async Task<FlightOfferDetailsResponse?> GetOfferDetailsAsync(
        FlightOfferDetailsRequest request,
        CancellationToken ct = default)
    {
        var tenantId = await ResolveTenantIdForOfferAsync(
            request.OfferId,
            request.IncludeExpired,
            ct);

        if (!tenantId.HasValue)
            return null;

        var now = DateTimeOffset.Now;

        var offerQuery = _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o =>
                o.TenantId == tenantId.Value &&
                o.Id == request.OfferId &&
                !o.IsDeleted);

        if (!request.IncludeExpired)
        {
            offerQuery = offerQuery.Where(o =>
                o.Status == OfferStatus.Active &&
                o.ExpiresAt > now);
        }

        var offerRow = await offerQuery
            .Join(
                _db.FlightFlights
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(f => f.TenantId == tenantId.Value && !f.IsDeleted),
                o => o.FlightId,
                f => f.Id,
                (o, f) => new { o, f })
            .Join(
                _db.FlightAirlines
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(a => a.TenantId == tenantId.Value && !a.IsDeleted),
                x => x.f.AirlineId,
                a => a.Id,
                (x, a) => new { x.o, x.f, a })
            .Join(
                _db.FlightFareClasses
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(fc => fc.TenantId == tenantId.Value && !fc.IsDeleted),
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

        var fareRule = await _db.FlightFareRules
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(r =>
                r.TenantId == tenantId.Value &&
                r.FareClassId == offerRow.fc.Id &&
                !r.IsDeleted)
            .Select(r => new FlightOfferDetailsFareRuleDto
            {
                Id = r.Id,
                IsActive = r.IsActive,
                RulesJson = r.RulesJson,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

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
            .Where(ap =>
                ap.TenantId == tenantId.Value &&
                airportIds.Contains(ap.Id) &&
                !ap.IsDeleted)
            .ToDictionaryAsync(
                ap => ap.Id,
                ap => new FlightOfferDetailsAirportDto
                {
                    Id = ap.Id,
                    LocationId = ap.LocationId,
                    Name = ap.Name,
                    Code = ap.Code,
                    IataCode = ap.IataCode,
                    IcaoCode = ap.IcaoCode,
                    TimeZone = ap.TimeZone
                },
                ct);

        var airportTimeZones = airportsMap.ToDictionary(x => x.Key, x => x.Value.TimeZone);
        var route = BuildRouteInfo(offerRow.f, segments, airportTimeZones);

        var lines = await _db.FlightOfferTaxFeeLines
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(l =>
                l.TenantId == tenantId.Value &&
                l.OfferId == request.OfferId &&
                !l.IsDeleted)
            .OrderBy(l => l.SortOrder)
            .Select(l => new FlightOfferDetailsTaxFeeLineDto
            {
                Id = l.Id,
                SortOrder = l.SortOrder,
                LineType = l.LineType.ToString(),
                Code = l.Code,
                Name = l.Name,
                CurrencyCode = l.CurrencyCode,
                Amount = l.Amount
            })
            .ToListAsync(ct);

        var segmentDtos = segments.Count > 0
            ? segments.Select(s => new FlightOfferDetailsSegmentDto
            {
                SegmentIndex = s.SegmentIndex,
                From = airportsMap.TryGetValue(s.FromAirportId, out var fromAirport)
                    ? fromAirport
                    : new FlightOfferDetailsAirportDto
                    {
                        Id = s.FromAirportId,
                        LocationId = Guid.Empty,
                        Name = string.Empty,
                        Code = string.Empty
                    },
                To = airportsMap.TryGetValue(s.ToAirportId, out var toAirport)
                    ? toAirport
                    : new FlightOfferDetailsAirportDto
                    {
                        Id = s.ToAirportId,
                        LocationId = Guid.Empty,
                        Name = string.Empty,
                        Code = string.Empty
                    },
                FlightNumber = s.FlightNumber,
                DepartureAt = s.DepartureAt,
                ArrivalAt = s.ArrivalAt
            }).ToList()
            : new List<FlightOfferDetailsSegmentDto>
            {
                new()
                {
                    SegmentIndex = 0,
                    From = airportsMap.TryGetValue(offerRow.f.FromAirportId, out var fromAirport)
                        ? fromAirport
                        : new FlightOfferDetailsAirportDto
                        {
                            Id = offerRow.f.FromAirportId,
                            LocationId = Guid.Empty,
                            Name = string.Empty,
                            Code = string.Empty
                        },
                    To = airportsMap.TryGetValue(offerRow.f.ToAirportId, out var toAirport)
                        ? toAirport
                        : new FlightOfferDetailsAirportDto
                        {
                            Id = offerRow.f.ToAirportId,
                            LocationId = Guid.Empty,
                            Name = string.Empty,
                            Code = string.Empty
                        },
                    FlightNumber = offerRow.f.FlightNumber,
                    DepartureAt = offerRow.f.DepartureAt,
                    ArrivalAt = offerRow.f.ArrivalAt
                }
            };

        return new FlightOfferDetailsResponse
        {
            Offer = new FlightOfferDetailsOfferDto
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
                ConditionsJson = offerRow.o.ConditionsJson,
                MetadataJson = offerRow.o.MetadataJson
            },
            Airline = new FlightOfferDetailsAirlineDto
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
            Flight = new FlightOfferDetailsFlightDto
            {
                Id = offerRow.f.Id,
                FlightNumber = route.DisplayFlightNumber,
                DepartureAt = route.DepartureAt,
                ArrivalAt = route.ArrivalAt,
                Status = offerRow.f.Status.ToString()
            },
            FareClass = new FlightOfferDetailsFareClassDto
            {
                Id = offerRow.fc.Id,
                Code = offerRow.fc.Code,
                Name = offerRow.fc.Name,
                CabinClass = offerRow.fc.CabinClass.ToString(),
                IsRefundable = offerRow.fc.IsRefundable,
                IsChangeable = offerRow.fc.IsChangeable
            },
            FareRule = fareRule,
            Segments = segmentDtos,
            TaxFeeLines = lines
        };
    }

    private async Task<IReadOnlyCollection<Guid>> ResolveTenantIdsForSearchAsync(
        string from,
        string to,
        CancellationToken ct)
    {
        if (_tenant.HasTenant && _tenant.TenantId.HasValue && _tenant.TenantId.Value != Guid.Empty)
            return new[] { _tenant.TenantId.Value };

        return await _tenantResolver.ResolveTenantIdsForSearchAsync(from, to, ct);
    }

    private async Task<IReadOnlyCollection<Guid>> ResolveTenantIdsForAirportLookupAsync(
        string? query,
        CancellationToken ct)
    {
        if (_tenant.HasTenant && _tenant.TenantId.HasValue && _tenant.TenantId.Value != Guid.Empty)
            return new[] { _tenant.TenantId.Value };

        return await _tenantResolver.ResolveTenantIdsForAirportLookupAsync(query, ct);
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

    private static string NormalizeCode(string? value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static FlightRouteInfo BuildRouteInfo(
        TicketBooking.Domain.Flight.Flight flight,
        IReadOnlyList<OfferSegment> segments,
        IReadOnlyDictionary<Guid, string?> airportTimeZones)
    {
        if (segments.Count == 0)
        {
            airportTimeZones.TryGetValue(flight.FromAirportId, out var fallbackTimeZone);
            return new FlightRouteInfo(
                flight.FromAirportId,
                flight.ToAirportId,
                flight.DepartureAt,
                flight.ArrivalAt,
                FlightOfferSupport.BuildDisplayFlightNumber(new[] { flight.FlightNumber }, flight.FlightNumber),
                fallbackTimeZone);
        }

        var first = segments[0];
        var last = segments[^1];
        airportTimeZones.TryGetValue(first.FromAirportId, out var fromAirportTimeZone);

        return new FlightRouteInfo(
            first.FromAirportId,
            last.ToAirportId,
            first.DepartureAt,
            last.ArrivalAt,
            FlightOfferSupport.BuildDisplayFlightNumber(segments.Select(x => x.FlightNumber), flight.FlightNumber),
            fromAirportTimeZone);
    }

    private sealed record FlightRouteInfo(
        Guid FromAirportId,
        Guid ToAirportId,
        DateTimeOffset DepartureAt,
        DateTimeOffset ArrivalAt,
        string DisplayFlightNumber,
        string? FromAirportTimeZone);
}

