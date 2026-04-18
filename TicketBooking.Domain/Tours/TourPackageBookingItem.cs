using System;
using System.Collections.Generic;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackageBookingItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourPackageBookingId { get; set; }
    public Guid TourPackageReservationItemId { get; set; }

    public Guid TourPackageComponentId { get; set; }
    public Guid TourPackageComponentOptionId { get; set; }

    public TourPackageComponentType ComponentType { get; set; } = TourPackageComponentType.Other;
    public TourPackageSourceType SourceType { get; set; } = TourPackageSourceType.Other;

    public Guid? SourceEntityId { get; set; }

    public TourPackageBookingItemStatus Status { get; set; } = TourPackageBookingItemStatus.Pending;

    public int Quantity { get; set; }

    public string CurrencyCode { get; set; } = "VND";
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }

    public string? SourceHoldToken { get; set; }

    public string? SnapshotJson { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public TourPackageBooking? TourPackageBooking { get; set; }
    public ICollection<TourPackageCancellationItem> CancellationItems { get; set; } = new List<TourPackageCancellationItem>();
    public ICollection<TourPackageRefund> Refunds { get; set; } = new List<TourPackageRefund>();
}

public enum TourPackageBookingItemStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2,
    Failed = 3,
    CancellationPending = 4,
    RefundPending = 5,
    Refunded = 6,
    RefundRejected = 7
}
