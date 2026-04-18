// FILE #238: TicketBooking.Domain/Tours/TourReview.cs
using System;

namespace TicketBooking.Domain.Tours;

public sealed class TourReview
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public Guid TourId { get; set; }

    public decimal Rating { get; set; }

    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ReviewerName { get; set; }

    public TourReviewStatus Status { get; set; } = TourReviewStatus.Pending;

    public bool IsApproved { get; set; }
    public bool IsPublic { get; set; } = true;

    public string? ModerationNote { get; set; }

    public string? ReplyContent { get; set; }
    public DateTimeOffset? ReplyAt { get; set; }
    public Guid? ReplyByUserId { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    public string? MetadataJson { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public Tour? Tour { get; set; }
}

public enum TourReviewStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Hidden = 3,
    Deleted = 4
}
