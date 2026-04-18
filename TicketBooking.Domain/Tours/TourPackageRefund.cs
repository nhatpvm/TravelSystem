using System;
using System.Collections.Generic;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackageRefund
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourPackageBookingId { get; set; }
    public Guid TourPackageBookingItemId { get; set; }
    public Guid TourPackageCancellationId { get; set; }
    public Guid TourPackageCancellationItemId { get; set; }

    public TourPackageRefundStatus Status { get; set; } = TourPackageRefundStatus.Pending;

    public string CurrencyCode { get; set; } = "VND";
    public decimal GrossLineAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal RefundAmount { get; set; }

    public string? Provider { get; set; }
    public string? ExternalReference { get; set; }
    public string? ExternalPayloadJson { get; set; }
    public string? WebhookState { get; set; }
    public string? LastProviderError { get; set; }

    public string? SnapshotJson { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset? PreparedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public TourPackageBooking? TourPackageBooking { get; set; }
    public TourPackageBookingItem? TourPackageBookingItem { get; set; }
    public TourPackageCancellation? TourPackageCancellation { get; set; }
    public TourPackageCancellationItem? TourPackageCancellationItem { get; set; }
    public ICollection<TourPackageRefundAttempt> Attempts { get; set; } = new List<TourPackageRefundAttempt>();
}

public enum TourPackageRefundStatus
{
    Pending = 0,
    ReadyForProvider = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4,
    Rejected = 5
}
