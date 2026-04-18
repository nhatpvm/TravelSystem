// FILE #254: TicketBooking.Api/Controllers/Tours/QlTourTourItineraryController.cs
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
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/itinerary")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class QlTourTourItineraryController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QlTourTourItineraryController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<QlTourItineraryDayPagedResponse>> ListDays(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] int? dayNumber = null,
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

        IQueryable<TourItineraryDay> query = includeDeleted
            ? _db.TourItineraryDays.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourItineraryDays.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        if (dayNumber.HasValue)
            query = query.Where(x => x.DayNumber == dayNumber.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Title.Contains(qq) ||
                (x.ShortDescription != null && x.ShortDescription.Contains(qq)) ||
                (x.StartLocation != null && x.StartLocation.Contains(qq)) ||
                (x.EndLocation != null && x.EndLocation.Contains(qq)) ||
                (x.AccommodationName != null && x.AccommodationName.Contains(qq)) ||
                (x.TransportationSummary != null && x.TransportationSummary.Contains(qq)) ||
                (x.Notes != null && x.Notes.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var days = await query
            .AsNoTracking()
            .OrderBy(x => x.DayNumber)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dayIds = days.Select(x => x.Id).ToList();

        var itemCounts = await _db.TourItineraryItems.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && dayIds.Contains(x.TourItineraryDayId) && !x.IsDeleted)
            .GroupBy(x => x.TourItineraryDayId)
            .Select(g => new { DayId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DayId, x => x.Count, ct);

        var items = days.Select(x =>
        {
            itemCounts.TryGetValue(x.Id, out var count);

            return new QlTourItineraryDayListItemDto
            {
                Id = x.Id,
                TourId = x.TourId,
                DayNumber = x.DayNumber,
                Title = x.Title,
                ShortDescription = x.ShortDescription,
                StartLocation = x.StartLocation,
                EndLocation = x.EndLocation,
                AccommodationName = x.AccommodationName,
                IncludesBreakfast = x.IncludesBreakfast,
                IncludesLunch = x.IncludesLunch,
                IncludesDinner = x.IncludesDinner,
                TransportationSummary = x.TransportationSummary,
                SortOrder = x.SortOrder,
                ItemCount = count,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            };
        }).ToList();

        return Ok(new QlTourItineraryDayPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{dayId:guid}")]
    public async Task<ActionResult<QlTourItineraryDayDetailDto>> GetDayById(
        Guid tourId,
        Guid dayId,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        IQueryable<TourItineraryDay> dayQuery = includeDeleted
            ? _db.TourItineraryDays.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourItineraryDays.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        var day = await dayQuery.AsNoTracking().FirstOrDefaultAsync(x => x.Id == dayId, ct);
        if (day is null)
            return NotFound(new { message = "Tour itinerary day not found in current tenant." });

        IQueryable<TourItineraryItem> itemQuery = includeDeleted
            ? _db.TourItineraryItems.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourItineraryDayId == dayId)
            : _db.TourItineraryItems.Where(x => x.TenantId == tenantId && x.TourItineraryDayId == dayId && !x.IsDeleted);

        var items = await itemQuery
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StartTime)
            .ThenBy(x => x.Title)
            .Select(x => MapItem(x))
            .ToListAsync(ct);

        return Ok(MapDay(day, items));
    }

    [HttpPost("days")]
    public async Task<ActionResult<QlTourCreateItineraryDayResponse>> CreateDay(
        Guid tourId,
        [FromBody] QlTourCreateItineraryDayRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await ValidateCreateDayAsync(tenantId, tourId, req, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = new TourItineraryDay
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tourId,
            DayNumber = req.DayNumber,
            Title = req.Title.Trim(),
            ShortDescription = NullIfWhiteSpace(req.ShortDescription),
            DescriptionMarkdown = NullIfWhiteSpace(req.DescriptionMarkdown),
            DescriptionHtml = NullIfWhiteSpace(req.DescriptionHtml),
            StartLocation = NullIfWhiteSpace(req.StartLocation),
            EndLocation = NullIfWhiteSpace(req.EndLocation),
            AccommodationName = NullIfWhiteSpace(req.AccommodationName),
            IncludesBreakfast = req.IncludesBreakfast ?? false,
            IncludesLunch = req.IncludesLunch ?? false,
            IncludesDinner = req.IncludesDinner ?? false,
            TransportationSummary = NullIfWhiteSpace(req.TransportationSummary),
            Notes = NullIfWhiteSpace(req.Notes),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            SortOrder = req.SortOrder ?? req.DayNumber,
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.TourItineraryDays.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetDayById),
            new { version = "1.0", tourId, dayId = entity.Id },
            new QlTourCreateItineraryDayResponse { Id = entity.Id });
    }

    [HttpPut("days/{dayId:guid}")]
    public async Task<IActionResult> UpdateDay(
        Guid tourId,
        Guid dayId,
        [FromBody] QlTourUpdateItineraryDayRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourItineraryDays.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == dayId, ct);

        if (entity is null)
            return NotFound(new { message = "Tour itinerary day not found in current tenant." });

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

        await ValidateUpdateDayAsync(tenantId, tourId, dayId, req, entity, ct);

        if (req.DayNumber.HasValue) entity.DayNumber = req.DayNumber.Value;
        if (req.Title is not null) entity.Title = req.Title.Trim();
        if (req.ShortDescription is not null) entity.ShortDescription = NullIfWhiteSpace(req.ShortDescription);
        if (req.DescriptionMarkdown is not null) entity.DescriptionMarkdown = NullIfWhiteSpace(req.DescriptionMarkdown);
        if (req.DescriptionHtml is not null) entity.DescriptionHtml = NullIfWhiteSpace(req.DescriptionHtml);
        if (req.StartLocation is not null) entity.StartLocation = NullIfWhiteSpace(req.StartLocation);
        if (req.EndLocation is not null) entity.EndLocation = NullIfWhiteSpace(req.EndLocation);
        if (req.AccommodationName is not null) entity.AccommodationName = NullIfWhiteSpace(req.AccommodationName);
        if (req.IncludesBreakfast.HasValue) entity.IncludesBreakfast = req.IncludesBreakfast.Value;
        if (req.IncludesLunch.HasValue) entity.IncludesLunch = req.IncludesLunch.Value;
        if (req.IncludesDinner.HasValue) entity.IncludesDinner = req.IncludesDinner.Value;
        if (req.TransportationSummary is not null) entity.TransportationSummary = NullIfWhiteSpace(req.TransportationSummary);
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;
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
            return Conflict(new { message = "Tour itinerary day was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("days/{dayId:guid}/delete")]
    public async Task<IActionResult> DeleteDay(Guid tourId, Guid dayId, CancellationToken ct = default)
        => await ToggleDayDeleted(tourId, dayId, true, ct);

    [HttpPost("days/{dayId:guid}/restore")]
    public async Task<IActionResult> RestoreDay(Guid tourId, Guid dayId, CancellationToken ct = default)
        => await ToggleDayDeleted(tourId, dayId, false, ct);

    [HttpPost("days/{dayId:guid}/activate")]
    public async Task<IActionResult> ActivateDay(Guid tourId, Guid dayId, CancellationToken ct = default)
        => await ToggleDayActive(tourId, dayId, true, ct);

    [HttpPost("days/{dayId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateDay(Guid tourId, Guid dayId, CancellationToken ct = default)
        => await ToggleDayActive(tourId, dayId, false, ct);

    [HttpGet("days/{dayId:guid}/items")]
    public async Task<ActionResult<List<QlTourItineraryItemDto>>> ListItems(
        Guid tourId,
        Guid dayId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureDayExistsAsync(tenantId, tourId, dayId, ct);

        IQueryable<TourItineraryItem> query = includeDeleted
            ? _db.TourItineraryItems.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourItineraryDayId == dayId)
            : _db.TourItineraryItems.Where(x => x.TenantId == tenantId && x.TourItineraryDayId == dayId && !x.IsDeleted);

        var items = await query
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.StartTime)
            .ThenBy(x => x.Title)
            .Select(x => MapItem(x))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPut("days/{dayId:guid}/items")]
    public async Task<IActionResult> ReplaceItems(
        Guid tourId,
        Guid dayId,
        [FromBody] QlTourReplaceItineraryItemsRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureDayExistsAsync(tenantId, tourId, dayId, ct);
        ValidateReplaceItemsPayload(req);

        var day = await _db.TourItineraryDays.IgnoreQueryFilters()
            .FirstAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == dayId, ct);

        var oldItems = await _db.TourItineraryItems.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourItineraryDayId == dayId)
            .ToListAsync(ct);

        if (oldItems.Count > 0)
            _db.TourItineraryItems.RemoveRange(oldItems);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        if (req.Items.Count > 0)
        {
            var newItems = req.Items
                .OrderBy(x => x.SortOrder ?? int.MaxValue)
                .ThenBy(x => x.StartTime)
                .Select((x, index) => new TourItineraryItem
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    TourItineraryDayId = dayId,
                    Type = x.Type,
                    Title = x.Title.Trim(),
                    ShortDescription = NullIfWhiteSpace(x.ShortDescription),
                    DescriptionMarkdown = NullIfWhiteSpace(x.DescriptionMarkdown),
                    DescriptionHtml = NullIfWhiteSpace(x.DescriptionHtml),
                    StartTime = x.StartTime,
                    EndTime = x.EndTime,
                    LocationName = NullIfWhiteSpace(x.LocationName),
                    AddressLine = NullIfWhiteSpace(x.AddressLine),
                    TransportationMode = NullIfWhiteSpace(x.TransportationMode),
                    IncludesTicket = x.IncludesTicket ?? false,
                    IncludesMeal = x.IncludesMeal ?? false,
                    IsOptional = x.IsOptional ?? false,
                    RequiresAdditionalFee = x.RequiresAdditionalFee ?? false,
                    Notes = NullIfWhiteSpace(x.Notes),
                    MetadataJson = NullIfWhiteSpace(x.MetadataJson),
                    SortOrder = x.SortOrder ?? (index + 1),
                    IsActive = x.IsActive ?? true,
                    IsDeleted = false,
                    CreatedAt = now,
                    CreatedByUserId = userId,
                    UpdatedAt = now,
                    UpdatedByUserId = userId
                })
                .ToList();

            _db.TourItineraryItems.AddRange(newItems);
        }

        day.UpdatedAt = now;
        day.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("days/reorder")]
    public async Task<IActionResult> ReorderDays(
        Guid tourId,
        [FromBody] QlTourReorderItineraryDaysRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        if (req.Items.Count == 0)
            return BadRequest(new { message = "Items is required." });

        var ids = req.Items.Select(x => x.DayId).Distinct().ToList();
        if (ids.Count != req.Items.Count)
            return BadRequest(new { message = "Duplicate DayId values are not allowed." });

        var days = await _db.TourItineraryDays.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TourId == tourId && ids.Contains(x.Id))
            .ToListAsync(ct);

        if (days.Count != ids.Count)
            return BadRequest(new { message = "One or more DayId values are invalid in current tenant." });

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        foreach (var item in req.Items)
        {
            var day = days.First(x => x.Id == item.DayId);
            day.DayNumber = item.DayNumber;
            day.SortOrder = item.SortOrder ?? item.DayNumber;
            day.UpdatedAt = now;
            day.UpdatedByUserId = userId;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleDayDeleted(Guid tourId, Guid dayId, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureDayExistsAsync(tenantId, tourId, dayId, ct);

        var entity = await _db.TourItineraryDays.IgnoreQueryFilters()
            .FirstAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == dayId, ct);

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleDayActive(Guid tourId, Guid dayId, bool isActive, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureDayExistsAsync(tenantId, tourId, dayId, ct);

        var entity = await _db.TourItineraryDays.IgnoreQueryFilters()
            .FirstAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == dayId, ct);

        entity.IsActive = isActive;
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

    private async Task EnsureDayExistsAsync(Guid tenantId, Guid tourId, Guid dayId, CancellationToken ct)
    {
        var exists = await _db.TourItineraryDays.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == dayId, ct);

        if (!exists)
            throw new KeyNotFoundException("Tour itinerary day not found in current tenant.");
    }

    private async Task ValidateCreateDayAsync(
        Guid tenantId,
        Guid tourId,
        QlTourCreateItineraryDayRequest req,
        CancellationToken ct)
    {
        ValidateDayPayload(
            req.DayNumber,
            req.Title,
            req.ShortDescription,
            req.StartLocation,
            req.EndLocation,
            req.AccommodationName,
            req.TransportationSummary,
            req.Notes);

        var duplicatedDayNumber = await _db.TourItineraryDays.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.DayNumber == req.DayNumber, ct);

        if (duplicatedDayNumber)
            throw new ArgumentException("DayNumber already exists in current tour.");
    }

    private async Task ValidateUpdateDayAsync(
        Guid tenantId,
        Guid tourId,
        Guid dayId,
        QlTourUpdateItineraryDayRequest req,
        TourItineraryDay current,
        CancellationToken ct)
    {
        var nextDayNumber = req.DayNumber ?? current.DayNumber;
        var nextTitle = req.Title ?? current.Title;

        ValidateDayPayload(
            nextDayNumber,
            nextTitle,
            req.ShortDescription,
            req.StartLocation,
            req.EndLocation,
            req.AccommodationName,
            req.TransportationSummary,
            req.Notes);

        var duplicatedDayNumber = await _db.TourItineraryDays.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.DayNumber == nextDayNumber && x.Id != dayId, ct);

        if (duplicatedDayNumber)
            throw new ArgumentException("DayNumber already exists in current tour.");
    }

    private static void ValidateDayPayload(
        int dayNumber,
        string title,
        string? shortDescription,
        string? startLocation,
        string? endLocation,
        string? accommodationName,
        string? transportationSummary,
        string? notes)
    {
        if (dayNumber <= 0)
            throw new ArgumentException("DayNumber must be greater than 0.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.");

        if (title.Trim().Length > 300)
            throw new ArgumentException("Title max length is 300.");

        if (shortDescription is not null && shortDescription.Length > 2000)
            throw new ArgumentException("ShortDescription max length is 2000.");

        if (startLocation is not null && startLocation.Length > 300)
            throw new ArgumentException("StartLocation max length is 300.");

        if (endLocation is not null && endLocation.Length > 300)
            throw new ArgumentException("EndLocation max length is 300.");

        if (accommodationName is not null && accommodationName.Length > 300)
            throw new ArgumentException("AccommodationName max length is 300.");

        if (transportationSummary is not null && transportationSummary.Length > 1000)
            throw new ArgumentException("TransportationSummary max length is 1000.");

        if (notes is not null && notes.Length > 2000)
            throw new ArgumentException("Notes max length is 2000.");
    }

    private static void ValidateReplaceItemsPayload(QlTourReplaceItineraryItemsRequest req)
    {
        var duplicateSortOrders = req.Items
            .Where(x => x.SortOrder.HasValue)
            .GroupBy(x => x.SortOrder!.Value)
            .Any(g => g.Count() > 1);

        if (duplicateSortOrders)
            throw new ArgumentException("Duplicate SortOrder values are not allowed.");

        foreach (var item in req.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Title))
                throw new ArgumentException("Each itinerary item Title is required.");

            if (item.Title.Trim().Length > 300)
                throw new ArgumentException("Itinerary item Title max length is 300.");

            if (item.ShortDescription is not null && item.ShortDescription.Length > 2000)
                throw new ArgumentException("Itinerary item ShortDescription max length is 2000.");

            if (item.LocationName is not null && item.LocationName.Length > 300)
                throw new ArgumentException("Itinerary item LocationName max length is 300.");

            if (item.AddressLine is not null && item.AddressLine.Length > 500)
                throw new ArgumentException("Itinerary item AddressLine max length is 500.");

            if (item.TransportationMode is not null && item.TransportationMode.Length > 100)
                throw new ArgumentException("Itinerary item TransportationMode max length is 100.");

            if (item.Notes is not null && item.Notes.Length > 2000)
                throw new ArgumentException("Itinerary item Notes max length is 2000.");

            if (item.SortOrder.HasValue && item.SortOrder <= 0)
                throw new ArgumentException("Itinerary item SortOrder must be greater than 0.");

            if (item.StartTime.HasValue && item.EndTime.HasValue && item.EndTime < item.StartTime)
                throw new ArgumentException("Itinerary item EndTime cannot be earlier than StartTime.");
        }
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

    private static QlTourItineraryDayDetailDto MapDay(TourItineraryDay day, List<QlTourItineraryItemDto> items)
    {
        return new QlTourItineraryDayDetailDto
        {
            Id = day.Id,
            TenantId = day.TenantId,
            TourId = day.TourId,
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
            MetadataJson = day.MetadataJson,
            SortOrder = day.SortOrder,
            IsActive = day.IsActive,
            IsDeleted = day.IsDeleted,
            CreatedAt = day.CreatedAt,
            CreatedByUserId = day.CreatedByUserId,
            UpdatedAt = day.UpdatedAt,
            UpdatedByUserId = day.UpdatedByUserId,
            RowVersionBase64 = day.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null,
            Items = items
        };
    }

    private static QlTourItineraryItemDto MapItem(TourItineraryItem x)
    {
        return new QlTourItineraryItemDto
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
            MetadataJson = x.MetadataJson,
            SortOrder = x.SortOrder,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted
        };
    }
}

public sealed class QlTourItineraryDayPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<QlTourItineraryDayListItemDto> Items { get; set; } = new();
}

public sealed class QlTourItineraryDayListItemDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public int DayNumber { get; set; }
    public string Title { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? StartLocation { get; set; }
    public string? EndLocation { get; set; }
    public string? AccommodationName { get; set; }
    public bool IncludesBreakfast { get; set; }
    public bool IncludesLunch { get; set; }
    public bool IncludesDinner { get; set; }
    public string? TransportationSummary { get; set; }
    public int SortOrder { get; set; }
    public int ItemCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class QlTourItineraryDayDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
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
    public string? MetadataJson { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
    public List<QlTourItineraryItemDto> Items { get; set; } = new();
}

public sealed class QlTourItineraryItemDto
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
    public string? MetadataJson { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class QlTourCreateItineraryDayRequest
{
    public int DayNumber { get; set; }
    public string Title { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? StartLocation { get; set; }
    public string? EndLocation { get; set; }
    public string? AccommodationName { get; set; }
    public bool? IncludesBreakfast { get; set; }
    public bool? IncludesLunch { get; set; }
    public bool? IncludesDinner { get; set; }
    public string? TransportationSummary { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class QlTourUpdateItineraryDayRequest
{
    public int? DayNumber { get; set; }
    public string? Title { get; set; }
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public string? StartLocation { get; set; }
    public string? EndLocation { get; set; }
    public string? AccommodationName { get; set; }
    public bool? IncludesBreakfast { get; set; }
    public bool? IncludesLunch { get; set; }
    public bool? IncludesDinner { get; set; }
    public string? TransportationSummary { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourReplaceItineraryItemsRequest
{
    public List<QlTourReplaceItineraryItemDto> Items { get; set; } = new();
}

public sealed class QlTourReplaceItineraryItemDto
{
    public TourItineraryItemType Type { get; set; } = TourItineraryItemType.Activity;
    public string Title { get; set; } = "";
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? LocationName { get; set; }
    public string? AddressLine { get; set; }
    public string? TransportationMode { get; set; }
    public bool? IncludesTicket { get; set; }
    public bool? IncludesMeal { get; set; }
    public bool? IsOptional { get; set; }
    public bool? RequiresAdditionalFee { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class QlTourReorderItineraryDaysRequest
{
    public List<QlTourReorderItineraryDayItem> Items { get; set; } = new();
}

public sealed class QlTourReorderItineraryDayItem
{
    public Guid DayId { get; set; }
    public int DayNumber { get; set; }
    public int? SortOrder { get; set; }
}

public sealed class QlTourCreateItineraryDayResponse
{
    public Guid Id { get; set; }
}
