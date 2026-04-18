using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackageReservationItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourPackageReservationId { get; set; }

    public Guid TourPackageComponentId { get; set; }
    public Guid TourPackageComponentOptionId { get; set; }

    public TourPackageComponentType ComponentType { get; set; } = TourPackageComponentType.Other;
    public TourPackageSourceType SourceType { get; set; } = TourPackageSourceType.Other;

    public Guid? SourceEntityId { get; set; }

    public TourPackageReservationItemStatus Status { get; set; } = TourPackageReservationItemStatus.Pending;

    public int Quantity { get; set; }

    public string CurrencyCode { get; set; } = "VND";
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }

    public string? SourceHoldToken { get; set; }
    public DateTimeOffset? HoldExpiresAt { get; set; }

    public string? SnapshotJson { get; set; }
    public string? Notes { get; set; }
    public string? ErrorMessage { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public TourPackageReservation? TourPackageReservation { get; set; }
}

public enum TourPackageReservationItemStatus
{
    Pending = 0,
    Held = 1,
    Validated = 2,
    Released = 3,
    Expired = 4,
    Failed = 5,
    Confirmed = 6
}
