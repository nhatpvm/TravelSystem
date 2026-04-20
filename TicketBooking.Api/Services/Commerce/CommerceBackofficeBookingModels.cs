using TicketBooking.Domain.Commerce;

namespace TicketBooking.Api.Services.Commerce;

public sealed class AdminCommerceBookingListResponse
{
    public AdminCommerceBookingSummaryDto Summary { get; set; } = new();
    public List<AdminCommerceBookingItemDto> Items { get; set; } = new();
}

public sealed class AdminCommerceBookingSummaryDto
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int PaidCount { get; set; }
    public int CancelledCount { get; set; }
    public int RefundAttentionCount { get; set; }
    public decimal TotalGrossAmount { get; set; }
    public decimal TotalCommissionAmount { get; set; }
    public decimal TotalTenantNetAmount { get; set; }
    public decimal TotalRefundedAmount { get; set; }
}

public sealed class AdminCommerceBookingItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string OrderCode { get; set; } = "";
    public string CurrencyCode { get; set; } = "VND";
    public string CustomerName { get; set; } = "";
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? TenantName { get; set; }
    public string ServiceTitle { get; set; } = "";
    public string? ServiceSubtitle { get; set; }
    public CustomerProductType ProductType { get; set; }
    public CustomerOrderStatus Status { get; set; }
    public CustomerPaymentStatus PaymentStatus { get; set; }
    public CustomerTicketStatus TicketStatus { get; set; }
    public CustomerRefundStatus RefundStatus { get; set; }
    public CustomerSettlementStatus SettlementStatus { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal PlatformCommissionAmount { get; set; }
    public decimal TenantNetAmount { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? TicketIssuedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? FailureReason { get; set; }
    public string? PaymentCode { get; set; }
    public string? ProviderInvoiceNumber { get; set; }
    public string? TicketCode { get; set; }
    public string? LatestRefundCode { get; set; }
    public decimal? LatestRefundAmount { get; set; }
    public int SupportTicketCount { get; set; }
    public int OpenSupportTicketCount { get; set; }
}
