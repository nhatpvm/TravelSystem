using System;
using System.Collections.Generic;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackageReservation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }
    public Guid TourScheduleId { get; set; }
    public Guid TourPackageId { get; set; }

    public Guid? UserId { get; set; }

    public string Code { get; set; } = "";
    public string HoldToken { get; set; } = "";

    public TourPackageReservationStatus Status { get; set; } = TourPackageReservationStatus.Pending;
    public TourPackageHoldStrategy HoldStrategy { get; set; } = TourPackageHoldStrategy.AllOrNothing;

    public string CurrencyCode { get; set; } = "VND";

    public int RequestedPax { get; set; }
    public int HeldCapacitySlots { get; set; }

    public decimal PackageSubtotalAmount { get; set; }

    public DateTimeOffset? HoldExpiresAt { get; set; }

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

    public ICollection<TourPackageReservationItem> Items { get; set; } = new List<TourPackageReservationItem>();
    public ICollection<TourPackageReschedule> SourceReschedules { get; set; } = new List<TourPackageReschedule>();
    public ICollection<TourPackageReschedule> TargetReschedules { get; set; } = new List<TourPackageReschedule>();
}

public enum TourPackageReservationStatus
{
    Pending = 0,
    Held = 1,
    PartiallyHeld = 2,
    Released = 3,
    Expired = 4,
    Failed = 5,
    Confirmed = 6
}
