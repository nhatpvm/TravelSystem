// FILE #201: TicketBooking.Api/Controllers/Hotels/QlksHotelsController.cs
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Hotels;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Hotels;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/qlks/hotels")]
[Authorize(Roles = RoleNames.QLKS + "," + RoleNames.Admin)]
public sealed class QlksHotelsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QlksHotelsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<HotelPagedResponse<HotelListItemDto>>> List(
        [FromQuery] string? q = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "Tenant context is required." });

        var tenantId = _tenant.TenantId.Value;

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        IQueryable<Hotel> query = includeDeleted
            ? _db.Hotels.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.Hotels.Where(x => x.TenantId == tenantId);

        if (!includeDeleted)
            query = query.Where(x => !x.IsDeleted);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(q) ||
                x.Name.Contains(q) ||
                (x.Slug != null && x.Slug.Contains(q)) ||
                (x.City != null && x.City.Contains(q)) ||
                (x.Province != null && x.Province.Contains(q)) ||
                (x.AddressLine != null && x.AddressLine.Contains(q)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new HotelListItemDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                Code = x.Code,
                Name = x.Name,
                Slug = x.Slug,
                City = x.City,
                Province = x.Province,
                CountryCode = x.CountryCode,
                StarRating = x.StarRating,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new HotelPagedResponse<HotelListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<HotelDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "Tenant context is required." });

        var tenantId = _tenant.TenantId.Value;

        IQueryable<Hotel> query = includeDeleted
            ? _db.Hotels.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.Hotels.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        var entity = await query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Hotel not found." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<CreateHotelResponse>> Create(
        [FromBody] CreateHotelRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "Tenant context is required." });

        ValidateCreate(req);

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var code = req.Code!.Trim();
        var slug = NormalizeSlug(req.Slug!);

        var codeExists = await _db.Hotels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (codeExists)
            return Conflict(new { message = "Hotel code already exists in this tenant." });

        var slugExists = await _db.Hotels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Slug == slug, ct);

        if (slugExists)
            return Conflict(new { message = "Hotel slug already exists in this tenant." });

        if (req.LocationId.HasValue)
        {
            var locationExists = await _db.Locations.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == req.LocationId.Value && !x.IsDeleted, ct);

            if (!locationExists)
                return BadRequest(new { message = "LocationId not found in current tenant." });
        }

        var entity = new Hotel
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = req.Name!.Trim(),
            Slug = slug,

            LocationId = req.LocationId,
            AddressLine = NullIfWhiteSpace(req.AddressLine),
            City = NullIfWhiteSpace(req.City),
            Province = NullIfWhiteSpace(req.Province),
            CountryCode = string.IsNullOrWhiteSpace(req.CountryCode) ? "VN" : req.CountryCode!.Trim(),
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            TimeZone = string.IsNullOrWhiteSpace(req.TimeZone) ? "Asia/Ho_Chi_Minh" : req.TimeZone!.Trim(),

            ShortDescription = NullIfWhiteSpace(req.ShortDescription),
            DescriptionMarkdown = NullIfWhiteSpace(req.DescriptionMarkdown),
            DescriptionHtml = NullIfWhiteSpace(req.DescriptionHtml),

            StarRating = req.StarRating ?? 0,
            Status = req.Status ?? HotelStatus.Active,

            DefaultCheckInTime = req.DefaultCheckInTime,
            DefaultCheckOutTime = req.DefaultCheckOutTime,

            Phone = NullIfWhiteSpace(req.Phone),
            Email = NullIfWhiteSpace(req.Email),
            WebsiteUrl = NullIfWhiteSpace(req.WebsiteUrl),

            SeoTitle = NullIfWhiteSpace(req.SeoTitle),
            SeoDescription = NullIfWhiteSpace(req.SeoDescription),
            SeoKeywords = NullIfWhiteSpace(req.SeoKeywords),
            CanonicalUrl = NullIfWhiteSpace(req.CanonicalUrl),
            Robots = NullIfWhiteSpace(req.Robots),
            OgImageUrl = NullIfWhiteSpace(req.OgImageUrl),
            SchemaJsonLd = NullIfWhiteSpace(req.SchemaJsonLd),

            CoverMediaAssetId = req.CoverMediaAssetId,
            CoverImageUrl = NullIfWhiteSpace(req.CoverImageUrl),

            PoliciesJson = NullIfWhiteSpace(req.PoliciesJson),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),

            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.Hotels.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new CreateHotelResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateHotelRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "Tenant context is required." });

        ValidateUpdate(req);

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.Hotels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel not found." });

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

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var newCode = req.Code.Trim();
            var codeExists = await _db.Hotels.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Code == newCode && x.Id != id, ct);

            if (codeExists)
                return Conflict(new { message = "Hotel code already exists in this tenant." });

            entity.Code = newCode;
        }

        if (!string.IsNullOrWhiteSpace(req.Slug))
        {
            var newSlug = NormalizeSlug(req.Slug);
            var slugExists = await _db.Hotels.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Slug == newSlug && x.Id != id, ct);

            if (slugExists)
                return Conflict(new { message = "Hotel slug already exists in this tenant." });

            entity.Slug = newSlug;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
            entity.Name = req.Name.Trim();

        if (req.LocationId.HasValue)
        {
            var locationExists = await _db.Locations.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == req.LocationId.Value && !x.IsDeleted, ct);

            if (!locationExists)
                return BadRequest(new { message = "LocationId not found in current tenant." });

            entity.LocationId = req.LocationId;
        }

        if (req.AddressLine is not null) entity.AddressLine = NullIfWhiteSpace(req.AddressLine);
        if (req.City is not null) entity.City = NullIfWhiteSpace(req.City);
        if (req.Province is not null) entity.Province = NullIfWhiteSpace(req.Province);
        if (req.CountryCode is not null) entity.CountryCode = NullIfWhiteSpace(req.CountryCode);
        if (req.ClearLatitude) entity.Latitude = null;
        else if (req.Latitude.HasValue) entity.Latitude = req.Latitude;
        if (req.ClearLongitude) entity.Longitude = null;
        else if (req.Longitude.HasValue) entity.Longitude = req.Longitude;
        if (req.TimeZone is not null) entity.TimeZone = NullIfWhiteSpace(req.TimeZone);

        if (req.ShortDescription is not null) entity.ShortDescription = NullIfWhiteSpace(req.ShortDescription);
        if (req.DescriptionMarkdown is not null) entity.DescriptionMarkdown = NullIfWhiteSpace(req.DescriptionMarkdown);
        if (req.DescriptionHtml is not null) entity.DescriptionHtml = NullIfWhiteSpace(req.DescriptionHtml);

        if (req.StarRating.HasValue) entity.StarRating = req.StarRating.Value;
        if (req.Status.HasValue) entity.Status = req.Status.Value;

        if (req.ClearDefaultCheckInTime) entity.DefaultCheckInTime = null;
        else if (req.DefaultCheckInTime.HasValue) entity.DefaultCheckInTime = req.DefaultCheckInTime;
        if (req.ClearDefaultCheckOutTime) entity.DefaultCheckOutTime = null;
        else if (req.DefaultCheckOutTime.HasValue) entity.DefaultCheckOutTime = req.DefaultCheckOutTime;

        if (req.Phone is not null) entity.Phone = NullIfWhiteSpace(req.Phone);
        if (req.Email is not null) entity.Email = NullIfWhiteSpace(req.Email);
        if (req.WebsiteUrl is not null) entity.WebsiteUrl = NullIfWhiteSpace(req.WebsiteUrl);

        if (req.SeoTitle is not null) entity.SeoTitle = NullIfWhiteSpace(req.SeoTitle);
        if (req.SeoDescription is not null) entity.SeoDescription = NullIfWhiteSpace(req.SeoDescription);
        if (req.SeoKeywords is not null) entity.SeoKeywords = NullIfWhiteSpace(req.SeoKeywords);
        if (req.CanonicalUrl is not null) entity.CanonicalUrl = NullIfWhiteSpace(req.CanonicalUrl);
        if (req.Robots is not null) entity.Robots = NullIfWhiteSpace(req.Robots);
        if (req.OgImageUrl is not null) entity.OgImageUrl = NullIfWhiteSpace(req.OgImageUrl);
        if (req.SchemaJsonLd is not null) entity.SchemaJsonLd = NullIfWhiteSpace(req.SchemaJsonLd);

        if (req.ClearCoverMediaAssetId) entity.CoverMediaAssetId = null;
        else if (req.CoverMediaAssetId.HasValue) entity.CoverMediaAssetId = req.CoverMediaAssetId;
        if (req.CoverImageUrl is not null) entity.CoverImageUrl = NullIfWhiteSpace(req.CoverImageUrl);

        if (req.PoliciesJson is not null) entity.PoliciesJson = NullIfWhiteSpace(req.PoliciesJson);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);

        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Data was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "Tenant context is required." });

        var tenantId = _tenant.TenantId.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.Hotels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel not found." });

        if (!entity.IsDeleted)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTimeOffset.Now;
            entity.UpdatedByUserId = userId;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "Tenant context is required." });

        var tenantId = _tenant.TenantId.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.Hotels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel not found." });

        if (entity.IsDeleted)
        {
            entity.IsDeleted = false;
            entity.IsActive = true;
            entity.UpdatedAt = DateTimeOffset.Now;
            entity.UpdatedByUserId = userId;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "Tenant context is required." });

        var tenantId = _tenant.TenantId.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.Hotels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel not found." });

        if (!entity.IsActive)
        {
            entity.IsActive = true;
            entity.UpdatedAt = DateTimeOffset.Now;
            entity.UpdatedByUserId = userId;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "Tenant context is required." });

        var tenantId = _tenant.TenantId.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.Hotels.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel not found." });

        if (entity.IsActive)
        {
            entity.IsActive = false;
            entity.UpdatedAt = DateTimeOffset.Now;
            entity.UpdatedByUserId = userId;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    private static void ValidateCreate(CreateHotelRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Code))
            throw new ArgumentException("Code is required.");

        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(req.Slug))
            throw new ArgumentException("Slug is required.");

        if (req.Code.Length > 50)
            throw new ArgumentException("Code max length is 50.");

        if (req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

        if (req.Slug.Length > 300)
            throw new ArgumentException("Slug max length is 300.");

        if (req.StarRating.HasValue && (req.StarRating < 0 || req.StarRating > 5))
            throw new ArgumentException("StarRating must be between 0 and 5.");
    }

    private static void ValidateUpdate(UpdateHotelRequest req)
    {
        if (req.Code is not null && req.Code.Length > 50)
            throw new ArgumentException("Code max length is 50.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

        if (req.Slug is not null && req.Slug.Length > 300)
            throw new ArgumentException("Slug max length is 300.");

        if (req.StarRating.HasValue && (req.StarRating < 0 || req.StarRating > 5))
            throw new ArgumentException("StarRating must be between 0 and 5.");

        if (req.ClearLocationId && req.LocationId.HasValue)
            throw new ArgumentException("LocationId cannot be provided when ClearLocationId is true.");

        if (req.ClearLatitude && req.Latitude.HasValue)
            throw new ArgumentException("Latitude cannot be provided when ClearLatitude is true.");

        if (req.ClearLongitude && req.Longitude.HasValue)
            throw new ArgumentException("Longitude cannot be provided when ClearLongitude is true.");

        if (req.ClearDefaultCheckInTime && req.DefaultCheckInTime.HasValue)
            throw new ArgumentException("DefaultCheckInTime cannot be provided when ClearDefaultCheckInTime is true.");

        if (req.ClearDefaultCheckOutTime && req.DefaultCheckOutTime.HasValue)
            throw new ArgumentException("DefaultCheckOutTime cannot be provided when ClearDefaultCheckOutTime is true.");

        if (req.ClearCoverMediaAssetId && req.CoverMediaAssetId.HasValue)
            throw new ArgumentException("CoverMediaAssetId cannot be provided when ClearCoverMediaAssetId is true.");
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeSlug(string input)
    {
        var text = input.Trim().ToLowerInvariant();

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var ch in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }

        text = sb.ToString().Normalize(NormalizationForm.FormC);
        text = text.Replace('đ', 'd').Replace('Đ', 'd');

        text = Regex.Replace(text, @"[^a-z0-9]+", "-");
        text = Regex.Replace(text, @"-+", "-").Trim('-');

        return string.IsNullOrWhiteSpace(text) ? "n-a" : text;
    }

    private static HotelDetailDto MapDetail(Hotel x) => new()
    {
        Id = x.Id,
        TenantId = x.TenantId,
        Code = x.Code,
        Name = x.Name,
        Slug = x.Slug,
        LocationId = x.LocationId,
        AddressLine = x.AddressLine,
        City = x.City,
        Province = x.Province,
        CountryCode = x.CountryCode,
        Latitude = x.Latitude,
        Longitude = x.Longitude,
        TimeZone = x.TimeZone,
        ShortDescription = x.ShortDescription,
        DescriptionMarkdown = x.DescriptionMarkdown,
        DescriptionHtml = x.DescriptionHtml,
        StarRating = x.StarRating,
        Status = x.Status,
        DefaultCheckInTime = x.DefaultCheckInTime,
        DefaultCheckOutTime = x.DefaultCheckOutTime,
        Phone = x.Phone,
        Email = x.Email,
        WebsiteUrl = x.WebsiteUrl,
        SeoTitle = x.SeoTitle,
        SeoDescription = x.SeoDescription,
        SeoKeywords = x.SeoKeywords,
        CanonicalUrl = x.CanonicalUrl,
        Robots = x.Robots,
        OgImageUrl = x.OgImageUrl,
        SchemaJsonLd = x.SchemaJsonLd,
        CoverMediaAssetId = x.CoverMediaAssetId,
        CoverImageUrl = x.CoverImageUrl,
        PoliciesJson = x.PoliciesJson,
        MetadataJson = x.MetadataJson,
        IsActive = x.IsActive,
        IsDeleted = x.IsDeleted,
        CreatedAt = x.CreatedAt,
        CreatedByUserId = x.CreatedByUserId,
        UpdatedAt = x.UpdatedAt,
        UpdatedByUserId = x.UpdatedByUserId,
        RowVersionBase64 = x.RowVersion is { Length: > 0 } ? Convert.ToBase64String(x.RowVersion) : null
    };
}

public sealed class HotelPagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}

public sealed class HotelListItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Slug { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public int StarRating { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class HotelDetailDto
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
    public HotelStatus Status { get; set; }

    public TimeOnly? DefaultCheckInTime { get; set; }
    public TimeOnly? DefaultCheckOutTime { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }

    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoKeywords { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? Robots { get; set; }
    public string? OgImageUrl { get; set; }
    public string? SchemaJsonLd { get; set; }

    public Guid? CoverMediaAssetId { get; set; }
    public string? CoverImageUrl { get; set; }

    public string? PoliciesJson { get; set; }
    public string? MetadataJson { get; set; }

    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public string? RowVersionBase64 { get; set; }
}

public sealed class CreateHotelRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
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

    public int? StarRating { get; set; }
    public HotelStatus? Status { get; set; }

    public TimeOnly? DefaultCheckInTime { get; set; }
    public TimeOnly? DefaultCheckOutTime { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }

    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoKeywords { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? Robots { get; set; }
    public string? OgImageUrl { get; set; }
    public string? SchemaJsonLd { get; set; }

    public Guid? CoverMediaAssetId { get; set; }
    public string? CoverImageUrl { get; set; }

    public string? PoliciesJson { get; set; }
    public string? MetadataJson { get; set; }

    public bool? IsActive { get; set; }
}

public sealed class UpdateHotelRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }

    public Guid? LocationId { get; set; }
    public bool ClearLocationId { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public bool ClearLatitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool ClearLongitude { get; set; }
    public string? TimeZone { get; set; }

    public string? ShortDescription { get; set; }
    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }

    public int? StarRating { get; set; }
    public HotelStatus? Status { get; set; }

    public TimeOnly? DefaultCheckInTime { get; set; }
    public bool ClearDefaultCheckInTime { get; set; }
    public TimeOnly? DefaultCheckOutTime { get; set; }
    public bool ClearDefaultCheckOutTime { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WebsiteUrl { get; set; }

    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoKeywords { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? Robots { get; set; }
    public string? OgImageUrl { get; set; }
    public string? SchemaJsonLd { get; set; }

    public Guid? CoverMediaAssetId { get; set; }
    public bool ClearCoverMediaAssetId { get; set; }
    public string? CoverImageUrl { get; set; }

    public string? PoliciesJson { get; set; }
    public string? MetadataJson { get; set; }

    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }

    public string? RowVersionBase64 { get; set; }
}

public sealed class CreateHotelResponse
{
    public Guid Id { get; set; }
}

