using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Commerce;
using TicketBooking.Domain.Commerce;
using TicketBooking.Domain.Hotels;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Commerce;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenant/commerce")]
[Authorize]
public sealed class TenantCommerceController : ControllerBase
{
    private readonly CommerceBackofficeService _service;
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public TenantCommerceController(
        CommerceBackofficeService service,
        AppDbContext db,
        ITenantContext tenantContext)
    {
        _service = service;
        _db = db;
        _tenantContext = tenantContext;
    }

    [HttpGet("finance")]
    [Authorize(Policy = "perm:tenant.finance.read")]
    public async Task<ActionResult<TenantFinanceDashboardDto>> GetFinance(CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "X-TenantId is required to view tenant finance." });

        return Ok(await _service.GetTenantFinanceDashboardAsync(_tenantContext.TenantId!.Value, ct));
    }

    [HttpGet("reports")]
    [Authorize(Policy = "perm:tenant.reports.read")]
    public async Task<ActionResult<TenantReportDashboardDto>> GetReports(
        [FromQuery] string? period = "year",
        CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "X-TenantId is required to view tenant reports." });

        return Ok(await _service.GetTenantReportDashboardAsync(_tenantContext.TenantId!.Value, period, ct));
    }

    [HttpGet("bookings")]
    [Authorize(Policy = "perm:tenant.bookings.read")]
    public async Task<ActionResult<AdminCommerceBookingListResponse>> ListBookings(
        [FromQuery] string? q = null,
        [FromQuery] CustomerOrderStatus? status = null,
        CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "X-TenantId is required to view tenant bookings." });

        return Ok(await _service.ListBookingsAsync(q, status, ct, _tenantContext.TenantId!.Value));
    }

    [HttpGet("bookings/{orderId:guid}")]
    [Authorize(Policy = "perm:tenant.bookings.read")]
    public async Task<ActionResult<AdminCommerceBookingDetailDto>> GetBooking(
        Guid orderId,
        CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "X-TenantId is required to view tenant bookings." });

        return Ok(await _service.GetBookingDetailAsync(orderId, ct, _tenantContext.TenantId!.Value));
    }

    [HttpGet("reviews")]
    [Authorize(Policy = "perm:tenant.reviews.read")]
    public async Task<ActionResult<TenantReviewListResponse>> ListReviews(
        [FromQuery] string? q = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "X-TenantId is required to view tenant reviews." });

        var tenantId = _tenantContext.TenantId!.Value;
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 100 : pageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;
        var keyword = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        IQueryable<HotelReview> hotelQuery = includeDeleted
            ? _db.HotelReviews.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.HotelReviews.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        IQueryable<TourReview> tourQuery = includeDeleted
            ? _db.TourReviews.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.TourReviews.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (keyword is not null)
        {
            hotelQuery = hotelQuery.Where(x =>
                (x.Title != null && x.Title.Contains(keyword)) ||
                (x.Content != null && x.Content.Contains(keyword)) ||
                (x.MetadataJson != null && x.MetadataJson.Contains(keyword)));

            tourQuery = tourQuery.Where(x =>
                (x.Title != null && x.Title.Contains(keyword)) ||
                (x.Content != null && x.Content.Contains(keyword)) ||
                (x.ReviewerName != null && x.ReviewerName.Contains(keyword)) ||
                (x.ModerationNote != null && x.ModerationNote.Contains(keyword)) ||
                (x.ReplyContent != null && x.ReplyContent.Contains(keyword)));
        }

        var hotelItems = await (
            from review in hotelQuery.AsNoTracking()
            join hotel in _db.Hotels.AsNoTracking() on review.HotelId equals hotel.Id
            where !hotel.IsDeleted
            select new TenantReviewListItem
            {
                Id = review.Id,
                Module = "hotel",
                EntityId = review.HotelId,
                EntityName = hotel.Name,
                Rating = review.Rating,
                Title = review.Title,
                Content = review.Content,
                ReviewerName = null,
                Status = review.Status.ToString(),
                IsApproved = review.Status == ReviewStatus.Published,
                IsPublic = review.Status == ReviewStatus.Published,
                HasReply = false,
                ReplyAt = null,
                IsVerifiedStay = review.IsVerifiedStay,
                IsDeleted = review.IsDeleted,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            }).ToListAsync(ct);

        var tourItems = await (
            from review in tourQuery.AsNoTracking()
            join tour in _db.Tours.AsNoTracking() on review.TourId equals tour.Id
            where !tour.IsDeleted
            select new TenantReviewListItem
            {
                Id = review.Id,
                Module = "tour",
                EntityId = review.TourId,
                EntityName = tour.Name,
                Rating = review.Rating,
                Title = review.Title,
                Content = review.Content,
                ReviewerName = review.ReviewerName,
                Status = review.Status.ToString(),
                IsApproved = review.IsApproved,
                IsPublic = review.IsPublic,
                HasReply = review.ReplyAt != null,
                ReplyAt = review.ReplyAt,
                IsVerifiedStay = false,
                IsDeleted = review.IsDeleted,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt
            }).ToListAsync(ct);

        var allItems = hotelItems
            .Concat(tourItems)
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ToList();

        return Ok(new TenantReviewListResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = allItems.Count,
            Items = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            Summary = new TenantReviewSummary
            {
                TotalCount = allItems.Count,
                PendingCount = allItems.Count(x => string.Equals(x.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                PublishedCount = allItems.Count(x => x.IsPublic && x.IsApproved),
                HiddenCount = allItems.Count(x => string.Equals(x.Status, "Hidden", StringComparison.OrdinalIgnoreCase)),
                RepliedCount = allItems.Count(x => x.HasReply),
                AverageRating = allItems.Count == 0 ? 0 : Math.Round(allItems.Average(x => x.Rating), 1)
            }
        });
    }

    [HttpPut("payout-account")]
    [Authorize(Policy = "perm:tenant.finance.write")]
    public async Task<ActionResult<TenantPayoutAccountDto>> UpsertPayoutAccount(
        [FromBody] UpsertTenantPayoutAccountRequest request,
        CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "X-TenantId is required to update tenant payout account." });

        return Ok(await _service.UpsertTenantPayoutAccountAsync(_tenantContext.TenantId!.Value, request, GetCurrentUserId(), ct));
    }

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}

public sealed class TenantReviewListResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public TenantReviewSummary Summary { get; set; } = new();
    public List<TenantReviewListItem> Items { get; set; } = new();
}

public sealed class TenantReviewSummary
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int PublishedCount { get; set; }
    public int HiddenCount { get; set; }
    public int RepliedCount { get; set; }
    public decimal AverageRating { get; set; }
}

public sealed class TenantReviewListItem
{
    public Guid Id { get; set; }
    public string Module { get; set; } = "";
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = "";
    public decimal Rating { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ReviewerName { get; set; }
    public string Status { get; set; } = "";
    public bool IsApproved { get; set; }
    public bool IsPublic { get; set; }
    public bool HasReply { get; set; }
    public DateTimeOffset? ReplyAt { get; set; }
    public bool IsVerifiedStay { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
