using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Bus;
using TicketBooking.Domain.Commerce;
using TicketBooking.Domain.Flight;
using TicketBooking.Domain.Train;

namespace TicketBooking.Api.Services.Commerce;

public sealed partial class CustomerOrderService
{
    private async Task<CustomerOrderDetailDto> CreateBusOrderAsync(
        CreateCustomerOrderRequest request,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var holdToken = NormalizeRequired(request.HoldToken, "Thiếu holdToken giữ ghế xe khách.");
        var (order, payment, vat) = CreateOrderSkeleton(CustomerProductType.Bus, request, userId, now);

        var holds = await _db.BusTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.HoldToken == holdToken &&
                x.UserId == userId &&
                !x.IsDeleted &&
                x.Status == SeatHoldStatus.Held)
            .ToListAsync(ct);

        if (holds.Count == 0)
            throw new InvalidOperationException("Không tìm thấy lượt giữ ghế xe khách hợp lệ.");

        if (holds.Any(x => x.HoldExpiresAt <= now))
            throw new InvalidOperationException("Lượt giữ ghế xe khách đã hết hạn.");

        var first = holds[0];
        if (holds.Any(x =>
                x.TenantId != first.TenantId ||
                x.TripId != first.TripId ||
                x.FromTripStopTimeId != first.FromTripStopTimeId ||
                x.ToTripStopTimeId != first.ToTripStopTimeId))
            throw new InvalidOperationException("Hold ghế xe khách không đồng nhất.");

        var trip = await _db.BusTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == first.TripId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy chuyến xe.");

        if (!trip.IsActive || trip.Status != TripStatus.Published)
            throw new InvalidOperationException("Chuyến xe không còn khả dụng để thanh toán.");

        var provider = await _db.Providers.IgnoreQueryFilters()
            .Where(x => x.Id == trip.ProviderId && !x.IsDeleted)
            .Select(x => new { x.Id, x.Name, x.LogoUrl })
            .FirstOrDefaultAsync(ct);

        var seatIds = holds.Select(x => x.SeatId).Distinct().ToList();
        var seats = await _db.Seats.IgnoreQueryFilters()
            .Where(x => seatIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new { x.Id, x.SeatNumber, x.PriceModifier })
            .ToListAsync(ct);

        if (seats.Count != seatIds.Count)
            throw new InvalidOperationException("Một hoặc nhiều ghế xe khách không còn hợp lệ.");

        var stopTimes = await _db.BusTripStopTimes.IgnoreQueryFilters()
            .Where(x =>
                x.TripId == trip.Id &&
                (x.Id == first.FromTripStopTimeId || x.Id == first.ToTripStopTimeId) &&
                !x.IsDeleted)
            .Select(x => new { x.Id, x.StopPointId, x.ArriveAt, x.DepartAt })
            .ToListAsync(ct);

        var fromStop = stopTimes.FirstOrDefault(x => x.Id == first.FromTripStopTimeId)
            ?? throw new InvalidOperationException("Điểm đi của chuyến xe không hợp lệ.");
        var toStop = stopTimes.FirstOrDefault(x => x.Id == first.ToTripStopTimeId)
            ?? throw new InvalidOperationException("Điểm đến của chuyến xe không hợp lệ.");

        var stopPoints = await _db.BusStopPoints.IgnoreQueryFilters()
            .Where(x => (x.Id == fromStop.StopPointId || x.Id == toStop.StopPointId) && !x.IsDeleted)
            .Select(x => new StopPointLookup { Id = x.Id, LocationId = x.LocationId, Name = x.Name })
            .ToListAsync(ct);
        var locations = await _db.Locations.IgnoreQueryFilters()
            .Where(x => stopPoints.Select(sp => sp.LocationId).Contains(x.Id) && !x.IsDeleted)
            .Select(x => new LocationLookup { Id = x.Id, Name = x.Name })
            .ToListAsync(ct);

        var fromName = ResolveStopName(fromStop.StopPointId, stopPoints, locations, "Điểm đi");
        var toName = ResolveStopName(toStop.StopPointId, stopPoints, locations, "Điểm đến");

        var segmentPrice = await _db.BusTripSegmentPrices.IgnoreQueryFilters()
            .Where(x =>
                x.TripId == trip.Id &&
                x.FromTripStopTimeId == first.FromTripStopTimeId &&
                x.ToTripStopTimeId == first.ToTripStopTimeId &&
                x.IsActive &&
                !x.IsDeleted)
            .Select(x => new { x.CurrencyCode, x.TotalPrice })
            .FirstOrDefaultAsync(ct);

        if (segmentPrice is null)
            throw new InvalidOperationException("Chuyến xe chưa có giá cho chặng đã chọn.");

        var lines = seats
            .OrderBy(x => x.SeatNumber)
            .Select(x =>
            {
                var lineAmount = segmentPrice.TotalPrice + (x.PriceModifier ?? 0m);
                return new CustomerOrderSnapshotLine
                {
                    Label = $"Ghế {x.SeatNumber}",
                    Quantity = 1,
                    UnitAmount = lineAmount,
                    LineAmount = lineAmount,
                };
            })
            .ToList();

        ApplyOrderAmounts(order, segmentPrice.CurrencyCode, lines.Sum(x => x.LineAmount));
        order.TenantId = trip.TenantId;
        order.SourceEntityId = trip.Id;
        order.ExpiresAt = holds.Min(x => x.HoldExpiresAt);
        order.SnapshotJson = SerializeJson(new CustomerOrderSnapshot
        {
            Title = $"{fromName} - {toName}",
            Subtitle = trip.Name,
            ProviderName = provider?.Name,
            RouteFrom = fromName,
            RouteTo = toName,
            SeatText = string.Join(", ", seats.OrderBy(x => x.SeatNumber).Select(x => x.SeatNumber)),
            PassengerText = $"{holds.Count} hành khách",
            DepartureAt = fromStop.DepartAt ?? fromStop.ArriveAt ?? trip.DepartureAt,
            ArrivalAt = toStop.ArriveAt ?? toStop.DepartAt ?? trip.ArrivalAt,
            TicketNote = "Vui lòng có mặt trước giờ khởi hành ít nhất 30 phút.",
            SourceCode = trip.Code,
            Lines = lines,
            MetadataJson = SerializeJson(new { passengers = request.Passengers, holdToken }),
        });
        order.MetadataJson = SerializeJson(new BusOrderMetadata
        {
            TripId = trip.Id,
            HoldToken = holdToken,
            FromTripStopTimeId = first.FromTripStopTimeId,
            ToTripStopTimeId = first.ToTripStopTimeId,
            SeatIds = seatIds,
            SeatNumbers = seats.OrderBy(x => x.SeatNumber).Select(x => x.SeatNumber).ToList(),
            Passengers = request.Passengers,
        });

        PreparePaymentAndVat(order, payment, vat);
        _db.CustomerOrders.Add(order);
        _db.CustomerPayments.Add(payment);
        if (vat is not null)
            _db.CustomerVatInvoiceRequests.Add(vat);

        await _db.SaveChangesAsync(ct);
        await CreateOrderCreatedNotificationAsync(order, ct);
        return await MapOrderDetailAsync(order.Id, ct);
    }

    private async Task<CustomerOrderDetailDto> CreateTrainOrderAsync(
        CreateCustomerOrderRequest request,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var holdToken = NormalizeRequired(request.HoldToken, "Thiếu holdToken giữ chỗ tàu.");
        var (order, payment, vat) = CreateOrderSkeleton(CustomerProductType.Train, request, userId, now);

        var holds = await _db.TrainTripSeatHolds.IgnoreQueryFilters()
            .Where(x =>
                x.HoldToken == holdToken &&
                x.UserId == userId &&
                !x.IsDeleted &&
                x.Status == TrainSeatHoldStatus.Held)
            .ToListAsync(ct);

        if (holds.Count == 0)
            throw new InvalidOperationException("Không tìm thấy lượt giữ chỗ tàu hợp lệ.");

        if (holds.Any(x => x.HoldExpiresAt <= now))
            throw new InvalidOperationException("Lượt giữ chỗ tàu đã hết hạn.");

        var first = holds[0];
        if (holds.Any(x =>
                x.TenantId != first.TenantId ||
                x.TripId != first.TripId ||
                x.FromTripStopTimeId != first.FromTripStopTimeId ||
                x.ToTripStopTimeId != first.ToTripStopTimeId))
            throw new InvalidOperationException("Hold chỗ tàu không đồng nhất.");

        var trip = await _db.TrainTrips.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == first.TripId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy chuyến tàu.");

        if (!trip.IsActive || trip.Status != TrainTripStatus.Published)
            throw new InvalidOperationException("Chuyến tàu không còn khả dụng để thanh toán.");

        var provider = await _db.Providers.IgnoreQueryFilters()
            .Where(x => x.Id == trip.ProviderId && !x.IsDeleted)
            .Select(x => new { x.Id, x.Name, x.LogoUrl })
            .FirstOrDefaultAsync(ct);

        var seatIds = holds.Select(x => x.TrainCarSeatId).Distinct().ToList();
        var seats = await _db.TrainCarSeats.IgnoreQueryFilters()
            .Where(x => seatIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new { x.Id, x.SeatNumber, x.SeatType, x.SeatClass, x.PriceModifier })
            .ToListAsync(ct);

        if (seats.Count != seatIds.Count)
            throw new InvalidOperationException("Một hoặc nhiều chỗ tàu không còn hợp lệ.");

        var stopTimes = await _db.TrainTripStopTimes.IgnoreQueryFilters()
            .Where(x =>
                x.TripId == trip.Id &&
                (x.Id == first.FromTripStopTimeId || x.Id == first.ToTripStopTimeId) &&
                !x.IsDeleted)
            .Select(x => new { x.Id, x.StopPointId, x.ArriveAt, x.DepartAt })
            .ToListAsync(ct);

        var fromStop = stopTimes.FirstOrDefault(x => x.Id == first.FromTripStopTimeId)
            ?? throw new InvalidOperationException("Ga đi không hợp lệ.");
        var toStop = stopTimes.FirstOrDefault(x => x.Id == first.ToTripStopTimeId)
            ?? throw new InvalidOperationException("Ga đến không hợp lệ.");

        var stopPoints = await _db.TrainStopPoints.IgnoreQueryFilters()
            .Where(x => (x.Id == fromStop.StopPointId || x.Id == toStop.StopPointId) && !x.IsDeleted)
            .Select(x => new StopPointLookup { Id = x.Id, LocationId = x.LocationId, Name = x.Name })
            .ToListAsync(ct);
        var locations = await _db.Locations.IgnoreQueryFilters()
            .Where(x => stopPoints.Select(sp => sp.LocationId).Contains(x.Id) && !x.IsDeleted)
            .Select(x => new LocationLookup { Id = x.Id, Name = x.Name })
            .ToListAsync(ct);

        var fromName = ResolveStopName(fromStop.StopPointId, stopPoints, locations, "Ga đi");
        var toName = ResolveStopName(toStop.StopPointId, stopPoints, locations, "Ga đến");

        var segmentPrice = await _db.TrainTripSegmentPrices.IgnoreQueryFilters()
            .Where(x =>
                x.TripId == trip.Id &&
                x.FromTripStopTimeId == first.FromTripStopTimeId &&
                x.ToTripStopTimeId == first.ToTripStopTimeId &&
                x.IsActive &&
                !x.IsDeleted)
            .Select(x => new { x.CurrencyCode, x.TotalPrice })
            .FirstOrDefaultAsync(ct);

        if (segmentPrice is null)
            throw new InvalidOperationException("Chuyến tàu chưa có giá cho chặng đã chọn.");

        var fareClasses = await _db.TrainFareClasses.IgnoreQueryFilters()
            .Where(x => x.TenantId == trip.TenantId && x.IsActive && !x.IsDeleted)
            .Select(x => new TrainFareClassPrice
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                SeatType = x.SeatType,
                DefaultModifier = x.DefaultModifier
            })
            .ToListAsync(ct);

        var fareClassIds = fareClasses.Select(x => x.Id).ToList();
        var fareRules = fareClassIds.Count == 0
            ? new List<TrainFareRulePrice>()
            : await _db.TrainFareRules.IgnoreQueryFilters()
                .Where(x =>
                    x.TenantId == trip.TenantId &&
                    fareClassIds.Contains(x.FareClassId) &&
                    x.FromStopIndex == first.FromStopIndex &&
                    x.ToStopIndex == first.ToStopIndex &&
                    x.IsActive &&
                    !x.IsDeleted &&
                    (!x.EffectiveFrom.HasValue || x.EffectiveFrom <= now) &&
                    (!x.EffectiveTo.HasValue || x.EffectiveTo >= now) &&
                    (x.TripId == trip.Id || x.RouteId == trip.RouteId))
                .Select(x => new TrainFareRulePrice
                {
                    FareClassId = x.FareClassId,
                    TripId = x.TripId,
                    RouteId = x.RouteId,
                    CurrencyCode = x.CurrencyCode,
                    TotalPrice = x.TotalPrice
                })
                .ToListAsync(ct);

        var lines = seats
            .OrderBy(x => x.SeatNumber)
            .Select(x =>
            {
                var fareClass = ResolveTrainFareClass(fareClasses, x.SeatClass, x.SeatType);
                var fareRule = fareClass is null
                    ? null
                    : fareRules
                        .Where(rule => rule.FareClassId == fareClass.Id)
                        .OrderByDescending(rule => rule.TripId == trip.Id)
                        .FirstOrDefault();

                var lineAmount = fareRule?.TotalPrice
                    ?? segmentPrice.TotalPrice + (x.PriceModifier ?? fareClass?.DefaultModifier ?? 0m);
                return new CustomerOrderSnapshotLine
                {
                    Label = $"Chỗ {x.SeatNumber}",
                    Quantity = 1,
                    UnitAmount = lineAmount,
                    LineAmount = lineAmount,
                };
            })
            .ToList();

        ApplyOrderAmounts(order, segmentPrice.CurrencyCode, lines.Sum(x => x.LineAmount));
        order.TenantId = trip.TenantId;
        order.SourceEntityId = trip.Id;
        order.ExpiresAt = holds.Min(x => x.HoldExpiresAt);
        order.SnapshotJson = SerializeJson(new CustomerOrderSnapshot
        {
            Title = $"{fromName} - {toName}",
            Subtitle = trip.Name,
            ProviderName = provider?.Name,
            RouteFrom = fromName,
            RouteTo = toName,
            SeatText = string.Join(", ", seats.OrderBy(x => x.SeatNumber).Select(x => x.SeatNumber)),
            PassengerText = $"{holds.Count} hành khách",
            DepartureAt = fromStop.DepartAt ?? fromStop.ArriveAt ?? trip.DepartureAt,
            ArrivalAt = toStop.ArriveAt ?? toStop.DepartAt ?? trip.ArrivalAt,
            TicketNote = "Vui lòng mang theo giấy tờ tùy thân trùng khớp với thông tin đặt vé.",
            SourceCode = trip.TrainNumber,
            Lines = lines,
            MetadataJson = SerializeJson(new { passengers = request.Passengers, holdToken }),
        });
        order.MetadataJson = SerializeJson(new TrainOrderMetadata
        {
            TripId = trip.Id,
            HoldToken = holdToken,
            FromTripStopTimeId = first.FromTripStopTimeId,
            ToTripStopTimeId = first.ToTripStopTimeId,
            TrainCarSeatIds = seatIds,
            SeatNumbers = seats.OrderBy(x => x.SeatNumber).Select(x => x.SeatNumber).ToList(),
            Passengers = request.Passengers,
        });

        PreparePaymentAndVat(order, payment, vat);
        _db.CustomerOrders.Add(order);
        _db.CustomerPayments.Add(payment);
        if (vat is not null)
            _db.CustomerVatInvoiceRequests.Add(vat);

        await _db.SaveChangesAsync(ct);
        await CreateOrderCreatedNotificationAsync(order, ct);
        return await MapOrderDetailAsync(order.Id, ct);
    }

    private async Task<CustomerOrderDetailDto> CreateFlightOrderAsync(
        CreateCustomerOrderRequest request,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (!request.OfferId.HasValue || request.OfferId == Guid.Empty)
            throw new InvalidOperationException("Thiếu offer chuyến bay để tạo đơn.");

        var (order, payment, vat) = CreateOrderSkeleton(CustomerProductType.Flight, request, userId, now);

        var offer = await _db.FlightOffers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.OfferId.Value && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy offer chuyến bay.");

        if (offer.Status != OfferStatus.Active || offer.ExpiresAt <= now)
            throw new InvalidOperationException("Offer chuyến bay đã hết hiệu lực.");

        var airline = await _db.FlightAirlines.IgnoreQueryFilters()
            .Where(x => x.Id == offer.AirlineId && !x.IsDeleted)
            .Select(x => new { x.Id, x.Name, x.IataCode, x.LogoUrl })
            .FirstOrDefaultAsync(ct);

        var segments = await _db.FlightOfferSegments.IgnoreQueryFilters()
            .Where(x => x.OfferId == offer.Id && !x.IsDeleted)
            .OrderBy(x => x.SegmentIndex)
            .Select(x => new
            {
                x.Id,
                x.SegmentIndex,
                x.FromAirportId,
                x.ToAirportId,
                x.DepartureAt,
                x.ArrivalAt,
                x.FlightNumber,
                x.CabinSeatMapId,
            })
            .ToListAsync(ct);

        if (segments.Count == 0)
            throw new InvalidOperationException("Offer chuyến bay chưa có segment hợp lệ.");

        var airportIds = segments.SelectMany(x => new[] { x.FromAirportId, x.ToAirportId }).Distinct().ToList();
        var airports = await _db.FlightAirports.IgnoreQueryFilters()
            .Where(x => airportIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new { x.Id, x.Name, x.IataCode, x.Code })
            .ToListAsync(ct);

        decimal seatModifier = 0m;
        string? seatNumber = null;
        if (request.SeatId.HasValue && request.SeatId != Guid.Empty)
        {
            var segmentSeatMapIds = segments.Where(x => x.CabinSeatMapId.HasValue).Select(x => x.CabinSeatMapId!.Value).Distinct().ToList();
            var seat = await _db.FlightCabinSeats.IgnoreQueryFilters()
                .Where(x => x.Id == request.SeatId.Value && segmentSeatMapIds.Contains(x.CabinSeatMapId) && !x.IsDeleted)
                .Select(x => new { x.Id, x.SeatNumber, x.PriceModifier })
                .FirstOrDefaultAsync(ct);

            if (seat is null)
                throw new InvalidOperationException("Ghế chuyến bay đã chọn không hợp lệ.");

            seatModifier = seat.PriceModifier ?? 0m;
            seatNumber = seat.SeatNumber;
        }

        var ancillaryIds = request.AncillaryIds.Where(x => x != Guid.Empty).Distinct().ToList();
        var ancillaries = ancillaryIds.Count == 0
            ? new List<AncillaryProjection>()
            : await _db.FlightAncillaryDefinitions.IgnoreQueryFilters()
                .Where(x =>
                    ancillaryIds.Contains(x.Id) &&
                    x.TenantId == offer.TenantId &&
                    x.AirlineId == offer.AirlineId &&
                    x.IsActive &&
                    !x.IsDeleted)
                .Select(x => new AncillaryProjection { Id = x.Id, Name = x.Name, CurrencyCode = x.CurrencyCode, Price = x.Price })
                .ToListAsync(ct);

        if (ancillaries.Count != ancillaryIds.Count)
            throw new InvalidOperationException("Một hoặc nhiều dịch vụ bổ sung chuyến bay không hợp lệ.");

        var currencyCode = string.IsNullOrWhiteSpace(offer.CurrencyCode) ? "VND" : offer.CurrencyCode;
        var lines = new List<CustomerOrderSnapshotLine>
        {
            new()
            {
                Label = "Giá vé chuyến bay",
                Quantity = 1,
                UnitAmount = offer.TotalPrice,
                LineAmount = offer.TotalPrice,
            },
        };

        if (seatModifier > 0)
        {
            lines.Add(new CustomerOrderSnapshotLine
            {
                Label = $"Phụ thu ghế {seatNumber}",
                Quantity = 1,
                UnitAmount = seatModifier,
                LineAmount = seatModifier,
            });
        }

        foreach (var ancillary in ancillaries.OrderBy(x => x.Name))
        {
            lines.Add(new CustomerOrderSnapshotLine
            {
                Label = ancillary.Name,
                Quantity = 1,
                UnitAmount = ancillary.Price,
                LineAmount = ancillary.Price,
            });
        }

        ApplyOrderAmounts(order, currencyCode, lines.Sum(x => x.LineAmount));
        order.TenantId = offer.TenantId;
        order.SourceEntityId = offer.Id;
        order.ExpiresAt = MinDateTime(offer.ExpiresAt, now.AddMinutes(_options.PendingPaymentMinutes));

        var firstSegment = segments[0];
        var lastSegment = segments[^1];
        var fromAirport = airports.FirstOrDefault(x => x.Id == firstSegment.FromAirportId);
        var toAirport = airports.FirstOrDefault(x => x.Id == lastSegment.ToAirportId);
        var routeFrom = fromAirport?.IataCode ?? fromAirport?.Code ?? "Sân bay đi";
        var routeTo = toAirport?.IataCode ?? toAirport?.Code ?? "Sân bay đến";

        order.SnapshotJson = SerializeJson(new CustomerOrderSnapshot
        {
            Title = $"{routeFrom} - {routeTo}",
            Subtitle = airline?.Name ?? "Chuyến bay",
            ProviderName = airline?.Name,
            RouteFrom = routeFrom,
            RouteTo = routeTo,
            SeatText = seatNumber,
            PassengerText = $"{Math.Max(1, request.AdultCount + request.ChildCount)} hành khách",
            DepartureAt = firstSegment.DepartureAt,
            ArrivalAt = lastSegment.ArrivalAt,
            TicketNote = "Vui lòng kiểm tra kỹ thông tin hành khách trước khi làm thủ tục bay.",
            SourceCode = firstSegment.FlightNumber,
            Lines = lines,
            MetadataJson = SerializeJson(new { passengers = request.Passengers, ancillaryIds }),
        });
        order.MetadataJson = SerializeJson(new FlightOrderMetadata
        {
            OfferId = offer.Id,
            SeatId = request.SeatId,
            SeatNumber = seatNumber,
            AncillaryIds = ancillaryIds,
            PassengerCount = Math.Max(1, request.AdultCount + request.ChildCount),
            Passengers = request.Passengers,
        });

        PreparePaymentAndVat(order, payment, vat);
        _db.CustomerOrders.Add(order);
        _db.CustomerPayments.Add(payment);
        if (vat is not null)
            _db.CustomerVatInvoiceRequests.Add(vat);

        await _db.SaveChangesAsync(ct);
        await CreateOrderCreatedNotificationAsync(order, ct);
        return await MapOrderDetailAsync(order.Id, ct);
    }

    private static TrainFareClassPrice? ResolveTrainFareClass(
        IReadOnlyCollection<TrainFareClassPrice> fareClasses,
        string? seatClass,
        TrainSeatType seatType)
    {
        if (!string.IsNullOrWhiteSpace(seatClass))
        {
            var normalized = seatClass.Trim();
            var exact = fareClasses.FirstOrDefault(x =>
                string.Equals(x.Code, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Name, normalized, StringComparison.OrdinalIgnoreCase));

            if (exact is not null)
                return exact;
        }

        return fareClasses.FirstOrDefault(x => x.SeatType == seatType);
    }

    private sealed class TrainFareClassPrice
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = "";
        public string Name { get; init; } = "";
        public TrainSeatType SeatType { get; init; }
        public decimal DefaultModifier { get; init; }
    }

    private sealed class TrainFareRulePrice
    {
        public Guid FareClassId { get; init; }
        public Guid? TripId { get; init; }
        public Guid? RouteId { get; init; }
        public string CurrencyCode { get; init; } = "VND";
        public decimal TotalPrice { get; init; }
    }
}
