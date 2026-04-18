// FILE #234: TicketBooking.Domain/Tours/TourScheduleAddonPrice.cs
using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourScheduleAddonPrice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourScheduleId { get; set; }
    public Guid TourAddonId { get; set; }

    public string CurrencyCode { get; set; } = "VND";

    // Schedule-specific selling price. If null, FE/BE may fall back to TourAddon.BasePrice.
    public decimal? Price { get; set; }
    public decimal? OriginalPrice { get; set; }

    public bool IsPerPerson { get; set; } = true;
    public bool IsRequired { get; set; }
    public bool IsDefaultSelected { get; set; }
    public bool AllowQuantitySelection { get; set; }

    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }

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

    public TourSchedule? TourSchedule { get; set; }
    public TourAddon? TourAddon { get; set; }
}
