// FILE #227: TicketBooking.Domain/Tours/Tour.cs
using System;
using System.Collections.Generic;

namespace TicketBooking.Domain.Tours;

public sealed class Tour
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    // Optional relation to catalog/provider if tenant wants to separate provider brand
    public Guid? ProviderId { get; set; }

    // Main destination / starting location (optional, flexible for later)
    public Guid? PrimaryLocationId { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";

    public TourType Type { get; set; } = TourType.Domestic;
    public TourStatus Status { get; set; } = TourStatus.Draft;
    public TourDifficulty Difficulty { get; set; } = TourDifficulty.Easy;

    public int DurationDays { get; set; }
    public int DurationNights { get; set; }

    public int? MinGuests { get; set; }
    public int? MaxGuests { get; set; }

    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    public bool IsFeatured { get; set; }
    public bool IsFeaturedOnHome { get; set; }
    public bool IsPrivateTourSupported { get; set; }
    public bool IsInstantConfirm { get; set; }

    public string? CountryCode { get; set; }
    public string? Province { get; set; }
    public string? City { get; set; }
    public string? MeetingPointSummary { get; set; }

    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }

    public string? HighlightsJson { get; set; }
    public string? IncludesJson { get; set; }
    public string? ExcludesJson { get; set; }
    public string? TermsJson { get; set; }
    public string? MetadataJson { get; set; }

    public string? CoverImageUrl { get; set; }
    public Guid? CoverMediaAssetId { get; set; }

    public string CurrencyCode { get; set; } = "VND";

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<TourImage> Images { get; set; } = new List<TourImage>();
    public ICollection<TourContact> Contacts { get; set; } = new List<TourContact>();
    public ICollection<TourPolicy> Policies { get; set; } = new List<TourPolicy>();
    public ICollection<TourFaq> Faqs { get; set; } = new List<TourFaq>();
    public ICollection<TourReview> Reviews { get; set; } = new List<TourReview>();
    public ICollection<TourItineraryDay> ItineraryDays { get; set; } = new List<TourItineraryDay>();
    public ICollection<TourSchedule> Schedules { get; set; } = new List<TourSchedule>();
    public ICollection<TourPackage> Packages { get; set; } = new List<TourPackage>();
    public ICollection<TourAddon> Addons { get; set; } = new List<TourAddon>();
    public ICollection<TourPickupPoint> PickupPoints { get; set; } = new List<TourPickupPoint>();
    public ICollection<TourDropoffPoint> DropoffPoints { get; set; } = new List<TourDropoffPoint>();
}

public enum TourType
{
    Domestic = 1,
    International = 2,
    Daily = 3,
    Combo = 4,
    Charter = 5
}

public enum TourStatus
{
    Draft = 0,
    Active = 1,
    Inactive = 2,
    Hidden = 3,
    Archived = 4
}

public enum TourDifficulty
{
    Easy = 1,
    Moderate = 2,
    Challenging = 3
}
