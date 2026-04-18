using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackageCancellationItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourPackageCancellationId { get; set; }
    public Guid TourPackageBookingItemId { get; set; }

    public TourPackageCancellationStatus Status { get; set; } = TourPackageCancellationStatus.Requested;

    public string CurrencyCode { get; set; } = "VND";
    public decimal GrossLineAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal RefundAmount { get; set; }

    public string? PolicyRuleJson { get; set; }
    public string? BookingItemSnapshotJson { get; set; }
    public string? SupplierSnapshotJson { get; set; }
    public string? SupplierNote { get; set; }
    public string? FailureReason { get; set; }
    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public TourPackageCancellation? TourPackageCancellation { get; set; }
    public TourPackageBookingItem? TourPackageBookingItem { get; set; }
    public TourPackageRefund? Refund { get; set; }
}
