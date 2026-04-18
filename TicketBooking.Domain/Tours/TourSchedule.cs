// FILE #228: TicketBooking.Domain/Tours/TourSchedule.cs
using System;
using System.Collections.Generic;

namespace TicketBooking.Domain.Tours;

public sealed class TourSchedule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }

    public string Code { get; set; } = "";
    public string? Name { get; set; }

    // Core departure window
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }

    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }

    // Booking window
    public DateTimeOffset? BookingOpenAt { get; set; }
    public DateTimeOffset? BookingCutoffAt { get; set; }

    // Ops / customer-facing summaries
    public string? MeetingPointSummary { get; set; }
    public string? PickupSummary { get; set; }
    public string? DropoffSummary { get; set; }
    public string? Notes { get; set; }
    public string? InternalNotes { get; set; }
    public string? CancellationNotes { get; set; }
    public string? MetadataJson { get; set; }

    public TourScheduleStatus Status { get; set; } = TourScheduleStatus.Draft;

    // Commercial / operation flags
    public bool IsGuaranteedDeparture { get; set; }
    public bool IsInstantConfirm { get; set; }
    public bool IsFeatured { get; set; }

    // Optional business rules at schedule level
    public int? MinGuestsToOperate { get; set; }
    public int? MaxGuests { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Tour? Tour { get; set; }

    public ICollection<TourSchedulePrice> Prices { get; set; } = new List<TourSchedulePrice>();
    public ICollection<TourScheduleCapacity> Capacities { get; set; } = new List<TourScheduleCapacity>();
    public ICollection<TourScheduleAddonPrice> AddonPrices { get; set; } = new List<TourScheduleAddonPrice>();
    public ICollection<TourPackageScheduleOptionOverride> PackageOptionOverrides { get; set; } = new List<TourPackageScheduleOptionOverride>();
}

public enum TourScheduleStatus
{
    Draft = 0,
    Open = 1,
    Closed = 2,
    Full = 3,
    Cancelled = 4,
    Completed = 5
}
