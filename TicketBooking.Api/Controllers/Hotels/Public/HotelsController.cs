// FILE #213: TicketBooking.Api/Controllers/Hotels/HotelsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Hotels;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Hotels;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hotels")]
public sealed class HotelsController : ControllerBase
{
    private readonly AppDbContext _db;

    public HotelsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<PublicHotelPagedResponse<PublicHotelListItemDto>>> List(
        [FromQuery] string? tenantCode = null,
        [FromQuery] Guid? tenantId = null,
        [FromQuery] string? q = null,
        [FromQuery] string? city = null,
        [FromQuery] string? province = null,
        [FromQuery] int? starMin = null,
        [FromQuery] int? starMax = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var resolvedTenantId = tenantId;

        if (!resolvedTenantId.HasValue && !string.IsNullOrWhiteSpace(tenantCode))
        {
            resolvedTenantId = await _db.Tenants.IgnoreQueryFilters()
                .Where(x => x.Code == tenantCode && !x.IsDeleted)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(ct);

            if (!resolvedTenantId.HasValue)
            {
                return Ok(new PublicHotelPagedResponse<PublicHotelListItemDto>
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = 0,
                    Items = new()
                });
            }
        }

        IQueryable<Hotel> query = _db.Hotels
            .AsNoTracking()
            .Where(x => !x.IsDeleted &&
                        x.IsActive &&
                        x.Status == HotelStatus.Active);

        if (resolvedTenantId.HasValue)
            query = query.Where(x => x.TenantId == resolvedTenantId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(q) ||
                x.Name.Contains(q) ||
                (x.Slug != null && x.Slug.Contains(q)) ||
                (x.City != null && x.City.Contains(q)) ||
                (x.Province != null && x.Province.Contains(q)) ||
                (x.AddressLine != null && x.AddressLine.Contains(q)) ||
                (x.ShortDescription != null && x.ShortDescription.Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            city = city.Trim();
            query = query.Where(x => x.City != null && x.City.Contains(city));
        }

        if (!string.IsNullOrWhiteSpace(province))
        {
            province = province.Trim();
            query = query.Where(x => x.Province != null && x.Province.Contains(province));
        }

        if (starMin.HasValue)
            query = query.Where(x => x.StarRating >= starMin.Value);

        if (starMax.HasValue)
            query = query.Where(x => x.StarRating <= starMax.Value);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(x => x.StarRating)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PublicHotelListItemDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                Code = x.Code,
                Name = x.Name,
                Slug = x.Slug,
                City = x.City,
                Province = x.Province,
                CountryCode = x.CountryCode,
                AddressLine = x.AddressLine,
                StarRating = x.StarRating,
                ShortDescription = x.ShortDescription,
                CoverImageUrl = x.CoverImageUrl,
                DefaultCheckInTime = x.DefaultCheckInTime,
                DefaultCheckOutTime = x.DefaultCheckOutTime
            })
            .ToListAsync(ct);

        return Ok(new PublicHotelPagedResponse<PublicHotelListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PublicHotelDetailDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var hotel = await _db.Hotels
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == HotelStatus.Active, ct);

        if (hotel is null)
            return NotFound(new { message = "Hotel not found." });

        return Ok(await BuildDetailAsync(hotel, ct));
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<PublicHotelDetailDto>> GetBySlug(string slug, CancellationToken ct = default)
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

        return Ok(await BuildDetailAsync(hotel, ct));
    }

    private async Task<PublicHotelDetailDto> BuildDetailAsync(Hotel hotel, CancellationToken ct)
    {
        var roomTypes = await _db.RoomTypes
            .AsNoTracking()
            .Where(x => x.TenantId == hotel.TenantId &&
                        x.HotelId == hotel.Id &&
                        !x.IsDeleted &&
                        x.IsActive &&
                        x.Status == RoomTypeStatus.Active)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new PublicHotelRoomTypeDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                DescriptionMarkdown = x.DescriptionMarkdown,
                DescriptionHtml = x.DescriptionHtml,
                AreaSquareMeters = x.AreaSquareMeters,
                HasBalcony = x.HasBalcony,
                HasWindow = x.HasWindow,
                SmokingAllowed = x.SmokingAllowed,
                DefaultAdults = x.DefaultAdults,
                DefaultChildren = x.DefaultChildren,
                MaxAdults = x.MaxAdults,
                MaxChildren = x.MaxChildren,
                MaxGuests = x.MaxGuests,
                CoverImageUrl = x.CoverImageUrl
            })
            .ToListAsync(ct);

        var extraServices = await _db.ExtraServices
            .AsNoTracking()
            .Where(x => x.TenantId == hotel.TenantId &&
                        x.HotelId == hotel.Id &&
                        !x.IsDeleted &&
                        x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new PublicHotelExtraServiceDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                Description = x.Description
            })
            .ToListAsync(ct);

        return new PublicHotelDetailDto
        {
            Id = hotel.Id,
            TenantId = hotel.TenantId,
            Code = hotel.Code,
            Name = hotel.Name,
            Slug = hotel.Slug,
            LocationId = hotel.LocationId,
            AddressLine = hotel.AddressLine,
            City = hotel.City,
            Province = hotel.Province,
            CountryCode = hotel.CountryCode,
            Latitude = hotel.Latitude,
            Longitude = hotel.Longitude,
            TimeZone = hotel.TimeZone,
            ShortDescription = hotel.ShortDescription,
            DescriptionMarkdown = hotel.DescriptionMarkdown,
            DescriptionHtml = hotel.DescriptionHtml,
            StarRating = hotel.StarRating,
            DefaultCheckInTime = hotel.DefaultCheckInTime,
            DefaultCheckOutTime = hotel.DefaultCheckOutTime,
            Phone = hotel.Phone,
            Email = hotel.Email,
            WebsiteUrl = hotel.WebsiteUrl,
            CoverImageUrl = hotel.CoverImageUrl,
            RoomTypes = roomTypes,
            ExtraServices = extraServices
        };
    }
}

public sealed class PublicHotelPagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}

public sealed class PublicHotelListItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public string? AddressLine { get; set; }
    public int StarRating { get; set; }
    public string? ShortDescription { get; set; }
    public string? CoverImageUrl { get; set; }
    public TimeOnly? DefaultCheckInTime { get; set; }
    public TimeOnly? DefaultCheckOutTime { get; set; }
}

public sealed class PublicHotelDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Slug { get; set; }

    public Guid? LocationId { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? TimeZone { get; set; }

    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }

    public int StarRating { get; set; }

    public TimeOnly? DefaultCheckInTime { get; set; }
    public TimeOnly? DefaultCheckOutTime { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? CoverImageUrl { get; set; }

    public List<PublicHotelRoomTypeDto> RoomTypes { get; set; } = new();
    public List<PublicHotelExtraServiceDto> ExtraServices { get; set; } = new();
}

public sealed class PublicHotelRoomTypeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }

    public int? AreaSquareMeters { get; set; }
    public bool? HasBalcony { get; set; }
    public bool? HasWindow { get; set; }
    public bool? SmokingAllowed { get; set; }

    public int DefaultAdults { get; set; }
    public int DefaultChildren { get; set; }
    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }
    public int MaxGuests { get; set; }

    public string? CoverImageUrl { get; set; }
}

public sealed class PublicHotelExtraServiceDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public ExtraServiceType Type { get; set; }
    public string? Description { get; set; }
}

