// FILE #262: TicketBooking.Api/Controllers/Tours/ToursController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tours")]
[AllowAnonymous]
public sealed class ToursController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TourLocalTimeService _tourLocalTimeService;

    public ToursController(AppDbContext db, TourLocalTimeService tourLocalTimeService)
    {
        _db = db;
        _tourLocalTimeService = tourLocalTimeService;
    }

    [HttpGet]
    public async Task<ActionResult<ToursPagedResponse>> List(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] string? q = null,
        [FromQuery] TourType? type = null,
        [FromQuery] TourDifficulty? difficulty = null,
        [FromQuery] string? province = null,
        [FromQuery] string? city = null,
        [FromQuery] int? durationDays = null,
        [FromQuery] bool? instantConfirm = null,
        [FromQuery] bool? privateTourSupported = null,
        [FromQuery] DateOnly? departureFrom = null,
        [FromQuery] DateOnly? departureTo = null,
        [FromQuery] decimal? maxAdultPrice = null,
        [FromQuery] bool featuredOnly = false,
        [FromQuery] bool upcomingOnly = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime);

        IQueryable<Tour> query = _db.Tours
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == TourStatus.Active &&
                _db.TourPackages.Any(p =>
                    p.TourId == x.Id &&
                    p.IsActive &&
                    !p.IsDeleted &&
                    p.Status == TourPackageStatus.Active));

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            query = query.Where(x => x.TenantId == tenantId.Value);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (difficulty.HasValue)
            query = query.Where(x => x.Difficulty == difficulty.Value);

        if (!string.IsNullOrWhiteSpace(province))
        {
            var p = province.Trim();
            query = query.Where(x => x.Province != null && x.Province.Contains(p));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var c = city.Trim();
            query = query.Where(x => x.City != null && x.City.Contains(c));
        }

        if (durationDays.HasValue)
            query = query.Where(x => x.DurationDays == durationDays.Value);

        if (instantConfirm.HasValue)
            query = query.Where(x => x.IsInstantConfirm == instantConfirm.Value);

        if (privateTourSupported.HasValue)
            query = query.Where(x => x.IsPrivateTourSupported == privateTourSupported.Value);

        if (featuredOnly)
            query = query.Where(x => x.IsFeatured || x.IsFeaturedOnHome);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(qq) ||
                x.Name.Contains(qq) ||
                x.Slug.Contains(qq) ||
                (x.City != null && x.City.Contains(qq)) ||
                (x.Province != null && x.Province.Contains(qq)) ||
                (x.ShortDescription != null && x.ShortDescription.Contains(qq)));
        }

        if (upcomingOnly || departureFrom.HasValue || departureTo.HasValue || maxAdultPrice.HasValue)
        {
            var effectiveFrom = departureFrom ?? (upcomingOnly ? today : (DateOnly?)null);

            query = query.Where(tour =>
                _db.TourSchedules.Any(s =>
                    s.TourId == tour.Id &&
                    s.IsActive &&
                    !s.IsDeleted &&
                    s.Status == TourScheduleStatus.Open &&
                    (!effectiveFrom.HasValue || s.DepartureDate >= effectiveFrom.Value) &&
                    (!departureTo.HasValue || s.DepartureDate <= departureTo.Value) &&
                    (!maxAdultPrice.HasValue ||
                     _db.TourSchedulePrices.Any(p =>
                         p.TourScheduleId == s.Id &&
                         p.IsActive &&
                         !p.IsDeleted &&
                         p.PriceType == TourPriceType.Adult &&
                         p.Price <= maxAdultPrice.Value))));
        }

        var total = await query.CountAsync(ct);

        var tours = await query
            .OrderByDescending(x => x.IsFeaturedOnHome)
            .ThenByDescending(x => x.IsFeatured)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var tourIds = tours.Select(x => x.Id).ToList();

        var upcomingSchedulesQuery = _db.TourSchedules
            .AsNoTracking()
            .Where(x =>
                tourIds.Contains(x.TourId) &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == TourScheduleStatus.Open);

        if (departureFrom.HasValue || upcomingOnly)
            upcomingSchedulesQuery = upcomingSchedulesQuery.Where(x => x.DepartureDate >= (departureFrom ?? today));

        if (departureTo.HasValue)
            upcomingSchedulesQuery = upcomingSchedulesQuery.Where(x => x.DepartureDate <= departureTo.Value);

        var schedules = await upcomingSchedulesQuery
            .OrderBy(x => x.DepartureDate)
            .ThenBy(x => x.DepartureTime)
            .ToListAsync(ct);

        var scheduleIds = schedules.Select(x => x.Id).ToList();

        var capacities = await _db.TourScheduleCapacities
            .AsNoTracking()
            .Where(x => scheduleIds.Contains(x.TourScheduleId) && x.IsActive && !x.IsDeleted)
            .ToDictionaryAsync(x => x.TourScheduleId, ct);

        var adultPrices = await _db.TourSchedulePrices
            .AsNoTracking()
            .Where(x =>
                scheduleIds.Contains(x.TourScheduleId) &&
                x.IsActive &&
                !x.IsDeleted &&
                x.PriceType == TourPriceType.Adult)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Price)
            .ToListAsync(ct);

        var adultPriceBySchedule = adultPrices
            .GroupBy(x => x.TourScheduleId)
            .ToDictionary(
                g => g.Key,
                g => TourPricingResolver.ResolveDisplayPrice(g, TourPriceType.Adult));

        var nextScheduleByTour = schedules
            .GroupBy(x => x.TourId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    foreach (var s in g.OrderBy(x => x.DepartureDate).ThenBy(x => x.DepartureTime))
                    {
                        capacities.TryGetValue(s.Id, out var cap);
                        if (cap is null || cap.AvailableSlots > 0 || cap.AllowWaitlist)
                            return s;
                    }

                    return g.OrderBy(x => x.DepartureDate).ThenBy(x => x.DepartureTime).First();
                });

        var reviewStats = await _db.TourReviews
            .AsNoTracking()
            .Where(x =>
                tourIds.Contains(x.TourId) &&
                !x.IsDeleted &&
                x.IsApproved &&
                x.IsPublic)
            .GroupBy(x => x.TourId)
            .Select(g => new
            {
                TourId = g.Key,
                Count = g.Count(),
                Average = g.Average(x => x.Rating)
            })
            .ToDictionaryAsync(x => x.TourId, ct);

        var items = tours.Select(tour =>
        {
            nextScheduleByTour.TryGetValue(tour.Id, out var nextSchedule);

            TourScheduleCapacity? nextCap = null;
            TourSchedulePrice? nextAdultPrice = null;

            if (nextSchedule is not null)
            {
                capacities.TryGetValue(nextSchedule.Id, out nextCap);
                adultPriceBySchedule.TryGetValue(nextSchedule.Id, out nextAdultPrice);
            }

            reviewStats.TryGetValue(tour.Id, out var reviewStat);

            return new TourListItemDto
            {
                Id = tour.Id,
                TenantId = tour.TenantId,
                Code = tour.Code,
                Name = tour.Name,
                Slug = tour.Slug,
                Type = tour.Type,
                Difficulty = tour.Difficulty,
                DurationDays = tour.DurationDays,
                DurationNights = tour.DurationNights,
                CountryCode = tour.CountryCode,
                Province = tour.Province,
                City = tour.City,
                ShortDescription = tour.ShortDescription,
                CoverImageUrl = tour.CoverImageUrl,
                CurrencyCode = nextAdultPrice?.CurrencyCode ?? tour.CurrencyCode,
                IsFeatured = tour.IsFeatured,
                IsFeaturedOnHome = tour.IsFeaturedOnHome,
                IsInstantConfirm = tour.IsInstantConfirm,
                IsPrivateTourSupported = tour.IsPrivateTourSupported,
                NextDepartureDate = nextSchedule?.DepartureDate,
                NextDepartureTime = nextSchedule?.DepartureTime,
                NextScheduleId = nextSchedule?.Id,
                AvailableSlots = nextCap?.AvailableSlots,
                FromAdultPrice = nextAdultPrice?.Price,
                OriginalAdultPrice = nextAdultPrice?.OriginalPrice,
                ReviewCount = reviewStat?.Count ?? 0,
                ReviewAverage = reviewStat?.Average
            };
        }).ToList();

        return Ok(new ToursPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourDetailDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var tour = await _db.Tours
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == TourStatus.Active, ct);

        if (tour is null)
            return NotFound(new { message = "Tour not found." });

        var currentTime = await _tourLocalTimeService.ResolveCurrentTimeAsync(tour.PrimaryLocationId, ct);
        var today = DateOnly.FromDateTime(currentTime.DateTime);

        var schedules = await _db.TourSchedules
            .AsNoTracking()
            .Where(x =>
                x.TourId == id &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == TourScheduleStatus.Open &&
                x.DepartureDate >= today)
            .OrderBy(x => x.DepartureDate)
            .ThenBy(x => x.DepartureTime)
            .Take(10)
            .ToListAsync(ct);

        var scheduleIds = schedules.Select(x => x.Id).ToList();

        var capacities = await _db.TourScheduleCapacities
            .AsNoTracking()
            .Where(x => scheduleIds.Contains(x.TourScheduleId) && x.IsActive && !x.IsDeleted)
            .ToDictionaryAsync(x => x.TourScheduleId, ct);

        var adultPrices = await _db.TourSchedulePrices
            .AsNoTracking()
            .Where(x =>
                scheduleIds.Contains(x.TourScheduleId) &&
                x.IsActive &&
                !x.IsDeleted &&
                x.PriceType == TourPriceType.Adult)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Price)
            .ToListAsync(ct);

        var adultPriceBySchedule = adultPrices
            .GroupBy(x => x.TourScheduleId)
            .ToDictionary(
                g => g.Key,
                g => TourPricingResolver.ResolveDisplayPrice(g, TourPriceType.Adult));

        var images = await _db.TourImages
            .AsNoTracking()
            .Where(x => x.TourId == id && x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.IsCover)
            .ThenByDescending(x => x.IsPrimary)
            .ThenByDescending(x => x.IsFeatured)
            .ThenBy(x => x.SortOrder)
            .Select(x => new TourImageDto
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

        var policies = await _db.TourPolicies
            .AsNoTracking()
            .Where(x => x.TourId == id && x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.IsHighlighted)
            .ThenBy(x => x.SortOrder)
            .Select(x => new TourPolicyDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                ShortDescription = x.ShortDescription,
                DescriptionMarkdown = x.DescriptionMarkdown,
                DescriptionHtml = x.DescriptionHtml,
                PolicyJson = x.PolicyJson,
                IsHighlighted = x.IsHighlighted,
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct);

        var addons = await _db.TourAddons
            .AsNoTracking()
            .Where(x => x.TourId == id && x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.IsRequired)
            .ThenByDescending(x => x.IsDefaultSelected)
            .ThenBy(x => x.SortOrder)
            .Select(x => new TourAddonDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                ShortDescription = x.ShortDescription,
                CurrencyCode = x.CurrencyCode,
                BasePrice = x.BasePrice,
                OriginalPrice = x.OriginalPrice,
                IsPerPerson = x.IsPerPerson,
                IsRequired = x.IsRequired,
                AllowQuantitySelection = x.AllowQuantitySelection,
                MinQuantity = x.MinQuantity,
                MaxQuantity = x.MaxQuantity,
                IsDefaultSelected = x.IsDefaultSelected,
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct);

        var pickupPoints = await _db.TourPickupPoints
            .AsNoTracking()
            .Where(x => x.TourId == id && x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.SortOrder)
            .Select(x => new TourPointDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                AddressLine = x.AddressLine,
                Ward = x.Ward,
                District = x.District,
                Province = x.Province,
                CountryCode = x.CountryCode,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                Time = x.PickupTime,
                IsDefault = x.IsDefault,
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct);

        var dropoffPoints = await _db.TourDropoffPoints
            .AsNoTracking()
            .Where(x => x.TourId == id && x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.SortOrder)
            .Select(x => new TourPointDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                AddressLine = x.AddressLine,
                Ward = x.Ward,
                District = x.District,
                Province = x.Province,
                CountryCode = x.CountryCode,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                Time = x.DropoffTime,
                IsDefault = x.IsDefault,
                SortOrder = x.SortOrder
            })
            .ToListAsync(ct);

        var reviewSummary = await _db.TourReviews
            .AsNoTracking()
            .Where(x =>
                x.TourId == id &&
                !x.IsDeleted &&
                x.IsApproved &&
                x.IsPublic)
            .GroupBy(x => x.TourId)
            .Select(g => new TourReviewSummaryDto
            {
                Count = g.Count(),
                Average = g.Average(x => x.Rating)
            })
            .FirstOrDefaultAsync(ct) ?? new TourReviewSummaryDto();

        return Ok(new TourDetailDto
        {
            Id = tour.Id,
            TenantId = tour.TenantId,
            Code = tour.Code,
            Name = tour.Name,
            Slug = tour.Slug,
            Type = tour.Type,
            Difficulty = tour.Difficulty,
            DurationDays = tour.DurationDays,
            DurationNights = tour.DurationNights,
            MinGuests = tour.MinGuests,
            MaxGuests = tour.MaxGuests,
            MinAge = tour.MinAge,
            MaxAge = tour.MaxAge,
            CountryCode = tour.CountryCode,
            Province = tour.Province,
            City = tour.City,
            MeetingPointSummary = tour.MeetingPointSummary,
            ShortDescription = tour.ShortDescription,
            DescriptionMarkdown = tour.DescriptionMarkdown,
            DescriptionHtml = tour.DescriptionHtml,
            HighlightsJson = tour.HighlightsJson,
            IncludesJson = tour.IncludesJson,
            ExcludesJson = tour.ExcludesJson,
            TermsJson = tour.TermsJson,
            CoverImageUrl = tour.CoverImageUrl,
            CurrencyCode = tour.CurrencyCode,
            IsFeatured = tour.IsFeatured,
            IsFeaturedOnHome = tour.IsFeaturedOnHome,
            IsInstantConfirm = tour.IsInstantConfirm,
            IsPrivateTourSupported = tour.IsPrivateTourSupported,
            Images = images,
            Policies = policies,
            Addons = addons,
            PickupPoints = pickupPoints,
            DropoffPoints = dropoffPoints,
            ReviewSummary = reviewSummary,
            UpcomingSchedules = schedules.Select(s =>
            {
                capacities.TryGetValue(s.Id, out var cap);
                adultPriceBySchedule.TryGetValue(s.Id, out var adultPrice);

                return new TourSchedulePublicDto
                {
                    Id = s.Id,
                    Code = s.Code,
                    Name = s.Name,
                    DepartureDate = s.DepartureDate,
                    ReturnDate = s.ReturnDate,
                    DepartureTime = s.DepartureTime,
                    ReturnTime = s.ReturnTime,
                    BookingOpenAt = s.BookingOpenAt,
                    BookingCutoffAt = s.BookingCutoffAt,
                    IsGuaranteedDeparture = s.IsGuaranteedDeparture,
                    IsInstantConfirm = s.IsInstantConfirm,
                    AvailableSlots = cap?.AvailableSlots,
                    AllowWaitlist = cap?.AllowWaitlist,
                    AdultPrice = adultPrice?.Price,
                    OriginalAdultPrice = adultPrice?.OriginalPrice,
                    CurrencyCode = adultPrice?.CurrencyCode ?? tour.CurrencyCode
                };
            }).ToList()
        });
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<TourDetailDto>> GetBySlug(
        string slug,
        [FromQuery] Guid? tenantId = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest(new { message = "Slug is required." });

        var query = _db.Tours
            .AsNoTracking()
            .Where(x =>
                x.Slug == slug.Trim() &&
                x.IsActive &&
                !x.IsDeleted &&
                x.Status == TourStatus.Active);

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            query = query.Where(x => x.TenantId == tenantId.Value);

        var matches = await query
            .Select(x => new { x.Id, x.TenantId })
            .Take(2)
            .ToListAsync(ct);

        if (matches.Count == 0)
            return NotFound(new { message = "Tour not found." });

        if (!tenantId.HasValue && matches.Count > 1)
        {
            return Conflict(new
            {
                message = "Slug exists in multiple tenants. Please provide tenantId."
            });
        }

        return await GetById(matches[0].Id, ct);
    }

}

public sealed class ToursPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TourListItemDto> Items { get; set; } = new();
}

public sealed class TourListItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public TourType Type { get; set; }
    public TourDifficulty Difficulty { get; set; }
    public int DurationDays { get; set; }
    public int DurationNights { get; set; }
    public string? CountryCode { get; set; }
    public string? Province { get; set; }
    public string? City { get; set; }
    public string? ShortDescription { get; set; }
    public string? CoverImageUrl { get; set; }
    public string CurrencyCode { get; set; } = "";
    public bool IsFeatured { get; set; }
    public bool IsFeaturedOnHome { get; set; }
    public bool IsInstantConfirm { get; set; }
    public bool IsPrivateTourSupported { get; set; }
    public Guid? NextScheduleId { get; set; }
    public DateOnly? NextDepartureDate { get; set; }
    public TimeOnly? NextDepartureTime { get; set; }
    public int? AvailableSlots { get; set; }
    public decimal? FromAdultPrice { get; set; }
    public decimal? OriginalAdultPrice { get; set; }
    public int ReviewCount { get; set; }
    public decimal? ReviewAverage { get; set; }
}

public sealed class TourDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public TourType Type { get; set; }
    public TourDifficulty Difficulty { get; set; }
    public int DurationDays { get; set; }
    public int DurationNights { get; set; }
    public int? MinGuests { get; set; }
    public int? MaxGuests { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public string? CountryCode { get; set; }
    public string? Province { get; set; }
    public string? City { get; set; }
    public string? MeetingPointSummary { get; set; }
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? HighlightsJson { get; set; }
    public string? IncludesJson { get; set; }
    public string? ExcludesJson { get; set; }
    public string? TermsJson { get; set; }
    public string? CoverImageUrl { get; set; }
    public string CurrencyCode { get; set; } = "";
    public bool IsFeatured { get; set; }
    public bool IsFeaturedOnHome { get; set; }
    public bool IsInstantConfirm { get; set; }
    public bool IsPrivateTourSupported { get; set; }
    public List<TourImageDto> Images { get; set; } = new();
    public List<TourPolicyDto> Policies { get; set; } = new();
    public List<TourAddonDto> Addons { get; set; } = new();
    public List<TourPointDto> PickupPoints { get; set; } = new();
    public List<TourPointDto> DropoffPoints { get; set; } = new();
    public TourReviewSummaryDto ReviewSummary { get; set; } = new();
    public List<TourSchedulePublicDto> UpcomingSchedules { get; set; } = new();
}

public sealed class TourSchedulePublicDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string? Name { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly ReturnDate { get; set; }
    public TimeOnly? DepartureTime { get; set; }
    public TimeOnly? ReturnTime { get; set; }
    public DateTimeOffset? BookingOpenAt { get; set; }
    public DateTimeOffset? BookingCutoffAt { get; set; }
    public bool IsGuaranteedDeparture { get; set; }
    public bool IsInstantConfirm { get; set; }
    public int? AvailableSlots { get; set; }
    public bool? AllowWaitlist { get; set; }
    public decimal? AdultPrice { get; set; }
    public decimal? OriginalAdultPrice { get; set; }
    public string CurrencyCode { get; set; } = "";
}

public sealed class TourImageDto
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

public sealed class TourPolicyDto
{
    public Guid Id { get; set; }
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

public sealed class TourAddonDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourAddonType Type { get; set; }
    public string? ShortDescription { get; set; }
    public string CurrencyCode { get; set; } = "";
    public decimal BasePrice { get; set; }
    public decimal? OriginalPrice { get; set; }
    public bool IsPerPerson { get; set; }
    public bool IsRequired { get; set; }
    public bool AllowQuantitySelection { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public bool IsDefaultSelected { get; set; }
    public int SortOrder { get; set; }
}

public sealed class TourPointDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public TimeOnly? Time { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
}

public sealed class TourReviewSummaryDto
{
    public int Count { get; set; }
    public decimal? Average { get; set; }
}
