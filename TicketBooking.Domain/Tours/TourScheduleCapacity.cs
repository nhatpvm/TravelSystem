// FILE #230: TicketBooking.Domain/Tours/TourScheduleCapacity.cs
using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourScheduleCapacity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourScheduleId { get; set; }

    // Total commercial seats/slots available for sale
    public int TotalSlots { get; set; }

    // Operational states
    public int SoldSlots { get; set; }
    public int HeldSlots { get; set; }
    public int BlockedSlots { get; set; }

    // Optional min/max rule at capacity layer
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuestsPerBooking { get; set; }

    // Optional operational thresholds
    public int? WarningThreshold { get; set; }

    public TourCapacityStatus Status { get; set; } = TourCapacityStatus.Open;

    public bool AllowWaitlist { get; set; }
    public bool AutoCloseWhenFull { get; set; } = true;

    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public TourSchedule? TourSchedule { get; set; }

    public int AvailableSlots
    {
        get
        {
            var value = TotalSlots - SoldSlots - HeldSlots - BlockedSlots;
            return value < 0 ? 0 : value;
        }
    }
}

public enum TourCapacityStatus
{
    Draft = 0,
    Open = 1,
    Limited = 2,
    Full = 3,
    Closed = 4,
    Cancelled = 5
}
