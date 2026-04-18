// FILE #271: TicketBooking.Api/Controllers/Tours/TourAddonsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tours;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tours/{tourId:guid}/addons")]
[AllowAnonymous]
public sealed class TourAddonsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TourAddonsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<TourPublicAddonPagedResponse>> List(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] TourAddonType? type = null,
        [FromQuery] bool? required = null,
        [FromQuery] bool? defaultSelected = null,
        [FromQuery] bool? perPerson = null,
        [FromQuery] Guid? scheduleId = null,
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

        if (scheduleId.HasValue && scheduleId.Value != Guid.Empty)
        {
            var scheduleExists = await _db.TourSchedules
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id == scheduleId.Value &&
                    x.TourId == tourId &&
                    x.IsActive &&
                    !x.IsDeleted, ct);

            if (!scheduleExists)
                return NotFound(new { message = "Tour schedule not found." });
        }

        IQueryable<TourAddon> query = _db.TourAddons
            .AsNoTracking()
            .Where(x =>
                x.TourId == tourId &&
                x.IsActive &&
                !x.IsDeleted);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(qq) ||
                x.Name.Contains(qq) ||
                (x.ShortDescription != null && x.ShortDescription.Contains(qq)) ||
                (x.DescriptionMarkdown != null && x.DescriptionMarkdown.Contains(qq)) ||
                (x.DescriptionHtml != null && x.DescriptionHtml.Contains(qq)) ||
                (x.Notes != null && x.Notes.Contains(qq)));
        }

        if (!scheduleId.HasValue || scheduleId.Value == Guid.Empty)
        {
            if (required.HasValue)
                query = query.Where(x => x.IsRequired == required.Value);

            if (defaultSelected.HasValue)
                query = query.Where(x => x.IsDefaultSelected == defaultSelected.Value);

            if (perPerson.HasValue)
                query = query.Where(x => x.IsPerPerson == perPerson.Value);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(x => x.IsRequired)
                .ThenByDescending(x => x.IsDefaultSelected)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new TourPublicAddonListItemDto
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

            return Ok(new TourPublicAddonPagedResponse
            {
                TourId = tour.Id,
                TourName = tour.Name,
                Page = page,
                PageSize = pageSize,
                Total = total,
                Items = items
            });
        }

        var resolvedQuery = BuildResolvedAddonsQuery(query, scheduleId.Value);

        if (required.HasValue)
            resolvedQuery = resolvedQuery.Where(x => x.IsRequired == required.Value);

        if (defaultSelected.HasValue)
            resolvedQuery = resolvedQuery.Where(x => x.IsDefaultSelected == defaultSelected.Value);

        if (perPerson.HasValue)
            resolvedQuery = resolvedQuery.Where(x => x.IsPerPerson == perPerson.Value);

        var totalResolved = await resolvedQuery.CountAsync(ct);

        var resolvedItems = await resolvedQuery
            .OrderByDescending(x => x.IsRequired)
            .ThenByDescending(x => x.IsDefaultSelected)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new TourPublicAddonPagedResponse
        {
            TourId = tour.Id,
            TourName = tour.Name,
            Page = page,
            PageSize = pageSize,
            Total = totalResolved,
            Items = resolvedItems.Select(MapListItem).ToList()
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TourPublicAddonDetailDto>> GetById(
        Guid tourId,
        Guid id,
        [FromQuery] Guid? scheduleId = null,
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

        if (scheduleId.HasValue && scheduleId.Value != Guid.Empty)
        {
            var scheduleExists = await _db.TourSchedules
                .AsNoTracking()
                .AnyAsync(x =>
                    x.Id == scheduleId.Value &&
                    x.TourId == tourId &&
                    x.IsActive &&
                    !x.IsDeleted, ct);

            if (!scheduleExists)
                return NotFound(new { message = "Tour schedule not found." });
        }

        if (!scheduleId.HasValue || scheduleId.Value == Guid.Empty)
        {
            var entity = await _db.TourAddons
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.TourId == tourId &&
                    x.IsActive &&
                    !x.IsDeleted, ct);

            if (entity is null)
                return NotFound(new { message = "Addon not found." });

            return Ok(new TourPublicAddonDetailDto
            {
                Id = entity.Id,
                TourId = entity.TourId,
                Code = entity.Code,
                Name = entity.Name,
                Type = entity.Type,
                ShortDescription = entity.ShortDescription,
                DescriptionMarkdown = entity.DescriptionMarkdown,
                DescriptionHtml = entity.DescriptionHtml,
                CurrencyCode = entity.CurrencyCode,
                BasePrice = entity.BasePrice,
                OriginalPrice = entity.OriginalPrice,
                IsPerPerson = entity.IsPerPerson,
                IsRequired = entity.IsRequired,
                AllowQuantitySelection = entity.AllowQuantitySelection,
                MinQuantity = entity.MinQuantity,
                MaxQuantity = entity.MaxQuantity,
                IsDefaultSelected = entity.IsDefaultSelected,
                SortOrder = entity.SortOrder
            });
        }

        var resolvedAddon = await BuildResolvedAddonsQuery(
                _db.TourAddons
                    .AsNoTracking()
                    .Where(x =>
                        x.Id == id &&
                        x.TourId == tourId &&
                        x.IsActive &&
                        !x.IsDeleted),
                scheduleId.Value)
            .FirstOrDefaultAsync(ct);

        if (resolvedAddon is null)
            return NotFound(new { message = "Addon not found." });

        return Ok(MapDetailItem(resolvedAddon));
    }

    private IQueryable<ResolvedTourAddon> BuildResolvedAddonsQuery(
        IQueryable<TourAddon> addonsQuery,
        Guid scheduleId)
    {
        var scheduleAddons = _db.TourScheduleAddonPrices
            .AsNoTracking()
            .Where(x =>
                x.TourScheduleId == scheduleId &&
                x.IsActive &&
                !x.IsDeleted);

        return
            from addon in addonsQuery
            join scheduleAddon in scheduleAddons on addon.Id equals scheduleAddon.TourAddonId into scheduleAddonGroup
            from scheduleAddon in scheduleAddonGroup.DefaultIfEmpty()
            select new ResolvedTourAddon
            {
                Id = addon.Id,
                TourId = addon.TourId,
                Code = addon.Code,
                Name = addon.Name,
                Type = addon.Type,
                ShortDescription = addon.ShortDescription,
                DescriptionMarkdown = addon.DescriptionMarkdown,
                DescriptionHtml = addon.DescriptionHtml,
                CurrencyCode = scheduleAddon != null
                    ? scheduleAddon.CurrencyCode ?? addon.CurrencyCode
                    : addon.CurrencyCode,
                BasePrice = scheduleAddon != null && scheduleAddon.Price.HasValue
                    ? scheduleAddon.Price.Value
                    : addon.BasePrice,
                OriginalPrice = scheduleAddon != null
                    ? scheduleAddon.OriginalPrice ?? addon.OriginalPrice
                    : addon.OriginalPrice,
                IsPerPerson = scheduleAddon != null
                    ? scheduleAddon.IsPerPerson
                    : addon.IsPerPerson,
                IsRequired = scheduleAddon != null
                    ? scheduleAddon.IsRequired
                    : addon.IsRequired,
                AllowQuantitySelection = scheduleAddon != null
                    ? scheduleAddon.AllowQuantitySelection
                    : addon.AllowQuantitySelection,
                MinQuantity = scheduleAddon != null
                    ? scheduleAddon.MinQuantity ?? addon.MinQuantity
                    : addon.MinQuantity,
                MaxQuantity = scheduleAddon != null
                    ? scheduleAddon.MaxQuantity ?? addon.MaxQuantity
                    : addon.MaxQuantity,
                IsDefaultSelected = scheduleAddon != null
                    ? scheduleAddon.IsDefaultSelected
                    : addon.IsDefaultSelected,
                SortOrder = scheduleAddon != null
                    ? scheduleAddon.SortOrder
                    : addon.SortOrder
            };
    }

    private static TourPublicAddonListItemDto MapListItem(ResolvedTourAddon addon)
    {
        return new TourPublicAddonListItemDto
        {
            Id = addon.Id,
            Code = addon.Code,
            Name = addon.Name,
            Type = addon.Type,
            ShortDescription = addon.ShortDescription,
            CurrencyCode = NormalizeCurrencyCode(addon.CurrencyCode),
            BasePrice = addon.BasePrice,
            OriginalPrice = addon.OriginalPrice,
            IsPerPerson = addon.IsPerPerson,
            IsRequired = addon.IsRequired,
            AllowQuantitySelection = addon.AllowQuantitySelection,
            MinQuantity = addon.MinQuantity,
            MaxQuantity = addon.MaxQuantity,
            IsDefaultSelected = addon.IsDefaultSelected,
            SortOrder = addon.SortOrder
        };
    }

    private static TourPublicAddonDetailDto MapDetailItem(ResolvedTourAddon addon)
    {
        return new TourPublicAddonDetailDto
        {
            Id = addon.Id,
            TourId = addon.TourId,
            Code = addon.Code,
            Name = addon.Name,
            Type = addon.Type,
            ShortDescription = addon.ShortDescription,
            DescriptionMarkdown = addon.DescriptionMarkdown,
            DescriptionHtml = addon.DescriptionHtml,
            CurrencyCode = NormalizeCurrencyCode(addon.CurrencyCode),
            BasePrice = addon.BasePrice,
            OriginalPrice = addon.OriginalPrice,
            IsPerPerson = addon.IsPerPerson,
            IsRequired = addon.IsRequired,
            AllowQuantitySelection = addon.AllowQuantitySelection,
            MinQuantity = addon.MinQuantity,
            MaxQuantity = addon.MaxQuantity,
            IsDefaultSelected = addon.IsDefaultSelected,
            SortOrder = addon.SortOrder
        };
    }

    private static string NormalizeCurrencyCode(string? currencyCode)
        => string.IsNullOrWhiteSpace(currencyCode) ? "" : currencyCode.Trim().ToUpperInvariant();
}

internal sealed class ResolvedTourAddon
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourAddonType Type { get; set; }
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
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

public sealed class TourPublicAddonPagedResponse
{
    public Guid TourId { get; set; }
    public string TourName { get; set; } = "";
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<TourPublicAddonListItemDto> Items { get; set; } = new();
}

public sealed class TourPublicAddonListItemDto
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

public sealed class TourPublicAddonDetailDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TourAddonType Type { get; set; }
    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }
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

