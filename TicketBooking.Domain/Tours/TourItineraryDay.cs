// FILE #231: TicketBooking.Domain/Tours/TourItineraryDay.cs
using System;
using System.Collections.Generic;

namespace TicketBooking.Domain.Tours;

public sealed class TourItineraryDay
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }

    // Day 1, Day 2, ...
    public int DayNumber { get; set; }

    public string Title { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }

    // Summary of movement / overnight
    public string? StartLocation { get; set; }
    public string? EndLocation { get; set; }
    public string? AccommodationName { get; set; }

    // Meal summary for FE badges
    public bool IncludesBreakfast { get; set; }
    public bool IncludesLunch { get; set; }
    public bool IncludesDinner { get; set; }

    public string? TransportationSummary { get; set; }
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

    public Tour? Tour { get; set; }

    public ICollection<TourItineraryItem> Items { get; set; } = new List<TourItineraryItem>();
}
