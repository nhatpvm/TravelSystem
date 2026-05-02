using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Commerce;

namespace TicketBooking.Api.Services.Commerce;

public sealed partial class CustomerOrderService
{
    private async Task<(CustomerOrder Order, CustomerPayment Payment)> LoadOrderGraphForUserAsync(
        string orderCode,
        Guid userId,
        CancellationToken ct)
    {
        var normalizedOrderCode = NormalizeRequired(orderCode, "Thiếu mã đơn hàng.");

        var order = await _db.CustomerOrders
            .FirstOrDefaultAsync(x =>
                x.OrderCode == normalizedOrderCode &&
                x.UserId == userId &&
                !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        var payment = await _db.CustomerPayments
            .FirstOrDefaultAsync(x => x.OrderId == order.Id && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy giao dịch thanh toán cho đơn hàng.");

        return (order, payment);
    }

    private async Task<CustomerOrderDetailDto> MapOrderDetailAsync(
        Guid orderId,
        CancellationToken ct,
        SePayCheckoutFormDto? checkoutForm = null)
    {
        var order = await _db.CustomerOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == orderId && !x.IsDeleted, ct)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        var payment = await _db.CustomerPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == order.Id && !x.IsDeleted, ct);

        var ticket = await _db.CustomerTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == order.Id && !x.IsDeleted, ct);

        var refunds = await _db.CustomerRefundRequests
            .AsNoTracking()
            .Where(x => x.OrderId == order.Id && !x.IsDeleted)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync(ct);

        return new CustomerOrderDetailDto
        {
            Id = order.Id,
            TenantId = order.TenantId,
            OrderCode = order.OrderCode,
            CurrencyCode = order.CurrencyCode,
            ProductType = order.ProductType,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            TicketStatus = order.TicketStatus,
            RefundStatus = order.RefundStatus,
            GrossAmount = order.GrossAmount,
            DiscountAmount = order.DiscountAmount,
            ServiceFeeAmount = order.ServiceFeeAmount,
            PlatformCommissionAmount = order.PlatformCommissionAmount,
            TenantNetAmount = order.TenantNetAmount,
            PayableAmount = order.PayableAmount,
            RefundedAmount = order.RefundedAmount,
            ContactFullName = order.ContactFullName,
            ContactPhone = order.ContactPhone,
            ContactEmail = order.ContactEmail,
            VatInvoiceRequested = order.VatInvoiceRequested,
            CustomerNote = order.CustomerNote,
            FailureReason = order.FailureReason,
            ExpiresAt = order.ExpiresAt,
            CreatedAt = order.CreatedAt,
            PaidAt = order.PaidAt,
            TicketIssuedAt = order.TicketIssuedAt,
            Payment = payment is null ? null : new CustomerPaymentDto
            {
                Id = payment.Id,
                PaymentCode = payment.PaymentCode,
                ProviderInvoiceNumber = payment.ProviderInvoiceNumber,
                ProviderOrderId = payment.ProviderOrderId,
                Provider = payment.Provider,
                Method = payment.Method,
                Status = payment.Status,
                CurrencyCode = payment.CurrencyCode,
                Amount = payment.Amount,
                PaidAmount = payment.PaidAmount,
                RefundedAmount = payment.RefundedAmount,
                ExpiresAt = payment.ExpiresAt,
                PaidAt = payment.PaidAt,
                LastSyncedAt = payment.LastSyncedAt,
                CheckoutForm = checkoutForm,
            },
            Ticket = ticket is null ? null : new CustomerTicketDto
            {
                Id = ticket.Id,
                TicketCode = ticket.TicketCode,
                Status = ticket.Status,
                Title = ticket.Title,
                Subtitle = ticket.Subtitle,
                IssuedAt = ticket.IssuedAt,
                Snapshot = ParseJsonElement(ticket.SnapshotJson),
            },
            Refunds = refunds.Select(MapRefund).ToList(),
            Snapshot = ParseJsonElement(order.SnapshotJson),
        };
    }

    private static CustomerRefundDto MapRefund(CustomerRefundRequest refund)
    {
        return new CustomerRefundDto
        {
            Id = refund.Id,
            RefundCode = refund.RefundCode,
            Status = refund.Status,
            CurrencyCode = refund.CurrencyCode,
            RequestedAmount = refund.RequestedAmount,
            ApprovedAmount = refund.ApprovedAmount,
            RefundedAmount = refund.RefundedAmount,
            ReasonCode = refund.ReasonCode,
            ReasonText = refund.ReasonText,
            ReviewNote = refund.ReviewNote,
            RequestedAt = refund.RequestedAt,
            ReviewedAt = refund.ReviewedAt,
            CompletedAt = refund.CompletedAt,
        };
    }

    private (CustomerOrder Order, CustomerPayment Payment, CustomerVatInvoiceRequest? VatInvoiceRequest) CreateOrderSkeleton(
        CustomerProductType productType,
        CreateCustomerOrderRequest request,
        Guid userId,
        DateTimeOffset now)
    {
        var order = new CustomerOrder
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductType = productType,
            OrderCode = GenerateCode("ORD"),
            Status = CustomerOrderStatus.PendingPayment,
            PaymentStatus = CustomerPaymentStatus.Pending,
            TicketStatus = CustomerTicketStatus.Pending,
            RefundStatus = CustomerRefundStatus.None,
            ContactFullName = request.Contact.FullName.Trim(),
            ContactPhone = request.Contact.Phone.Trim(),
            ContactEmail = request.Contact.Email.Trim(),
            VatInvoiceRequested = request.Vat is not null,
            CustomerNote = NormalizeOptional(request.CustomerNote),
            SnapshotJson = "{}",
            ExpiresAt = now.AddMinutes(_options.PendingPaymentMinutes),
            CreatedAt = now,
            CreatedByUserId = userId,
        };

        var payment = new CustomerPayment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OrderId = order.Id,
            Provider = CustomerPaymentProvider.SePay,
            Method = CustomerPaymentMethod.BankTransfer,
            Status = CustomerPaymentStatus.Pending,
            PaymentCode = GenerateCode("PAY"),
            ProviderInvoiceNumber = order.OrderCode,
            CurrencyCode = "VND",
            ExpiresAt = order.ExpiresAt,
            CreatedAt = now,
            CreatedByUserId = userId,
        };

        CustomerVatInvoiceRequest? vat = null;
        if (request.Vat is not null)
        {
            vat = new CustomerVatInvoiceRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                OrderId = order.Id,
                RequestCode = GenerateCode("VAT"),
                Status = CustomerVatInvoiceStatus.Requested,
                CompanyName = request.Vat.CompanyName.Trim(),
                TaxCode = request.Vat.TaxCode.Trim(),
                CompanyAddress = request.Vat.CompanyAddress.Trim(),
                InvoiceEmail = string.IsNullOrWhiteSpace(request.Vat.InvoiceEmail)
                    ? request.Contact.Email.Trim()
                    : request.Vat.InvoiceEmail.Trim(),
                CreatedAt = now,
                CreatedByUserId = userId,
            };
        }

        return (order, payment, vat);
    }

    private void PreparePaymentAndVat(CustomerOrder order, CustomerPayment payment, CustomerVatInvoiceRequest? vat)
    {
        payment.TenantId = order.TenantId;
        payment.CurrencyCode = order.CurrencyCode;
        payment.Amount = order.PayableAmount;
        payment.ExpiresAt = order.ExpiresAt;

        if (vat is not null)
            vat.TenantId = order.TenantId;
    }

    private void ApplyOrderAmounts(CustomerOrder order, string currencyCode, decimal grossAmount)
    {
        order.CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "VND" : currencyCode;
        order.GrossAmount = grossAmount;
        order.DiscountAmount = 0m;
        order.ServiceFeeAmount = 0m;
        order.PlatformCommissionAmount = CalculateCommissionAmount(grossAmount, order.CurrencyCode);
        order.TenantNetAmount = Math.Max(0m, grossAmount - order.PlatformCommissionAmount);
        order.PayableAmount = grossAmount;
    }

    private decimal CalculateCommissionAmount(decimal amount, string currencyCode)
    {
        var raw = amount * (_options.DefaultCommissionPercent / 100m);
        var decimals = string.Equals(currencyCode, "VND", StringComparison.OrdinalIgnoreCase) ? 0 : 2;
        return decimal.Round(raw, decimals, MidpointRounding.AwayFromZero);
    }

    private static void ValidateCreateRequest(CreateCustomerOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProductType))
            throw new InvalidOperationException("Thiếu loại sản phẩm để tạo đơn hàng.");
        if (string.IsNullOrWhiteSpace(request.Contact.FullName))
            throw new InvalidOperationException("Thiếu họ tên liên hệ.");
        if (string.IsNullOrWhiteSpace(request.Contact.Phone))
            throw new InvalidOperationException("Thiếu số điện thoại liên hệ.");
        if (string.IsNullOrWhiteSpace(request.Contact.Email))
            throw new InvalidOperationException("Thiếu email nhận vé.");
    }

    private static CustomerProductType ParseProductType(string raw)
    {
        return raw.Trim().ToLowerInvariant() switch
        {
            "bus" => CustomerProductType.Bus,
            "train" => CustomerProductType.Train,
            "flight" => CustomerProductType.Flight,
            "hotel" => CustomerProductType.Hotel,
            "tour" => CustomerProductType.Tour,
            _ => throw new InvalidOperationException("Loại sản phẩm không được hỗ trợ."),
        };
    }

    private static CustomerOrderSnapshot DeserializeSnapshot(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new CustomerOrderSnapshot();

        try
        {
            return JsonSerializer.Deserialize<CustomerOrderSnapshot>(json, JsonOptions) ?? new CustomerOrderSnapshot();
        }
        catch
        {
            return new CustomerOrderSnapshot();
        }
    }

    private static T? DeserializeMetadata<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch
        {
            return default;
        }
    }

    private static string SerializeJson<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static JsonElement ParseJsonElement(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return JsonDocument.Parse("{}").RootElement.Clone();

        try
        {
            return JsonDocument.Parse(json).RootElement.Clone();
        }
        catch
        {
            return JsonDocument.Parse("{}").RootElement.Clone();
        }
    }

    private static string GenerateCode(string prefix)
    {
        var suffix = $"{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid():N}"[..20];
        return $"{prefix}-{suffix}".ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeRequired(string? value, string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(errorMessage);

        return value.Trim();
    }

    private static string ResolveStopName(
        Guid stopPointId,
        IEnumerable<StopPointLookup> stopPoints,
        IEnumerable<LocationLookup> locations,
        string fallback)
    {
        var stopPoint = stopPoints.FirstOrDefault(x => x.Id == stopPointId);
        if (stopPoint is null)
            return fallback;

        var locationName = locations.FirstOrDefault(x => x.Id == stopPoint.LocationId)?.Name;
        return string.IsNullOrWhiteSpace(locationName)
            ? (string.IsNullOrWhiteSpace(stopPoint.Name) ? fallback : stopPoint.Name)
            : locationName;
    }

    private static DateTimeOffset? BuildTourDateTime(DateOnly? date, TimeOnly? time)
    {
        if (!date.HasValue)
            return null;

        var localDateTime = date.Value.ToDateTime(time ?? TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(localDateTime, TimeSpan.FromHours(7));
    }

    private static DateTimeOffset MinDateTime(DateTimeOffset left, DateTimeOffset right) => left <= right ? left : right;

    private static IEnumerable<DateOnly> EachNight(DateOnly checkInDate, DateOnly checkOutDate)
    {
        for (var current = checkInDate; current < checkOutDate; current = current.AddDays(1))
            yield return current;
    }

    private string BuildGatewayOrderDescription(CustomerOrder order, CustomerOrderSnapshot snapshot)
    {
        var title = string.IsNullOrWhiteSpace(snapshot.Title)
            ? $"Thanh toán đơn {order.OrderCode}"
            : snapshot.Title;

        var normalized = $"{GetProductLabel(order.ProductType)} - {title}";
        return normalized.Length > 120 ? normalized[..120] : normalized;
    }

    private static string GetPaymentStatusLabel(CustomerPaymentStatus status)
    {
        return status switch
        {
            CustomerPaymentStatus.Pending => "chờ thanh toán",
            CustomerPaymentStatus.Paid => "đã thanh toán",
            CustomerPaymentStatus.Cancelled => "đã hủy",
            CustomerPaymentStatus.Expired => "đã hết hạn",
            CustomerPaymentStatus.Failed => "thanh toán thất bại",
            CustomerPaymentStatus.RefundedPartial => "hoàn tiền một phần",
            CustomerPaymentStatus.RefundedFull => "hoàn tiền toàn phần",
            _ => "không xác định",
        };
    }

    private static string GetProductLabel(CustomerProductType productType)
    {
        return productType switch
        {
            CustomerProductType.Bus => "Xe khách",
            CustomerProductType.Train => "Tàu hỏa",
            CustomerProductType.Flight => "Chuyến bay",
            CustomerProductType.Hotel => "Khách sạn",
            CustomerProductType.Tour => "Tour",
            _ => "Dịch vụ",
        };
    }
}

internal sealed class StopPointLookup
{
    public Guid Id { get; set; }
    public Guid LocationId { get; set; }
    public string Name { get; set; } = "";
}

internal sealed class LocationLookup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

internal sealed class AncillaryProjection
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string CurrencyCode { get; set; } = "VND";
    public decimal Price { get; set; }
}

internal sealed class BusOrderMetadata
{
    public Guid TripId { get; set; }
    public string HoldToken { get; set; } = "";
    public Guid FromTripStopTimeId { get; set; }
    public Guid ToTripStopTimeId { get; set; }
    public List<Guid> SeatIds { get; set; } = new();
    public List<string> SeatNumbers { get; set; } = new();
    public List<CustomerPassengerInput> Passengers { get; set; } = new();
}

internal sealed class TrainOrderMetadata
{
    public Guid TripId { get; set; }
    public string HoldToken { get; set; } = "";
    public Guid FromTripStopTimeId { get; set; }
    public Guid ToTripStopTimeId { get; set; }
    public List<Guid> TrainCarSeatIds { get; set; } = new();
    public List<string> SeatNumbers { get; set; } = new();
    public List<CustomerPassengerInput> Passengers { get; set; } = new();
}

internal sealed class FlightOrderMetadata
{
    public Guid OfferId { get; set; }
    public Guid? EffectiveOfferId { get; set; }
    public Guid? SeatId { get; set; }
    public string? SeatNumber { get; set; }
    public List<Guid> AncillaryIds { get; set; } = new();
    public int PassengerCount { get; set; }
    public bool InventoryHeld { get; set; }
    public bool InventoryConfirmed { get; set; }
    public List<CustomerPassengerInput> Passengers { get; set; } = new();
}

internal sealed class HotelOrderMetadata
{
    public Guid HotelId { get; set; }
    public Guid RoomTypeId { get; set; }
    public Guid RatePlanId { get; set; }
    public Guid InventoryHoldId { get; set; }
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int RoomCount { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public List<CustomerPassengerInput> Passengers { get; set; } = new();
}

internal sealed class TourOrderMetadata
{
    public Guid TourId { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid PackageId { get; set; }
    public Guid ReservationId { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public List<CustomerPassengerInput> Passengers { get; set; } = new();
}
