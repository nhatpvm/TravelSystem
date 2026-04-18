// FILE #237: TicketBooking.Domain/Tours/TourContact.cs
using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourContact
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }

    public string Name { get; set; } = "";
    public string? Title { get; set; }
    public string? Department { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }

    public TourContactType ContactType { get; set; } = TourContactType.General;

    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }

    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Tour? Tour { get; set; }
}

public enum TourContactType
{
    General = 1,
    Sales = 2,
    Hotline = 3,
    Guide = 4,
    Emergency = 5,
    Operation = 6,
    Support = 7
}
