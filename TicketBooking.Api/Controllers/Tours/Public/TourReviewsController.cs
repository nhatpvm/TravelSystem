// FILE #266: TicketBooking.Api/Controllers/Tours/TourReviewsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tours/{tourId:guid}/reviews")]
[AllowAnonymous]
public sealed class TourReviewsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TourReviewsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<TourPublicReviewPagedResponse>> List(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] decimal? rating = null,
        [FromQuery] bool withReplyOnly = false,
        [FromQuery] string sort = "newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        if (rating.HasValue && (rating.Value < 0 || rating.Value > 5))
            return BadRequest(new { message = "rating must be between 0 and 5." });

        var tour = await _db.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == tourId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == TourStatus.Active, ct);

        if (tour is null)
            return NotFound(new { message = "Tour not found." });

        IQueryable<TourReview> query = _db.TourReviews
            .AsNoTracking()
            .Where(x =>
                x.TourId == tourId &&
                !x.IsDeleted &&
                x.IsApproved &&
                x.IsPublic);

        if (rating.HasValue)
            query = query.Where(x => x.Rating == rating.Value);

        if (withReplyOnly)
            query = query.Where(x => x.ReplyContent != null && x.ReplyContent != "");

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                (x.Title != null && x.Title.Contains(qq)) ||
                (x.Content != null && x.Content.Contains(qq)) ||
                (x.ReviewerName != null && x.ReviewerName.Contains(qq)) ||
                (x.ReplyContent != null && x.ReplyContent.Contains(qq)));
        }

        var reviewSummary = await _db.TourReviews
            .AsNoTracking()
            .Where(x =>
                x.TourId == tourId &&
                !x.IsDeleted &&
                x.IsApproved &&
                x.IsPublic)
            .GroupBy(x => x.TourId)
            .Select(g => new TourPublicReviewSummaryDto
            {
                Count = g.Count(),
                Average = g.Average(x => x.Rating),
                Star1 = g.Count(x => x.Rating >= 1m && x.Rating < 2m),
                Star2 = g.Count(x => x.Rating >= 2m && x.Rating < 3m),
                Star3 = g.Count(x => x.Rating >= 3m && x.Rating < 4m),
                Star4 = g.Count(x => x.Rating >= 4m && x.Rating < 5m),
                Star5 = g.Count(x => x.Rating == 5m)
            })
            .FirstOrDefaultAsync(ct) ?? new TourPublicReviewSummaryDto();

        query = ApplySort(query, sort);

        var total = await query.CountAsync(ct);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TourPublicReviewListItemDto
            {
                Id = x.Id,
                Rating = x.Rating,
                Title = x.Title,
                Content = x.Content,
                ReviewerName = x.ReviewerName,
                ReplyContent = x.ReplyContent,
                ReplyAt = x.ReplyAt,
                PublishedAt = x.PublishedAt,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new TourPublicReviewPagedResponse
        {
            TourId = tour.Id,
            TourName = tour.Name,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Summary = reviewSummary,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourPublicReviewDetailDto>> GetById(
        Guid tourId,
        Guid id,
        CancellationToken ct = default)
    {
        var tour = await _db.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == tourId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == TourStatus.Active, ct);

        if (tour is null)
            return NotFound(new { message = "Tour not found." });

        var entity = await _db.TourReviews
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TourId == tourId &&
                !x.IsDeleted &&
                x.IsApproved &&
                x.IsPublic, ct);

        if (entity is null)
            return NotFound(new { message = "Review not found." });

        return Ok(new TourPublicReviewDetailDto
        {
            Id = entity.Id,
            TourId = entity.TourId,
            Rating = entity.Rating,
            Title = entity.Title,
            Content = entity.Content,
            ReviewerName = entity.ReviewerName,
            ReplyContent = entity.ReplyContent,
            ReplyAt = entity.ReplyAt,
            PublishedAt = entity.PublishedAt,
            CreatedAt = entity.CreatedAt
        });
    }

    private static IQueryable<TourReview> ApplySort(IQueryable<TourReview> query, string? sort)
    {
        return (sort ?? "newest").Trim().ToLowerInvariant() switch
        {
            "oldest" => query.OrderBy(x => x.PublishedAt ?? x.CreatedAt).ThenBy(x => x.CreatedAt),
            "highest" => query.OrderByDescending(x => x.Rating).ThenByDescending(x => x.PublishedAt ?? x.CreatedAt),
            "lowest" => query.OrderBy(x => x.Rating).ThenByDescending(x => x.PublishedAt ?? x.CreatedAt),
            "replied" => query.OrderByDescending(x => x.ReplyAt.HasValue).ThenByDescending(x => x.PublishedAt ?? x.CreatedAt),
            _ => query.OrderByDescending(x => x.PublishedAt ?? x.CreatedAt).ThenByDescending(x => x.CreatedAt)
        };
    }
}

public sealed class TourPublicReviewPagedResponse
{
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public TourPublicReviewSummaryDto Summary { get; set; } = new();
    public List<TourPublicReviewListItemDto> Items { get; set; } = new();
}

public sealed class TourPublicReviewSummaryDto
{
    public int Count { get; set; }
    public decimal? Average { get; set; }
    public int Star1 { get; set; }
    public int Star2 { get; set; }
    public int Star3 { get; set; }
    public int Star4 { get; set; }
    public int Star5 { get; set; }
}

public sealed class TourPublicReviewListItemDto
{
    public Guid Id { get; set; }
    public decimal Rating { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ReviewerName { get; set; }
    public string? ReplyContent { get; set; }
    public DateTimeOffset? ReplyAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class TourPublicReviewDetailDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public decimal Rating { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ReviewerName { get; set; }
    public string? ReplyContent { get; set; }
    public DateTimeOffset? ReplyAt { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
