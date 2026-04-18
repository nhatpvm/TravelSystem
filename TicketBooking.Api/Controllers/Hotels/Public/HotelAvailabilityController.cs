// FILE #214: TicketBooking.Api/Controllers/Hotels/HotelAvailabilityController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Hotels;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Hotels;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hotels")]
public sealed class HotelAvailabilityController : ControllerBase
{
    private readonly AppDbContext _db;

    public HotelAvailabilityController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{hotelId:guid}/availability")]
    public async Task<ActionResult<HotelAvailabilityResponse>> GetByHotelId(
        Guid hotelId,
        [FromQuery] DateOnly checkInDate,
        [FromQuery] DateOnly checkOutDate,
        [FromQuery] int adults = 1,
        [FromQuery] int children = 0,
        [FromQuery] int rooms = 1,
        CancellationToken ct = default)
    {
        var validation = ValidateQuery(checkInDate, checkOutDate, adults, children, rooms);
        if (validation is not null)
            return validation;

        var hotel = await _db.Hotels
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == hotelId &&
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == HotelStatus.Active, ct);

        if (hotel is null)
            return NotFound(new { message = "Hotel not found." });

        var hotelDateValidation = ValidateHotelLocalDates(hotel, checkInDate);
        if (hotelDateValidation is not null)
            return hotelDateValidation;

        return Ok(await BuildResponseAsync(hotel, checkInDate, checkOutDate, adults, children, rooms, ct));
    }

    [HttpGet("slug/{slug}/availability")]
    public async Task<ActionResult<HotelAvailabilityResponse>> GetBySlug(
        string slug,
        [FromQuery] DateOnly checkInDate,
        [FromQuery] DateOnly checkOutDate,
        [FromQuery] int adults = 1,
        [FromQuery] int children = 0,
        [FromQuery] int rooms = 1,
        CancellationToken ct = default)
    {
        var validation = ValidateQuery(checkInDate, checkOutDate, adults, children, rooms);
        if (validation is not null)
            return validation;

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

        var hotelDateValidation = ValidateHotelLocalDates(hotel, checkInDate);
        if (hotelDateValidation is not null)
            return hotelDateValidation;

        return Ok(await BuildResponseAsync(hotel, checkInDate, checkOutDate, adults, children, rooms, ct));
    }

    private async Task<HotelAvailabilityResponse> BuildResponseAsync(
        Hotel hotel,
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int adults,
        int children,
        int rooms,
        CancellationToken ct)
    {
        var stayDates = EachNight(checkInDate, checkOutDate).ToList();
        var nights = stayDates.Count;

        var today = GetHotelLocalDate(hotel.TimeZone);
        var advanceDays = checkInDate.DayNumber - today.DayNumber;

        var roomTypes = await _db.RoomTypes
            .AsNoTracking()
            .Where(x =>
                x.TenantId == hotel.TenantId &&
                x.HotelId == hotel.Id &&
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == RoomTypeStatus.Active)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(ct);

        var roomTypeIds = roomTypes.Select(x => x.Id).ToList();

        var occupancyRules = await _db.RoomTypeOccupancyRules
            .AsNoTracking()
            .Where(x =>
                x.TenantId == hotel.TenantId &&
                roomTypeIds.Contains(x.RoomTypeId) &&
                !x.IsDeleted &&
                x.IsActive)
            .ToListAsync(ct);

        var inventories = await _db.RoomTypeInventories
            .AsNoTracking()
            .Where(x =>
                x.TenantId == hotel.TenantId &&
                roomTypeIds.Contains(x.RoomTypeId) &&
                !x.IsDeleted &&
                x.Status == InventoryStatus.Open &&
                stayDates.Contains(x.Date))
            .ToListAsync(ct);

        var ratePlans = await _db.RatePlans
            .AsNoTracking()
            .Where(x =>
                x.TenantId == hotel.TenantId &&
                x.HotelId == hotel.Id &&
                !x.IsDeleted &&
                x.IsActive &&
                x.Status == RatePlanStatus.Active)
            .ToListAsync(ct);

        var ratePlanIds = ratePlans.Select(x => x.Id).ToList();

        var mappings = await _db.RatePlanRoomTypes
            .AsNoTracking()
            .Where(x =>
                x.TenantId == hotel.TenantId &&
                ratePlanIds.Contains(x.RatePlanId) &&
                roomTypeIds.Contains(x.RoomTypeId) &&
                !x.IsDeleted &&
                x.IsActive)
            .ToListAsync(ct);

        var mappingIds = mappings.Select(x => x.Id).ToList();

        var dailyRates = await _db.DailyRates
            .AsNoTracking()
            .Where(x =>
                x.TenantId == hotel.TenantId &&
                mappingIds.Contains(x.RatePlanRoomTypeId) &&
                !x.IsDeleted &&
                x.IsActive &&
                stayDates.Contains(x.Date))
            .ToListAsync(ct);

        var roomTypeResults = new List<HotelAvailabilityRoomTypeDto>();

        foreach (var roomType in roomTypes)
        {
            if (!PassesOccupancy(roomType, occupancyRules, adults, children, rooms))
                continue;

            var roomInventories = inventories
                .Where(x => x.RoomTypeId == roomType.Id)
                .ToList();

            if (roomInventories.Count != nights)
                continue;

            var availablePerNight = roomInventories
                .Select(x => x.TotalUnits - x.SoldUnits - x.HeldUnits)
                .ToList();

            if (availablePerNight.Any(x => x < rooms))
                continue;

            if (roomInventories.Any(x => x.MinNights.HasValue && nights < x.MinNights.Value))
                continue;

            if (roomInventories.Any(x => x.MaxNights.HasValue && nights > x.MaxNights.Value))
                continue;

            var roomMappings = mappings
                .Where(x => x.RoomTypeId == roomType.Id)
                .ToList();

            var options = new List<HotelAvailabilityRatePlanOptionDto>();

            foreach (var mapping in roomMappings)
            {
                var ratePlan = ratePlans.FirstOrDefault(x => x.Id == mapping.RatePlanId);
                if (ratePlan is null)
                    continue;

                if (ratePlan.MinNights.HasValue && nights < ratePlan.MinNights.Value)
                    continue;

                if (ratePlan.MaxNights.HasValue && nights > ratePlan.MaxNights.Value)
                    continue;

                if (ratePlan.MinAdvanceDays.HasValue && advanceDays < ratePlan.MinAdvanceDays.Value)
                    continue;

                if (ratePlan.MaxAdvanceDays.HasValue && advanceDays > ratePlan.MaxAdvanceDays.Value)
                    continue;

                var nightlyBreakdown = new List<HotelAvailabilityNightRateDto>();
                decimal totalPrice = 0m;
                decimal totalBasePrice = 0m;
                decimal totalTaxes = 0m;
                decimal totalFees = 0m;
                string currencyCode = string.IsNullOrWhiteSpace(mapping.CurrencyCode) ? "VND" : mapping.CurrencyCode;

                var hasInvalidNight = false;

                foreach (var date in stayDates)
                {
                    var daily = dailyRates.FirstOrDefault(x =>
                        x.RatePlanRoomTypeId == mapping.Id &&
                        x.Date == date);

                    var perRoomNightlyPrice = daily?.Price ?? mapping.BasePrice;
                    if (!perRoomNightlyPrice.HasValue)
                    {
                        hasInvalidNight = true;
                        break;
                    }

                    var perRoomNightlyBase = daily?.BasePrice ?? mapping.BasePrice ?? perRoomNightlyPrice.Value;
                    var perRoomNightlyTaxes = daily?.Taxes ?? 0m;
                    var perRoomNightlyFees = daily?.Fees ?? 0m;

                    if (!string.IsNullOrWhiteSpace(daily?.CurrencyCode))
                        currencyCode = daily!.CurrencyCode;

                    var nightlyPrice = perRoomNightlyPrice.Value * rooms;
                    var nightlyBase = perRoomNightlyBase * rooms;
                    var nightlyTaxes = perRoomNightlyTaxes * rooms;
                    var nightlyFees = perRoomNightlyFees * rooms;

                    totalPrice += nightlyPrice;
                    totalBasePrice += nightlyBase;
                    totalTaxes += nightlyTaxes;
                    totalFees += nightlyFees;

                    nightlyBreakdown.Add(new HotelAvailabilityNightRateDto
                    {
                        Date = date,
                        Price = nightlyPrice,
                        BasePrice = nightlyBase,
                        Taxes = nightlyTaxes,
                        Fees = nightlyFees,
                        CurrencyCode = currencyCode
                    });
                }

                if (hasInvalidNight)
                    continue;

                options.Add(new HotelAvailabilityRatePlanOptionDto
                {
                    RatePlanId = ratePlan.Id,
                    RatePlanCode = ratePlan.Code,
                    RatePlanName = ratePlan.Name,
                    Refundable = ratePlan.Refundable,
                    BreakfastIncluded = ratePlan.BreakfastIncluded,
                    RequiresGuarantee = ratePlan.RequiresGuarantee,
                    CurrencyCode = currencyCode,
                    NightCount = nights,
                    TotalBasePrice = totalBasePrice,
                    TotalTaxes = totalTaxes,
                    TotalFees = totalFees,
                    TotalPrice = totalPrice,
                    NightlyRates = nightlyBreakdown
                });
            }

            if (options.Count == 0)
                continue;

            roomTypeResults.Add(new HotelAvailabilityRoomTypeDto
            {
                RoomTypeId = roomType.Id,
                RoomTypeCode = roomType.Code,
                RoomTypeName = roomType.Name,
                AreaSquareMeters = roomType.AreaSquareMeters,
                DefaultAdults = roomType.DefaultAdults,
                DefaultChildren = roomType.DefaultChildren,
                MaxAdults = roomType.MaxAdults,
                MaxChildren = roomType.MaxChildren,
                MaxGuests = roomType.MaxGuests,
                AvailableUnits = availablePerNight.Min(),
                CoverImageUrl = roomType.CoverImageUrl,
                Options = options
                    .OrderBy(x => x.TotalPrice)
                    .ThenBy(x => x.RatePlanName)
                    .ToList()
            });
        }

        return new HotelAvailabilityResponse
        {
            Hotel = new HotelAvailabilityHotelDto
            {
                Id = hotel.Id,
                TenantId = hotel.TenantId,
                Code = hotel.Code,
                Name = hotel.Name,
                Slug = hotel.Slug,
                AddressLine = hotel.AddressLine,
                City = hotel.City,
                Province = hotel.Province,
                CountryCode = hotel.CountryCode,
                StarRating = hotel.StarRating,
                CoverImageUrl = hotel.CoverImageUrl,
                DefaultCheckInTime = hotel.DefaultCheckInTime,
                DefaultCheckOutTime = hotel.DefaultCheckOutTime
            },
            Query = new HotelAvailabilityQueryDto
            {
                CheckInDate = checkInDate,
                CheckOutDate = checkOutDate,
                NightCount = nights,
                Adults = adults,
                Children = children,
                Rooms = rooms
            },
            RoomTypes = roomTypeResults
        };
    }

    private static bool PassesOccupancy(
        RoomType roomType,
        List<RoomTypeOccupancyRule> rules,
        int adults,
        int children,
        int rooms)
    {
        if (rooms < 1)
            return false;

        var allowedPairs = BuildAllowedOccupancies(roomType, rules);
        if (allowedPairs.Count == 0)
            return false;

        var totalGuests = adults + children;
        var minAdults = allowedPairs.Min(x => x.Adults) * rooms;
        var maxAdults = allowedPairs.Max(x => x.Adults) * rooms;
        var minChildren = allowedPairs.Min(x => x.Children) * rooms;
        var maxChildren = allowedPairs.Max(x => x.Children) * rooms;
        var minGuests = allowedPairs.Min(x => x.Adults + x.Children) * rooms;
        var maxGuests = allowedPairs.Max(x => x.Adults + x.Children) * rooms;

        if (adults < minAdults || adults > maxAdults)
            return false;

        if (children < minChildren || children > maxChildren)
            return false;

        if (totalGuests < minGuests || totalGuests > maxGuests)
            return false;

        var reachable = new bool[adults + 1, children + 1];
        reachable[0, 0] = true;

        for (var roomIndex = 0; roomIndex < rooms; roomIndex++)
        {
            var next = new bool[adults + 1, children + 1];

            for (var adultCount = 0; adultCount <= adults; adultCount++)
            {
                for (var childCount = 0; childCount <= children; childCount++)
                {
                    if (!reachable[adultCount, childCount])
                        continue;

                    foreach (var pair in allowedPairs)
                    {
                        var nextAdults = adultCount + pair.Adults;
                        var nextChildren = childCount + pair.Children;

                        if (nextAdults <= adults && nextChildren <= children)
                            next[nextAdults, nextChildren] = true;
                    }
                }
            }

            reachable = next;
        }

        return reachable[adults, children];
    }

    private static List<(int Adults, int Children)> BuildAllowedOccupancies(
        RoomType roomType,
        List<RoomTypeOccupancyRule> rules)
    {
        var allowedPairs = new HashSet<(int Adults, int Children)>();
        var roomRules = rules
            .Where(x => x.RoomTypeId == roomType.Id)
            .ToList();

        if (roomRules.Count > 0)
        {
            foreach (var rule in roomRules)
            {
                for (var adults = rule.MinAdults; adults <= rule.MaxAdults; adults++)
                {
                    for (var children = rule.MinChildren; children <= rule.MaxChildren; children++)
                    {
                        var guests = adults + children;
                        if (guests == 0)
                            continue;

                        if (guests < rule.MinGuests || guests > rule.MaxGuests)
                            continue;

                        allowedPairs.Add((adults, children));
                    }
                }
            }
        }
        else
        {
            for (var adults = 0; adults <= roomType.MaxAdults; adults++)
            {
                for (var children = 0; children <= roomType.MaxChildren; children++)
                {
                    var guests = adults + children;
                    if (guests == 0 || guests > roomType.MaxGuests)
                        continue;

                    allowedPairs.Add((adults, children));
                }
            }
        }

        return allowedPairs.ToList();
    }

    private static ActionResult? ValidateQuery(
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int adults,
        int children,
        int rooms)
    {
        if (checkOutDate <= checkInDate)
            return new BadRequestObjectResult(new { message = "CheckOutDate must be greater than CheckInDate." });

        if (adults < 1)
            return new BadRequestObjectResult(new { message = "Adults must be at least 1." });

        if (children < 0)
            return new BadRequestObjectResult(new { message = "Children cannot be negative." });

        if (rooms < 1)
            return new BadRequestObjectResult(new { message = "Rooms must be at least 1." });

        if (checkOutDate.DayNumber - checkInDate.DayNumber > 30)
            return new BadRequestObjectResult(new { message = "Stay cannot exceed 30 nights." });

        return null;
    }

    private static ActionResult? ValidateHotelLocalDates(Hotel hotel, DateOnly checkInDate)
    {
        var hotelLocalDate = GetHotelLocalDate(hotel.TimeZone);
        if (checkInDate < hotelLocalDate)
        {
            return new BadRequestObjectResult(new
            {
                message = $"CheckInDate cannot be earlier than the hotel's current local date ({hotelLocalDate:yyyy-MM-dd})."
            });
        }

        return null;
    }

    private static IEnumerable<DateOnly> EachNight(DateOnly checkInDate, DateOnly checkOutDate)
    {
        for (var d = checkInDate; d < checkOutDate; d = d.AddDays(1))
            yield return d;
    }

    private static DateOnly GetHotelLocalDate(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return DateOnly.FromDateTime(DateTime.UtcNow);
    }
}

public sealed class HotelAvailabilityResponse
{
    public HotelAvailabilityHotelDto Hotel { get; set; } = new();
    public HotelAvailabilityQueryDto Query { get; set; } = new();
    public List<HotelAvailabilityRoomTypeDto> RoomTypes { get; set; } = new();
}

public sealed class HotelAvailabilityHotelDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public int StarRating { get; set; }
    public string? CoverImageUrl { get; set; }
    public TimeOnly? DefaultCheckInTime { get; set; }
    public TimeOnly? DefaultCheckOutTime { get; set; }
}

public sealed class HotelAvailabilityQueryDto
{
    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int NightCount { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public int Rooms { get; set; }
}

public sealed class HotelAvailabilityRoomTypeDto
{
    public Guid RoomTypeId { get; set; }
    public string RoomTypeCode { get; set; } = "";
    public string RoomTypeName { get; set; } = "";
    public int? AreaSquareMeters { get; set; }
    public int DefaultAdults { get; set; }
    public int DefaultChildren { get; set; }
    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }
    public int MaxGuests { get; set; }
    public int AvailableUnits { get; set; }
    public string? CoverImageUrl { get; set; }
    public List<HotelAvailabilityRatePlanOptionDto> Options { get; set; } = new();
}

public sealed class HotelAvailabilityRatePlanOptionDto
{
    public Guid RatePlanId { get; set; }
    public string RatePlanCode { get; set; } = "";
    public string RatePlanName { get; set; } = "";
    public bool Refundable { get; set; }
    public bool BreakfastIncluded { get; set; }
    public bool RequiresGuarantee { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public int NightCount { get; set; }
    public decimal TotalBasePrice { get; set; }
    public decimal TotalTaxes { get; set; }
    public decimal TotalFees { get; set; }
    public decimal TotalPrice { get; set; }
    public List<HotelAvailabilityNightRateDto> NightlyRates { get; set; } = new();
}

public sealed class HotelAvailabilityNightRateDto
{
    public DateOnly Date { get; set; }
    public decimal Price { get; set; }
    public decimal BasePrice { get; set; }
    public decimal Taxes { get; set; }
    public decimal Fees { get; set; }
    public string CurrencyCode { get; set; } = "VND";
}

