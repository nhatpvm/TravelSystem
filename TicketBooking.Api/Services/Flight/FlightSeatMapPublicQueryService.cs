using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Contracts.Flight;
using TicketBooking.Domain.Flight;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Services.Flight;

/// <summary>
/// Public/customer read-only seat-map queries for Flight.
/// Supports 2 modes:
/// - Scoped mode: when X-TenantId is already resolved by middleware.
/// - Public/global mode: when tenant is not supplied, resolve tenant automatically.
/// </summary>
public sealed class FlightSeatMapPublicQueryService : IFlightSeatMapPublicQueryService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IFlightPublicTenantResolver _tenantResolver;

    public FlightSeatMapPublicQueryService(
        AppDbContext db,
        ITenantContext tenant,
        IFlightPublicTenantResolver tenantResolver)
    {
        _db = db;
        _tenant = tenant;
        _tenantResolver = tenantResolver;
    }

    public async Task<FlightSeatMapResponse?> GetByOfferAsync(
        FlightSeatMapByOfferRequest request,
        CancellationToken ct = default)
    {
        var tenantId = await ResolveTenantIdForOfferAsync(request.OfferId, ct);
        if (!tenantId.HasValue)
            return null;

        var now = DateTimeOffset.Now;

        var offer = await _db.FlightOffers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o =>
                o.TenantId == tenantId.Value &&
                o.Id == request.OfferId &&
                !o.IsDeleted &&
                o.Status == OfferStatus.Active &&
                o.ExpiresAt > now)
            .Select(o => new
            {
                o.Id,
                o.FlightId,
                o.FareClassId
            })
            .FirstOrDefaultAsync(ct);

        if (offer is null)
            return null;

        var flight = await _db.FlightFlights
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(f =>
                f.TenantId == tenantId.Value &&
                f.Id == offer.FlightId &&
                !f.IsDeleted)
            .Select(f => new
            {
                f.Id,
                f.AircraftId,
                f.FlightNumber,
                f.FromAirportId,
                f.ToAirportId,
                f.DepartureAt,
                f.ArrivalAt
            })
            .FirstOrDefaultAsync(ct);

        if (flight is null)
            return null;

        var fareClassCabinClass = await _db.FlightFareClasses
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(fc =>
                fc.TenantId == tenantId.Value &&
                fc.Id == offer.FareClassId &&
                !fc.IsDeleted)
            .Select(fc => (CabinClass?)fc.CabinClass)
            .FirstOrDefaultAsync(ct);

        if (!fareClassCabinClass.HasValue)
            return null;

        var currentSeatsAvailable = await FlightOfferSupport.ResolveCurrentSeatsAvailableAsync(
            _db,
            tenantId.Value,
            offer.FlightId,
            offer.FareClassId,
            now,
            ct);

        var rawSegments = await _db.FlightOfferSegments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s =>
                s.TenantId == tenantId.Value &&
                s.OfferId == request.OfferId &&
                !s.IsDeleted)
            .OrderBy(s => s.SegmentIndex)
            .Select(s => new FlightSeatMapOfferSegmentContext
            {
                SegmentIndex = s.SegmentIndex,
                FlightId = s.FlightId,
                CabinSeatMapId = s.CabinSeatMapId,
                CabinClass = s.CabinClass,
                FromAirportId = s.FromAirportId,
                ToAirportId = s.ToAirportId,
                DepartureAt = s.DepartureAt,
                ArrivalAt = s.ArrivalAt,
                FlightNumber = s.FlightNumber
            })
            .ToListAsync(ct);

        if (rawSegments.Count == 0)
        {
            rawSegments.Add(new FlightSeatMapOfferSegmentContext
            {
                SegmentIndex = 0,
                FlightId = flight.Id,
                CabinClass = fareClassCabinClass.Value,
                FromAirportId = flight.FromAirportId,
                ToAirportId = flight.ToAirportId,
                DepartureAt = flight.DepartureAt,
                ArrivalAt = flight.ArrivalAt,
                FlightNumber = flight.FlightNumber
            });
        }

        var flightIds = rawSegments
            .Select(x => x.FlightId ?? flight.Id)
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var flightAircraftModels = await _db.FlightFlights
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(f =>
                f.TenantId == tenantId.Value &&
                flightIds.Contains(f.Id) &&
                !f.IsDeleted)
            .Join(
                _db.FlightAircrafts
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .Where(a => a.TenantId == tenantId.Value && !a.IsDeleted),
                f => f.AircraftId,
                a => a.Id,
                (f, a) => new
                {
                    f.Id,
                    a.AircraftModelId
                })
            .ToDictionaryAsync(x => x.Id, x => x.AircraftModelId, ct);

        var explicitCabinSeatMapIds = rawSegments
            .Where(x => x.CabinSeatMapId.HasValue && x.CabinSeatMapId.Value != Guid.Empty)
            .Select(x => x.CabinSeatMapId!.Value)
            .Distinct()
            .ToList();

        var aircraftModelIds = flightAircraftModels.Values
            .Distinct()
            .ToList();

        var seatMapLookups = await _db.FlightCabinSeatMaps
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(sm =>
                sm.TenantId == tenantId.Value &&
                !sm.IsDeleted &&
                sm.IsActive &&
                (explicitCabinSeatMapIds.Contains(sm.Id) || aircraftModelIds.Contains(sm.AircraftModelId)))
            .Select(sm => new FlightSeatMapLookup
            {
                Id = sm.Id,
                AircraftModelId = sm.AircraftModelId,
                CabinClass = sm.CabinClass,
                Code = sm.Code
            })
            .ToListAsync(ct);

        var seatMapLookupById = seatMapLookups.ToDictionary(x => x.Id);
        var seatMapLookupByModelAndCabin = seatMapLookups
            .GroupBy(x => new { x.AircraftModelId, x.CabinClass })
            .ToDictionary(
                g => (g.Key.AircraftModelId, g.Key.CabinClass),
                g => g
                    .OrderBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                    .First()
                    .Id);

        var airportIds = rawSegments
            .SelectMany(x => new[] { x.FromAirportId, x.ToAirportId })
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
                x => new FlightAirportLiteDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code,
                    IataCode = x.IataCode,
                    IcaoCode = x.IcaoCode,
                    TimeZone = x.TimeZone
                },
                ct);

        var resolvedSegments = rawSegments
            .Select(x =>
            {
                Guid? resolvedCabinSeatMapId = null;
                if (x.CabinSeatMapId.HasValue &&
                    x.CabinSeatMapId.Value != Guid.Empty &&
                    seatMapLookupById.ContainsKey(x.CabinSeatMapId.Value))
                {
                    resolvedCabinSeatMapId = x.CabinSeatMapId.Value;
                }
                else
                {
                    var resolvedFlightId = x.FlightId ?? flight.Id;
                    if (flightAircraftModels.TryGetValue(resolvedFlightId, out var aircraftModelId))
                    {
                        var targetCabinClass = x.CabinClass ?? fareClassCabinClass.Value;
                        if (seatMapLookupByModelAndCabin.TryGetValue((aircraftModelId, targetCabinClass), out var inferredCabinSeatMapId))
                            resolvedCabinSeatMapId = inferredCabinSeatMapId;
                    }
                }

                return new FlightSeatMapResolvedSegmentContext
                {
                    SegmentIndex = x.SegmentIndex,
                    FlightId = x.FlightId ?? flight.Id,
                    CabinSeatMapId = resolvedCabinSeatMapId,
                    FlightNumber = x.FlightNumber,
                    DepartureAt = x.DepartureAt,
                    ArrivalAt = x.ArrivalAt,
                    From = airportsMap.TryGetValue(x.FromAirportId, out var fromAirport)
                        ? fromAirport
                        : new FlightAirportLiteDto
                        {
                            Id = x.FromAirportId,
                            Name = string.Empty,
                            Code = string.Empty
                        },
                    To = airportsMap.TryGetValue(x.ToAirportId, out var toAirport)
                        ? toAirport
                        : new FlightAirportLiteDto
                        {
                            Id = x.ToAirportId,
                            Name = string.Empty,
                            Code = string.Empty
                        }
                };
            })
            .ToList();

        var primarySegment = resolvedSegments.FirstOrDefault(x => x.CabinSeatMapId.HasValue);
        if (primarySegment is null || !primarySegment.CabinSeatMapId.HasValue)
            return null;

        foreach (var segment in resolvedSegments)
            segment.IsPrimarySelectedMap = segment.SegmentIndex == primarySegment.SegmentIndex;

        var offerContext = new FlightSeatMapOfferContext
        {
            OfferId = request.OfferId,
            SeatsAvailable = currentSeatsAvailable,
            Segments = resolvedSegments
        };

        return await BuildResponseAsync(
            tenantId.Value,
            primarySegment.CabinSeatMapId.Value,
            offerContext,
            ct);
    }

    public async Task<FlightSeatMapResponse?> GetByIdAsync(
        FlightSeatMapByIdRequest request,
        CancellationToken ct = default)
    {
        Guid? tenantId = null;

        if (_tenant.HasTenant && _tenant.TenantId.HasValue && _tenant.TenantId.Value != Guid.Empty)
        {
            tenantId = _tenant.TenantId.Value;
        }
        else
        {
            tenantId = await _db.FlightCabinSeatMaps
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(sm =>
                    sm.Id == request.CabinSeatMapId &&
                    !sm.IsDeleted &&
                    sm.IsActive)
                .Select(sm => (Guid?)sm.TenantId)
                .FirstOrDefaultAsync(ct);
        }

        if (!tenantId.HasValue)
            return null;

        return await BuildResponseAsync(
            tenantId.Value,
            request.CabinSeatMapId,
            offerContext: null,
            ct);
    }

    private async Task<FlightSeatMapResponse?> BuildResponseAsync(
        Guid tenantId,
        Guid cabinSeatMapId,
        FlightSeatMapOfferContext? offerContext,
        CancellationToken ct)
    {
        var soldOut = offerContext is not null && offerContext.SeatsAvailable <= 0;
        IReadOnlyCollection<Guid> seatMapIds = offerContext is null
            ? new List<Guid> { cabinSeatMapId }
            : offerContext.Segments
                .Where(x => x.CabinSeatMapId.HasValue && x.CabinSeatMapId.Value != Guid.Empty)
                .Select(x => x.CabinSeatMapId!.Value)
                .Append(cabinSeatMapId)
                .Distinct()
                .ToList();

        var snapshots = await LoadSeatMapSnapshotsAsync(
            tenantId,
            seatMapIds,
            soldOut,
            ct);

        if (!snapshots.TryGetValue(cabinSeatMapId, out var primarySnapshot))
            return null;

        var segments = offerContext?.Segments
            .Select(x => new FlightSeatMapSegmentDto
            {
                SegmentIndex = x.SegmentIndex,
                FlightId = x.FlightId,
                CabinSeatMapId = x.CabinSeatMapId,
                FlightNumber = x.FlightNumber,
                DepartureAt = x.DepartureAt,
                ArrivalAt = x.ArrivalAt,
                IsPrimarySelectedMap = x.IsPrimarySelectedMap,
                From = x.From,
                To = x.To
            })
            .ToList() ?? new List<FlightSeatMapSegmentDto>();

        var segmentSeatMaps = offerContext?.Segments
            .Select(x =>
            {
                FlightSeatMapSnapshot? snapshot = null;
                if (x.CabinSeatMapId.HasValue && x.CabinSeatMapId.Value != Guid.Empty)
                    snapshots.TryGetValue(x.CabinSeatMapId.Value, out snapshot);

                return new FlightSegmentSeatMapDto
                {
                    SegmentIndex = x.SegmentIndex,
                    FlightId = x.FlightId,
                    CabinSeatMapId = x.CabinSeatMapId,
                    FlightNumber = x.FlightNumber,
                    DepartureAt = x.DepartureAt,
                    ArrivalAt = x.ArrivalAt,
                    IsPrimarySelectedMap = x.IsPrimarySelectedMap,
                    ActiveSeatCount = snapshot?.ActiveSeatCount ?? 0,
                    From = x.From,
                    To = x.To,
                    CabinSeatMap = snapshot?.CabinSeatMap,
                    Seats = snapshot is null
                        ? new List<FlightSeatDto>()
                        : CloneSeats(snapshot.Seats)
                };
            })
            .ToList() ?? new List<FlightSegmentSeatMapDto>();

        return new FlightSeatMapResponse
        {
            OfferId = offerContext?.OfferId,
            TenantId = tenantId,
            UsesPooledInventory = offerContext is not null,
            SeatsAvailable = offerContext?.SeatsAvailable,
            ActiveSeatCount = primarySnapshot.ActiveSeatCount,
            InventoryNote = offerContext is null
                ? null
                : "Seat numbers are informational only; live availability is managed as pooled offer inventory.",
            CabinSeatMap = primarySnapshot.CabinSeatMap,
            Segments = segments,
            SegmentSeatMaps = segmentSeatMaps,
            Seats = CloneSeats(primarySnapshot.Seats)
        };
    }

    private async Task<Dictionary<Guid, FlightSeatMapSnapshot>> LoadSeatMapSnapshotsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> cabinSeatMapIds,
        bool soldOut,
        CancellationToken ct)
    {
        if (cabinSeatMapIds.Count == 0)
            return new Dictionary<Guid, FlightSeatMapSnapshot>();

        var seatMaps = await _db.FlightCabinSeatMaps
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(sm =>
                sm.TenantId == tenantId &&
                cabinSeatMapIds.Contains(sm.Id) &&
                !sm.IsDeleted &&
                sm.IsActive)
            .Select(sm => new FlightSeatMapDto
            {
                Id = sm.Id,
                Code = sm.Code,
                Name = sm.Name,
                CabinClass = sm.CabinClass.ToString(),
                TotalRows = sm.TotalRows,
                TotalColumns = sm.TotalColumns,
                DeckCount = sm.DeckCount,
                IsActive = sm.IsActive
            })
            .ToListAsync(ct);

        if (seatMaps.Count == 0)
            return new Dictionary<Guid, FlightSeatMapSnapshot>();

        var resolvedSeatMapIds = seatMaps
            .Select(x => x.Id)
            .ToList();

        var seatRows = await _db.FlightCabinSeats
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s =>
                s.TenantId == tenantId &&
                resolvedSeatMapIds.Contains(s.CabinSeatMapId) &&
                !s.IsDeleted)
            .OrderBy(s => s.CabinSeatMapId)
            .ThenBy(s => s.DeckIndex)
            .ThenBy(s => s.RowIndex)
            .ThenBy(s => s.ColumnIndex)
            .ThenBy(s => s.SeatNumber)
            .Select(s => new
            {
                s.CabinSeatMapId,
                Seat = new FlightSeatDto
                {
                    Id = s.Id,
                    SeatNumber = s.SeatNumber,
                    RowIndex = s.RowIndex,
                    ColumnIndex = s.ColumnIndex,
                    DeckIndex = s.DeckIndex,
                    SeatType = string.IsNullOrWhiteSpace(s.SeatType) ? "Standard" : s.SeatType,
                    SeatClass = s.SeatClass,
                    IsWindow = s.IsWindow,
                    IsAisle = s.IsAisle,
                    PriceModifier = s.PriceModifier,
                    IsActive = s.IsActive,
                    Status = s.IsActive ? "available" : "inactive"
                }
            })
            .ToListAsync(ct);

        var seatMap = seatRows
            .GroupBy(x => x.CabinSeatMapId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var seats = g.Select(x => x.Seat).ToList();
                    if (soldOut)
                    {
                        foreach (var seat in seats.Where(x => x.IsActive))
                            seat.Status = "booked";
                    }

                    return seats;
                });

        return seatMaps.ToDictionary(
            x => x.Id,
            x =>
            {
                seatMap.TryGetValue(x.Id, out var seats);
                seats ??= new List<FlightSeatDto>();

                return new FlightSeatMapSnapshot
                {
                    CabinSeatMap = x,
                    ActiveSeatCount = seats.Count(s => s.IsActive),
                    Seats = seats
                };
            });
    }

    private static List<FlightSeatDto> CloneSeats(IReadOnlyCollection<FlightSeatDto> seats)
        => seats.Select(s => new FlightSeatDto
        {
            Id = s.Id,
            SeatNumber = s.SeatNumber,
            RowIndex = s.RowIndex,
            ColumnIndex = s.ColumnIndex,
            DeckIndex = s.DeckIndex,
            SeatType = s.SeatType,
            SeatClass = s.SeatClass,
            IsWindow = s.IsWindow,
            IsAisle = s.IsAisle,
            PriceModifier = s.PriceModifier,
            IsActive = s.IsActive,
            Status = s.Status,
            HoldToken = s.HoldToken,
            HoldExpiresAt = s.HoldExpiresAt
        }).ToList();

    private async Task<Guid?> ResolveTenantIdForOfferAsync(Guid offerId, CancellationToken ct)
    {
        if (_tenant.HasTenant && _tenant.TenantId.HasValue && _tenant.TenantId.Value != Guid.Empty)
            return _tenant.TenantId.Value;

        return await _tenantResolver.ResolveTenantIdForOfferAsync(
            offerId,
            includeExpired: false,
            ct);
    }

    private sealed class FlightSeatMapOfferContext
    {
        public Guid OfferId { get; set; }
        public int SeatsAvailable { get; set; }
        public List<FlightSeatMapResolvedSegmentContext> Segments { get; set; } = new();
    }

    private sealed class FlightSeatMapOfferSegmentContext
    {
        public int SegmentIndex { get; set; }
        public Guid? FlightId { get; set; }
        public Guid? CabinSeatMapId { get; set; }
        public CabinClass? CabinClass { get; set; }
        public Guid FromAirportId { get; set; }
        public Guid ToAirportId { get; set; }
        public DateTimeOffset DepartureAt { get; set; }
        public DateTimeOffset ArrivalAt { get; set; }
        public string? FlightNumber { get; set; }
    }

    private sealed class FlightSeatMapResolvedSegmentContext
    {
        public int SegmentIndex { get; set; }
        public Guid FlightId { get; set; }
        public Guid? CabinSeatMapId { get; set; }
        public string? FlightNumber { get; set; }
        public DateTimeOffset DepartureAt { get; set; }
        public DateTimeOffset ArrivalAt { get; set; }
        public bool IsPrimarySelectedMap { get; set; }
        public FlightAirportLiteDto From { get; set; } = new();
        public FlightAirportLiteDto To { get; set; } = new();
    }

    private sealed class FlightSeatMapLookup
    {
        public Guid Id { get; set; }
        public Guid AircraftModelId { get; set; }
        public CabinClass CabinClass { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private sealed class FlightSeatMapSnapshot
    {
        public FlightSeatMapDto CabinSeatMap { get; set; } = new();
        public int ActiveSeatCount { get; set; }
        public List<FlightSeatDto> Seats { get; set; } = new();
    }
}
