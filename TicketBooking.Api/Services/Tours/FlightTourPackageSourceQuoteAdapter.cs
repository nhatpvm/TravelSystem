using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Flight;
using TicketBooking.Domain.Flight;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class FlightTourPackageSourceQuoteAdapter : ITourPackageSourceQuoteAdapter
{
    private readonly AppDbContext _db;

    public FlightTourPackageSourceQuoteAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Flight;

    public async Task<TourPackageSourceQuoteResult> ResolveAsync(
        TourPackageSourceQuoteAdapterRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Option.BindingMode switch
        {
            TourPackageBindingMode.StaticReference => await ResolveStaticAsync(request, ct),
            TourPackageBindingMode.SearchTemplate => await ResolveFromSearchTemplateAsync(request, ct),
            _ => TourPackageSourceQuoteResultFactory.Unavailable(request, "Flight source does not support this binding mode.")
        };
    }

    private async Task<TourPackageSourceQuoteResult> ResolveStaticAsync(
        TourPackageSourceQuoteAdapterRequest request,
        CancellationToken ct)
    {
        if (request.RequestedQuantity <= 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Flight source requires a quantity greater than 0.");

        var now = DateTimeOffset.Now;

        var offer = await _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.SourceEntityId &&
                !x.IsDeleted, ct);

        if (offer is null)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Flight offer source was not found.");

        if (offer.Status != OfferStatus.Active)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Flight offer source is no longer active.");

        if (offer.ExpiresAt <= now)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Flight offer source has expired.");

        var currentSeatsAvailable = await FlightOfferSupport.ResolveCurrentSeatsAvailableAsync(
            _db,
            offer.TenantId,
            offer.FlightId,
            offer.FareClassId,
            now,
            ct);

        if (currentSeatsAvailable < request.RequestedQuantity)
        {
            return TourPackageSourceQuoteResultFactory.Unavailable(
                request,
                $"Flight source only has {currentSeatsAvailable} seat(s) available.");
        }

        var flight = await _db.FlightFlights
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == offer.FlightId &&
                !x.IsDeleted &&
                x.IsActive, ct);

        if (flight is null)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Flight source could not resolve the underlying flight.");

        var fareClass = await _db.FlightFareClasses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == offer.FareClassId &&
                !x.IsDeleted &&
                x.IsActive, ct);

        var segments = await _db.FlightOfferSegments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.OfferId == offer.Id &&
                !x.IsDeleted)
            .OrderBy(x => x.SegmentIndex)
            .ToListAsync(ct);

        var airportTimeZones = await LoadAirportTimeZonesAsync(
            offer.TenantId,
            segments.SelectMany(x => new[] { x.FromAirportId, x.ToAirportId })
                .Concat(new[] { flight.FromAirportId, flight.ToAirportId })
                .Distinct()
                .ToList(),
            ct);

        var route = BuildRouteInfo(flight, segments, airportTimeZones);

        return new TourPackageSourceQuoteResult
        {
            OptionId = request.Option.Id,
            SourceType = request.Option.SourceType,
            WasResolved = true,
            IsAvailable = true,
            BoundSourceEntityId = offer.Id,
            CurrencyCode = TourPackageQuoteSupport.NormalizeCurrency(
                request.ScheduleOverride?.CurrencyCode,
                offer.CurrencyCode,
                request.Option.CurrencyCode,
                request.Package.CurrencyCode,
                request.Tour.CurrencyCode),
            UnitBasePrice = offer.BaseFare,
            UnitTaxes = offer.TaxesFees,
            UnitFees = 0m,
            UnitTotalPrice = offer.TotalPrice,
            UnitCost = offer.TotalPrice,
            SourceCode = route.DisplayFlightNumber,
            SourceName = $"Flight {route.DisplayFlightNumber}",
            Note = $"Live flight offer expires at {offer.ExpiresAt:yyyy-MM-dd HH:mm:ss zzz}.",
            SelectionReason = "Pinned/static flight source.",
            ServiceStartAt = route.DepartureAt,
            ServiceEndAt = route.ArrivalAt,
            StopCount = route.StopCount,
            IsRefundable = fareClass?.IsRefundable
        };
    }

    private async Task<TourPackageSourceQuoteResult> ResolveFromSearchTemplateAsync(
        TourPackageSourceQuoteAdapterRequest request,
        CancellationToken ct)
    {
        if (request.RequestedQuantity <= 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Flight source requires a quantity greater than 0.");

        TourPackageSearchTemplate template;
        try
        {
            template = TourPackageSearchTemplateSupport.ParseRequired(request.Option.SearchTemplateJson);
        }
        catch (ArgumentException ex)
        {
            return TourPackageSourceQuoteResultFactory.Unavailable(request, ex.Message);
        }

        var travelDate = TourPackageSearchTemplateSupport.ResolveServiceDate(request.Schedule, request.Component, template);
        var now = DateTimeOffset.Now;

        var fromAirportIds = await ResolveAirportIdsAsync(template.TenantId, template.FromAirportId, template.FromAirportCode, ct);
        var toAirportIds = await ResolveAirportIdsAsync(template.TenantId, template.ToAirportId, template.ToAirportCode, ct);

        if (fromAirportIds.Count == 0 || toAirportIds.Count == 0)
        {
            return TourPackageSourceQuoteResultFactory.Unavailable(
                request,
                "Flight search template requires resolvable from/to airports.");
        }

        var offerRows = await _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o =>
                !o.IsDeleted &&
                o.Status == OfferStatus.Active &&
                o.ExpiresAt > now &&
                (!template.TenantId.HasValue || o.TenantId == template.TenantId.Value) &&
                (!template.AirlineId.HasValue || o.AirlineId == template.AirlineId.Value))
            .Join(
                _db.FlightFlights
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(f =>
                        !f.IsDeleted &&
                        f.IsActive &&
                        f.Status == FlightStatus.Published &&
                        (!template.TenantId.HasValue || f.TenantId == template.TenantId.Value) &&
                        (!template.AirlineId.HasValue || f.AirlineId == template.AirlineId.Value)),
                o => o.FlightId,
                f => f.Id,
                (o, f) => new { Offer = o, Flight = f })
            .Join(
                _db.FlightFareClasses
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(fc =>
                        !fc.IsDeleted &&
                        fc.IsActive &&
                        (!template.TenantId.HasValue || fc.TenantId == template.TenantId.Value) &&
                        (!template.AirlineId.HasValue || fc.AirlineId == template.AirlineId.Value)),
                x => x.Offer.FareClassId,
                fc => fc.Id,
                (x, fc) => new { x.Offer, x.Flight, FareClass = fc })
            .ToListAsync(ct);

        if (template.CabinClass.HasValue)
        {
            offerRows = offerRows
                .Where(x => x.FareClass.CabinClass == template.CabinClass.Value)
                .ToList();
        }

        if (offerRows.Count == 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Flight search template did not find any available offer.");

        var canonicalOffers = offerRows
            .GroupBy(x => new { x.Offer.TenantId, x.Offer.FlightId, x.Offer.FareClassId })
            .Select(g => g
                .OrderByDescending(x => x.Offer.RequestedAt)
                .ThenByDescending(x => x.Offer.CreatedAt)
                .ThenByDescending(x => x.Offer.Id)
                .First())
            .ToList();

        var offerIds = canonicalOffers.Select(x => x.Offer.Id).Distinct().ToList();
        var segments = offerIds.Count == 0
            ? new List<OfferSegment>()
            : await _db.FlightOfferSegments
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x =>
                    offerIds.Contains(x.OfferId) &&
                    !x.IsDeleted)
                .OrderBy(x => x.OfferId)
                .ThenBy(x => x.SegmentIndex)
                .ToListAsync(ct);

        var segmentMap = segments
            .GroupBy(x => x.OfferId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<OfferSegment>)g
                    .OrderBy(x => x.SegmentIndex)
                    .ToList());

        var airportTimeZones = await LoadAirportTimeZonesAsync(
            template.TenantId,
            segments.SelectMany(x => new[] { x.FromAirportId, x.ToAirportId })
                .Concat(canonicalOffers.SelectMany(x => new[] { x.Flight.FromAirportId, x.Flight.ToAirportId }))
                .Distinct()
                .ToList(),
            ct);

        var candidates = canonicalOffers
            .Select(x =>
            {
                segmentMap.TryGetValue(x.Offer.Id, out var offerSegments);
                offerSegments ??= Array.Empty<OfferSegment>();

                var route = BuildRouteInfo(x.Flight, offerSegments, airportTimeZones);
                if (!fromAirportIds.Contains(route.FromAirportId) ||
                    !toAirportIds.Contains(route.ToAirportId) ||
                    FlightOfferSupport.ResolveLocalDate(route.DepartureAt, route.FromAirportTimeZone) != travelDate ||
                    x.Offer.SeatsAvailable < request.RequestedQuantity)
                {
                    return null;
                }

                return new FlightSearchCandidate
                {
                    OfferId = x.Offer.Id,
                    FlightNumber = route.DisplayFlightNumber,
                    DepartureAt = route.DepartureAt,
                    ArrivalAt = route.ArrivalAt,
                    CurrencyCode = x.Offer.CurrencyCode,
                    BaseFare = x.Offer.BaseFare,
                    TaxesFees = x.Offer.TaxesFees,
                    TotalPrice = x.Offer.TotalPrice,
                    ExpiresAt = x.Offer.ExpiresAt,
                    IsRefundable = x.FareClass.IsRefundable,
                    StopCount = route.StopCount
                };
            })
            .Where(x => x is not null)
            .Cast<FlightSearchCandidate>()
            .ToList();

        if (candidates.Count == 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Flight search template did not find any available offer.");

        var decision = TourPackageOptimizationSupport.SelectBestCandidate(
            candidates,
            template,
            candidate => new TourPackageOptimizationCandidateContext
            {
                Label = candidate.FlightNumber,
                UnitPrice = candidate.TotalPrice,
                ServiceStartAt = candidate.DepartureAt,
                ServiceEndAt = candidate.ArrivalAt,
                StopCount = candidate.StopCount,
                IsRefundable = candidate.IsRefundable
            });

        var chosen = decision.Candidate;

        return new TourPackageSourceQuoteResult
        {
            OptionId = request.Option.Id,
            SourceType = request.Option.SourceType,
            WasResolved = true,
            IsAvailable = true,
            BoundSourceEntityId = chosen.OfferId,
            CurrencyCode = TourPackageQuoteSupport.NormalizeCurrency(
                request.ScheduleOverride?.CurrencyCode,
                chosen.CurrencyCode,
                request.Option.CurrencyCode,
                request.Package.CurrencyCode,
                request.Tour.CurrencyCode),
            UnitBasePrice = chosen.BaseFare,
            UnitTaxes = chosen.TaxesFees,
            UnitFees = 0m,
            UnitTotalPrice = chosen.TotalPrice,
            UnitCost = chosen.TotalPrice,
            SourceCode = chosen.FlightNumber,
            SourceName = $"Flight {chosen.FlightNumber}",
            Note = $"Resolved from flight search template. Offer expires at {chosen.ExpiresAt:yyyy-MM-dd HH:mm:ss zzz}.",
            WasOptimizedSelection = true,
            OptimizationStrategy = decision.StrategyLabel,
            SelectionReason = decision.Reason,
            OptimizationScore = decision.Score,
            CandidateCount = decision.CandidateCount,
            ServiceStartAt = chosen.DepartureAt,
            ServiceEndAt = chosen.ArrivalAt,
            StopCount = chosen.StopCount,
            IsRefundable = chosen.IsRefundable
        };
    }

    private async Task<List<Guid>> ResolveAirportIdsAsync(
        Guid? tenantId,
        Guid? airportId,
        string? airportCode,
        CancellationToken ct)
    {
        if (airportId.HasValue && airportId.Value != Guid.Empty)
            return new List<Guid> { airportId.Value };

        var normalizedCode = (airportCode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedCode))
            return new List<Guid>();

        return await _db.FlightAirports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                (!tenantId.HasValue || x.TenantId == tenantId.Value) &&
                (x.Code.ToUpper() == normalizedCode ||
                 (x.IataCode != null && x.IataCode.ToUpper() == normalizedCode)))
            .Select(x => x.Id)
            .Distinct()
            .ToListAsync(ct);
    }

    private async Task<Dictionary<Guid, string?>> LoadAirportTimeZonesAsync(
        Guid? tenantId,
        IReadOnlyCollection<Guid> airportIds,
        CancellationToken ct)
    {
        if (airportIds.Count == 0)
            return new Dictionary<Guid, string?>();

        return await _db.FlightAirports
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                airportIds.Contains(x.Id) &&
                !x.IsDeleted &&
                (!tenantId.HasValue || x.TenantId == tenantId.Value))
            .ToDictionaryAsync(x => x.Id, x => x.TimeZone, ct);
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
                fallbackTimeZone,
                0);
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
            fromAirportTimeZone,
            Math.Max(segments.Count - 1, 0));
    }

    private sealed record FlightRouteInfo(
        Guid FromAirportId,
        Guid ToAirportId,
        DateTimeOffset DepartureAt,
        DateTimeOffset ArrivalAt,
        string DisplayFlightNumber,
        string? FromAirportTimeZone,
        int StopCount);

    private sealed class FlightSearchCandidate
    {
        public Guid OfferId { get; set; }
        public string FlightNumber { get; set; } = "";
        public DateTimeOffset DepartureAt { get; set; }
        public DateTimeOffset ArrivalAt { get; set; }
        public string CurrencyCode { get; set; } = "";
        public decimal BaseFare { get; set; }
        public decimal TaxesFees { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public bool IsRefundable { get; set; }
        public int StopCount { get; set; }
    }
}
