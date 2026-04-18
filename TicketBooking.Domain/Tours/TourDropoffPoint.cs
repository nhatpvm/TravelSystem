
// FILE #241: TicketBooking.Domain/Tours/TourDropoffPoint.cs
using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourDropoffPoint
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public TimeOnly? DropoffTime { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }

    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Tour? Tour { get; set; }
}
