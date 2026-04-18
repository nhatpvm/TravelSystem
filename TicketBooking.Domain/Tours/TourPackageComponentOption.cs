using System;
using System.Collections.Generic;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackageComponentOption
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourPackageComponentId { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public TourPackageSourceType SourceType { get; set; } = TourPackageSourceType.Other;
    public TourPackageBindingMode BindingMode { get; set; } = TourPackageBindingMode.StaticReference;

    public Guid? SourceEntityId { get; set; }

    public string? SearchTemplateJson { get; set; }
    public string? RuleJson { get; set; }

    public TourPackagePricingMode PricingMode { get; set; } = TourPackagePricingMode.Included;
    public string CurrencyCode { get; set; } = "VND";

    public decimal? PriceOverride { get; set; }
    public decimal? CostOverride { get; set; }
    public decimal? MarkupPercent { get; set; }
    public decimal? MarkupAmount { get; set; }

    public TourPackageQuantityMode QuantityMode { get; set; } = TourPackageQuantityMode.PerBooking;
    public int DefaultQuantity { get; set; } = 1;
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }

    public bool IsDefaultSelected { get; set; }
    public bool IsFallback { get; set; }
    public bool IsDynamicCandidate { get; set; }

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

    public TourPackageComponent? TourPackageComponent { get; set; }

    public ICollection<TourPackageScheduleOptionOverride> ScheduleOverrides { get; set; } = new List<TourPackageScheduleOptionOverride>();
}

public enum TourPackageSourceType
{
    Flight = 1,
    Hotel = 2,
    Bus = 3,
    Train = 4,
    Activity = 5,
    InternalService = 6,
    Other = 99
}

public enum TourPackageBindingMode
{
    StaticReference = 1,
    SearchTemplate = 2,
    ManualFulfillment = 3
}

public enum TourPackagePricingMode
{
    Included = 1,
    PassThrough = 2,
    Override = 3,
    Markup = 4
}

public enum TourPackageQuantityMode
{
    PerBooking = 1,
    PerPax = 2,
    PerNight = 3,
    PerRoom = 4,
    Custom = 99
}
