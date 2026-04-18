using System.Data;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Commerce;
using TicketBooking.Domain.Hotels;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Api.Services.Commerce;

public sealed partial class CustomerOrderService
{
    private async Task<CustomerOrderDetailDto> CreateHotelOrderAsync(
        CreateCustomerOrderRequest request,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (!request.HotelId.HasValue || request.HotelId == Guid.Empty)
            throw new InvalidOperationException("Thiếu khách sạn để tạo đơn.");
        if (!request.RoomTypeId.HasValue || request.RoomTypeId == Guid.Empty)
            throw new InvalidOperationException("Thiếu loại phòng để tạo đơn.");
        if (!request.RatePlanId.HasValue || request.RatePlanId == Guid.Empty)
            throw new InvalidOperationException("Thiếu rate plan để tạo đơn.");
        if (!request.CheckInDate.HasValue || !request.CheckOutDate.HasValue)
            throw new InvalidOperationException("Thiếu ngày nhận hoặc trả phòng.");
        if (request.CheckOutDate <= request.CheckInDate)
            throw new InvalidOperationException("Ngày trả phòng phải sau ngày nhận phòng.");

        var (order, payment, vat) = CreateOrderSkeleton(CustomerProductType.Hotel, request, userId, now);
        var stayDates = EachNight(request.CheckInDate.Value, request.CheckOutDate.Value).ToList();
        if (stayDates.Count == 0)
            throw new InvalidOperationException("Khoảng lưu trú không hợp lệ.");

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var hotel = await _db.Hotels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.HotelId.Value && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy khách sạn.");
        if (!hotel.IsActive || hotel.Status != HotelStatus.Active)
            throw new InvalidOperationException("Khách sạn không còn mở bán.");

        var roomType = await _db.RoomTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.RoomTypeId.Value && x.HotelId == hotel.Id && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy loại phòng.");
        if (!roomType.IsActive || roomType.Status != RoomTypeStatus.Active)
            throw new InvalidOperationException("Loại phòng không còn mở bán.");

        var ratePlan = await _db.RatePlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.RatePlanId.Value && x.HotelId == hotel.Id && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy gói giá.");
        if (!ratePlan.IsActive || ratePlan.Status != RatePlanStatus.Active)
            throw new InvalidOperationException("Gói giá không còn mở bán.");

        var mapping = await _db.RatePlanRoomTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.RatePlanId == ratePlan.Id && x.RoomTypeId == roomType.Id && x.IsActive && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Gói giá không áp dụng cho loại phòng đã chọn.");

        await ReleaseExpiredHotelHoldsAsync(roomType.Id, now, ct);

        var roomCount = request.RoomCount < 1 ? 1 : request.RoomCount;
        var adults = request.AdultCount < 1 ? 1 : request.AdultCount;
        var children = request.ChildCount < 0 ? 0 : request.ChildCount;

        if (roomType.MaxAdults > 0 && adults > roomType.MaxAdults * roomCount)
            throw new InvalidOperationException("Số người lớn vượt quá sức chứa loại phòng.");
        if (roomType.MaxChildren > 0 && children > roomType.MaxChildren * roomCount)
            throw new InvalidOperationException("Số trẻ em vượt quá sức chứa loại phòng.");
        if (roomType.MaxGuests > 0 && adults + children > roomType.MaxGuests * roomCount)
            throw new InvalidOperationException("Tổng số khách vượt quá sức chứa loại phòng.");

        var inventories = await _db.RoomTypeInventories.IgnoreQueryFilters()
            .Where(x => x.RoomTypeId == roomType.Id && stayDates.Contains(x.Date) && !x.IsDeleted)
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        if (inventories.Count != stayDates.Count)
            throw new InvalidOperationException("Khách sạn chưa mở tồn phòng đủ cho khoảng ngày đã chọn.");

        foreach (var inventory in inventories)
        {
            if (inventory.Status != InventoryStatus.Open)
                throw new InvalidOperationException($"Ngày {inventory.Date:dd/MM/yyyy} hiện không nhận đặt phòng.");

            var availableUnits = inventory.TotalUnits - inventory.SoldUnits - inventory.HeldUnits;
            if (availableUnits < roomCount)
                throw new InvalidOperationException($"Khách sạn không còn đủ phòng cho ngày {inventory.Date:dd/MM/yyyy}.");
        }

        var dailyRates = await _db.DailyRates.IgnoreQueryFilters()
            .Where(x =>
                x.RatePlanRoomTypeId == mapping.Id &&
                stayDates.Contains(x.Date) &&
                x.IsActive &&
                !x.IsDeleted)
            .OrderBy(x => x.Date)
            .ToListAsync(ct);

        var currencyCode = string.IsNullOrWhiteSpace(mapping.CurrencyCode) ? "VND" : mapping.CurrencyCode;
        var lines = new List<CustomerOrderSnapshotLine>();
        foreach (var date in stayDates)
        {
            var daily = dailyRates.FirstOrDefault(x => x.Date == date);
            var nightlyPrice = daily?.Price ?? mapping.BasePrice;
            if (!nightlyPrice.HasValue)
                throw new InvalidOperationException($"Chưa có giá cho đêm {date:dd/MM/yyyy}.");

            if (!string.IsNullOrWhiteSpace(daily?.CurrencyCode))
                currencyCode = daily!.CurrencyCode;

            lines.Add(new CustomerOrderSnapshotLine
            {
                Label = $"Đêm {date:dd/MM/yyyy}",
                Quantity = roomCount,
                UnitAmount = nightlyPrice.Value,
                LineAmount = nightlyPrice.Value * roomCount,
            });
        }

        ApplyOrderAmounts(order, currencyCode, lines.Sum(x => x.LineAmount));
        order.TenantId = hotel.TenantId;
        order.SourceEntityId = hotel.Id;
        order.ExpiresAt = now.AddMinutes(_options.PendingPaymentMinutes);

        var inventoryHold = new InventoryHold
        {
            Id = Guid.NewGuid(),
            TenantId = hotel.TenantId,
            RoomTypeId = roomType.Id,
            BookingId = order.Id,
            CheckInDate = request.CheckInDate.Value,
            CheckOutDate = request.CheckOutDate.Value,
            Units = roomCount,
            Status = HoldStatus.Held,
            HoldExpiresAt = order.ExpiresAt,
            CorrelationId = order.OrderCode,
            Notes = request.CustomerNote,
            CreatedAt = now,
            CreatedByUserId = userId,
        };

        foreach (var inventory in inventories)
        {
            inventory.HeldUnits += roomCount;
            inventory.UpdatedAt = now;
            inventory.UpdatedByUserId = userId;
        }

        order.SourceReservationId = inventoryHold.Id;
        order.SnapshotJson = SerializeJson(new CustomerOrderSnapshot
        {
            Title = hotel.Name,
            Subtitle = roomType.Name,
            ProviderName = hotel.Name,
            LocationText = string.Join(", ", new[] { hotel.City, hotel.Province }.Where(x => !string.IsNullOrWhiteSpace(x))),
            RoomText = $"{roomCount} phòng · {roomType.Name}",
            PassengerText = $"{adults} người lớn" + (children > 0 ? $" · {children} trẻ em" : string.Empty),
            TicketNote = "Vui lòng xuất trình mã đơn hàng tại quầy check-in của khách sạn.",
            SourceCode = ratePlan.Code,
            Lines = lines,
            MetadataJson = SerializeJson(new
            {
                checkInDate = request.CheckInDate,
                checkOutDate = request.CheckOutDate,
                passengers = request.Passengers,
            }),
        });
        order.MetadataJson = SerializeJson(new HotelOrderMetadata
        {
            HotelId = hotel.Id,
            RoomTypeId = roomType.Id,
            RatePlanId = ratePlan.Id,
            InventoryHoldId = inventoryHold.Id,
            CheckInDate = request.CheckInDate.Value,
            CheckOutDate = request.CheckOutDate.Value,
            RoomCount = roomCount,
            Adults = adults,
            Children = children,
            Passengers = request.Passengers,
        });

        PreparePaymentAndVat(order, payment, vat);
        _db.InventoryHolds.Add(inventoryHold);
        _db.CustomerOrders.Add(order);
        _db.CustomerPayments.Add(payment);
        if (vat is not null)
            _db.CustomerVatInvoiceRequests.Add(vat);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        await CreateOrderCreatedNotificationAsync(order, ct);
        return await MapOrderDetailAsync(order.Id, ct);
    }

    private async Task<CustomerOrderDetailDto> CreateTourOrderAsync(
        CreateCustomerOrderRequest request,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (!request.TourId.HasValue || request.TourId == Guid.Empty)
            throw new InvalidOperationException("Thiếu tour để tạo đơn.");
        if (!request.ScheduleId.HasValue || request.ScheduleId == Guid.Empty)
            throw new InvalidOperationException("Thiếu lịch tour để tạo đơn.");

        var (order, payment, vat) = CreateOrderSkeleton(CustomerProductType.Tour, request, userId, now);
        var totalPax = Math.Max(1, request.AdultCount + request.ChildCount);

        var reservationResult = await _tourReservationService.HoldAsync(
            request.TourId.Value,
            new TourPackageReservationCreateRequest
            {
                ScheduleId = request.ScheduleId.Value,
                PackageId = request.PackageId,
                TotalPax = totalPax,
                IncludeDefaultPackageOptions = true,
                ClientToken = order.OrderCode,
                Notes = request.CustomerNote,
            },
            userId,
            false,
            ct);

        var reservation = reservationResult.Reservation;
        if (!reservation.HoldExpiresAt.HasValue)
            throw new InvalidOperationException("Không xác định được thời hạn giữ chỗ tour.");

        var tour = await _db.Tours.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == request.TourId.Value && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy tour.");
        var schedule = await _db.TourSchedules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == reservation.ScheduleId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy lịch tour.");
        var package = await _db.TourPackages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == reservation.PackageId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy gói tour.");

        var pricing = await ResolveTourCustomerPricingAsync(
            schedule.Id,
            reservation.CurrencyCode,
            request.AdultCount,
            request.ChildCount,
            package.Name,
            reservation.PackageSubtotalAmount,
            ct);

        ApplyOrderAmounts(order, pricing.CurrencyCode, pricing.TotalAmount);
        order.TenantId = tour.TenantId;
        order.SourceEntityId = tour.Id;
        order.SourceReservationId = reservation.Id;
        order.ExpiresAt = reservation.HoldExpiresAt.Value;
        order.SnapshotJson = SerializeJson(new CustomerOrderSnapshot
        {
            Title = tour.Name,
            Subtitle = package.Name,
            ImageUrl = tour.CoverImageUrl,
            ProviderName = tour.City,
            LocationText = string.Join(", ", new[] { tour.City, tour.Province }.Where(x => !string.IsNullOrWhiteSpace(x))),
            PassengerText = $"{request.AdultCount} người lớn" + (request.ChildCount > 0 ? $" · {request.ChildCount} trẻ em" : string.Empty),
            DepartureAt = BuildTourDateTime(schedule.DepartureDate, schedule.DepartureTime),
            ArrivalAt = BuildTourDateTime(schedule.ReturnDate, schedule.ReturnTime),
            TicketNote = "Tour chỉ được xác nhận chính thức sau khi hệ thống ghi nhận thanh toán thành công.",
            SourceCode = reservation.Code,
            Lines = pricing.Lines,
            MetadataJson = SerializeJson(new
            {
                passengers = request.Passengers,
                reservationId = reservation.Id,
            }),
        });
        order.MetadataJson = SerializeJson(new TourOrderMetadata
        {
            TourId = tour.Id,
            ScheduleId = reservation.ScheduleId,
            PackageId = reservation.PackageId,
            ReservationId = reservation.Id,
            Adults = request.AdultCount,
            Children = request.ChildCount,
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

    private async Task<(string CurrencyCode, decimal TotalAmount, List<CustomerOrderSnapshotLine> Lines)> ResolveTourCustomerPricingAsync(
        Guid scheduleId,
        string? expectedCurrency,
        int adultCount,
        int childCount,
        string packageName,
        decimal packageSubtotalAmount,
        CancellationToken ct)
    {
        var requestedPriceTypes = new List<TourPriceType>();
        if (adultCount > 0)
            requestedPriceTypes.Add(TourPriceType.Adult);
        if (childCount > 0)
            requestedPriceTypes.Add(TourPriceType.Child);

        var schedulePrices = await _db.TourSchedulePrices.IgnoreQueryFilters()
            .Where(x =>
                x.TourScheduleId == scheduleId &&
                x.IsActive &&
                !x.IsDeleted &&
                requestedPriceTypes.Contains(x.PriceType))
            .ToListAsync(ct);

        var currencyCode = string.IsNullOrWhiteSpace(expectedCurrency)
            ? "VND"
            : expectedCurrency.Trim().ToUpperInvariant();
        var passengerRequests = new List<TourPriceCalculationPassengerLineRequest>();

        if (adultCount > 0)
        {
            var adultPrice = TourPricingResolver.ResolvePriceForQuantity(schedulePrices, TourPriceType.Adult, adultCount)
                ?? throw new InvalidOperationException("Thiếu giá người lớn cho lịch tour đã chọn.");

            currencyCode = string.IsNullOrWhiteSpace(adultPrice.CurrencyCode)
                ? currencyCode
                : adultPrice.CurrencyCode.Trim().ToUpperInvariant();

            passengerRequests.Add(new TourPriceCalculationPassengerLineRequest
            {
                PriceType = TourPriceType.Adult,
                DisplayName = "Người lớn",
                Quantity = adultCount,
                CurrencyCode = adultPrice.CurrencyCode,
                UnitPrice = adultPrice.Price,
                UnitOriginalPrice = adultPrice.OriginalPrice,
                UnitTaxes = adultPrice.Taxes,
                UnitFees = adultPrice.Fees,
                IsIncludedTax = adultPrice.IsIncludedTax,
                IsIncludedFee = adultPrice.IsIncludedFee,
                Label = adultPrice.Label,
            });
        }

        if (childCount > 0)
        {
            var childPrice = TourPricingResolver.ResolvePriceForQuantity(schedulePrices, TourPriceType.Child, childCount)
                ?? throw new InvalidOperationException("Thiếu giá trẻ em cho lịch tour đã chọn.");

            currencyCode = string.IsNullOrWhiteSpace(childPrice.CurrencyCode)
                ? currencyCode
                : childPrice.CurrencyCode.Trim().ToUpperInvariant();

            passengerRequests.Add(new TourPriceCalculationPassengerLineRequest
            {
                PriceType = TourPriceType.Child,
                DisplayName = "Trẻ em",
                Quantity = childCount,
                CurrencyCode = childPrice.CurrencyCode,
                UnitPrice = childPrice.Price,
                UnitOriginalPrice = childPrice.OriginalPrice,
                UnitTaxes = childPrice.Taxes,
                UnitFees = childPrice.Fees,
                IsIncludedTax = childPrice.IsIncludedTax,
                IsIncludedFee = childPrice.IsIncludedFee,
                Label = childPrice.Label,
            });
        }

        var passengerPricing = new TourPriceCalculator().Calculate(new TourPriceCalculationRequest
        {
            CurrencyCode = currencyCode,
            PassengerLines = passengerRequests,
        });

        var lines = passengerPricing.PassengerLines
            .Select(line =>
            {
                var lineAmount = line.LineBaseAmount + line.LineTaxAmount + line.LineFeeAmount;
                var unitAmount = line.Quantity <= 0
                    ? lineAmount
                    : decimal.Round(lineAmount / line.Quantity, 0, MidpointRounding.AwayFromZero);

                return new CustomerOrderSnapshotLine
                {
                    Label = line.Name,
                    Quantity = line.Quantity,
                    UnitAmount = unitAmount,
                    LineAmount = lineAmount,
                };
            })
            .ToList();

        if (packageSubtotalAmount > 0)
        {
            lines.Add(new CustomerOrderSnapshotLine
            {
                Label = packageName,
                Quantity = 1,
                UnitAmount = packageSubtotalAmount,
                LineAmount = packageSubtotalAmount,
            });
        }

        return (currencyCode, passengerPricing.TotalAmount + packageSubtotalAmount, lines);
    }
}
