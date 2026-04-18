using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackageRefundAttempt
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourPackageRefundId { get; set; }

    public TourPackageRefundStatus Status { get; set; } = TourPackageRefundStatus.Pending;

    public string? Provider { get; set; }
    public string? ExternalReference { get; set; }
    public string? ExternalPayloadJson { get; set; }
    public string? ResponsePayloadJson { get; set; }
    public string? WebhookState { get; set; }
    public string? LastProviderError { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset? AttemptedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public TourPackageRefund? TourPackageRefund { get; set; }
}
