using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Hotels;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Hotels;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hotels")]
public sealed class HotelReviewsController : ControllerBase
{
    private readonly AppDbContext _db;

    public HotelReviewsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{hotelId:guid}/reviews")]
    public async Task<ActionResult<PublicHotelReviewPagedResponse>> GetByHotelId(
        Guid hotelId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var hotel = await _db.Hotels
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == hotelId &&
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == HotelStatus.Active, ct);

        if (hotel is null)
            return NotFound(new { message = "Hotel not found." });

        return Ok(await BuildResponseAsync(hotel, page, pageSize, ct));
    }

    [HttpGet("slug/{slug}/reviews")]
    public async Task<ActionResult<PublicHotelReviewPagedResponse>> GetBySlug(
        string slug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest(new { message = "Slug is required." });

        slug = slug.Trim();

        var hotel = await _db.Hotels
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Slug == slug &&
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == HotelStatus.Active, ct);

        if (hotel is null)
            return NotFound(new { message = "Hotel not found." });

        return Ok(await BuildResponseAsync(hotel, page, pageSize, ct));
    }

    private async Task<PublicHotelReviewPagedResponse> BuildResponseAsync(
        Hotel hotel,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var baseQuery = _db.HotelReviews
            .AsNoTracking()
            .Where(x =>
                x.TenantId == hotel.TenantId &&
                x.HotelId == hotel.Id &&
                !x.IsDeleted &&
                x.Status == ReviewStatus.Published);

        var total = await baseQuery.CountAsync(ct);

        var ratings = await baseQuery
            .Select(x => x.Rating)
            .ToListAsync(ct);

        var avg = ratings.Count == 0 ? 0m : Math.Round(ratings.Average(x => (decimal)x), 2);
        var ratingCounts = new int[5];
        foreach (var rating in ratings.Where(x => x >= 1 && x <= 5))
        {
            ratingCounts[rating - 1]++;
        }

        var items = await baseQuery
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PublicHotelReviewItemDto
            {
                Id = x.Id,
                Rating = x.Rating,
                Title = x.Title,
                Content = x.Content,
                ReviewerName = null,
                IsVerifiedStay = x.IsVerifiedStay,
                CreatedAt = x.CreatedAt,
                ReplyContent = null,
                ReplyAt = null
            })
            .ToListAsync(ct);

        return new PublicHotelReviewPagedResponse
        {
            Hotel = new PublicHotelReviewHotelDto
            {
                Id = hotel.Id,
                TenantId = hotel.TenantId,
                Code = hotel.Code,
                Name = hotel.Name,
                Slug = hotel.Slug
            },
            Summary = new PublicHotelReviewSummaryDto
            {
                TotalReviews = total,
                AverageRating = avg,
                Rating1Count = ratingCounts[0],
                Rating2Count = ratingCounts[1],
                Rating3Count = ratingCounts[2],
                Rating4Count = ratingCounts[3],
                Rating5Count = ratingCounts[4]
            },
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        };
    }
}

public sealed class PublicHotelReviewPagedResponse
{
    public PublicHotelReviewHotelDto Hotel { get; set; } = new();
    public PublicHotelReviewSummaryDto Summary { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<PublicHotelReviewItemDto> Items { get; set; } = new();
}

public sealed class PublicHotelReviewHotelDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
}

public sealed class PublicHotelReviewSummaryDto
{
    public int TotalReviews { get; set; }
    public decimal AverageRating { get; set; }
    public int Rating1Count { get; set; }
    public int Rating2Count { get; set; }
    public int Rating3Count { get; set; }
    public int Rating4Count { get; set; }
    public int Rating5Count { get; set; }
}

public sealed class PublicHotelReviewItemDto
{
    public Guid Id { get; set; }
    public decimal Rating { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ReviewerName { get; set; }
    public bool IsVerifiedStay { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? ReplyContent { get; set; }
    public DateTimeOffset? ReplyAt { get; set; }
}
