using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourPackageAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }
    public Guid? TourScheduleId { get; set; }
    public Guid? TourPackageId { get; set; }
    public Guid? TourPackageReservationId { get; set; }
    public Guid? TourPackageBookingId { get; set; }
    public Guid? TourPackageBookingItemId { get; set; }
    public Guid? TourPackageCancellationId { get; set; }
    public Guid? TourPackageRefundId { get; set; }
    public Guid? TourPackageRescheduleId { get; set; }

    public Guid? ActorUserId { get; set; }
    public TourPackageSourceType? SourceType { get; set; }

    public string EventType { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Status { get; set; }
    public string? Description { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? Amount { get; set; }

    public TourPackageAuditSeverity Severity { get; set; } = TourPackageAuditSeverity.Info;
    public bool IsSystemGenerated { get; set; } = true;

    public string? SnapshotJson { get; set; }
    public string? MetadataJson { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public enum TourPackageAuditSeverity
{
    Info = 1,
    Warning = 2,
    Error = 3
}
