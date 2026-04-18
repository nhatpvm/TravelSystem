// FILE #236: TicketBooking.Domain/Tours/TourImage.cs
using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourImage
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }

    public Guid? MediaAssetId { get; set; }

    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }

    public bool IsPrimary { get; set; }
    public bool IsCover { get; set; }
    public bool IsFeatured { get; set; }

    public int SortOrder { get; set; }

    public string? MetadataJson { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Tour? Tour { get; set; }
}
