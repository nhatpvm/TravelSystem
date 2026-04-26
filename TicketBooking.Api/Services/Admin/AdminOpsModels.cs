namespace TicketBooking.Api.Services.Admin;

public sealed class AdminOpsOverviewDto
{
    public int BookingCount { get; set; }
    public int PaidBookingCount { get; set; }
    public decimal GmvAmount { get; set; }
    public int FailedPaymentCount { get; set; }
    public decimal FailedPaymentRate { get; set; }
    public int PendingTenantCount { get; set; }
    public int PendingOnboardingCount { get; set; }
    public int OpenSupportTicketCount { get; set; }
    public int PendingRefundCount { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }
    public List<AdminOpsAuditEventDto> RecentActivities { get; set; } = new();
}

public sealed class AdminOpsAuditEventListResponse
{
    public AdminOpsAuditSummaryDto Summary { get; set; } = new();
    public List<AdminOpsAuditEventDto> Items { get; set; } = new();
}

public sealed class AdminOpsAuditSummaryDto
{
    public int TotalCount { get; set; }
    public int BookingEvents { get; set; }
    public int PaymentEvents { get; set; }
    public int RefundEvents { get; set; }
    public int TenantEvents { get; set; }
    public int SupportEvents { get; set; }
    public int SettlementEvents { get; set; }
    public int NotificationEvents { get; set; }
    public int PromoEvents { get; set; }
    public int OnboardingEvents { get; set; }
}

public sealed class AdminOpsAuditEventDto
{
    public string Id { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public Guid? ActorUserId { get; set; }
    public string ActorName { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string EntityCode { get; set; } = "";
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public string? IpAddress { get; set; }
    public string Source { get; set; } = "Derived";
    public string Severity { get; set; } = "Info";
    public string Description { get; set; } = "";
}

public sealed class AdminOpsOutboxListResponse
{
    public AdminOpsOutboxSummaryDto Summary { get; set; } = new();
    public List<AdminOpsOutboxMessageDto> Items { get; set; } = new();
}

public sealed class AdminOpsOutboxSummaryDto
{
    public int TotalCount { get; set; }
    public int InAppCount { get; set; }
    public int UnreadCount { get; set; }
    public int ReadCount { get; set; }
    public int TenantScopedCount { get; set; }
}

public sealed class AdminOpsOutboxMessageDto
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Channel { get; set; } = "InApp";
    public string Status { get; set; } = "";
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string? ActionUrl { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public Guid UserId { get; set; }
    public string RecipientName { get; set; } = "";
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
    public string Source { get; set; } = "CustomerNotifications";
}

public sealed class AdminOpsPromoReadinessDto
{
    public AdminOpsPromoSummaryDto Summary { get; set; } = new();
    public List<AdminOpsPromoTenantDto> Tenants { get; set; } = new();
    public List<AdminOpsReadinessItemDto> Readiness { get; set; } = new();
}

public sealed class AdminOpsPromoSummaryDto
{
    public int HotelOverrideCount { get; set; }
    public int ActiveHotelOverrideCount { get; set; }
    public int UpcomingHotelOverrideCount { get; set; }
    public int ExpiredHotelOverrideCount { get; set; }
    public int TenantCount { get; set; }
}

public sealed class AdminOpsPromoTenantDto
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = "";
    public string TenantCode { get; set; } = "";
    public int OverrideCount { get; set; }
    public int ActiveCount { get; set; }
    public DateOnly? FirstStartDate { get; set; }
    public DateOnly? LastEndDate { get; set; }
    public DateTimeOffset? LastUpdatedAt { get; set; }
}

public sealed class AdminOpsReadinessItemDto
{
    public string Area { get; set; } = "";
    public string Status { get; set; } = "";
    public string Note { get; set; } = "";
    public string? ActionUrl { get; set; }
}
