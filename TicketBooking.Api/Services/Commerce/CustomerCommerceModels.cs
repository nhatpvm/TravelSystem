using System.Text.Json;
using TicketBooking.Domain.Commerce;

namespace TicketBooking.Api.Services.Commerce;

public sealed class CommerceOptions
{
    public decimal DefaultCommissionPercent { get; set; } = 10m;
    public int PendingPaymentMinutes { get; set; } = 15;
}

public sealed class SePayGatewayOptions
{
    public string ApiBaseUrl { get; set; } = "https://pgapi-sandbox.sepay.vn";
    public string PayBaseUrl { get; set; } = "https://pay-sandbox.sepay.vn";
    public string MerchantId { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string? WebhookSecret { get; set; }
    public string DefaultPaymentMethod { get; set; } = "BANK_TRANSFER";
    public string? FrontendBaseUrl { get; set; }
    public bool AllowClientAppBaseUrlInDevelopment { get; set; } = true;
}

public sealed class CreateCustomerOrderRequest
{
    public string ProductType { get; set; } = "";
    public Guid? TripId { get; set; }
    public Guid? OfferId { get; set; }
    public Guid? TourId { get; set; }
    public Guid? ScheduleId { get; set; }
    public Guid? PackageId { get; set; }
    public Guid? HotelId { get; set; }
    public Guid? RoomTypeId { get; set; }
    public Guid? RatePlanId { get; set; }
    public Guid? SeatId { get; set; }
    public List<Guid> AncillaryIds { get; set; } = new();
    public string? HoldToken { get; set; }
    public int AdultCount { get; set; } = 1;
    public int ChildCount { get; set; }
    public int RoomCount { get; set; } = 1;
    public DateOnly? CheckInDate { get; set; }
    public DateOnly? CheckOutDate { get; set; }
    public CustomerContactInput Contact { get; set; } = new();
    public List<CustomerPassengerInput> Passengers { get; set; } = new();
    public CustomerVatInput? Vat { get; set; }
    public string? CustomerNote { get; set; }
}

public sealed class CustomerContactInput
{
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
}

public sealed class CustomerVatInput
{
    public string CompanyName { get; set; } = "";
    public string TaxCode { get; set; } = "";
    public string CompanyAddress { get; set; } = "";
    public string? InvoiceEmail { get; set; }
}

public sealed class CustomerPassengerInput
{
    public string FullName { get; set; } = "";
    public string PassengerType { get; set; } = "adult";
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? NationalityCode { get; set; }
    public string? IdNumber { get; set; }
    public string? PassportNumber { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Notes { get; set; }
}

public sealed class StartPaymentRequest
{
    public string? AppBaseUrl { get; set; }
}

public sealed class CreateRefundRequestInput
{
    public decimal? RequestedAmount { get; set; }
    public string ReasonCode { get; set; } = "";
    public string? ReasonText { get; set; }
}

public sealed class CreateCustomerSupportTicketRequest
{
    public string Subject { get; set; } = "";
    public string Category { get; set; } = "";
    public string Content { get; set; } = "";
    public string? OrderCode { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public sealed class CustomerOrderListResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<CustomerOrderSummaryDto> Items { get; set; } = new();
}

public sealed class CustomerOrderSummaryDto
{
    public Guid Id { get; set; }
    public string OrderCode { get; set; } = "";
    public string CurrencyCode { get; set; } = "VND";
    public CustomerProductType ProductType { get; set; }
    public CustomerOrderStatus Status { get; set; }
    public CustomerPaymentStatus PaymentStatus { get; set; }
    public CustomerTicketStatus TicketStatus { get; set; }
    public CustomerRefundStatus RefundStatus { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public JsonElement Snapshot { get; set; }
}

public sealed class CustomerOrderDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string OrderCode { get; set; } = "";
    public string CurrencyCode { get; set; } = "VND";
    public CustomerProductType ProductType { get; set; }
    public CustomerOrderStatus Status { get; set; }
    public CustomerPaymentStatus PaymentStatus { get; set; }
    public CustomerTicketStatus TicketStatus { get; set; }
    public CustomerRefundStatus RefundStatus { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ServiceFeeAmount { get; set; }
    public decimal PlatformCommissionAmount { get; set; }
    public decimal TenantNetAmount { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public string ContactFullName { get; set; } = "";
    public string ContactPhone { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public bool VatInvoiceRequested { get; set; }
    public string? CustomerNote { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? TicketIssuedAt { get; set; }
    public CustomerPaymentDto? Payment { get; set; }
    public CustomerTicketDto? Ticket { get; set; }
    public List<CustomerRefundDto> Refunds { get; set; } = new();
    public JsonElement Snapshot { get; set; }
}

public sealed class CustomerPaymentDto
{
    public Guid Id { get; set; }
    public string PaymentCode { get; set; } = "";
    public string ProviderInvoiceNumber { get; set; } = "";
    public string? ProviderOrderId { get; set; }
    public CustomerPaymentProvider Provider { get; set; }
    public CustomerPaymentMethod Method { get; set; }
    public CustomerPaymentStatus Status { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
    public SePayCheckoutFormDto? CheckoutForm { get; set; }
}

public sealed class CustomerTicketDto
{
    public Guid Id { get; set; }
    public string TicketCode { get; set; } = "";
    public CustomerTicketStatus Status { get; set; }
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public JsonElement Snapshot { get; set; }
}

public sealed class CustomerRefundDto
{
    public Guid Id { get; set; }
    public string RefundCode { get; set; } = "";
    public CustomerRefundStatus Status { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal RequestedAmount { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? RefundedAmount { get; set; }
    public string ReasonCode { get; set; } = "";
    public string? ReasonText { get; set; }
    public string? ReviewNote { get; set; }
    public DateTimeOffset RequestedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public sealed class CustomerOrderTimelineEventDto
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public string Tone { get; set; } = "default";
    public bool IsCurrent { get; set; }
    public string? ActionUrl { get; set; }
}

public sealed class CustomerRefundEstimateDto
{
    public string EligibleAction { get; set; } = "none";
    public CustomerSettlementStatus SettlementStatus { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal GrossAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal RemainingRefundableAmount { get; set; }
    public decimal SuggestedAmount { get; set; }
    public decimal EstimatedRefundAmount { get; set; }
    public decimal EstimatedCommissionReversalAmount { get; set; }
    public decimal EstimatedTenantAdjustmentAmount { get; set; }
    public bool SupportsPartialRefund { get; set; }
    public bool RequiresAdminReview { get; set; }
    public string TimingNote { get; set; } = "";
    public string SettlementImpact { get; set; } = "";
    public string StatusNote { get; set; } = "";
    public List<string> RuleSummary { get; set; } = new();
    public List<string> WarningMessages { get; set; } = new();
}

public sealed class SePayCheckoutFormDto
{
    public string ActionUrl { get; set; } = "";
    public List<SePayCheckoutFieldDto> Fields { get; set; } = new();
}

public sealed class SePayCheckoutFieldDto
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

public sealed class CustomerPaymentMethodResponse
{
    public List<CustomerSupportedPaymentMethodDto> Methods { get; set; } = new();
    public List<string> SecurityBadges { get; set; } = new();
}

public sealed class CustomerSupportedPaymentMethodDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsDefault { get; set; }
}

public sealed class CustomerSupportTicketDto
{
    public Guid Id { get; set; }
    public string TicketCode { get; set; } = "";
    public CustomerSupportTicketStatus Status { get; set; }
    public string Category { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
    public string? OrderCode { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ResolutionNote { get; set; }
    public bool HasUnreadStaffReply { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? FirstResponseAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? LastActivityAt { get; set; }
}

public sealed class CustomerOrderSnapshot
{
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public string? ImageUrl { get; set; }
    public string? ProviderName { get; set; }
    public string? LocationText { get; set; }
    public string? RouteFrom { get; set; }
    public string? RouteTo { get; set; }
    public string? SeatText { get; set; }
    public string? RoomText { get; set; }
    public string? PassengerText { get; set; }
    public DateTimeOffset? DepartureAt { get; set; }
    public DateTimeOffset? ArrivalAt { get; set; }
    public string? TicketNote { get; set; }
    public string? SourceCode { get; set; }
    public string? MetadataJson { get; set; }
    public List<CustomerOrderSnapshotLine> Lines { get; set; } = new();
}

public sealed class CustomerOrderSnapshotLine
{
    public string Label { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public decimal UnitAmount { get; set; }
    public decimal LineAmount { get; set; }
}
