// FILE #226: TicketBooking.Api/Controllers/Hotels/HotelGalleryController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Hotels;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Hotels;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hotels")]
public sealed class HotelGalleryController : ControllerBase
{
    private readonly AppDbContext _db;

    public HotelGalleryController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{hotelId:guid}/gallery")]
    public async Task<ActionResult<PublicHotelGalleryResponse>> GetByHotelId(
        Guid hotelId,
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

        return Ok(await BuildResponseAsync(hotel, ct));
    }

    [HttpGet("slug/{slug}/gallery")]
    public async Task<ActionResult<PublicHotelGalleryResponse>> GetBySlug(
        string slug,
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

        return Ok(await BuildResponseAsync(hotel, ct));
    }

    private async Task<PublicHotelGalleryResponse> BuildResponseAsync(Hotel hotel, CancellationToken ct)
    {
        var hotelImages = await _db.HotelImages
            .AsNoTracking()
            .Where(x => x.TenantId == hotel.TenantId &&
                        x.HotelId == hotel.Id &&
                        !x.IsDeleted &&
                        x.IsActive)
            .ToListAsync(ct);

        var roomTypes = await _db.RoomTypes
            .AsNoTracking()
            .Where(x => x.TenantId == hotel.TenantId &&
                        x.HotelId == hotel.Id &&
                        !x.IsDeleted &&
                        x.IsActive &&
                        x.Status == RoomTypeStatus.Active)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        var roomTypeIds = roomTypes.Select(x => x.Id).ToList();

        var roomTypeImages = await _db.RoomTypeImages
            .AsNoTracking()
            .Where(x => x.TenantId == hotel.TenantId &&
                        roomTypeIds.Contains(x.RoomTypeId) &&
                        !x.IsDeleted &&
                        x.IsActive)
            .ToListAsync(ct);

        var hotelGallery = hotelImages
            .OrderByDescending(x => x.Kind == ImageKind.Cover)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .Select(x => new PublicGalleryImageDto
            {
                Id = x.Id,
                MediaAssetId = x.MediaAssetId,
                Url = x.ImageUrl,
                Caption = x.Title,
                AltText = x.AltText,
                Title = x.Title,
                IsPrimary = x.Kind == ImageKind.Cover,
                SortOrder = x.SortOrder
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Url))
            .ToList();

        var roomTypeGalleries = roomTypes
            .Select(roomType =>
            {
                var images = roomTypeImages
                    .Where(x => x.RoomTypeId == roomType.Id)
                    .OrderByDescending(x => x.Kind == ImageKind.Cover)
                    .ThenBy(x => x.SortOrder)
                    .ThenBy(x => x.CreatedAt)
                    .Select(x => new PublicGalleryImageDto
                    {
                        Id = x.Id,
                        MediaAssetId = x.MediaAssetId,
                        Url = x.ImageUrl,
                        Caption = x.Title,
                        AltText = x.AltText,
                        Title = x.Title,
                        IsPrimary = x.Kind == ImageKind.Cover,
                        SortOrder = x.SortOrder
                    })
                    .Where(x => !string.IsNullOrWhiteSpace(x.Url))
                    .ToList();

                return new PublicRoomTypeGalleryDto
                {
                    RoomTypeId = roomType.Id,
                    RoomTypeCode = roomType.Code,
                    RoomTypeName = roomType.Name,
                    CoverImageUrl = roomType.CoverImageUrl,
                    Images = images
                };
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.CoverImageUrl) || x.Images.Count > 0)
            .ToList();

        return new PublicHotelGalleryResponse
        {
            Hotel = new PublicHotelGalleryHotelDto
            {
                Id = hotel.Id,
                TenantId = hotel.TenantId,
                Code = hotel.Code,
                Name = hotel.Name,
                Slug = hotel.Slug,
                City = hotel.City,
                Province = hotel.Province,
                CountryCode = hotel.CountryCode,
                CoverImageUrl = hotel.CoverImageUrl
            },
            HotelImages = hotelGallery,
            RoomTypes = roomTypeGalleries
        };
    }

}

public sealed class PublicHotelGalleryResponse
{
    public PublicHotelGalleryHotelDto Hotel { get; set; } = new();
    public List<PublicGalleryImageDto> HotelImages { get; set; } = new();
    public List<PublicRoomTypeGalleryDto> RoomTypes { get; set; } = new();
}

public sealed class PublicHotelGalleryHotelDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public string? CoverImageUrl { get; set; }
}

public sealed class PublicRoomTypeGalleryDto
{
    public Guid RoomTypeId { get; set; }
    public string RoomTypeCode { get; set; } = "";
    public string RoomTypeName { get; set; } = "";
    public string? CoverImageUrl { get; set; }
    public List<PublicGalleryImageDto> Images { get; set; } = new();
}

public sealed class PublicGalleryImageDto
{
    public Guid Id { get; set; }
    public Guid? MediaAssetId { get; set; }
    public string? Url { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public bool IsPrimary { get; set; }
    public int? SortOrder { get; set; }
}
