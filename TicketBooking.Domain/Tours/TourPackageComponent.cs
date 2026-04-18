using System;
using System.Collections.Generic;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackageComponent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourPackageId { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public TourPackageComponentType ComponentType { get; set; } = TourPackageComponentType.Other;
    public TourPackageSelectionMode SelectionMode { get; set; } = TourPackageSelectionMode.RequiredSingle;

    public int? MinSelect { get; set; }
    public int? MaxSelect { get; set; }

    public int? DayOffsetFromDeparture { get; set; }
    public int? NightCount { get; set; }

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

    public TourPackage? TourPackage { get; set; }

    public ICollection<TourPackageComponentOption> Options { get; set; } = new List<TourPackageComponentOption>();
}

public enum TourPackageComponentType
{
    OutboundTransport = 1,
    ReturnTransport = 2,
    Accommodation = 3,
    Transfer = 4,
    Activity = 5,
    Insurance = 6,
    Meal = 7,
    Guide = 8,
    Support = 9,
    Other = 99
}

public enum TourPackageSelectionMode
{
    RequiredSingle = 1,
    RequiredMulti = 2,
    OptionalSingle = 3,
    OptionalMulti = 4
}
