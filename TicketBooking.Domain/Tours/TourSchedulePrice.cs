// FILE #229: TicketBooking.Domain/Tours/TourSchedulePrice.cs
using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourSchedulePrice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourScheduleId { get; set; }

    // Price bucket: Adult / Child / Baby / Senior / SingleSupplement / PrivateGroup...
    public TourPriceType PriceType { get; set; } = TourPriceType.Adult;

    public string CurrencyCode { get; set; } = "VND";

    // Main selling price
    public decimal Price { get; set; }

    // Optional compare/base price for UI discount display
    public decimal? OriginalPrice { get; set; }

    // Optional tax/fee split if later need quote breakdown
    public decimal? Taxes { get; set; }
    public decimal? Fees { get; set; }

    // Quantity/age rule
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    // For group/private/custom tiers
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }

    // Business flags
    public bool IsDefault { get; set; }
    public bool IsIncludedTax { get; set; }
    public bool IsIncludedFee { get; set; }

    public string? Label { get; set; }
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
}

public enum TourPriceType
{
    Adult = 1,
    Child = 2,
    Baby = 3,
    Senior = 4,
    SingleSupplement = 5,
    PrivateGroup = 6,
    ExtraBed = 7
}
