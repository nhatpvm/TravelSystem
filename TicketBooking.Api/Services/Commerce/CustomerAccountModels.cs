using System.Text.Json;
using TicketBooking.Domain.Commerce;

namespace TicketBooking.Api.Services.Commerce;

public sealed class CustomerSavedPassengerDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = "";
    public CustomerPassengerType PassengerType { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? NationalityCode { get; set; }
    public string? IdNumber { get; set; }
    public string? PassportNumber { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsDefault { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class UpsertCustomerSavedPassengerRequest
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
    public bool IsDefault { get; set; }
    public string? Notes { get; set; }
}

public sealed class CustomerWishlistItemDto
{
    public Guid Id { get; set; }
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
    public JsonElement Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class UpsertCustomerWishlistRequest
{
    public string ProductType { get; set; } = "";
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
}

public sealed class CustomerNotificationListResponse
{
    public int Total { get; set; }
    public int UnreadCount { get; set; }
    public List<CustomerNotificationDto> Items { get; set; } = new();
}

public sealed class CustomerNotificationDto
{
    public Guid Id { get; set; }
    public CustomerNotificationStatus Status { get; set; }
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? ActionUrl { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public JsonElement Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

public sealed class CustomerPaymentHistoryItemDto
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public string PaymentCode { get; set; } = "";
    public string OrderCode { get; set; } = "";
    public CustomerProductType ProductType { get; set; }
    public CustomerPaymentStatus PaymentStatus { get; set; }
    public CustomerOrderStatus OrderStatus { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public string Title { get; set; } = "";
    public string? Subtitle { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

public sealed class CustomerVatInvoiceDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = "";
    public string RequestCode { get; set; } = "";
    public CustomerVatInvoiceStatus Status { get; set; }
    public string CompanyName { get; set; } = "";
    public string TaxCode { get; set; } = "";
    public string CompanyAddress { get; set; } = "";
    public string InvoiceEmail { get; set; } = "";
    public string? InvoiceNumber { get; set; }
    public string? PdfUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

public sealed class CreateCustomerVatInvoiceRequest
{
    public string OrderCode { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string TaxCode { get; set; } = "";
    public string CompanyAddress { get; set; } = "";
    public string InvoiceEmail { get; set; } = "";
    public string? Notes { get; set; }
}

public sealed class CustomerAccountPreferenceDto
{
    public string LanguageCode { get; set; } = "vi";
    public string CurrencyCode { get; set; } = "VND";
    public string ThemeMode { get; set; } = "light";
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool SmsNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; } = true;
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class UpdateCustomerAccountPreferenceRequest
{
    public string LanguageCode { get; set; } = "vi";
    public string CurrencyCode { get; set; } = "VND";
    public string ThemeMode { get; set; } = "light";
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool SmsNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; } = true;
}
