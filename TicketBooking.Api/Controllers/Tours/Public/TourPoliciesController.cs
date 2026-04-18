// FILE #270: TicketBooking.Api/Controllers/Tours/TourPoliciesController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;
namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tours/{tourId:guid}/policies")]
[AllowAnonymous]
public sealed class TourPoliciesController : ControllerBase
{
    private readonly AppDbContext _db;
    public TourPoliciesController(AppDbContext db)
    {
        _db = db;
    }
    [HttpGet]
    public async Task<ActionResult<TourPublicPolicyPagedResponse>> List(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] TourPolicyType? type = null,
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
        IQueryable<TourPolicy> query = _db.TourPolicies
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
                x.Code.Contains(qq) ||
                x.Name.Contains(qq) ||
                (x.ShortDescription != null && x.ShortDescription.Contains(qq)) ||
                (x.DescriptionMarkdown != null && x.DescriptionMarkdown.Contains(qq)) ||
                (x.DescriptionHtml != null && x.DescriptionHtml.Contains(qq)));
        }
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.IsHighlighted)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Type)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new TourPublicPolicyListItemDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                ShortDescription = x.ShortDescription,
                IsHighlighted = x.IsHighlighted,
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct);
        return Ok(new TourPublicPolicyPagedResponse
        {
            TourId = tour.Id,
            TourName = tour.Name,
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourPublicPolicyDetailDto>> GetById(
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
        var entity = await _db.TourPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted, ct);
        if (entity is null)
            return NotFound(new { message = "Policy not found." });
        return Ok(new TourPublicPolicyDetailDto
        {
            Id = entity.Id,
            TourId = entity.TourId,
            Code = entity.Code,
            Name = entity.Name,
            Type = entity.Type,
            ShortDescription = entity.ShortDescription,
            DescriptionMarkdown = entity.DescriptionMarkdown,
            DescriptionHtml = entity.DescriptionHtml,
            PolicyJson = entity.PolicyJson,
            IsHighlighted = entity.IsHighlighted,
            SortOrder = entity.SortOrder
        });
    }
}
public sealed class TourPublicPolicyPagedResponse
{
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TourPublicPolicyListItemDto> Items { get; set; } = new();
}
public sealed class TourPublicPolicyListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPolicyType Type { get; set; }
    public string? ShortDescription { get; set; }
    public bool IsHighlighted { get; set; }
    public int SortOrder { get; set; }
}
public sealed class TourPublicPolicyDetailDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourPolicyType Type { get; set; }
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? PolicyJson { get; set; }
    public bool IsHighlighted { get; set; }
    public int SortOrder { get; set; }
}

