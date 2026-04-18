// FILE #269: TicketBooking.Api/Controllers/Tours/TourItineraryController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tours/{tourId:guid}/itinerary")]
[AllowAnonymous]
public sealed class TourItineraryController : ControllerBase
{
    private readonly AppDbContext _db;

    public TourItineraryController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<TourPublicItineraryPagedResponse>> ListDays(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] int? dayNumber = null,
        [FromQuery] bool includeItems = false,
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

        IQueryable<TourItineraryDay> query = _db.TourItineraryDays
            .AsNoTracking()
            .Where(x =>
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted);

        if (dayNumber.HasValue)
            query = query.Where(x => x.DayNumber == dayNumber.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Title.Contains(qq) ||
                (x.ShortDescription != null && x.ShortDescription.Contains(qq)) ||
                (x.StartLocation != null && x.StartLocation.Contains(qq)) ||
                (x.EndLocation != null && x.EndLocation.Contains(qq)) ||
                (x.AccommodationName != null && x.AccommodationName.Contains(qq)) ||
                (x.TransportationSummary != null && x.TransportationSummary.Contains(qq)));
        }

        var days = await query
            .OrderBy(x => x.DayNumber)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(ct);

        var dayIds = days.Select(x => x.Id).ToList();

        var items = await _db.TourItineraryItems
            .AsNoTracking()
            .Where(x =>
                dayIds.Contains(x.TourItineraryDayId) &&
                x.IsActive &&
                !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StartTime)
            .ThenBy(x => x.Title)
            .ToListAsync(ct);

        var itemsByDay = items
            .GroupBy(x => x.TourItineraryDayId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var result = days.Select(day =>
        {
            itemsByDay.TryGetValue(day.Id, out var dayItems);
            dayItems ??= new List<TourItineraryItem>();

            return new TourPublicItineraryDayDto
            {
                Id = day.Id,
                DayNumber = day.DayNumber,
                Title = day.Title,
                ShortDescription = day.ShortDescription,
                DescriptionMarkdown = day.DescriptionMarkdown,
                DescriptionHtml = day.DescriptionHtml,
                StartLocation = day.StartLocation,
                EndLocation = day.EndLocation,
                AccommodationName = day.AccommodationName,
                IncludesBreakfast = day.IncludesBreakfast,
                IncludesLunch = day.IncludesLunch,
                IncludesDinner = day.IncludesDinner,
                TransportationSummary = day.TransportationSummary,
                SortOrder = day.SortOrder,
                ItemCount = dayItems.Count,
                Items = includeItems
                    ? dayItems.Select(MapItem).ToList()
                    : new List<TourPublicItineraryItemDto>()
            };
        }).ToList();

        return Ok(new TourPublicItineraryPagedResponse
        {
            TourId = tour.Id,
            TourName = tour.Name,
            TotalDays = result.Count,
            Items = result
        });
    }

    [HttpGet("{dayId:guid}")]
    public async Task<ActionResult<TourPublicItineraryDayDetailDto>> GetDayById(
        Guid tourId,
        Guid dayId,
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

        var day = await _db.TourItineraryDays
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == dayId &&
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted, ct);

        if (day is null)
            return NotFound(new { message = "Itinerary day not found." });

        var items = await _db.TourItineraryItems
            .AsNoTracking()
            .Where(x =>
                x.TourItineraryDayId == dayId &&
                x.IsActive &&
                !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StartTime)
            .ThenBy(x => x.Title)
            .ToListAsync(ct);

        return Ok(new TourPublicItineraryDayDetailDto
        {
            Id = day.Id,
            TourId = day.TourId,
            TourName = tour.Name,
            DayNumber = day.DayNumber,
            Title = day.Title,
            ShortDescription = day.ShortDescription,
            DescriptionMarkdown = day.DescriptionMarkdown,
            DescriptionHtml = day.DescriptionHtml,
            StartLocation = day.StartLocation,
            EndLocation = day.EndLocation,
            AccommodationName = day.AccommodationName,
            IncludesBreakfast = day.IncludesBreakfast,
            IncludesLunch = day.IncludesLunch,
            IncludesDinner = day.IncludesDinner,
            TransportationSummary = day.TransportationSummary,
            Notes = day.Notes,
            SortOrder = day.SortOrder,
            Items = items.Select(MapItem).ToList()
        });
    }

    private static TourPublicItineraryItemDto MapItem(TourItineraryItem x)
    {
        return new TourPublicItineraryItemDto
        {
            Id = x.Id,
            TourItineraryDayId = x.TourItineraryDayId,
            Type = x.Type,
            Title = x.Title,
            ShortDescription = x.ShortDescription,
            DescriptionMarkdown = x.DescriptionMarkdown,
            DescriptionHtml = x.DescriptionHtml,
            StartTime = x.StartTime,
            EndTime = x.EndTime,
            LocationName = x.LocationName,
            AddressLine = x.AddressLine,
            TransportationMode = x.TransportationMode,
            IncludesTicket = x.IncludesTicket,
            IncludesMeal = x.IncludesMeal,
            IsOptional = x.IsOptional,
            RequiresAdditionalFee = x.RequiresAdditionalFee,
            Notes = x.Notes,
            SortOrder = x.SortOrder
        };
    }
}

public sealed class TourPublicItineraryPagedResponse
{
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public int TotalDays { get; set; }
    public List<TourPublicItineraryDayDto> Items { get; set; } = new();
}

public sealed class TourPublicItineraryDayDto
{
    public Guid Id { get; set; }
    public int DayNumber { get; set; }
    public string Title { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? StartLocation { get; set; }
    public string? EndLocation { get; set; }
    public string? AccommodationName { get; set; }
    public bool IncludesBreakfast { get; set; }
    public bool IncludesLunch { get; set; }
    public bool IncludesDinner { get; set; }
    public string? TransportationSummary { get; set; }
    public int SortOrder { get; set; }
    public int ItemCount { get; set; }
    public List<TourPublicItineraryItemDto> Items { get; set; } = new();
}

public sealed class TourPublicItineraryDayDetailDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public int DayNumber { get; set; }
    public string Title { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? StartLocation { get; set; }
    public string? EndLocation { get; set; }
    public string? AccommodationName { get; set; }
    public bool IncludesBreakfast { get; set; }
    public bool IncludesLunch { get; set; }
    public bool IncludesDinner { get; set; }
    public string? TransportationSummary { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
    public List<TourPublicItineraryItemDto> Items { get; set; } = new();
}

public sealed class TourPublicItineraryItemDto
{
    public Guid Id { get; set; }
    public Guid TourItineraryDayId { get; set; }
    public TourItineraryItemType Type { get; set; }
    public string Title { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? LocationName { get; set; }
    public string? AddressLine { get; set; }
    public string? TransportationMode { get; set; }
    public bool IncludesTicket { get; set; }
    public bool IncludesMeal { get; set; }
    public bool IsOptional { get; set; }
    public bool RequiresAdditionalFee { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
}
