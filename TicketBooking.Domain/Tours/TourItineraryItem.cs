// FILE #232: TicketBooking.Domain/Tours/TourItineraryItem.cs
using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourItineraryItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourItineraryDayId { get; set; }

    public TourItineraryItemType Type { get; set; } = TourItineraryItemType.Activity;

    public string Title { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }

    // Optional timeline
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    // Optional location / logistics
    public string? LocationName { get; set; }
    public string? AddressLine { get; set; }
    public string? TransportationMode { get; set; }

    // Optional service markers
    public bool IncludesTicket { get; set; }
    public bool IncludesMeal { get; set; }
    public bool IsOptional { get; set; }
    public bool RequiresAdditionalFee { get; set; }

    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public TourItineraryDay? TourItineraryDay { get; set; }
}

public enum TourItineraryItemType
{
    Activity = 1,
    Transfer = 2,
    Meal = 3,
    CheckIn = 4,
    CheckOut = 5,
    Sightseeing = 6,
    FreeTime = 7,
    Accommodation = 8,
    Other = 9
}
