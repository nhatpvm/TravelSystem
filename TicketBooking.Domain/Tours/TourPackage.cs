using System;
using System.Collections.Generic;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public TourPackageMode Mode { get; set; } = TourPackageMode.Fixed;
    public TourPackageStatus Status { get; set; } = TourPackageStatus.Draft;

    public string CurrencyCode { get; set; } = "VND";

    public bool IsDefault { get; set; }
    public bool AutoRepriceBeforeConfirm { get; set; } = true;
    public TourPackageHoldStrategy HoldStrategy { get; set; } = TourPackageHoldStrategy.AllOrNothing;

    public string? PricingRuleJson { get; set; }
    public string? MetadataJson { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Tour? Tour { get; set; }

    public ICollection<TourPackageComponent> Components { get; set; } = new List<TourPackageComponent>();
}

public enum TourPackageMode
{
    Fixed = 1,
    Configurable = 2,
    Dynamic = 3
}

public enum TourPackageStatus
{
    Draft = 0,
    Active = 1,
    Inactive = 2,
    Archived = 3
}

public enum TourPackageHoldStrategy
{
    None = 0,
    BestEffort = 1,
    AllOrNothing = 2
}
