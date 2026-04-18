using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Train;
using TicketBooking.Domain.Tours;
using TicketBooking.Domain.Train;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Services.Tours;

public sealed class TrainTourPackageSourceQuoteAdapter : ITourPackageSourceQuoteAdapter
{
    private readonly AppDbContext _db;

    public TrainTourPackageSourceQuoteAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Train;

    public async Task<TourPackageSourceQuoteResult> ResolveAsync(
        TourPackageSourceQuoteAdapterRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Option.BindingMode switch
        {
            TourPackageBindingMode.StaticReference => await ResolveStaticAsync(request, ct),
            TourPackageBindingMode.SearchTemplate => await ResolveFromSearchTemplateAsync(request, ct),
            _ => TourPackageSourceQuoteResultFactory.Unavailable(request, "Train source does not support this binding mode.")
        };
    }

    private async Task<TourPackageSourceQuoteResult> ResolveStaticAsync(
        TourPackageSourceQuoteAdapterRequest request,
        CancellationToken ct)
    {
        var segmentPrice = await _db.TrainTripSegmentPrices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.SourceEntityId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (segmentPrice is null)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Train segment price source was not found.");

        var trip = await _db.TrainTrips
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == segmentPrice.TripId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (trip is null)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Train trip source was not found.");

        if (trip.Status != TrainTripStatus.Published)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Train trip source is not published.");

        var stopRefs = await _db.TrainTripStopTimes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.TripId == trip.Id &&
                !x.IsDeleted &&
                (x.Id == segmentPrice.FromTripStopTimeId || x.Id == segmentPrice.ToTripStopTimeId))
            .Select(x => new
            {
                x.Id,
                x.ArriveAt,
                x.DepartAt
            })
            .ToListAsync(ct);

        var fromStop = stopRefs.FirstOrDefault(x => x.Id == segmentPrice.FromTripStopTimeId);
        var toStop = stopRefs.FirstOrDefault(x => x.Id == segmentPrice.ToTripStopTimeId);

        var serviceStartAt = fromStop?.DepartAt ?? fromStop?.ArriveAt ?? trip.DepartureAt;
        var serviceEndAt = toStop?.ArriveAt ?? toStop?.DepartAt ?? trip.ArrivalAt;

        if (serviceStartAt <= DateTimeOffset.UtcNow)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Train trip source has already departed.");

        return new TourPackageSourceQuoteResult
        {
            OptionId = request.Option.Id,
            SourceType = request.Option.SourceType,
            WasResolved = true,
            IsAvailable = true,
            BoundSourceEntityId = segmentPrice.Id,
            CurrencyCode = TourPackageQuoteSupport.NormalizeCurrency(
                request.ScheduleOverride?.CurrencyCode,
                segmentPrice.CurrencyCode,
                request.Option.CurrencyCode,
                request.Package.CurrencyCode,
                request.Tour.CurrencyCode),
            UnitBasePrice = segmentPrice.BaseFare,
            UnitTaxes = segmentPrice.TaxesFees ?? 0m,
            UnitFees = 0m,
            UnitTotalPrice = segmentPrice.TotalPrice,
            UnitCost = segmentPrice.TotalPrice,
            SourceCode = trip.Code,
            SourceName = trip.Name,
            Note = $"Live train segment price from trip {trip.Code}.",
            SelectionReason = "Pinned/static train source.",
            ServiceStartAt = serviceStartAt,
            ServiceEndAt = serviceEndAt,
            StopCount = Math.Max(segmentPrice.ToStopIndex - segmentPrice.FromStopIndex - 1, 0)
        };
    }

    private async Task<TourPackageSourceQuoteResult> ResolveFromSearchTemplateAsync(
        TourPackageSourceQuoteAdapterRequest request,
        CancellationToken ct)
    {
        TourPackageSearchTemplate template;
        try
        {
            template = TourPackageSearchTemplateSupport.ParseRequired(request.Option.SearchTemplateJson);
        }
        catch (ArgumentException ex)
        {
            return TourPackageSourceQuoteResultFactory.Unavailable(request, ex.Message);
        }

        var fromLocationId = template.FromLocationId ?? Guid.Empty;
        var toLocationId = template.ToLocationId ?? Guid.Empty;
        if (fromLocationId == Guid.Empty || toLocationId == Guid.Empty || fromLocationId == toLocationId)
        {
            return TourPackageSourceQuoteResultFactory.Unavailable(
                request,
                "Train search template requires valid FromLocationId and ToLocationId.");
        }

        var travelDate = TourPackageSearchTemplateSupport.ResolveServiceDate(request.Schedule, request.Component, template);
        var start = new DateTimeOffset(travelDate.Year, travelDate.Month, travelDate.Day, 0, 0, 0, TimeSpan.FromHours(7));
        var end = start.AddDays(1);
        var now = DateTimeOffset.Now;

        var fromStopPointIds = await _db.TrainStopPoints
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.LocationId == fromLocationId &&
                !x.IsDeleted &&
                x.IsActive &&
                (!template.TenantId.HasValue || x.TenantId == template.TenantId.Value))
            .Select(x => x.Id)
            .ToListAsync(ct);

        var toStopPointIds = await _db.TrainStopPoints
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.LocationId == toLocationId &&
                !x.IsDeleted &&
                x.IsActive &&
                (!template.TenantId.HasValue || x.TenantId == template.TenantId.Value))
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (fromStopPointIds.Count == 0 || toStopPointIds.Count == 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Train search template could not resolve the requested stop locations.");

        var trips = await _db.TrainTrips
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == TrainTripStatus.Published &&
                x.ArrivalAt >= start &&
                x.DepartureAt < end &&
                (!template.TenantId.HasValue || x.TenantId == template.TenantId.Value) &&
                (!template.ProviderId.HasValue || x.ProviderId == template.ProviderId.Value) &&
                (!template.TripId.HasValue || x.Id == template.TripId.Value) &&
                (string.IsNullOrWhiteSpace(template.TripCode) || x.Code == template.TripCode.Trim()))
            .Select(x => new
            {
                x.Id,
                x.ProviderId,
                x.TrainNumber,
                x.Code,
                x.Name,
                x.DepartureAt,
                x.ArrivalAt
            })
            .ToListAsync(ct);

        if (trips.Count == 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Train search template did not find any published trip.");

        var tripIds = trips.Select(x => x.Id).ToList();

        var stopTimes = await _db.TrainTripStopTimes
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                tripIds.Contains(x.TripId) &&
                !x.IsDeleted &&
                x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.TripId,
                x.StopPointId,
                x.StopIndex,
                x.ArriveAt,
                x.DepartAt
            })
            .ToListAsync(ct);

        var segmentPrices = await _db.TrainTripSegmentPrices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                tripIds.Contains(x.TripId) &&
                !x.IsDeleted &&
                x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.TripId,
                x.FromStopIndex,
                x.ToStopIndex,
                x.CurrencyCode,
                x.BaseFare,
                x.TaxesFees,
                x.TotalPrice
            })
            .ToListAsync(ct);

        var seatCounts = await _db.TrainCars
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c =>
                tripIds.Contains(c.TripId) &&
                !c.IsDeleted &&
                c.IsActive)
            .Join(
                _db.TrainCarSeats
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(s => !s.IsDeleted && s.IsActive),
                c => c.Id,
                s => s.CarId,
                (c, s) => new { c.TripId, SeatId = s.Id })
            .GroupBy(x => x.TripId)
            .Select(g => new { TripId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TripId, x => x.Count, ct);

        var holds = await _db.TrainTripSeatHolds
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                tripIds.Contains(x.TripId) &&
                !x.IsDeleted)
            .WhereActiveSeatOccupancy(now)
            .Select(x => new
            {
                x.TripId,
                x.TrainCarSeatId,
                x.FromStopIndex,
                x.ToStopIndex
            })
            .ToListAsync(ct);

        var stopTimesByTrip = stopTimes.GroupBy(x => x.TripId).ToDictionary(g => g.Key, g => g.ToList());
        var segmentPricesByTrip = segmentPrices.GroupBy(x => x.TripId).ToDictionary(g => g.Key, g => g.ToList());
        var holdsByTrip = holds.GroupBy(x => x.TripId).ToDictionary(g => g.Key, g => g.ToList());

        var candidates = new List<TrainSearchCandidate>();

        foreach (var trip in trips)
        {
            if (!stopTimesByTrip.TryGetValue(trip.Id, out var tripStopTimes))
                continue;

            var fromPick = tripStopTimes
                .Where(x => fromStopPointIds.Contains(x.StopPointId))
                .OrderBy(x => x.StopIndex)
                .FirstOrDefault();

            if (fromPick is null)
                continue;

            var toPick = tripStopTimes
                .Where(x => toStopPointIds.Contains(x.StopPointId) && x.StopIndex > fromPick.StopIndex)
                .OrderBy(x => x.StopIndex)
                .FirstOrDefault();

            if (toPick is null)
                continue;

            var boardingDepartureAt = fromPick.DepartAt ?? fromPick.ArriveAt ?? trip.DepartureAt;
            if (boardingDepartureAt < start || boardingDepartureAt >= end)
                continue;

            var segmentArrivalAt = toPick.ArriveAt ?? toPick.DepartAt ?? trip.ArrivalAt;

            if (!segmentPricesByTrip.TryGetValue(trip.Id, out var tripSegmentPrices))
                continue;

            var chosenPrice = tripSegmentPrices
                .Where(x => x.FromStopIndex == fromPick.StopIndex && x.ToStopIndex == toPick.StopIndex)
                .OrderBy(x => x.TotalPrice)
                .FirstOrDefault()
                ?? tripSegmentPrices
                    .Where(x => x.FromStopIndex <= fromPick.StopIndex && x.ToStopIndex >= toPick.StopIndex)
                    .OrderBy(x => x.TotalPrice)
                    .FirstOrDefault();

            if (chosenPrice is null)
                continue;

            var occupiedSeatCount = holdsByTrip.TryGetValue(trip.Id, out var tripHolds)
                ? tripHolds
                    .Where(h => h.FromStopIndex < toPick.StopIndex && fromPick.StopIndex < h.ToStopIndex)
                    .Select(h => h.TrainCarSeatId)
                    .Distinct()
                    .Count()
                : 0;

            var capacity = seatCounts.TryGetValue(trip.Id, out var totalSeats) ? totalSeats : 0;
            var availableSeatCount = Math.Max(0, capacity - occupiedSeatCount);
            if (availableSeatCount < request.RequestedQuantity)
                continue;

            candidates.Add(new TrainSearchCandidate
            {
                TripId = trip.Id,
                TripCode = trip.Code,
                TripName = trip.Name,
                DepartureAt = boardingDepartureAt,
                ArrivalAt = segmentArrivalAt,
                SegmentPriceId = chosenPrice.Id,
                CurrencyCode = chosenPrice.CurrencyCode,
                BaseFare = chosenPrice.BaseFare,
                TaxesFees = chosenPrice.TaxesFees ?? 0m,
                TotalPrice = chosenPrice.TotalPrice,
                StopCount = Math.Max(toPick.StopIndex - fromPick.StopIndex - 1, 0)
            });
        }

        if (candidates.Count == 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Train search template did not find any available trip.");

        var decision = TourPackageOptimizationSupport.SelectBestCandidate(
            candidates,
            template,
            candidate => new TourPackageOptimizationCandidateContext
            {
                Label = candidate.TripCode,
                UnitPrice = candidate.TotalPrice,
                ServiceStartAt = candidate.DepartureAt,
                ServiceEndAt = candidate.ArrivalAt,
                StopCount = candidate.StopCount
            });

        var candidate = decision.Candidate;

        return new TourPackageSourceQuoteResult
        {
            OptionId = request.Option.Id,
            SourceType = request.Option.SourceType,
            WasResolved = true,
            IsAvailable = true,
            BoundSourceEntityId = candidate.SegmentPriceId,
            CurrencyCode = TourPackageQuoteSupport.NormalizeCurrency(
                request.ScheduleOverride?.CurrencyCode,
                candidate.CurrencyCode,
                request.Option.CurrencyCode,
                request.Package.CurrencyCode,
                request.Tour.CurrencyCode),
            UnitBasePrice = candidate.BaseFare,
            UnitTaxes = candidate.TaxesFees,
            UnitFees = 0m,
            UnitTotalPrice = candidate.TotalPrice,
            UnitCost = candidate.TotalPrice,
            SourceCode = candidate.TripCode,
            SourceName = candidate.TripName,
            Note = "Resolved from train search template.",
            WasOptimizedSelection = true,
            OptimizationStrategy = decision.StrategyLabel,
            SelectionReason = decision.Reason,
            OptimizationScore = decision.Score,
            CandidateCount = decision.CandidateCount,
            ServiceStartAt = candidate.DepartureAt,
            ServiceEndAt = candidate.ArrivalAt,
            StopCount = candidate.StopCount
        };
    }

    private sealed class TrainSearchCandidate
    {
        public Guid TripId { get; set; }
        public string TripCode { get; set; } = "";
        public string TripName { get; set; } = "";
        public DateTimeOffset DepartureAt { get; set; }
        public DateTimeOffset ArrivalAt { get; set; }
        public Guid SegmentPriceId { get; set; }
        public string CurrencyCode { get; set; } = "";
        public decimal BaseFare { get; set; }
        public decimal TaxesFees { get; set; }
        public decimal TotalPrice { get; set; }
        public int StopCount { get; set; }
    }
}
