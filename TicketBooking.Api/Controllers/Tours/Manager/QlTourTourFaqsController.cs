// FILE #257: TicketBooking.Api/Controllers/Tours/QlTourTourFaqsController.cs
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
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/faqs")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class QlTourTourFaqsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QlTourTourFaqsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<QlTourFaqPagedResponse>> List(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] TourFaqType? type = null,
        [FromQuery] bool? highlighted = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : pageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        IQueryable<TourFaq> query = includeDeleted
            ? _db.TourFaqs.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourFaqs.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (highlighted.HasValue)
            query = query.Where(x => x.IsHighlighted == highlighted.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Question.Contains(qq) ||
                x.AnswerMarkdown.Contains(qq) ||
                (x.AnswerHtml != null && x.AnswerHtml.Contains(qq)) ||
                (x.Notes != null && x.Notes.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(x => x.IsHighlighted)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Type)
            .ThenBy(x => x.Question)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QlTourFaqListItemDto
            {
                Id = x.Id,
                TourId = x.TourId,
                Question = x.Question,
                Type = x.Type,
                IsHighlighted = x.IsHighlighted,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new QlTourFaqPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QlTourFaqDetailDto>> GetById(
        Guid tourId,
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        IQueryable<TourFaq> query = includeDeleted
            ? _db.TourFaqs.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourFaqs.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        var entity = await query
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour FAQ not found in current tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<QlTourCreateFaqResponse>> Create(
        Guid tourId,
        [FromBody] QlTourCreateFaqRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        ValidatePayload(req.Question, req.AnswerMarkdown, req.Notes);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = new TourFaq
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tourId,
            Question = req.Question.Trim(),
            AnswerMarkdown = req.AnswerMarkdown.Trim(),
            AnswerHtml = NullIfWhiteSpace(req.AnswerHtml),
            Type = req.Type,
            IsHighlighted = req.IsHighlighted ?? false,
            SortOrder = req.SortOrder ?? 0,
            Notes = NullIfWhiteSpace(req.Notes),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.TourFaqs.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", tourId, id = entity.Id },
            new QlTourCreateFaqResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid tourId,
        Guid id,
        [FromBody] QlTourUpdateFaqRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourFaqs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour FAQ not found in current tenant." });

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
            req.Question ?? entity.Question,
            req.AnswerMarkdown ?? entity.AnswerMarkdown,
            req.Notes);

        if (req.Question is not null) entity.Question = req.Question.Trim();
        if (req.AnswerMarkdown is not null) entity.AnswerMarkdown = req.AnswerMarkdown.Trim();
        if (req.AnswerHtml is not null) entity.AnswerHtml = NullIfWhiteSpace(req.AnswerHtml);
        if (req.Type.HasValue) entity.Type = req.Type.Value;
        if (req.IsHighlighted.HasValue) entity.IsHighlighted = req.IsHighlighted.Value;
        if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Tour FAQ was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, id, true, ct);

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, id, false, ct);

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, id, true, ct);

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, id, false, ct);

    [HttpPost("{id:guid}/highlight")]
    public async Task<IActionResult> Highlight(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleHighlighted(tourId, id, true, ct);

    [HttpPost("{id:guid}/unhighlight")]
    public async Task<IActionResult> Unhighlight(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleHighlighted(tourId, id, false, ct);

    private async Task<IActionResult> ToggleDeleted(Guid tourId, Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourFaqs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour FAQ not found in current tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid tourId, Guid id, bool isActive, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourFaqs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour FAQ not found in current tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleHighlighted(Guid tourId, Guid id, bool isHighlighted, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourFaqs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour FAQ not found in current tenant." });

        entity.IsHighlighted = isHighlighted;
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
            throw new KeyNotFoundException("Tour not found in current tenant.");
    }

    private static void ValidatePayload(string question, string answerMarkdown, string? notes)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question is required.");

        if (string.IsNullOrWhiteSpace(answerMarkdown))
            throw new ArgumentException("AnswerMarkdown is required.");

        if (question.Trim().Length > 1000)
            throw new ArgumentException("Question max length is 1000.");

        if (notes is not null && notes.Length > 2000)
            throw new ArgumentException("Notes max length is 2000.");
    }

    private Guid RequireTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            throw new InvalidOperationException("QLTour operations require tenant context.");

        return _tenant.TenantId.Value;
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static QlTourFaqDetailDto MapDetail(TourFaq x)
    {
        return new QlTourFaqDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            TourId = x.TourId,
            Question = x.Question,
            AnswerMarkdown = x.AnswerMarkdown,
            AnswerHtml = x.AnswerHtml,
            Type = x.Type,
            IsHighlighted = x.IsHighlighted,
            SortOrder = x.SortOrder,
            Notes = x.Notes,
            MetadataJson = x.MetadataJson,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            CreatedByUserId = x.CreatedByUserId,
            UpdatedAt = x.UpdatedAt,
            UpdatedByUserId = x.UpdatedByUserId,
            RowVersionBase64 = x.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
        };
    }
}

public sealed class QlTourFaqPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<QlTourFaqListItemDto> Items { get; set; } = new();
}

public sealed class QlTourFaqListItemDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string Question { get; set; } = "";
    public TourFaqType Type { get; set; }
    public bool IsHighlighted { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class QlTourFaqDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public string Question { get; set; } = "";
    public string AnswerMarkdown { get; set; } = "";
    public string? AnswerHtml { get; set; }
    public TourFaqType Type { get; set; }
    public bool IsHighlighted { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreateFaqRequest
{
    public string Question { get; set; } = "";
    public string AnswerMarkdown { get; set; } = "";
    public string? AnswerHtml { get; set; }
    public TourFaqType Type { get; set; } = TourFaqType.General;
    public bool? IsHighlighted { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class QlTourUpdateFaqRequest
{
    public string? Question { get; set; }
    public string? AnswerMarkdown { get; set; }
    public string? AnswerHtml { get; set; }
    public TourFaqType? Type { get; set; }
    public bool? IsHighlighted { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreateFaqResponse
{
    public Guid Id { get; set; }
}
