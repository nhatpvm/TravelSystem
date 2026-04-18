using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackageScheduleOptionOverride
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourScheduleId { get; set; }
    public Guid TourPackageComponentOptionId { get; set; }

    public TourPackageScheduleOverrideStatus Status { get; set; } = TourPackageScheduleOverrideStatus.Active;

    public string? CurrencyCode { get; set; }
    public decimal? PriceOverride { get; set; }
    public decimal? CostOverride { get; set; }

    public Guid? BoundSourceEntityId { get; set; }

    public string? BoundSnapshotJson { get; set; }
    public string? RuleOverrideJson { get; set; }
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
    public TourPackageComponentOption? TourPackageComponentOption { get; set; }
}

public enum TourPackageScheduleOverrideStatus
{
    Active = 1,
    Disabled = 2,
    Pinned = 3
}
