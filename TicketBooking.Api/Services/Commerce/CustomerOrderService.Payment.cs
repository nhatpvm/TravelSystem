using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Commerce;
using TicketBooking.Domain.Hotels;

namespace TicketBooking.Api.Services.Commerce;

public sealed partial class CustomerOrderService
{
    public async Task<CustomerOrderDetailDto> CancelOrderAsync(
        string orderCode,
        Guid userId,
        CancellationToken ct = default)
    {
        var (order, payment) = await LoadOrderGraphForUserAsync(orderCode, userId, ct);
        await EnsurePendingOrderStateAsync(order, payment, ct);

        if (payment.Status == CustomerPaymentStatus.Paid)
            throw new InvalidOperationException("Đơn hàng đã thanh toán. Vui lòng tạo yêu cầu hoàn tiền thay vì hủy trực tiếp.");

        if (payment.Status != CustomerPaymentStatus.Pending)
            return await MapOrderDetailAsync(order.Id, ct);

        await MarkOrderAsUnpaidFinalAsync(
            order,
            payment,
            CustomerPaymentStatus.Cancelled,
            userId,
            ct,
            shouldCancelProviderOrder: true);
        await _db.SaveChangesAsync(ct);
        return await MapOrderDetailAsync(order.Id, ct);
    }

    public async Task<CustomerOrderDetailDto> StartPaymentAsync(
        string orderCode,
        Guid userId,
        StartPaymentRequest request,
        CancellationToken ct = default)
    {
        var (order, payment) = await LoadOrderGraphForUserAsync(orderCode, userId, ct);
        await EnsurePendingOrderStateAsync(order, payment, ct);

        if (order.PaymentStatus == CustomerPaymentStatus.Paid)
            return await MapOrderDetailAsync(order.Id, ct);

        if (order.Status is CustomerOrderStatus.Cancelled or CustomerOrderStatus.Expired or CustomerOrderStatus.Failed)
            throw new InvalidOperationException("Đơn hàng không còn hợp lệ để thanh toán.");

        var snapshot = DeserializeSnapshot(order.SnapshotJson);
        var checkoutForm = _sePayGatewayService.BuildCheckoutForm(
            order,
            payment,
            request.AppBaseUrl,
            BuildGatewayOrderDescription(order, snapshot),
            userId.ToString("N"));

        payment.RequestPayloadJson = SerializeJson(checkoutForm);
        payment.ProviderResponseJson = null;
        payment.LastSyncedAt = DateTimeOffset.UtcNow;
        payment.UpdatedAt = payment.LastSyncedAt;
        payment.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return await MapOrderDetailAsync(order.Id, ct, checkoutForm);
    }

    public async Task<CustomerOrderDetailDto> SyncPaymentAsync(
        string orderCode,
        Guid userId,
        CancellationToken ct = default)
    {
        var (order, payment) = await LoadOrderGraphForUserAsync(orderCode, userId, ct);
        await EnsurePendingOrderStateAsync(order, payment, ct);

        if (payment.Status is CustomerPaymentStatus.Paid or CustomerPaymentStatus.Cancelled or CustomerPaymentStatus.Expired or CustomerPaymentStatus.Failed)
            return await MapOrderDetailAsync(order.Id, ct);

        var syncResult = await _sePayGatewayService.FindOrderByInvoiceAsync(payment.ProviderInvoiceNumber, ct);
        if (syncResult is null)
        {
            payment.LastSyncedAt = DateTimeOffset.UtcNow;
            payment.UpdatedAt = payment.LastSyncedAt;
            payment.UpdatedByUserId = userId;
            await _db.SaveChangesAsync(ct);
            return await MapOrderDetailAsync(order.Id, ct);
        }

        await ApplySyncedPaymentStateAsync(order, payment, syncResult, userId, ct);
        return await MapOrderDetailAsync(order.Id, ct);
    }

    public async Task<CustomerRefundDto> CreateRefundRequestAsync(
        string orderCode,
        Guid userId,
        CreateRefundRequestInput request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (order, payment) = await LoadOrderGraphForUserAsync(orderCode, userId, ct);
        await EnsurePendingOrderStateAsync(order, payment, ct);

        if (order.PaymentStatus != CustomerPaymentStatus.Paid)
            throw new InvalidOperationException("Chỉ có thể gửi yêu cầu hoàn tiền cho đơn đã thanh toán.");

        var remainingRefundable = Math.Max(0m, order.PayableAmount - order.RefundedAmount);
        if (remainingRefundable <= 0)
            throw new InvalidOperationException("Đơn hàng này không còn số dư để hoàn tiền.");

        var requestedAmount = request.RequestedAmount.GetValueOrDefault(remainingRefundable);
        if (requestedAmount <= 0 || requestedAmount > remainingRefundable)
            throw new InvalidOperationException("Số tiền yêu cầu hoàn không hợp lệ.");

        var now = DateTimeOffset.UtcNow;
        var refund = new CustomerRefundRequest
        {
            Id = Guid.NewGuid(),
            TenantId = order.TenantId,
            UserId = userId,
            OrderId = order.Id,
            PaymentId = payment.Id,
            RefundCode = GenerateCode("RFD"),
            CurrencyCode = order.CurrencyCode,
            Status = CustomerRefundStatus.Requested,
            RequestedAmount = requestedAmount,
            ReasonCode = string.IsNullOrWhiteSpace(request.ReasonCode) ? "CUSTOMER_REQUEST" : request.ReasonCode.Trim().ToUpperInvariant(),
            ReasonText = NormalizeOptional(request.ReasonText),
            SnapshotJson = order.SnapshotJson,
            RequestedAt = now,
            CreatedAt = now,
            CreatedByUserId = userId,
        };

        order.RefundStatus = CustomerRefundStatus.Requested;
        order.Status = CustomerOrderStatus.RefundRequested;
        order.UpdatedAt = now;
        order.UpdatedByUserId = userId;

        _db.CustomerRefundRequests.Add(refund);
        await _db.SaveChangesAsync(ct);

        await _notificationService.CreateAsync(
            userId,
            order.TenantId,
            "refund",
            "Đã ghi nhận yêu cầu hoàn tiền",
            $"Yêu cầu hoàn tiền cho đơn {order.OrderCode} đã được gửi và đang chờ xét duyệt.",
            $"/my-account/bookings/{order.OrderCode}",
            "customer-order",
            order.Id,
            ct: ct);

        return MapRefund(refund);
    }

    public async Task<bool> HandleGatewayWebhookAsync(
        string rawBody,
        string? authorizationHeader,
        string? secretHeader,
        CancellationToken ct = default)
    {
        if (!IsWebhookAuthorized(authorizationHeader, secretHeader))
            return false;

        var payload = ParseWebhookPayload(rawBody);
        if (payload is null)
            return false;

        if (!payload.ShouldProcess)
            return true;

        if (string.IsNullOrWhiteSpace(payload.ProviderInvoiceNumber))
            return false;

        try
        {
            var payment = await _db.CustomerPayments
                .FirstOrDefaultAsync(x =>
                    x.ProviderInvoiceNumber == payload.ProviderInvoiceNumber &&
                    !x.IsDeleted, ct);

            if (payment is null)
                return true;

            var order = await _db.CustomerOrders
                .FirstOrDefaultAsync(x => x.Id == payment.OrderId && !x.IsDeleted, ct);

            if (order is null)
                return true;

            var sync = new SePayOrderSyncResult
            {
                ProviderInvoiceNumber = payload.ProviderInvoiceNumber,
                ProviderOrderId = payload.ProviderOrderId,
                RawStatus = payload.RawStatus,
                PaymentStatus = SePayGatewayService.MapPaymentStatus(payload.RawStatus),
                OrderStatus = SePayGatewayService.MapOrderStatus(payload.RawStatus),
                PaidAmount = payload.Amount,
                RawPayloadJson = rawBody,
            };

            await ApplySyncedPaymentStateAsync(order, payment, sync, order.UserId, ct, fromWebhook: true);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return await IsGatewayWebhookAlreadyAppliedAsync(payload, ct);
        }
        catch (DbUpdateException)
        {
            if (await IsGatewayWebhookAlreadyAppliedAsync(payload, ct))
                return true;

            throw;
        }
    }

    private async Task ApplySyncedPaymentStateAsync(
        CustomerOrder order,
        CustomerPayment payment,
        SePayOrderSyncResult syncResult,
        Guid actorUserId,
        CancellationToken ct,
        bool fromWebhook = false)
    {
        var now = DateTimeOffset.UtcNow;
        payment.ProviderOrderId = string.IsNullOrWhiteSpace(syncResult.ProviderOrderId)
            ? payment.ProviderOrderId
            : syncResult.ProviderOrderId;
        payment.ProviderResponseJson = syncResult.RawPayloadJson;
        payment.LastSyncedAt = now;
        payment.UpdatedAt = now;
        payment.UpdatedByUserId = actorUserId;

        if (fromWebhook)
        {
            payment.LastWebhookJson = syncResult.RawPayloadJson;
            payment.WebhookReceivedAt = now;
        }

        if (IsPaidTerminal(order, payment) || IsUnpaidTerminal(order, payment))
        {
            await _db.SaveChangesAsync(ct);
            return;
        }

        switch (syncResult.PaymentStatus)
        {
            case CustomerPaymentStatus.Paid when payment.Status != CustomerPaymentStatus.Paid:
                payment.Status = CustomerPaymentStatus.Paid;
                payment.PaidAmount = syncResult.PaidAmount.GetValueOrDefault(payment.Amount);
                payment.PaidAt = payment.PaidAt ?? now;
                payment.FailureReason = null;
                order.PaymentStatus = CustomerPaymentStatus.Paid;
                order.Status = CustomerOrderStatus.Paid;
                order.PaidAt = order.PaidAt ?? now;
                order.FailureReason = null;
                order.UpdatedAt = now;
                order.UpdatedByUserId = actorUserId;

                await FulfillPaidOrderAsync(order, actorUserId, ct);
                await _notificationService.CreateAsync(
                    order.UserId,
                    order.TenantId,
                    "payment",
                    "Thanh toán thành công",
                    $"Đơn hàng {order.OrderCode} đã được xác nhận thanh toán thành công.",
                    $"/payment?orderCode={Uri.EscapeDataString(order.OrderCode)}",
                    "customer-order",
                    order.Id,
                    ct: ct);
                break;

            case CustomerPaymentStatus.Cancelled:
            case CustomerPaymentStatus.Expired:
            case CustomerPaymentStatus.Failed:
                await MarkOrderAsUnpaidFinalAsync(order, payment, syncResult.PaymentStatus, actorUserId, ct);
                break;
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task FulfillPaidOrderAsync(CustomerOrder order, Guid actorUserId, CancellationToken ct)
    {
        switch (order.ProductType)
        {
            case CustomerProductType.Bus:
                await ConfirmBusOrderAsync(order, actorUserId, ct);
                break;
            case CustomerProductType.Train:
                await ConfirmTrainOrderAsync(order, actorUserId, ct);
                break;
            case CustomerProductType.Flight:
                await ConfirmFlightOrderAsync(order, actorUserId, ct);
                break;
            case CustomerProductType.Hotel:
                await ConfirmHotelOrderAsync(order, actorUserId, ct);
                break;
            case CustomerProductType.Tour:
                await ConfirmTourOrderAsync(order, actorUserId, ct);
                break;
        }

        await IssueTicketAsync(order, actorUserId, ct);
    }

    private async Task MarkOrderAsUnpaidFinalAsync(
        CustomerOrder order,
        CustomerPayment payment,
        CustomerPaymentStatus finalStatus,
        Guid actorUserId,
        CancellationToken ct,
        bool shouldCancelProviderOrder = false)
    {
        if (IsPaidTerminal(order, payment))
            return;

        var now = DateTimeOffset.UtcNow;
        var failureReason = finalStatus switch
        {
            CustomerPaymentStatus.Cancelled => "Khách hàng đã hủy hoặc đóng phiên thanh toán.",
            CustomerPaymentStatus.Expired => "Phiên thanh toán đã hết hạn.",
            CustomerPaymentStatus.Failed => "Thanh toán không thành công.",
            _ => payment.FailureReason,
        };

        payment.Status = finalStatus;
        payment.FailureReason = failureReason;
        payment.CancelledAt = finalStatus == CustomerPaymentStatus.Cancelled ? payment.CancelledAt ?? now : payment.CancelledAt;
        payment.FailedAt = finalStatus == CustomerPaymentStatus.Failed ? payment.FailedAt ?? now : payment.FailedAt;
        payment.UpdatedAt = now;
        payment.UpdatedByUserId = actorUserId;

        if (shouldCancelProviderOrder)
            await TryCancelGatewayOrderAsync(payment, ct);

        if (order.Status is CustomerOrderStatus.Cancelled or CustomerOrderStatus.Expired or CustomerOrderStatus.Failed)
            return;

        order.PaymentStatus = finalStatus;
        order.Status = finalStatus switch
        {
            CustomerPaymentStatus.Cancelled => CustomerOrderStatus.Cancelled,
            CustomerPaymentStatus.Expired => CustomerOrderStatus.Expired,
            CustomerPaymentStatus.Failed => CustomerOrderStatus.Failed,
            _ => order.Status,
        };
        order.FailureReason = failureReason;
        order.CancelledAt = finalStatus == CustomerPaymentStatus.Cancelled ? order.CancelledAt ?? now : order.CancelledAt;
        order.UpdatedAt = now;
        order.UpdatedByUserId = actorUserId;

        await ReleaseOrderResourcesAsync(order, actorUserId, ct, finalStatus);
        await _notificationService.CreateAsync(
            order.UserId,
            order.TenantId,
            "payment",
            finalStatus == CustomerPaymentStatus.Expired ? "Đơn hàng đã hết hạn" : "Thanh toán chưa hoàn tất",
            $"Đơn hàng {order.OrderCode} hiện ở trạng thái {GetPaymentStatusLabel(finalStatus)}.",
            $"/payment?orderCode={Uri.EscapeDataString(order.OrderCode)}",
            "customer-order",
            order.Id,
            ct: ct);
    }

    private async Task EnsurePendingOrderStateAsync(CustomerOrder order, CustomerPayment payment, CancellationToken ct)
    {
        if (payment.Status != CustomerPaymentStatus.Pending)
            return;

        if (order.ExpiresAt > DateTimeOffset.UtcNow)
            return;

        await MarkOrderAsUnpaidFinalAsync(
            order,
            payment,
            CustomerPaymentStatus.Expired,
            order.UserId,
            ct,
            shouldCancelProviderOrder: true);
        await _db.SaveChangesAsync(ct);
    }

    private async Task ExpireDueOrdersAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var dueOrderIds = await _db.CustomerOrders
            .Where(x =>
                x.UserId == userId &&
                !x.IsDeleted &&
                x.PaymentStatus == CustomerPaymentStatus.Pending &&
                x.ExpiresAt <= now)
            .Select(x => x.Id)
            .ToListAsync(ct);

        foreach (var orderId in dueOrderIds)
        {
            var order = await _db.CustomerOrders.FirstOrDefaultAsync(x => x.Id == orderId && !x.IsDeleted, ct);
            var payment = order is null
                ? null
                : await _db.CustomerPayments.FirstOrDefaultAsync(x => x.OrderId == order.Id && !x.IsDeleted, ct);

            if (order is null || payment is null || payment.Status != CustomerPaymentStatus.Pending)
                continue;

            await MarkOrderAsUnpaidFinalAsync(
                order,
                payment,
                CustomerPaymentStatus.Expired,
                order.UserId,
                ct,
                shouldCancelProviderOrder: true);
        }

        if (dueOrderIds.Count > 0)
            await _db.SaveChangesAsync(ct);
    }

    private async Task ReleaseOrderResourcesAsync(
        CustomerOrder order,
        Guid actorUserId,
        CancellationToken ct,
        CustomerPaymentStatus finalStatus)
    {
        switch (order.ProductType)
        {
            case CustomerProductType.Bus:
                await ReleaseBusOrderAsync(order, actorUserId, ct, finalStatus);
                break;
            case CustomerProductType.Train:
                await ReleaseTrainOrderAsync(order, actorUserId, ct, finalStatus);
                break;
            case CustomerProductType.Hotel:
                await ReleaseHotelOrderAsync(order, actorUserId, ct, finalStatus);
                break;
            case CustomerProductType.Tour:
                await ReleaseTourOrderAsync(order, actorUserId, ct);
                break;
        }
    }

    private async Task ReleaseHotelOrderAsync(
        CustomerOrder order,
        Guid actorUserId,
        CancellationToken ct,
        CustomerPaymentStatus finalStatus)
    {
        var metadata = DeserializeMetadata<HotelOrderMetadata>(order.MetadataJson);
        if (metadata is null)
            return;

        var hold = await _db.InventoryHolds.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == metadata.InventoryHoldId && !x.IsDeleted, ct);

        if (hold is null || hold.Status != HoldStatus.Held)
            return;

        var stayDates = EachNight(metadata.CheckInDate, metadata.CheckOutDate).ToList();
        var inventories = await _db.RoomTypeInventories.IgnoreQueryFilters()
            .Where(x =>
                x.RoomTypeId == metadata.RoomTypeId &&
                stayDates.Contains(x.Date) &&
                !x.IsDeleted)
            .ToListAsync(ct);

        foreach (var inventory in inventories)
        {
            inventory.HeldUnits = Math.Max(0, inventory.HeldUnits - metadata.RoomCount);
            inventory.UpdatedAt = DateTimeOffset.UtcNow;
            inventory.UpdatedByUserId = actorUserId;
        }

        hold.Status = finalStatus == CustomerPaymentStatus.Expired ? HoldStatus.Expired : HoldStatus.Cancelled;
        hold.UpdatedAt = DateTimeOffset.UtcNow;
        hold.UpdatedByUserId = actorUserId;
    }

    private async Task ReleaseExpiredHotelHoldsAsync(Guid roomTypeId, DateTimeOffset now, CancellationToken ct)
    {
        var expiredHolds = await _db.InventoryHolds.IgnoreQueryFilters()
            .Where(x =>
                x.RoomTypeId == roomTypeId &&
                x.Status == HoldStatus.Held &&
                x.HoldExpiresAt <= now &&
                !x.IsDeleted)
            .ToListAsync(ct);

        if (expiredHolds.Count == 0)
            return;

        foreach (var hold in expiredHolds)
        {
            var stayDates = EachNight(hold.CheckInDate, hold.CheckOutDate).ToList();
            var inventories = await _db.RoomTypeInventories.IgnoreQueryFilters()
                .Where(x =>
                    x.RoomTypeId == hold.RoomTypeId &&
                    stayDates.Contains(x.Date) &&
                    !x.IsDeleted)
                .ToListAsync(ct);

            foreach (var inventory in inventories)
            {
                inventory.HeldUnits = Math.Max(0, inventory.HeldUnits - hold.Units);
                inventory.UpdatedAt = now;
            }

            hold.Status = HoldStatus.Expired;
            hold.UpdatedAt = now;
        }
    }

    private async Task IssueTicketAsync(CustomerOrder order, Guid actorUserId, CancellationToken ct)
    {
        var existing = await _db.CustomerTickets
            .FirstOrDefaultAsync(x => x.OrderId == order.Id && !x.IsDeleted, ct);

        if (existing is not null && existing.Status == CustomerTicketStatus.Issued)
        {
            order.TicketStatus = CustomerTicketStatus.Issued;
            order.Status = CustomerOrderStatus.TicketIssued;
            order.TicketIssuedAt ??= existing.IssuedAt;
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var snapshot = DeserializeSnapshot(order.SnapshotJson);
        var ticketSnapshotJson = SerializeJson(new
        {
            order.OrderCode,
            productType = order.ProductType.ToString(),
            contact = new
            {
                order.ContactFullName,
                order.ContactPhone,
                order.ContactEmail,
            },
            issuedAt = now,
            summary = snapshot,
        });

        if (existing is null)
        {
            existing = new CustomerTicket
            {
                Id = Guid.NewGuid(),
                TenantId = order.TenantId,
                UserId = order.UserId,
                OrderId = order.Id,
                ProductType = order.ProductType,
                Status = CustomerTicketStatus.Issued,
                TicketCode = GenerateCode("TKT"),
                Title = snapshot.Title,
                Subtitle = snapshot.Subtitle,
                SnapshotJson = ticketSnapshotJson,
                IssuedAt = now,
                CreatedAt = now,
                CreatedByUserId = actorUserId,
            };

            _db.CustomerTickets.Add(existing);
        }
        else
        {
            existing.Status = CustomerTicketStatus.Issued;
            existing.Title = snapshot.Title;
            existing.Subtitle = snapshot.Subtitle;
            existing.SnapshotJson = ticketSnapshotJson;
            existing.UpdatedAt = now;
            existing.UpdatedByUserId = actorUserId;
        }

        order.TicketStatus = CustomerTicketStatus.Issued;
        order.Status = CustomerOrderStatus.TicketIssued;
        order.TicketIssuedAt ??= now;
        order.UpdatedAt = now;
        order.UpdatedByUserId = actorUserId;

        await _notificationService.CreateAsync(
            order.UserId,
            order.TenantId,
            "ticket",
            "Vé điện tử đã sẵn sàng",
            $"Đơn hàng {order.OrderCode} đã được phát hành vé điện tử.",
            $"/ticket/success?orderCode={Uri.EscapeDataString(order.OrderCode)}",
            "customer-order",
            order.Id,
            ct: ct);
    }

    private async Task CreateOrderCreatedNotificationAsync(CustomerOrder order, CancellationToken ct)
    {
        await _notificationService.CreateAsync(
            order.UserId,
            order.TenantId,
            "order",
            "Đã tạo đơn hàng chờ thanh toán",
            $"Đơn hàng {order.OrderCode} đã được tạo và đang chờ bạn thanh toán.",
            $"/payment?orderCode={Uri.EscapeDataString(order.OrderCode)}",
            "customer-order",
            order.Id,
            ct: ct);
    }

    private bool IsWebhookAuthorized(string? authorizationHeader, string? secretHeader)
    {
        var configuredSecret = NormalizeOptional(_sePayGatewayService.WebhookSecret);
        if (configuredSecret is null)
            return _hostEnvironment.IsDevelopment();

        var authValue = NormalizeWebhookSecret(authorizationHeader);
        var secretValue = NormalizeWebhookSecret(secretHeader);

        return string.Equals(authValue, configuredSecret, StringComparison.Ordinal) ||
               string.Equals(secretValue, configuredSecret, StringComparison.Ordinal);
    }

    private static string? NormalizeWebhookSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (normalized.StartsWith("Apikey ", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["Apikey ".Length..].Trim();
        if (normalized.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            normalized = normalized["Bearer ".Length..].Trim();
        return normalized;
    }

    private static SePayWebhookPayload? ParseWebhookPayload(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
            return null;

        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;

            if (root.TryGetProperty("order", out var orderElement) &&
                orderElement.ValueKind == JsonValueKind.Object)
            {
                var hasTransaction = root.TryGetProperty("transaction", out var transactionElement) &&
                                     transactionElement.ValueKind == JsonValueKind.Object;
                var notificationType = GetWebhookString(root, "notification_type");
                var orderStatus = GetWebhookString(orderElement, "order_status");
                var transactionStatus = hasTransaction
                    ? GetWebhookString(transactionElement, "transaction_status")
                    : null;

                return new SePayWebhookPayload
                {
                    ProviderInvoiceNumber = GetWebhookString(orderElement, "order_invoice_number"),
                    ProviderOrderId = GetWebhookString(orderElement, "order_id"),
                    RawStatus = ResolveGatewayNotificationStatus(notificationType, orderStatus, transactionStatus),
                    Amount = hasTransaction
                        ? GetWebhookDecimal(transactionElement, "transaction_amount")
                        : GetWebhookDecimal(orderElement, "order_amount"),
                    ShouldProcess = true,
                };
            }

            var transferType = GetWebhookString(root, "transferType", "transfer_type");
            var isIncomingTransfer = string.IsNullOrWhiteSpace(transferType) ||
                                     string.Equals(transferType, "in", StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(transferType, "credit", StringComparison.OrdinalIgnoreCase);

            return new SePayWebhookPayload
            {
                ProviderInvoiceNumber = GetWebhookString(root, "order_invoice_number", "invoice_number", "invoiceNumber", "orderCode", "code", "payment_code"),
                ProviderOrderId = GetWebhookString(root, "order_id", "orderId"),
                RawStatus = ResolveGatewayNotificationStatus(
                    null,
                    GetWebhookString(root, "order_status", "status", "payment_status"),
                    null,
                    isIncomingTransfer),
                Amount = GetWebhookDecimal(root, "order_amount", "amount", "paid_amount", "transferAmount", "transfer_amount"),
                ShouldProcess = isIncomingTransfer,
            };
        }
        catch
        {
            return null;
        }
    }

    private static string ResolveGatewayNotificationStatus(
        string? notificationType,
        string? orderStatus,
        string? transactionStatus,
        bool incomingTransferByDefault = true)
    {
        if (!string.IsNullOrWhiteSpace(orderStatus))
            return orderStatus!;

        if (!string.IsNullOrWhiteSpace(transactionStatus))
        {
            if (string.Equals(transactionStatus, "APPROVED", StringComparison.OrdinalIgnoreCase))
                return "CAPTURED";
            if (string.Equals(transactionStatus, "FAILED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(transactionStatus, "DECLINED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(transactionStatus, "REJECTED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(transactionStatus, "ERROR", StringComparison.OrdinalIgnoreCase))
            {
                return "FAILED";
            }
            if (string.Equals(transactionStatus, "VOID", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(transactionStatus, "VOIDED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(transactionStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(transactionStatus, "CANCELED", StringComparison.OrdinalIgnoreCase))
            {
                return "CANCELLED";
            }

            return transactionStatus!;
        }

        if (string.Equals(notificationType, "ORDER_PAID", StringComparison.OrdinalIgnoreCase))
            return "CAPTURED";
        if (string.Equals(notificationType, "TRANSACTION_VOID", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(notificationType, "ORDER_CANCELLED", StringComparison.OrdinalIgnoreCase))
            return "CANCELLED";
        if (string.Equals(notificationType, "ORDER_FAILED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(notificationType, "PAYMENT_FAILED", StringComparison.OrdinalIgnoreCase))
        {
            return "FAILED";
        }
        if (string.Equals(notificationType, "ORDER_EXPIRED", StringComparison.OrdinalIgnoreCase))
            return "EXPIRED";

        return incomingTransferByDefault ? "CAPTURED" : "PENDING";
    }

    private async Task<bool> IsGatewayWebhookAlreadyAppliedAsync(SePayWebhookPayload payload, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(payload.ProviderInvoiceNumber))
            return false;

        var payment = await _db.CustomerPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ProviderInvoiceNumber == payload.ProviderInvoiceNumber &&
                !x.IsDeleted, ct);

        if (payment is null)
            return true;

        var order = await _db.CustomerOrders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == payment.OrderId && !x.IsDeleted, ct);

        if (order is null)
            return true;

        return IsPaidTerminal(order, payment) || IsUnpaidTerminal(order, payment);
    }

    private async Task TryCancelGatewayOrderAsync(CustomerPayment payment, CancellationToken ct)
    {
        if (!_sePayGatewayService.IsConfigured ||
            payment.Provider != CustomerPaymentProvider.SePay ||
            string.IsNullOrWhiteSpace(payment.ProviderInvoiceNumber) ||
            (string.IsNullOrWhiteSpace(payment.RequestPayloadJson) && string.IsNullOrWhiteSpace(payment.ProviderOrderId)))
        {
            return;
        }

        try
        {
            var result = await _sePayGatewayService.CancelOrderByInvoiceAsync(payment.ProviderInvoiceNumber, ct);
            if (!string.IsNullOrWhiteSpace(result.RawPayloadJson))
                payment.ProviderResponseJson = result.RawPayloadJson;
        }
        catch
        {
            // Local order cancellation/expiry should still complete even if the provider cancel call fails.
        }
    }

    private static bool IsPaidTerminal(CustomerOrder order, CustomerPayment payment)
    {
        return payment.Status == CustomerPaymentStatus.Paid ||
               order.PaymentStatus == CustomerPaymentStatus.Paid ||
               order.Status is CustomerOrderStatus.Paid or CustomerOrderStatus.TicketIssued or CustomerOrderStatus.Completed ||
               order.TicketStatus == CustomerTicketStatus.Issued;
    }

    private static bool IsUnpaidTerminal(CustomerOrder order, CustomerPayment payment)
    {
        return payment.Status is CustomerPaymentStatus.Cancelled or CustomerPaymentStatus.Expired or CustomerPaymentStatus.Failed ||
               order.PaymentStatus is CustomerPaymentStatus.Cancelled or CustomerPaymentStatus.Expired or CustomerPaymentStatus.Failed ||
               order.Status is CustomerOrderStatus.Cancelled or CustomerOrderStatus.Expired or CustomerOrderStatus.Failed;
    }

    private static string? GetWebhookString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var element))
                continue;

            return element.ValueKind == JsonValueKind.String ? element.GetString() : element.GetRawText();
        }

        return null;
    }

    private static decimal? GetWebhookDecimal(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var element))
                continue;

            if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number))
                return number;

            if (element.ValueKind == JsonValueKind.String &&
                decimal.TryParse(element.GetString(), out var parsed))
                return parsed;
        }

        return null;
    }
}

internal sealed class SePayWebhookPayload
{
    public string? ProviderInvoiceNumber { get; set; }
    public string? ProviderOrderId { get; set; }
    public string RawStatus { get; set; } = "PENDING";
    public decimal? Amount { get; set; }
    public bool ShouldProcess { get; set; } = true;
}
