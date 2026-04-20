using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Commerce;

namespace TicketBooking.Api.Services.Commerce;

public sealed partial class CustomerOrderService
{
    public async Task<List<CustomerOrderTimelineEventDto>> GetOrderTimelineAsync(
        string orderCode,
        Guid userId,
        CancellationToken ct = default)
    {
        var (order, payment) = await LoadOrderGraphForUserAsync(orderCode, userId, ct);
        await EnsurePendingOrderStateAsync(order, payment, ct);

        var ticket = await _db.CustomerTickets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == order.Id && !x.IsDeleted, ct);

        var refunds = await _db.CustomerRefundRequests
            .AsNoTracking()
            .Where(x => x.OrderId == order.Id && !x.IsDeleted)
            .OrderBy(x => x.RequestedAt)
            .ToListAsync(ct);

        var supportTickets = await _db.CustomerSupportTickets
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.OrderId == order.Id && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(ct);

        var events = new List<CustomerOrderTimelineEventDto>
        {
            new()
            {
                Key = "order-created",
                Title = "Đã tạo đơn hàng",
                Description = $"Đơn {order.OrderCode} đã được tạo trên nền tảng và chờ xử lý thanh toán.",
                OccurredAt = order.CreatedAt,
                Tone = "info",
                ActionUrl = $"/payment?orderCode={Uri.EscapeDataString(order.OrderCode)}",
            },
        };

        if (payment.Status == CustomerPaymentStatus.Pending)
        {
            events.Add(new CustomerOrderTimelineEventDto
            {
                Key = "payment-pending",
                Title = "Đang chờ thanh toán",
                Description = $"Phiên thanh toán hiện còn hiệu lực đến {order.ExpiresAt:dd/MM/yyyy HH:mm}.",
                OccurredAt = payment.LastSyncedAt ?? order.CreatedAt,
                Tone = "warning",
                IsCurrent = true,
                ActionUrl = $"/payment?orderCode={Uri.EscapeDataString(order.OrderCode)}",
            });
        }

        if (order.PaidAt.HasValue || payment.PaidAt.HasValue || payment.Status == CustomerPaymentStatus.Paid)
        {
            events.Add(new CustomerOrderTimelineEventDto
            {
                Key = "payment-paid",
                Title = "Thanh toán thành công",
                Description = "Platform đã xác nhận dòng tiền hợp lệ và bắt đầu phát hành dịch vụ từ backend.",
                OccurredAt = order.PaidAt ?? payment.PaidAt ?? order.UpdatedAt ?? order.CreatedAt,
                Tone = "success",
                ActionUrl = $"/payment?orderCode={Uri.EscapeDataString(order.OrderCode)}",
            });
        }

        if (order.Status == CustomerOrderStatus.Cancelled || payment.Status == CustomerPaymentStatus.Cancelled)
        {
            events.Add(new CustomerOrderTimelineEventDto
            {
                Key = "payment-cancelled",
                Title = "Đơn hàng đã hủy",
                Description = order.FailureReason ?? "Phiên thanh toán đã bị đóng trước khi hệ thống ghi nhận tiền vào.",
                OccurredAt = order.CancelledAt ?? payment.CancelledAt ?? order.UpdatedAt ?? order.CreatedAt,
                Tone = "danger",
            });
        }
        else if (order.Status == CustomerOrderStatus.Expired || payment.Status == CustomerPaymentStatus.Expired)
        {
            events.Add(new CustomerOrderTimelineEventDto
            {
                Key = "payment-expired",
                Title = "Phiên thanh toán hết hạn",
                Description = order.FailureReason ?? "Hệ thống đã đóng phiên thanh toán vì quá thời gian chờ.",
                OccurredAt = order.UpdatedAt ?? payment.UpdatedAt ?? order.ExpiresAt,
                Tone = "danger",
            });
        }
        else if (order.Status == CustomerOrderStatus.Failed || payment.Status == CustomerPaymentStatus.Failed)
        {
            events.Add(new CustomerOrderTimelineEventDto
            {
                Key = "payment-failed",
                Title = "Thanh toán chưa hoàn tất",
                Description = order.FailureReason ?? payment.FailureReason ?? "Gateway chưa xác nhận giao dịch thành công.",
                OccurredAt = payment.FailedAt ?? order.UpdatedAt ?? order.CreatedAt,
                Tone = "danger",
                ActionUrl = $"/payment?orderCode={Uri.EscapeDataString(order.OrderCode)}",
            });
        }

        if (ticket is not null && (order.TicketIssuedAt.HasValue || ticket.Status == CustomerTicketStatus.Issued))
        {
            events.Add(new CustomerOrderTimelineEventDto
            {
                Key = "ticket-issued",
                Title = "Vé / voucher đã phát hành",
                Description = "Vé được sinh từ backend sau khi payment hợp lệ, không phụ thuộc redirect frontend.",
                OccurredAt = order.TicketIssuedAt ?? ticket.IssuedAt,
                Tone = "success",
                ActionUrl = $"/ticket/success?orderCode={Uri.EscapeDataString(order.OrderCode)}",
            });
        }

        foreach (var refund in refunds)
        {
            events.Add(new CustomerOrderTimelineEventDto
            {
                Key = $"refund-requested-{refund.Id:N}",
                Title = "Đã gửi yêu cầu hoàn tiền",
                Description = $"Hệ thống ghi nhận yêu cầu {refund.RefundCode} với số tiền {FormatMoney(refund.RequestedAmount, refund.CurrencyCode)}.",
                OccurredAt = refund.RequestedAt,
                Tone = "warning",
                ActionUrl = $"/my-account/bookings/{Uri.EscapeDataString(order.OrderCode)}",
            });

            if (refund.Status == CustomerRefundStatus.Rejected && refund.ReviewedAt.HasValue)
            {
                events.Add(new CustomerOrderTimelineEventDto
                {
                    Key = $"refund-rejected-{refund.Id:N}",
                    Title = "Yêu cầu hoàn tiền bị từ chối",
                    Description = refund.ReviewNote ?? "Platform đã từ chối yêu cầu hoàn tiền này.",
                    OccurredAt = refund.ReviewedAt.Value,
                    Tone = "danger",
                    ActionUrl = $"/my-account/bookings/{Uri.EscapeDataString(order.OrderCode)}",
                });
                continue;
            }

            if (refund.ReviewedAt.HasValue && refund.Status is CustomerRefundStatus.Approved or CustomerRefundStatus.Processing or CustomerRefundStatus.RefundedPartial or CustomerRefundStatus.RefundedFull)
            {
                events.Add(new CustomerOrderTimelineEventDto
                {
                    Key = $"refund-approved-{refund.Id:N}",
                    Title = "Platform đã duyệt hoàn tiền",
                    Description = $"Số tiền duyệt hoàn hiện tại là {FormatMoney(refund.ApprovedAmount ?? refund.RequestedAmount, refund.CurrencyCode)}.",
                    OccurredAt = refund.ReviewedAt.Value,
                    Tone = "info",
                    ActionUrl = $"/my-account/bookings/{Uri.EscapeDataString(order.OrderCode)}",
                });
            }

            if (refund.CompletedAt.HasValue && refund.Status is CustomerRefundStatus.RefundedPartial or CustomerRefundStatus.RefundedFull)
            {
                events.Add(new CustomerOrderTimelineEventDto
                {
                    Key = $"refund-completed-{refund.Id:N}",
                    Title = refund.Status == CustomerRefundStatus.RefundedFull
                        ? "Đã hoàn tiền toàn phần"
                        : "Đã hoàn tiền một phần",
                    Description = $"Hệ thống đã cập nhật hoàn {FormatMoney(refund.RefundedAmount ?? refund.ApprovedAmount ?? refund.RequestedAmount, refund.CurrencyCode)} cho đơn này.",
                    OccurredAt = refund.CompletedAt.Value,
                    Tone = "success",
                    ActionUrl = $"/my-account/bookings/{Uri.EscapeDataString(order.OrderCode)}",
                });
            }
        }

        foreach (var support in supportTickets)
        {
            if (support.FirstResponseAt.HasValue)
            {
                events.Add(new CustomerOrderTimelineEventDto
                {
                    Key = $"support-response-{support.Id:N}",
                    Title = "Support đã phản hồi",
                    Description = $"Ticket {support.TicketCode} có cập nhật mới từ đội ngũ hỗ trợ.",
                    OccurredAt = support.LastActivityAt ?? support.FirstResponseAt.Value,
                    Tone = support.Status == CustomerSupportTicketStatus.Resolved ? "success" : "info",
                    ActionUrl = "/support",
                });
            }
        }

        if (order.CompletedAt.HasValue)
        {
            events.Add(new CustomerOrderTimelineEventDto
            {
                Key = "order-completed",
                Title = "Đơn hàng hoàn tất",
                Description = "Dịch vụ đã đi đến mốc hoàn tất trên hệ thống.",
                OccurredAt = order.CompletedAt.Value,
                Tone = "success",
            });
        }

        var orderedEvents = events
            .OrderBy(x => x.OccurredAt)
            .ThenBy(x => x.Key, StringComparer.Ordinal)
            .ToList();

        if (orderedEvents.Count > 0 && !orderedEvents.Any(x => x.IsCurrent))
        {
            orderedEvents[^1].IsCurrent = orderedEvents[^1].Tone is "warning" or "info";
        }

        return orderedEvents;
    }

    public async Task<CustomerRefundEstimateDto> GetRefundEstimateAsync(
        string orderCode,
        Guid userId,
        decimal? requestedAmount = null,
        CancellationToken ct = default)
    {
        var (order, payment) = await LoadOrderGraphForUserAsync(orderCode, userId, ct);
        await EnsurePendingOrderStateAsync(order, payment, ct);

        var remainingRefundable = Math.Max(0m, order.PayableAmount - order.RefundedAmount);
        var eligibleAction = payment.Status == CustomerPaymentStatus.Pending && order.Status == CustomerOrderStatus.PendingPayment
            ? "cancel"
            : IsRefundEligible(order)
                ? "refund"
                : "none";

        var suggestedAmount = eligibleAction == "refund"
            ? ClampMoney(requestedAmount ?? remainingRefundable, 0m, remainingRefundable)
            : 0m;

        var grossBasis = order.GrossAmount > 0 ? order.GrossAmount : order.PayableAmount;
        var commissionRatio = grossBasis > 0
            ? order.PlatformCommissionAmount / grossBasis
            : _options.DefaultCommissionPercent / 100m;
        var estimatedCommissionReversal = eligibleAction == "refund"
            ? RoundCurrencyAmount(suggestedAmount * commissionRatio, order.CurrencyCode)
            : 0m;
        var estimatedTenantAdjustment = eligibleAction == "refund"
            ? Math.Max(0m, suggestedAmount - estimatedCommissionReversal)
            : 0m;

        var dto = new CustomerRefundEstimateDto
        {
            EligibleAction = eligibleAction,
            SettlementStatus = order.SettlementStatus,
            CurrencyCode = order.CurrencyCode,
            GrossAmount = order.GrossAmount,
            PaidAmount = payment.PaidAmount > 0 ? payment.PaidAmount : order.PayableAmount,
            RefundedAmount = order.RefundedAmount,
            RemainingRefundableAmount = remainingRefundable,
            SuggestedAmount = suggestedAmount,
            EstimatedRefundAmount = eligibleAction == "refund" ? suggestedAmount : 0m,
            EstimatedCommissionReversalAmount = estimatedCommissionReversal,
            EstimatedTenantAdjustmentAmount = estimatedTenantAdjustment,
            SupportsPartialRefund = true,
            RequiresAdminReview = eligibleAction == "refund",
            TimingNote = BuildRefundTimingNote(eligibleAction, order),
            SettlementImpact = BuildSettlementImpact(order.SettlementStatus),
            StatusNote = BuildRefundStatusNote(eligibleAction, order, payment, remainingRefundable),
            RuleSummary = BuildRefundRules(eligibleAction, order),
            WarningMessages = BuildRefundWarnings(eligibleAction, order, remainingRefundable, suggestedAmount),
        };

        return dto;
    }

    private static bool IsRefundEligible(CustomerOrder order)
    {
        return (order.PaymentStatus is CustomerPaymentStatus.Paid or CustomerPaymentStatus.RefundedPartial)
            && (order.RefundStatus is CustomerRefundStatus.None or CustomerRefundStatus.Rejected or CustomerRefundStatus.Cancelled);
    }

    private static decimal ClampMoney(decimal value, decimal min, decimal max)
    {
        if (value < min)
            return min;

        return value > max ? max : value;
    }

    private static decimal RoundCurrencyAmount(decimal value, string currencyCode)
    {
        var decimals = string.Equals(currencyCode, "VND", StringComparison.OrdinalIgnoreCase) ? 0 : 2;
        return decimal.Round(value, decimals, MidpointRounding.AwayFromZero);
    }

    private static string BuildRefundTimingNote(string eligibleAction, CustomerOrder order)
    {
        if (eligibleAction == "cancel")
            return "Đơn chưa có tiền vào nên hệ thống sẽ hủy phiên thanh toán thay vì chạy luồng refund.";

        if (eligibleAction == "refund")
            return "Refund được admin/platform duyệt và ghi nhận hoàn thủ công rồi cập nhật lại hệ thống.";

        return order.RefundStatus switch
        {
            CustomerRefundStatus.Requested or CustomerRefundStatus.UnderReview or CustomerRefundStatus.Approved or CustomerRefundStatus.Processing
                => "Đơn đang có yêu cầu refund mở, vui lòng theo dõi tiến trình ở timeline.",
            _ => "Đơn hiện chưa đủ điều kiện để tạo yêu cầu refund mới.",
        };
    }

    private static string BuildSettlementImpact(CustomerSettlementStatus settlementStatus)
    {
        return settlementStatus switch
        {
            CustomerSettlementStatus.Settled or CustomerSettlementStatus.Adjusted
                => "Khoản refund này sẽ được cấn trừ vào batch settlement tháng sau của tenant.",
            CustomerSettlementStatus.InSettlement
                => "Đơn đang nằm trong batch settlement, hệ thống sẽ chuyển trạng thái sang adjusted khi hoàn tiền.",
            CustomerSettlementStatus.OnHold
                => "Đơn đang bị giữ lại ở settlement, refund sẽ tiếp tục được admin theo dõi trong cùng luồng đối soát.",
            _ => "Nếu refund được duyệt trước khi settlement, hệ thống sẽ trừ trực tiếp vào tenant net của đơn này.",
        };
    }

    private static string BuildRefundStatusNote(
        string eligibleAction,
        CustomerOrder order,
        CustomerPayment payment,
        decimal remainingRefundable)
    {
        if (eligibleAction == "cancel")
            return "Phiên thanh toán vẫn đang pending nên khách chưa bị ghi nhận thanh toán thành công.";

        if (eligibleAction == "refund")
            return remainingRefundable > 0
                ? $"Đơn đã thanh toán và còn {FormatMoney(remainingRefundable, order.CurrencyCode)} có thể đưa vào luồng refund."
                : "Đơn đã dùng hết hạn mức refundable hiện tại.";

        return payment.Status switch
        {
            CustomerPaymentStatus.RefundedFull => "Đơn đã hoàn toàn bộ tiền, không thể tạo refund mới.",
            CustomerPaymentStatus.Cancelled or CustomerPaymentStatus.Expired or CustomerPaymentStatus.Failed => "Đơn chưa có payment hợp lệ để đưa vào luồng refund.",
            _ => "Hệ thống đang chờ điều kiện phù hợp để cho phép refund.",
        };
    }

    private static List<string> BuildRefundRules(string eligibleAction, CustomerOrder order)
    {
        var rules = new List<string>();

        if (eligibleAction == "cancel")
        {
            rules.Add("Đơn pending có thể hủy trực tiếp mà không tạo refund.");
            rules.Add("Ticket/voucher chỉ phát hành sau khi payment hợp lệ nên đơn pending chưa có gì để hoàn.");
            return rules;
        }

        rules.Add("Platform giữ commission 10% trên gross và hoàn commission theo đúng tỷ lệ refund.");
        rules.Add("Hệ thống hỗ trợ partial refund, không bắt buộc phải hoàn toàn phần.");
        rules.Add("Refund là nghiệp vụ của platform/admin, tenant không tự duyệt ngoài hệ thống.");

        if (order.SettlementStatus is CustomerSettlementStatus.Settled or CustomerSettlementStatus.Adjusted)
            rules.Add("Nếu tenant đã settlement, khoản điều chỉnh sẽ được trừ vào batch tháng sau.");
        else
            rules.Add("Nếu tenant chưa settlement, khoản refund sẽ được trừ trực tiếp vào tenant net.");

        return rules;
    }

    private static List<string> BuildRefundWarnings(
        string eligibleAction,
        CustomerOrder order,
        decimal remainingRefundable,
        decimal suggestedAmount)
    {
        var warnings = new List<string>();

        if (eligibleAction == "cancel")
        {
            warnings.Add("Hủy đơn sẽ đóng phiên thanh toán hiện tại và giải phóng giữ chỗ nếu có.");
            return warnings;
        }

        if (eligibleAction != "refund")
        {
            warnings.Add("Đơn hiện chưa mở thêm refund mới được.");
            return warnings;
        }

        if (remainingRefundable <= 0)
            warnings.Add("Đơn không còn số dư refundable.");
        else if (suggestedAmount < remainingRefundable)
            warnings.Add("Bạn đang gửi partial refund; phần còn lại vẫn giữ trên đơn cho đến khi có yêu cầu khác.");

        if (order.RefundStatus is CustomerRefundStatus.Requested or CustomerRefundStatus.UnderReview or CustomerRefundStatus.Approved or CustomerRefundStatus.Processing)
            warnings.Add("Đơn đang có luồng refund mở, nên tránh gửi trùng nếu không thật sự cần.");

        warnings.Add("Refund thực tế được cập nhật thủ công sau khi platform hoàn tiền cho khách.");
        return warnings;
    }

    private static string FormatMoney(decimal amount, string currencyCode)
    {
        return string.Equals(currencyCode, "VND", StringComparison.OrdinalIgnoreCase)
            ? $"{amount:N0} {currencyCode}"
            : $"{amount:N2} {currencyCode}";
    }
}
