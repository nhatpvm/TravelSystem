// FILE #267: TicketBooking.Api/Controllers/Tours/TourFaqsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tours/{tourId:guid}/faqs")]
[AllowAnonymous]
public sealed class TourFaqsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TourFaqsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<TourPublicFaqPagedResponse>> List(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] TourFaqType? type = null,
        [FromQuery] bool highlightedOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var tour = await _db.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == tourId &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == TourStatus.Active, ct);

        if (tour is null)
            return NotFound(new { message = "Tour not found." });

        IQueryable<TourFaq> query = _db.TourFaqs
            .AsNoTracking()
            .Where(x =>
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (highlightedOnly)
            query = query.Where(x => x.IsHighlighted);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Question.Contains(qq) ||
                x.AnswerMarkdown.Contains(qq) ||
                (x.AnswerHtml != null && x.AnswerHtml.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.IsHighlighted)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Type)
            .ThenBy(x => x.Question)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TourPublicFaqListItemDto
            {
                Id = x.Id,
                Question = x.Question,
                AnswerMarkdown = x.AnswerMarkdown,
                AnswerHtml = x.AnswerHtml,
                Type = x.Type,
                IsHighlighted = x.IsHighlighted,
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct);

        var summary = await _db.TourFaqs
            .AsNoTracking()
            .Where(x =>
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted)
            .GroupBy(x => x.TourId)
            .Select(g => new TourPublicFaqSummaryDto
            {
                TotalCount = g.Count(),
                HighlightedCount = g.Count(x => x.IsHighlighted),
                GeneralCount = g.Count(x => x.Type == TourFaqType.General),
                BookingCount = g.Count(x => x.Type == TourFaqType.Booking),
                CancellationCount = g.Count(x => x.Type == TourFaqType.Cancellation),
                PickupDropoffCount = g.Count(x => x.Type == TourFaqType.PickupDropoff),
                PaymentCount = g.Count(x => x.Type == TourFaqType.Payment),
                OtherCount = g.Count(x => x.Type == TourFaqType.Other)
            })
            .FirstOrDefaultAsync(ct) ?? new TourPublicFaqSummaryDto();

        return Ok(new TourPublicFaqPagedResponse
        {
            TourId = tour.Id,
            TourName = tour.Name,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Summary = summary,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourPublicFaqDetailDto>> GetById(
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

        var entity = await _db.TourFaqs
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (entity is null)
            return NotFound(new { message = "FAQ not found." });

        return Ok(new TourPublicFaqDetailDto
        {
            Id = entity.Id,
            TourId = entity.TourId,
            Question = entity.Question,
            AnswerMarkdown = entity.AnswerMarkdown,
            AnswerHtml = entity.AnswerHtml,
            Type = entity.Type,
            IsHighlighted = entity.IsHighlighted,
            SortOrder = entity.SortOrder
        });
    }
}

public sealed class TourPublicFaqPagedResponse
{
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public TourPublicFaqSummaryDto Summary { get; set; } = new();
    public List<TourPublicFaqListItemDto> Items { get; set; } = new();
}

public sealed class TourPublicFaqSummaryDto
{
    public int TotalCount { get; set; }
    public int HighlightedCount { get; set; }
    public int GeneralCount { get; set; }
    public int BookingCount { get; set; }
    public int CancellationCount { get; set; }
    public int PickupDropoffCount { get; set; }
    public int PaymentCount { get; set; }
    public int OtherCount { get; set; }
}

public sealed class TourPublicFaqListItemDto
{
    public Guid Id { get; set; }
    public string Question { get; set; } = "";
    public string AnswerMarkdown { get; set; } = "";
    public string? AnswerHtml { get; set; }
    public TourFaqType Type { get; set; }
    public bool IsHighlighted { get; set; }
    public int SortOrder { get; set; }
}

public sealed class TourPublicFaqDetailDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string Question { get; set; } = "";
    public string AnswerMarkdown { get; set; } = "";
    public string? AnswerHtml { get; set; }
    public TourFaqType Type { get; set; }
    public bool IsHighlighted { get; set; }
    public int SortOrder { get; set; }
}
