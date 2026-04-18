using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Hotels;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Hotels;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/hotel-reviews")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class HotelReviewsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public HotelReviewsAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<AdminHotelReviewPagedResponse>> List(
        [FromQuery] Guid? hotelId = null,
        [FromQuery] string? q = null,
        [FromQuery] string? status = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool includeHidden = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        IQueryable<HotelReview> query = includeDeleted
            ? _db.HotelReviews.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.HotelReviews.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (hotelId.HasValue)
            query = query.Where(x => x.HotelId == hotelId.Value);

        if (!includeHidden)
            query = query.Where(x => x.Status == ReviewStatus.Published);

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TryParseReviewStatus(status, out var parsedStatus))
                return BadRequest(new { message = "Status is invalid." });

            query = query.Where(x => x.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                (x.Title != null && x.Title.Contains(qq)) ||
                (x.Content != null && x.Content.Contains(qq)) ||
                (x.MetadataJson != null && x.MetadataJson.Contains(qq)));
        }

        var total = await query.CountAsync(ct);
        var items = await query.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminHotelReviewListItemDto
            {
                Id = x.Id,
                HotelId = x.HotelId,
                CustomerUserId = x.CustomerUserId,
                Rating = x.Rating,
                Title = x.Title,
                ReviewerName = null,
                Status = x.Status.ToString(),
                IsApproved = x.Status == ReviewStatus.Published,
                IsPublic = x.Status == ReviewStatus.Published,
                HasReply = false,
                IsVerifiedStay = x.IsVerifiedStay,
                HelpfulCount = x.HelpfulCount,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new AdminHotelReviewPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminHotelReviewDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        IQueryable<HotelReview> query = includeDeleted
            ? _db.HotelReviews.IgnoreQueryFilters()
            : _db.HotelReviews;

        var entity = await query.AsNoTracking()
            .Where(x => x.TenantId == tenantId && (includeDeleted || !x.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel review not found in current switched tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] AdminUpdateHotelReviewRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.HotelReviews.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel review not found in current switched tenant." });

        if (req.Rating.HasValue && (req.Rating.Value < 1 || req.Rating.Value > 5))
            return BadRequest(new { message = "Rating must be between 1 and 5." });

        if (req.Title is not null && req.Title.Length > 300)
            return BadRequest(new { message = "Title max length is 300." });

        if (!string.IsNullOrWhiteSpace(req.RowVersionBase64))
        {
            try
            {
                var bytes = Convert.FromBase64String(req.RowVersionBase64);
                _db.Entry(entity).Property(x => x.RowVersion).OriginalValue = bytes;
            }
            catch
            {
                return BadRequest(new { message = "RowVersionBase64 is invalid." });
            }
        }

        if (req.Rating.HasValue)
            entity.Rating = (int)Math.Round(req.Rating.Value, MidpointRounding.AwayFromZero);

        if (req.Title is not null)
            entity.Title = NullIfWhiteSpace(req.Title);

        if (req.Content is not null)
            entity.Content = NullIfWhiteSpace(req.Content);

        if (req.MetadataJson is not null)
            entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);

        if (req.IsVerifiedStay.HasValue)
            entity.IsVerifiedStay = req.IsVerifiedStay.Value;

        if (req.HelpfulCount.HasValue)
        {
            if (req.HelpfulCount.Value < 0)
                return BadRequest(new { message = "HelpfulCount must be >= 0." });

            entity.HelpfulCount = req.HelpfulCount.Value;
        }

        var statusResult = ApplyLegacyStatusInputs(entity, req);
        if (statusResult is not null)
            return statusResult;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Data was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct = default)
        => await SetStatusAsync(id, ReviewStatus.Published, ct);

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] AdminRejectHotelReviewRequest? req,
        CancellationToken ct = default)
        => await SetStatusAsync(id, ReviewStatus.Hidden, ct);

    [HttpPost("{id:guid}/hide")]
    public async Task<IActionResult> Hide(Guid id, CancellationToken ct = default)
        => await SetStatusAsync(id, ReviewStatus.Hidden, ct);

    [HttpPost("{id:guid}/show")]
    public async Task<IActionResult> Show(Guid id, CancellationToken ct = default)
        => await SetStatusAsync(id, ReviewStatus.Published, ct);

    [HttpPost("{id:guid}/reply")]
    public IActionResult Reply(
        Guid id,
        [FromBody] AdminReplyHotelReviewRequest req,
        CancellationToken ct = default)
        => BadRequest(new { message = "Replies are not supported by the current hotel review schema." });

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        => await ToggleDeleted(id, true, ct);

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
        => await ToggleDeleted(id, false, ct);

    private async Task<IActionResult> SetStatusAsync(Guid id, ReviewStatus status, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var entity = await FindReviewForWriteAsync(id, ct);
        if (entity is null)
            return NotFound(new { message = "Hotel review not found in current switched tenant." });

        entity.Status = status;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var entity = await FindReviewForWriteAsync(id, ct);
        if (entity is null)
            return NotFound(new { message = "Hotel review not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private ActionResult? ApplyLegacyStatusInputs(HotelReview entity, AdminUpdateHotelReviewRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.Status))
        {
            if (!TryParseReviewStatus(req.Status, out var parsedStatus))
                return BadRequest(new { message = "Status is invalid." });

            entity.Status = parsedStatus;
            return null;
        }

        if (req.IsPublic.HasValue)
        {
            entity.Status = req.IsPublic.Value ? ReviewStatus.Published : ReviewStatus.Hidden;
            return null;
        }

        if (req.IsApproved.HasValue)
        {
            entity.Status = req.IsApproved.Value ? ReviewStatus.Published : ReviewStatus.Pending;
        }

        return null;
    }

    private async Task<HotelReview?> FindReviewForWriteAsync(Guid id, CancellationToken ct)
    {
        var tenantId = _tenant.TenantId!.Value;
        return await _db.HotelReviews.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);
    }

    private void RequireAdminWriteTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Admin write requires switched tenant context (X-TenantId).");
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static bool TryParseReviewStatus(string? raw, out ReviewStatus status)
    {
        status = default;
        return !string.IsNullOrWhiteSpace(raw)
            && Enum.TryParse(raw.Trim(), true, out status);
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AdminHotelReviewDetailDto MapDetail(HotelReview x) => new()
    {
        Id = x.Id,
        TenantId = x.TenantId,
        HotelId = x.HotelId,
        BookingId = x.BookingId,
        CustomerUserId = x.CustomerUserId,
        Rating = x.Rating,
        Title = x.Title,
        Content = x.Content,
        ReviewerName = null,
        Status = x.Status.ToString(),
        IsApproved = x.Status == ReviewStatus.Published,
        IsPublic = x.Status == ReviewStatus.Published,
        ModerationNote = null,
        ReplyContent = null,
        ReplyAt = null,
        IsVerifiedStay = x.IsVerifiedStay,
        HelpfulCount = x.HelpfulCount,
        MetadataJson = x.MetadataJson,
        IsDeleted = x.IsDeleted,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt,
        RowVersionBase64 = x.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
    };
}

public sealed class AdminHotelReviewPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminHotelReviewListItemDto> Items { get; set; } = new();
}

public sealed class AdminHotelReviewListItemDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public Guid? CustomerUserId { get; set; }
    public decimal Rating { get; set; }
    public string? Title { get; set; }
    public string? ReviewerName { get; set; }
    public string Status { get; set; } = "";
    public bool? IsApproved { get; set; }
    public bool? IsPublic { get; set; }
    public bool HasReply { get; set; }
    public bool IsVerifiedStay { get; set; }
    public int HelpfulCount { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AdminHotelReviewDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? CustomerUserId { get; set; }
    public decimal Rating { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ReviewerName { get; set; }
    public string Status { get; set; } = "";
    public bool? IsApproved { get; set; }
    public bool? IsPublic { get; set; }
    public string? ModerationNote { get; set; }
    public string? ReplyContent { get; set; }
    public DateTimeOffset? ReplyAt { get; set; }
    public bool IsVerifiedStay { get; set; }
    public int HelpfulCount { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminUpdateHotelReviewRequest
{
    public decimal? Rating { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ReviewerName { get; set; }
    public bool? IsApproved { get; set; }
    public bool? IsPublic { get; set; }
    public string? Status { get; set; }
    public string? ReplyContent { get; set; }
    public bool? IsVerifiedStay { get; set; }
    public int? HelpfulCount { get; set; }
    public string? MetadataJson { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminReplyHotelReviewRequest
{
    public string? ReplyContent { get; set; }
}

public sealed class AdminRejectHotelReviewRequest
{
    public string? Reason { get; set; }
}
