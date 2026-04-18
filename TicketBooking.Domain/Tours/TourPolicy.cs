
// FILE #235: TicketBooking.Domain/Tours/TourPolicy.cs
using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourPolicy
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public TourPolicyType Type { get; set; } = TourPolicyType.General;

    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }

    // Optional machine-readable structure for future booking/refund rule engine
    public string? PolicyJson { get; set; }

    public bool IsHighlighted { get; set; }
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

public enum TourPolicyType
{
    General = 1,
    Cancellation = 2,
    Child = 3,
    Booking = 4,
    Payment = 5,
    ChangeDate = 6,
    IncludedExcluded = 7,
    VisaPassport = 8,
    HealthSafety = 9,
    Other = 10
}
