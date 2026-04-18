using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackageReschedule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }

    public Guid SourceTourPackageBookingId { get; set; }
    public Guid SourceTourPackageReservationId { get; set; }
    public Guid SourceTourScheduleId { get; set; }
    public Guid SourceTourPackageId { get; set; }

    public Guid TargetTourScheduleId { get; set; }
    public Guid TargetTourPackageId { get; set; }
    public Guid? TargetTourPackageReservationId { get; set; }
    public Guid? TargetTourPackageBookingId { get; set; }
    public Guid? SourceTourPackageCancellationId { get; set; }

    public Guid? RequestedByUserId { get; set; }

    public string Code { get; set; } = "";
    public string ClientToken { get; set; } = "";

    public TourPackageRescheduleStatus Status { get; set; } = TourPackageRescheduleStatus.Requested;
    public TourPackageHoldStrategy HoldStrategy { get; set; } = TourPackageHoldStrategy.AllOrNothing;

    public string CurrencyCode { get; set; } = "VND";

    public int RequestedPax { get; set; }

    public decimal SourcePackageSubtotalAmount { get; set; }
    public decimal? TargetPackageSubtotalAmount { get; set; }
    public decimal? PriceDifferenceAmount { get; set; }

    public DateTimeOffset? HoldExpiresAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }

    public string? ReasonCode { get; set; }
    public string? ReasonText { get; set; }
    public string? OverrideNote { get; set; }
    public string? FailureReason { get; set; }

    public string? SnapshotJson { get; set; }
    public string? ResolutionSnapshotJson { get; set; }
    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Tour? Tour { get; set; }
    public TourPackageBooking? SourceTourPackageBooking { get; set; }
    public TourPackageReservation? SourceTourPackageReservation { get; set; }
    public TourSchedule? SourceTourSchedule { get; set; }
    public TourPackage? SourceTourPackage { get; set; }
    public TourSchedule? TargetTourSchedule { get; set; }
    public TourPackage? TargetTourPackage { get; set; }
    public TourPackageReservation? TargetTourPackageReservation { get; set; }
    public TourPackageBooking? TargetTourPackageBooking { get; set; }
    public TourPackageCancellation? SourceTourPackageCancellation { get; set; }
}

public enum TourPackageRescheduleStatus
{
    Requested = 0,
    Held = 1,
    Confirming = 2,
    Completed = 3,
    Released = 4,
    Failed = 5,
    AttentionRequired = 6
}
