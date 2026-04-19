using System;

namespace TicketBooking.Domain.Commerce;

public enum CustomerProductType
{
    Bus = 1,
    Train = 2,
    Flight = 3,
    Hotel = 4,
    Tour = 5
}

public enum CustomerOrderStatus
{
    PendingPayment = 1,
    Paid = 2,
    TicketIssued = 3,
    Completed = 4,
    Cancelled = 5,
    Expired = 6,
    Failed = 7,
    RefundRequested = 8,
    RefundedPartial = 9,
    RefundedFull = 10
}

public enum CustomerPaymentStatus
{
    Pending = 1,
    Paid = 2,
    Cancelled = 3,
    Expired = 4,
    Failed = 5,
    RefundedPartial = 6,
    RefundedFull = 7
}

public enum CustomerTicketStatus
{
    Pending = 1,
    Issued = 2,
    Cancelled = 3,
    Refunded = 4
}

public enum CustomerRefundStatus
{
    None = 0,
    Requested = 1,
    UnderReview = 2,
    Approved = 3,
    Rejected = 4,
    Processing = 5,
    RefundedPartial = 6,
    RefundedFull = 7,
    Cancelled = 8
}

public enum CustomerPaymentProvider
{
    SePay = 1
}

public enum CustomerPaymentMethod
{
    BankTransfer = 1
}

public enum CustomerPassengerType
{
    Adult = 1,
    Child = 2,
    Infant = 3
}

public enum CustomerNotificationStatus
{
    Unread = 1,
    Read = 2
}

public enum CustomerVatInvoiceStatus
{
    Requested = 1,
    Issued = 2,
    Rejected = 3
}

public enum CustomerSupportTicketStatus
{
    Open = 1,
    Processing = 2,
    Resolved = 3,
    Closed = 4
}

public enum CustomerSettlementStatus
{
    Unsettled = 1,
    InSettlement = 2,
    Settled = 3,
    Adjusted = 4,
    OnHold = 5
}

public enum CustomerSettlementBatchStatus
{
    Draft = 1,
    Processing = 2,
    Completed = 3,
    Cancelled = 4
}

public sealed class CustomerOrder
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    public CustomerProductType ProductType { get; set; }

    public Guid? SourceEntityId { get; set; }
    public Guid? SourceReservationId { get; set; }
    public Guid? SourceBookingId { get; set; }

    public string OrderCode { get; set; } = "";
    public string CurrencyCode { get; set; } = "VND";

    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ServiceFeeAmount { get; set; }
    public decimal PlatformCommissionAmount { get; set; }
    public decimal TenantNetAmount { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal RefundedAmount { get; set; }

    public CustomerOrderStatus Status { get; set; } = CustomerOrderStatus.PendingPayment;
    public CustomerPaymentStatus PaymentStatus { get; set; } = CustomerPaymentStatus.Pending;
    public CustomerTicketStatus TicketStatus { get; set; } = CustomerTicketStatus.Pending;
    public CustomerRefundStatus RefundStatus { get; set; } = CustomerRefundStatus.None;
    public CustomerSettlementStatus SettlementStatus { get; set; } = CustomerSettlementStatus.Unsettled;

    public string ContactFullName { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string ContactEmail { get; set; } = "";

    public bool VatInvoiceRequested { get; set; }
    public string? CustomerNote { get; set; }
    public string SnapshotJson { get; set; } = "{}";
    public string? MetadataJson { get; set; }
    public string? FailureReason { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? TicketIssuedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? SettledAt { get; set; }
    public Guid? SettlementBatchId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerPayment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }

    public CustomerPaymentProvider Provider { get; set; } = CustomerPaymentProvider.SePay;
    public CustomerPaymentMethod Method { get; set; } = CustomerPaymentMethod.BankTransfer;
    public CustomerPaymentStatus Status { get; set; } = CustomerPaymentStatus.Pending;

    public string PaymentCode { get; set; } = "";
    public string ProviderInvoiceNumber { get; set; } = "";
    public string? ProviderOrderId { get; set; }

    public string CurrencyCode { get; set; } = "VND";
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RefundedAmount { get; set; }

    public string? RequestPayloadJson { get; set; }
    public string? ProviderResponseJson { get; set; }
    public string? LastWebhookJson { get; set; }
    public string? FailureReason { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? FailedAt { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public DateTimeOffset? WebhookReceivedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerTicket
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }

    public CustomerProductType ProductType { get; set; }
    public CustomerTicketStatus Status { get; set; } = CustomerTicketStatus.Pending;

    public string TicketCode { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string SnapshotJson { get; set; } = "{}";

    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerRefundRequest
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? PaymentId { get; set; }

    public string RefundCode { get; set; } = "";
    public string CurrencyCode { get; set; } = "VND";
    public CustomerRefundStatus Status { get; set; } = CustomerRefundStatus.Requested;

    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? RefundedAmount { get; set; }

    public string ReasonCode { get; set; } = "";
    public string? ReasonText { get; set; }
    public string? ReviewNote { get; set; }
    public string? SnapshotJson { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerSavedPassenger
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string FullName { get; set; } = "";
    public CustomerPassengerType PassengerType { get; set; } = CustomerPassengerType.Adult;
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? NationalityCode { get; set; }
    public string? IdNumber { get; set; }
    public string? PassportNumber { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsDefault { get; set; }
    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerWishlistItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public CustomerProductType ProductType { get; set; }
    public Guid? TargetId { get; set; }
    public string? TargetSlug { get; set; }

    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? LocationText { get; set; }
    public string? PriceText { get; set; }
    public decimal? PriceValue { get; set; }
    public string? CurrencyCode { get; set; }
    public string? ImageUrl { get; set; }
    public string? TargetUrl { get; set; }
    public string? MetadataJson { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerNotification
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }

    public CustomerNotificationStatus Status { get; set; } = CustomerNotificationStatus.Unread;
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? ActionUrl { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? MetadataJson { get; set; }

    public DateTimeOffset? ReadAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerVatInvoiceRequest
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }

    public string RequestCode { get; set; } = "";
    public CustomerVatInvoiceStatus Status { get; set; } = CustomerVatInvoiceStatus.Requested;

    public string CompanyName { get; set; } = "";
    public string TaxCode { get; set; } = "";
    public string CompanyAddress { get; set; } = "";
    public string InvoiceEmail { get; set; } = "";
    public string? Notes { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? PdfUrl { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerAccountPreference
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public string LanguageCode { get; set; } = "vi";
    public string CurrencyCode { get; set; } = "VND";
    public string ThemeMode { get; set; } = "light";
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool SmsNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; } = true;

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerCheckoutDraft
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public CustomerProductType ProductType { get; set; }
    public string CheckoutKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string ResumeUrl { get; set; } = "";
    public string SnapshotJson { get; set; } = "{}";
    public DateTimeOffset LastActivityAt { get; set; }
    public int ResumeCount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerRecentView
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public CustomerProductType ProductType { get; set; }
    public Guid? TargetId { get; set; }
    public string? TargetSlug { get; set; }
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? LocationText { get; set; }
    public string? PriceText { get; set; }
    public decimal? PriceValue { get; set; }
    public string? CurrencyCode { get; set; }
    public string? ImageUrl { get; set; }
    public string? TargetUrl { get; set; }
    public string? MetadataJson { get; set; }
    public DateTimeOffset ViewedAt { get; set; }
    public int ViewCount { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerRecentSearch
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    public CustomerProductType ProductType { get; set; }
    public string SearchKey { get; set; } = "";
    public string? QueryText { get; set; }
    public string? SummaryText { get; set; }
    public string SearchUrl { get; set; } = "";
    public string CriteriaJson { get; set; } = "{}";
    public string? MetadataJson { get; set; }
    public DateTimeOffset SearchedAt { get; set; }
    public int SearchCount { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerSupportTicket
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? OrderId { get; set; }

    public string TicketCode { get; set; } = "";
    public CustomerSupportTicketStatus Status { get; set; } = CustomerSupportTicketStatus.Open;
    public string Category { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ResolutionNote { get; set; }
    public bool HasUnreadStaffReply { get; set; }

    public DateTimeOffset? FirstResponseAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerTenantPayoutAccount
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string BankName { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string AccountHolder { get; set; } = "";
    public string? BankBranch { get; set; }
    public string? Note { get; set; }
    public bool IsDefault { get; set; }
    public bool IsVerified { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerSettlementBatch
{
    public Guid Id { get; set; }
    public string BatchCode { get; set; } = "";
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public CustomerSettlementBatchStatus Status { get; set; } = CustomerSettlementBatchStatus.Draft;
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

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class CustomerSettlementBatchLine
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? RefundRequestId { get; set; }

    public CustomerSettlementStatus Status { get; set; } = CustomerSettlementStatus.InSettlement;
    public string CurrencyCode { get; set; } = "VND";
    public decimal GrossAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CommissionAdjustmentAmount { get; set; }
    public decimal TenantNetAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal NetPayoutAmount { get; set; }
    public string Description { get; set; } = "";
    public string? MetadataJson { get; set; }
    public DateTimeOffset? SettledAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
