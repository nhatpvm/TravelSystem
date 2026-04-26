using TicketBooking.Domain.Commerce;

namespace TicketBooking.Api.Services.Commerce;

public sealed class AdminCommercePaymentListResponse
{
    public AdminCommercePaymentSummaryDto Summary { get; set; } = new();
    public List<AdminCommercePaymentItemDto> Items { get; set; } = new();
}

public sealed class AdminCommercePaymentSummaryDto
{
    public int TotalCount { get; set; }
    public int PaidCount { get; set; }
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public int RefundedCount { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class AdminCommercePaymentItemDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid TenantId { get; set; }
    public string? TenantName { get; set; }
    public string PaymentCode { get; set; } = "";
    public string OrderCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string? ServiceTitle { get; set; }
    public CustomerProductType ProductType { get; set; }
    public CustomerPaymentProvider Provider { get; set; }
    public CustomerPaymentMethod Method { get; set; }
    public CustomerPaymentStatus Status { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public string ProviderInvoiceNumber { get; set; } = "";
    public string? ProviderOrderId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? WebhookReceivedAt { get; set; }
}

public sealed class AdminCommerceRefundListResponse
{
    public AdminCommerceRefundSummaryDto Summary { get; set; } = new();
    public List<AdminCommerceRefundItemDto> Items { get; set; } = new();
}

public sealed class AdminCommerceRefundSummaryDto
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int CompletedCount { get; set; }
    public int RejectedCount { get; set; }
    public decimal PendingAmount { get; set; }
}

public sealed class AdminCommerceRefundItemDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid TenantId { get; set; }
    public string? TenantName { get; set; }
    public string RefundCode { get; set; } = "";
    public string OrderCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string? ServiceTitle { get; set; }
    public CustomerProductType ProductType { get; set; }
    public CustomerRefundStatus Status { get; set; }
    public CustomerSettlementStatus SettlementStatus { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal PayableAmount { get; set; }
    public decimal AlreadyRefundedAmount { get; set; }
    public decimal RemainingRefundableAmount { get; set; }
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? RefundedAmount { get; set; }
    public string ReasonCode { get; set; } = "";
    public string? ReasonText { get; set; }
    public string? InternalNote { get; set; }
    public string? RefundReference { get; set; }
    public string SettlementImpactNote { get; set; } = "";
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class ReviewRefundRequest
{
    public decimal? ApprovedAmount { get; set; }
    public string? InternalNote { get; set; }
    public string? ReviewNote { get; set; }
}

public sealed class CompleteRefundRequest
{
    public decimal? RefundedAmount { get; set; }
    public string? RefundReference { get; set; }
    public string? InternalNote { get; set; }
    public string? ReviewNote { get; set; }
}

public sealed class AdminCommerceSupportListResponse
{
    public AdminCommerceSupportSummaryDto Summary { get; set; } = new();
    public List<AdminCommerceSupportTicketDto> Items { get; set; } = new();
}

public sealed class AdminCommerceSupportSummaryDto
{
    public int OpenCount { get; set; }
    public int ProcessingCount { get; set; }
    public int ResolvedCount { get; set; }
    public int HighPriorityCount { get; set; }
}

public sealed class AdminCommerceSupportTicketDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string TicketCode { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Category { get; set; } = "";
    public string Priority { get; set; } = "medium";
    public CustomerSupportTicketStatus Status { get; set; }
    public string CustomerName { get; set; } = "";
    public string? OrderCode { get; set; }
    public string Content { get; set; } = "";
    public string? ResolutionNote { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public bool HasUnreadStaffReply { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? FirstResponseAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }
    public List<AdminCommerceSupportMessageDto> Messages { get; set; } = new();
}

public sealed class AdminCommerceSupportMessageDto
{
    public string From { get; set; } = "";
    public string Text { get; set; } = "";
    public DateTimeOffset? At { get; set; }
}

public sealed class ReplySupportTicketRequest
{
    public string Message { get; set; } = "";
    public bool MarkResolved { get; set; }
}

public sealed class AdminSettlementDashboardDto
{
    public AdminSettlementSummaryDto Summary { get; set; } = new();
    public List<AdminSettlementBatchDto> Batches { get; set; } = new();
    public List<AdminSettlementPayoutDto> Payouts { get; set; } = new();
}

public sealed class AdminSettlementSummaryDto
{
    public decimal TotalBatchAmount { get; set; }
    public decimal ProcessingAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int TenantCount { get; set; }
}

public sealed class AdminSettlementBatchDto
{
    public Guid Id { get; set; }
    public string BatchCode { get; set; } = "";
    public string PeriodType { get; set; } = "month";
    public string PeriodLabel { get; set; } = "";
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public CustomerSettlementBatchStatus Status { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal TotalGrossAmount { get; set; }
    public decimal TotalCommissionAmount { get; set; }
    public decimal TotalCommissionAdjustmentAmount { get; set; }
    public decimal TotalTenantNetAmount { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public decimal TotalNetPayoutAmount { get; set; }
    public int TenantCount { get; set; }
    public int LineCount { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}

public sealed class AdminSettlementPayoutDto
{
    public Guid BatchId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = "";
    public string CurrencyCode { get; set; } = "VND";
    public decimal Amount { get; set; }
    public int LineCount { get; set; }
    public CustomerSettlementStatus Status { get; set; }
    public string? BankName { get; set; }
    public string? AccountNumberMasked { get; set; }
    public string? AccountHolder { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}

public sealed class GenerateSettlementBatchRequest
{
    public string? PeriodType { get; set; }
    public int? Year { get; set; }
    public int? Month { get; set; }
    public int? Quarter { get; set; }
    public int? Day { get; set; }
    public string? Notes { get; set; }
}

public sealed class MarkSettlementBatchPaidRequest
{
    public string? Notes { get; set; }
    public string? BankTransactionCode { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
}

public sealed class TenantFinanceDashboardDto
{
    public TenantFinanceSummaryDto Summary { get; set; } = new();
    public TenantPayoutAccountDto? PayoutAccount { get; set; }
    public List<TenantFinanceMonthlyPointDto> MonthlySeries { get; set; } = new();
    public List<TenantFinanceTransactionDto> Transactions { get; set; } = new();
}

public sealed class TenantFinanceSummaryDto
{
    public decimal CurrentMonthGrossAmount { get; set; }
    public decimal CurrentMonthNetAmount { get; set; }
    public decimal PendingSettlementAmount { get; set; }
    public decimal AdjustedAmount { get; set; }
    public decimal SettledAmount { get; set; }
    public DateOnly NextSettlementDate { get; set; }
}

public sealed class TenantFinanceMonthlyPointDto
{
    public string Label { get; set; } = "";
    public decimal GrossAmount { get; set; }
    public decimal NetAmount { get; set; }
}

public sealed class TenantFinanceTransactionDto
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string CurrencyCode { get; set; } = "VND";
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public CustomerSettlementStatus SettlementStatus { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string? OrderCode { get; set; }
    public string? BatchCode { get; set; }
}

public sealed class TenantPayoutAccountDto
{
    public Guid Id { get; set; }
    public string BankName { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountHolder { get; set; } = "";
    public string? BankBranch { get; set; }
    public string? Note { get; set; }
    public bool IsDefault { get; set; }
    public bool IsVerified { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class UpsertTenantPayoutAccountRequest
{
    public string BankName { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountHolder { get; set; } = "";
    public string? BankBranch { get; set; }
    public string? Note { get; set; }
}
