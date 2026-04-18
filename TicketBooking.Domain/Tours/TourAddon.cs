// FILE #233: TicketBooking.Domain/Tours/TourAddon.cs
using System;
using System.Collections.Generic;

namespace TicketBooking.Domain.Tours;

public sealed class TourAddon
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public TourAddonType Type { get; set; } = TourAddonType.Other;

    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }

    public string CurrencyCode { get; set; } = "VND";

    // Default/base price. Schedule-level override can be defined in TourScheduleAddonPrice.
    public decimal BasePrice { get; set; }
    public decimal? OriginalPrice { get; set; }

    public bool IsPerPerson { get; set; } = true;
    public bool IsRequired { get; set; }
    public bool AllowQuantitySelection { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }

    public bool IsDefaultSelected { get; set; }

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

    public ICollection<TourScheduleAddonPrice> SchedulePrices { get; set; } = new List<TourScheduleAddonPrice>();
}

public enum TourAddonType
{
    Meal = 1,
    Transfer = 2,
    SingleSupplement = 3,
    Insurance = 4,
    Ticket = 5,
    ExtraService = 6,
    Upgrade = 7,
    Gift = 8,
    Other = 9
}
