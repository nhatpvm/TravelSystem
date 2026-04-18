using System;
using System.Collections.Generic;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackageCancellation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }
    public Guid TourScheduleId { get; set; }
    public Guid TourPackageId { get; set; }
    public Guid TourPackageBookingId { get; set; }

    public Guid? RequestedByUserId { get; set; }

    public TourPackageCancellationStatus Status { get; set; } = TourPackageCancellationStatus.Requested;

    public bool IsAdminOverride { get; set; }

    public string CurrencyCode { get; set; } = "VND";
    public decimal PenaltyAmount { get; set; }
    public decimal RefundAmount { get; set; }

    public string? PolicyCode { get; set; }
    public string? PolicyName { get; set; }
    public string? ReasonCode { get; set; }
    public string? ReasonText { get; set; }
    public string? OverrideNote { get; set; }
    public string? FailureReason { get; set; }

    public string? BookingSnapshotJson { get; set; }
    public string? DecisionSnapshotJson { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public TourPackageBooking? TourPackageBooking { get; set; }
    public ICollection<TourPackageCancellationItem> Items { get; set; } = new List<TourPackageCancellationItem>();
    public ICollection<TourPackageRefund> Refunds { get; set; } = new List<TourPackageRefund>();
}

public enum TourPackageCancellationStatus
{
    Requested = 0,
    Approved = 1,
    Rejected = 2,
    Completed = 3
}
