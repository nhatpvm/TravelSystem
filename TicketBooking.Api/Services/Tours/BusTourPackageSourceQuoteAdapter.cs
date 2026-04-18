using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Bus;
using TicketBooking.Domain.Bus;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using VehicleEntity = TicketBooking.Domain.Fleet.Vehicle;

namespace TicketBooking.Api.Services.Tours;

public sealed class BusTourPackageSourceQuoteAdapter : ITourPackageSourceQuoteAdapter
{
    private readonly AppDbContext _db;

    public BusTourPackageSourceQuoteAdapter(AppDbContext db)
    {
        _db = db;
    }

    public bool CanHandle(TourPackageSourceType sourceType)
        => sourceType == TourPackageSourceType.Bus;

    public async Task<TourPackageSourceQuoteResult> ResolveAsync(
        TourPackageSourceQuoteAdapterRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Option.BindingMode switch
        {
            TourPackageBindingMode.StaticReference => await ResolveStaticAsync(request, ct),
            TourPackageBindingMode.SearchTemplate => await ResolveFromSearchTemplateAsync(request, ct),
            _ => TourPackageSourceQuoteResultFactory.Unavailable(request, "Bus source does not support this binding mode.")
        };
    }

    private async Task<TourPackageSourceQuoteResult> ResolveStaticAsync(
        TourPackageSourceQuoteAdapterRequest request,
        CancellationToken ct)
    {
        var segmentPrice = await _db.BusTripSegmentPrices
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.SourceEntityId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (segmentPrice is null)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Bus segment price source was not found.");

        var trip = await _db.BusTrips
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == segmentPrice.TripId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (trip is null)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Bus trip source was not found.");

        if (trip.Status != TripStatus.Published)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Bus trip source is not published.");

        if (trip.DepartureAt <= DateTimeOffset.UtcNow)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Bus trip source has already departed.");

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
            Note = $"Live bus segment price from trip {trip.Code}.",
            SelectionReason = "Pinned/static bus source.",
            ServiceStartAt = trip.DepartureAt,
            ServiceEndAt = trip.ArrivalAt,
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
                "Bus search template requires valid FromLocationId and ToLocationId.");
        }

        var travelDate = TourPackageSearchTemplateSupport.ResolveServiceDate(request.Schedule, request.Component, template);
        var start = new DateTimeOffset(travelDate.Year, travelDate.Month, travelDate.Day, 0, 0, 0, TimeSpan.FromHours(7));
        var end = start.AddDays(1);
        var now = DateTimeOffset.Now;

        var fromStopPointIds = await _db.BusStopPoints
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                x.LocationId == fromLocationId &&
                !x.IsDeleted &&
                x.IsActive &&
                (!template.TenantId.HasValue || x.TenantId == template.TenantId.Value))
            .Select(x => x.Id)
            .ToListAsync(ct);

        var toStopPointIds = await _db.BusStopPoints
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
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Bus search template could not resolve the requested stop locations.");

        var trips = await _db.BusTrips
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == TripStatus.Published &&
                x.ArrivalAt >= start &&
                x.DepartureAt < end &&
                (!template.TenantId.HasValue || x.TenantId == template.TenantId.Value) &&
                (!template.ProviderId.HasValue || x.ProviderId == template.ProviderId.Value) &&
                (!template.TripId.HasValue || x.Id == template.TripId.Value) &&
                (string.IsNullOrWhiteSpace(template.TripCode) || x.Code == template.TripCode.Trim()))
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.ProviderId,
                x.VehicleId,
                x.Code,
                x.Name,
                x.DepartureAt,
                x.ArrivalAt
            })
            .ToListAsync(ct);

        if (trips.Count == 0)
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Bus search template did not find any published trip.");

        var tripIds = trips.Select(x => x.Id).ToList();

        var stopTimes = await _db.BusTripStopTimes
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

        var segmentPrices = await _db.BusTripSegmentPrices
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

        var holds = await _db.BusTripSeatHolds
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                tripIds.Contains(x.TripId) &&
                !x.IsDeleted)
            .WhereActiveSeatOccupancy(now)
            .Select(x => new
            {
                x.TripId,
                x.SeatId,
                x.FromStopIndex,
                x.ToStopIndex
            })
            .ToListAsync(ct);

        var vehicles = await _db.Vehicles
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x =>
                trips.Select(t => t.VehicleId).Contains(x.Id) &&
                !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.SeatMapId,
                x.SeatCapacity
            })
            .ToListAsync(ct);

        var seatMapIds = vehicles
            .Where(x => x.SeatMapId.HasValue)
            .Select(x => x.SeatMapId!.Value)
            .Distinct()
            .ToList();

        var activeSeatCounts = seatMapIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _db.Seats
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x =>
                    seatMapIds.Contains(x.SeatMapId) &&
                    !x.IsDeleted &&
                    x.IsActive)
                .GroupBy(x => x.SeatMapId)
                .Select(g => new { SeatMapId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SeatMapId, x => x.Count, ct);

        var stopTimesByTrip = stopTimes.GroupBy(x => x.TripId).ToDictionary(g => g.Key, g => g.ToList());
        var segmentPricesByTrip = segmentPrices.GroupBy(x => x.TripId).ToDictionary(g => g.Key, g => g.ToList());
        var holdsByTrip = holds.GroupBy(x => x.TripId).ToDictionary(g => g.Key, g => g.ToList());
        var vehiclesById = vehicles.ToDictionary(x => x.Id);

        var candidates = new List<BusSearchCandidate>();

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

            var capacity = 0;
            if (vehiclesById.TryGetValue(trip.VehicleId, out var vehicle))
            {
                if (vehicle.SeatMapId.HasValue && activeSeatCounts.TryGetValue(vehicle.SeatMapId.Value, out var activeSeats))
                    capacity = activeSeats;
                else
                    capacity = vehicle.SeatCapacity;
            }

            var occupiedSeatCount = holdsByTrip.TryGetValue(trip.Id, out var tripHolds)
                ? tripHolds
                    .Where(h => h.FromStopIndex < toPick.StopIndex && fromPick.StopIndex < h.ToStopIndex)
                    .Select(h => h.SeatId)
                    .Distinct()
                    .Count()
                : 0;

            var availableSeatCount = Math.Max(0, capacity - occupiedSeatCount);
            if (availableSeatCount < request.RequestedQuantity)
                continue;

            candidates.Add(new BusSearchCandidate
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
            return TourPackageSourceQuoteResultFactory.Unavailable(request, "Bus search template did not find any available trip.");

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
            Note = "Resolved from bus search template.",
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

    private sealed class BusSearchCandidate
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
