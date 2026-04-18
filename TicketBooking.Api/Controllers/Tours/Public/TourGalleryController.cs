// FILE #265: TicketBooking.Api/Controllers/Tours/TourGalleryController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tours/{tourId:guid}/gallery")]
[AllowAnonymous]
public sealed class TourGalleryController : ControllerBase
{
    private readonly AppDbContext _db;

    public TourGalleryController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<TourGalleryResponse>> GetGallery(
        Guid tourId,
        [FromQuery] bool featuredOnly = false,
        [FromQuery] bool includePrimary = true,
        [FromQuery] bool includeCover = true,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        limit = limit < 1 ? 1 : limit;
        limit = limit > 500 ? 500 : limit;

        var tour = await _db.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == tourId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (tour is null)
            return NotFound(new { message = "Tour not found." });

        IQueryable<Domain.Tours.TourImage> query = _db.TourImages
            .AsNoTracking()
            .Where(x =>
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted);

        if (featuredOnly)
            query = query.Where(x => x.IsFeatured);

        if (!includePrimary)
            query = query.Where(x => !x.IsPrimary);

        if (!includeCover)
            query = query.Where(x => !x.IsCover);

        var items = await query
            .OrderByDescending(x => x.IsCover)
            .ThenByDescending(x => x.IsPrimary)
            .ThenByDescending(x => x.IsFeatured)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .Take(limit)
            .Select(x => new TourGalleryItemDto
            {
                Id = x.Id,
                MediaAssetId = x.MediaAssetId,
                ImageUrl = x.ImageUrl,
                Caption = x.Caption,
                AltText = x.AltText,
                Title = x.Title,
                IsPrimary = x.IsPrimary,
                IsCover = x.IsCover,
                IsFeatured = x.IsFeatured,
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct);

        return Ok(new TourGalleryResponse
        {
            TourId = tour.Id,
            TourName = tour.Name,
            CoverImageUrl = tour.CoverImageUrl,
            Total = items.Count,
            Items = items
        });
    }
}

public sealed class TourGalleryResponse
{
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public string? CoverImageUrl { get; set; }
    public int Total { get; set; }
    public List<TourGalleryItemDto> Items { get; set; } = new();
}

public sealed class TourGalleryItemDto
{
    public Guid Id { get; set; }
    public Guid? MediaAssetId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsCover { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
}
