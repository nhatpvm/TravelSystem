// FILE #239: TicketBooking.Domain/Tours/TourFaq.cs
using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourFaq
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }

    public string Question { get; set; } = "";
    public string AnswerMarkdown { get; set; } = "";
    public string? AnswerHtml { get; set; }

    public TourFaqType Type { get; set; } = TourFaqType.General;

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

public enum TourFaqType
{
    General = 1,
    Booking = 2,
    Payment = 3,
    Cancellation = 4,
    ChildPolicy = 5,
    PickupDropoff = 6,
    VisaPassport = 7,
    HealthSafety = 8,
    Luggage = 9,
    Other = 10
}
