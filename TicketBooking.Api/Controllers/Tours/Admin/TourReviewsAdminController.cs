// FILE #260: TicketBooking.Api/Controllers/Tours/TourReviewsAdminController.cs
using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/tour-reviews")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class TourReviewsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public TourReviewsAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<TourReviewsAdminPagedResponse>> List(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] Guid? tourId = null,
        [FromQuery] string? q = null,
        [FromQuery] TourReviewStatus? status = null,
        [FromQuery] bool? isApproved = null,
        [FromQuery] bool? isPublic = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : pageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        IQueryable<TourReview> query = includeDeleted
            ? _db.TourReviews.IgnoreQueryFilters()
            : _db.TourReviews.Where(x => !x.IsDeleted);

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            query = query.Where(x => x.TenantId == tenantId.Value);

        if (tourId.HasValue && tourId.Value != Guid.Empty)
            query = query.Where(x => x.TourId == tourId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (isApproved.HasValue)
            query = query.Where(x => x.IsApproved == isApproved.Value);

        if (isPublic.HasValue)
            query = query.Where(x => x.IsPublic == isPublic.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                (x.Title != null && x.Title.Contains(qq)) ||
                (x.Content != null && x.Content.Contains(qq)) ||
                (x.ReviewerName != null && x.ReviewerName.Contains(qq)) ||
                (x.ModerationNote != null && x.ModerationNote.Contains(qq)) ||
                (x.ReplyContent != null && x.ReplyContent.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderBy(x => x.TenantId)
            .ThenBy(x => x.TourId)
            .ThenByDescending(x => x.PublishedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TourReviewsAdminListItemDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                TourId = x.TourId,
                Rating = x.Rating,
                Title = x.Title,
                ReviewerName = x.ReviewerName,
                Status = x.Status,
                IsApproved = x.IsApproved,
                IsPublic = x.IsPublic,
                PublishedAt = x.PublishedAt,
                ApprovedAt = x.ApprovedAt,
                ReplyAt = x.ReplyAt,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new TourReviewsAdminPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourReviewsAdminDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        IQueryable<TourReview> query = includeDeleted
            ? _db.TourReviews.IgnoreQueryFilters()
            : _db.TourReviews.Where(x => !x.IsDeleted);

        var entity = await query
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour review not found." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<TourReviewsAdminCreateResponse>> Create(
        [FromBody] TourReviewsAdminCreateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireWriteTenant();

        if (req.TourId == Guid.Empty)
            return BadRequest(new { message = "TourId is required." });

        await EnsureTourExistsAsync(tenantId, req.TourId, ct);
        ValidatePayload(req.Rating, req.Title, req.Content, req.ReviewerName, req.ModerationNote, req.ReplyContent);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = new TourReview
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = req.TourId,
            Rating = req.Rating,
            Title = NullIfWhiteSpace(req.Title),
            Content = NullIfWhiteSpace(req.Content),
            ReviewerName = NullIfWhiteSpace(req.ReviewerName),
            Status = req.Status ?? TourReviewStatus.Pending,
            IsApproved = req.IsApproved ?? false,
            IsPublic = req.IsPublic ?? true,
            ModerationNote = NullIfWhiteSpace(req.ModerationNote),
            ReplyContent = NullIfWhiteSpace(req.ReplyContent),
            ReplyAt = !string.IsNullOrWhiteSpace(req.ReplyContent) ? (req.ReplyAt ?? now) : req.ReplyAt,
            ReplyByUserId = !string.IsNullOrWhiteSpace(req.ReplyContent) ? (req.ReplyByUserId ?? userId) : req.ReplyByUserId,
            PublishedAt = req.PublishedAt,
            ApprovedAt = req.IsApproved == true ? (req.ApprovedAt ?? now) : req.ApprovedAt,
            ApprovedByUserId = req.IsApproved == true ? (req.ApprovedByUserId ?? userId) : req.ApprovedByUserId,
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        NormalizeReviewState(entity, userId, now);

        _db.TourReviews.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new TourReviewsAdminCreateResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] TourReviewsAdminUpdateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.TourReviews.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour review not found in current switched tenant." });

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

        ValidatePayload(
            req.Rating ?? entity.Rating,
            req.Title ?? entity.Title,
            req.Content ?? entity.Content,
            req.ReviewerName ?? entity.ReviewerName,
            req.ModerationNote,
            req.ReplyContent);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        if (req.Rating.HasValue) entity.Rating = req.Rating.Value;
        if (req.Title is not null) entity.Title = NullIfWhiteSpace(req.Title);
        if (req.Content is not null) entity.Content = NullIfWhiteSpace(req.Content);
        if (req.ReviewerName is not null) entity.ReviewerName = NullIfWhiteSpace(req.ReviewerName);
        if (req.Status.HasValue) entity.Status = req.Status.Value;
        if (req.IsApproved.HasValue) entity.IsApproved = req.IsApproved.Value;
        if (req.IsPublic.HasValue) entity.IsPublic = req.IsPublic.Value;
        if (req.ModerationNote is not null) entity.ModerationNote = NullIfWhiteSpace(req.ModerationNote);
        if (req.ReplyContent is not null) entity.ReplyContent = NullIfWhiteSpace(req.ReplyContent);
        if (req.ReplyAt.HasValue) entity.ReplyAt = req.ReplyAt;
        if (req.ReplyByUserId.HasValue) entity.ReplyByUserId = req.ReplyByUserId;
        if (req.PublishedAt.HasValue) entity.PublishedAt = req.PublishedAt;
        if (req.ApprovedAt.HasValue) entity.ApprovedAt = req.ApprovedAt;
        if (req.ApprovedByUserId.HasValue) entity.ApprovedByUserId = req.ApprovedByUserId;
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        NormalizeReviewState(entity, userId, now);

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Tour review was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromBody] TourReviewsAdminApproveRequest? req,
        CancellationToken ct = default)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.TourReviews.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour review not found in current switched tenant." });

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        entity.IsApproved = true;
        entity.Status = TourReviewStatus.Approved;
        entity.ApprovedAt = req?.ApprovedAt ?? now;
        entity.ApprovedByUserId = req?.ApprovedByUserId ?? userId;
        entity.PublishedAt ??= req?.PublishedAt ?? now;
        entity.IsPublic = req?.IsPublic ?? entity.IsPublic;
        entity.ModerationNote = req?.ModerationNote is not null ? NullIfWhiteSpace(req.ModerationNote) : entity.ModerationNote;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] TourReviewsAdminRejectRequest? req,
        CancellationToken ct = default)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.TourReviews.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour review not found in current switched tenant." });

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        entity.IsApproved = false;
        entity.Status = TourReviewStatus.Rejected;
        entity.IsPublic = false;
        entity.ModerationNote = req?.ModerationNote is not null ? NullIfWhiteSpace(req.ModerationNote) : entity.ModerationNote;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/hide")]
    public async Task<IActionResult> Hide(Guid id, CancellationToken ct = default)
        => await ChangeVisibility(id, false, TourReviewStatus.Hidden, ct);

    [HttpPost("{id:guid}/public")]
    public async Task<IActionResult> MakePublic(Guid id, CancellationToken ct = default)
        => await ChangeVisibility(id, true, null, ct);

    [HttpPost("{id:guid}/reply")]
    public async Task<IActionResult> Reply(
        Guid id,
        [FromBody] TourReviewsAdminReplyRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireWriteTenant();

        if (string.IsNullOrWhiteSpace(req.ReplyContent))
            return BadRequest(new { message = "ReplyContent is required." });

        if (req.ReplyContent.Length > 4000)
            return BadRequest(new { message = "ReplyContent max length is 4000." });

        var entity = await _db.TourReviews.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour review not found in current switched tenant." });

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        entity.ReplyContent = req.ReplyContent.Trim();
        entity.ReplyAt = req.ReplyAt ?? now;
        entity.ReplyByUserId = req.ReplyByUserId ?? userId;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        => await ToggleDeleted(id, true, ct);

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
        => await ToggleDeleted(id, false, ct);

    private async Task<IActionResult> ToggleDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.TourReviews.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour review not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ChangeVisibility(
        Guid id,
        bool isPublic,
        TourReviewStatus? status,
        CancellationToken ct)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.TourReviews.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour review not found in current switched tenant." });

        entity.IsPublic = isPublic;
        if (status.HasValue) entity.Status = status.Value;
        if (isPublic && entity.IsApproved && entity.PublishedAt is null)
            entity.PublishedAt = DateTimeOffset.Now;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task EnsureTourExistsAsync(Guid tenantId, Guid tourId, CancellationToken ct)
    {
        var exists = await _db.Tours.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == tourId && !x.IsDeleted, ct);

        if (!exists)
            throw new KeyNotFoundException("Tour not found in current switched tenant.");
    }

    private static void ValidatePayload(
        decimal rating,
        string? title,
        string? content,
        string? reviewerName,
        string? moderationNote,
        string? replyContent)
    {
        if (rating < 0 || rating > 5)
            throw new ArgumentException("Rating must be between 0 and 5.");

        if (title is not null && title.Length > 300)
            throw new ArgumentException("Title max length is 300.");

        if (content is not null && content.Length > 4000)
            throw new ArgumentException("Content max length is 4000.");

        if (reviewerName is not null && reviewerName.Length > 200)
            throw new ArgumentException("ReviewerName max length is 200.");

        if (moderationNote is not null && moderationNote.Length > 2000)
            throw new ArgumentException("ModerationNote max length is 2000.");

        if (replyContent is not null && replyContent.Length > 4000)
            throw new ArgumentException("ReplyContent max length is 4000.");
    }

    private static void NormalizeReviewState(TourReview entity, Guid? currentUserId, DateTimeOffset now)
    {
        if (entity.IsApproved)
        {
            entity.Status = TourReviewStatus.Approved;
            entity.ApprovedAt ??= now;
            entity.ApprovedByUserId ??= currentUserId;
            if (entity.IsPublic)
                entity.PublishedAt ??= now;
        }
        else
        {
            if (entity.Status == TourReviewStatus.Approved)
                entity.Status = TourReviewStatus.Pending;

            if (!entity.IsPublic)
                entity.PublishedAt = null;
        }

        if (!string.IsNullOrWhiteSpace(entity.ReplyContent))
        {
            entity.ReplyAt ??= now;
            entity.ReplyByUserId ??= currentUserId;
        }
        else
        {
            entity.ReplyAt = null;
            entity.ReplyByUserId = null;
        }
    }

    private Guid RequireWriteTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue || _tenant.TenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Admin write operations require switched tenant context.");

        return _tenant.TenantId.Value;
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TourReviewsAdminDetailDto MapDetail(TourReview x)
    {
        return new TourReviewsAdminDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            TourId = x.TourId,
            Rating = x.Rating,
            Title = x.Title,
            Content = x.Content,
            ReviewerName = x.ReviewerName,
            Status = x.Status,
            IsApproved = x.IsApproved,
            IsPublic = x.IsPublic,
            ModerationNote = x.ModerationNote,
            ReplyContent = x.ReplyContent,
            ReplyAt = x.ReplyAt,
            ReplyByUserId = x.ReplyByUserId,
            PublishedAt = x.PublishedAt,
            ApprovedAt = x.ApprovedAt,
            ApprovedByUserId = x.ApprovedByUserId,
            MetadataJson = x.MetadataJson,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            CreatedByUserId = x.CreatedByUserId,
            UpdatedAt = x.UpdatedAt,
            UpdatedByUserId = x.UpdatedByUserId,
            RowVersionBase64 = x.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
        };
    }
}

public sealed class TourReviewsAdminPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TourReviewsAdminListItemDto> Items { get; set; } = new();
}

public sealed class TourReviewsAdminListItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public decimal Rating { get; set; }
    public string? Title { get; set; }
    public string? ReviewerName { get; set; }
    public TourReviewStatus Status { get; set; }
    public bool IsApproved { get; set; }
    public bool IsPublic { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? ReplyAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class TourReviewsAdminDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public decimal Rating { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ReviewerName { get; set; }
    public TourReviewStatus Status { get; set; }
    public bool IsApproved { get; set; }
    public bool IsPublic { get; set; }
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
    public string? RowVersionBase64 { get; set; }
}

public sealed class TourReviewsAdminCreateRequest
{
    public Guid TourId { get; set; }
    public decimal Rating { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ReviewerName { get; set; }
    public TourReviewStatus? Status { get; set; }
    public bool? IsApproved { get; set; }
    public bool? IsPublic { get; set; }
    public string? ModerationNote { get; set; }
    public string? ReplyContent { get; set; }
    public DateTimeOffset? ReplyAt { get; set; }
    public Guid? ReplyByUserId { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class TourReviewsAdminUpdateRequest
{
    public decimal? Rating { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? ReviewerName { get; set; }
    public TourReviewStatus? Status { get; set; }
    public bool? IsApproved { get; set; }
    public bool? IsPublic { get; set; }
    public string? ModerationNote { get; set; }
    public string? ReplyContent { get; set; }
    public DateTimeOffset? ReplyAt { get; set; }
    public Guid? ReplyByUserId { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class TourReviewsAdminApproveRequest
{
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public bool? IsPublic { get; set; }
    public string? ModerationNote { get; set; }
}

public sealed class TourReviewsAdminRejectRequest
{
    public string? ModerationNote { get; set; }
}

public sealed class TourReviewsAdminReplyRequest
{
    public string ReplyContent { get; set; } = "";
    public DateTimeOffset? ReplyAt { get; set; }
    public Guid? ReplyByUserId { get; set; }
}

public sealed class TourReviewsAdminCreateResponse
{
    public Guid Id { get; set; }
}
