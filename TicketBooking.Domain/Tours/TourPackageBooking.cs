using System;
using System.Collections.Generic;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackageBooking
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }
    public Guid TourScheduleId { get; set; }
    public Guid TourPackageId { get; set; }
    public Guid TourPackageReservationId { get; set; }

    public Guid? UserId { get; set; }

    public string Code { get; set; } = "";

    public TourPackageBookingStatus Status { get; set; } = TourPackageBookingStatus.Pending;
    public TourPackageHoldStrategy HoldStrategy { get; set; } = TourPackageHoldStrategy.AllOrNothing;

    public string CurrencyCode { get; set; } = "VND";

    public int RequestedPax { get; set; }
    public int ConfirmedCapacitySlots { get; set; }

    public decimal PackageSubtotalAmount { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    public string? SnapshotJson { get; set; }
    public string? Notes { get; set; }
    public string? FailureReason { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Tour? Tour { get; set; }
    public TourSchedule? TourSchedule { get; set; }
    public TourPackage? TourPackage { get; set; }
    public TourPackageReservation? TourPackageReservation { get; set; }

    public ICollection<TourPackageBookingItem> Items { get; set; } = new List<TourPackageBookingItem>();
    public ICollection<TourPackageCancellation> Cancellations { get; set; } = new List<TourPackageCancellation>();
    public ICollection<TourPackageRefund> Refunds { get; set; } = new List<TourPackageRefund>();
    public ICollection<TourPackageReschedule> SourceReschedules { get; set; } = new List<TourPackageReschedule>();
    public ICollection<TourPackageReschedule> TargetReschedules { get; set; } = new List<TourPackageReschedule>();
}

public enum TourPackageBookingStatus
{
    Pending = 0,
    Confirmed = 1,
    PartiallyConfirmed = 2,
    Cancelled = 3,
    Failed = 4,
    PartiallyCancelled = 5
}
