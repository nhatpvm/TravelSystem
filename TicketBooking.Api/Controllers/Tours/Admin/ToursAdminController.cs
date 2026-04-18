// FILE #258: TicketBooking.Api/Controllers/Tours/ToursAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/tours")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class ToursAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public ToursAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<ToursAdminPagedResponse>> List(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] string? q = null,
        [FromQuery] TourStatus? status = null,
        [FromQuery] TourType? type = null,
        [FromQuery] TourDifficulty? difficulty = null,
        [FromQuery] bool? featured = null,
        [FromQuery] bool? featuredOnHome = null,
        [FromQuery] bool? active = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        IQueryable<Tour> query = includeDeleted
            ? _db.Tours.IgnoreQueryFilters()
            : _db.Tours.Where(x => !x.IsDeleted);

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            query = query.Where(x => x.TenantId == tenantId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (difficulty.HasValue)
            query = query.Where(x => x.Difficulty == difficulty.Value);

        if (featured.HasValue)
            query = query.Where(x => x.IsFeatured == featured.Value);

        if (featuredOnHome.HasValue)
            query = query.Where(x => x.IsFeaturedOnHome == featuredOnHome.Value);

        if (active.HasValue)
            query = query.Where(x => x.IsActive == active.Value);

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

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderBy(x => x.TenantId)
            .ThenByDescending(x => x.IsFeaturedOnHome)
            .ThenByDescending(x => x.IsFeatured)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ToursAdminListItemDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                ProviderId = x.ProviderId,
                PrimaryLocationId = x.PrimaryLocationId,
                Code = x.Code,
                Name = x.Name,
                Slug = x.Slug,
                Type = x.Type,
                Status = x.Status,
                Difficulty = x.Difficulty,
                DurationDays = x.DurationDays,
                DurationNights = x.DurationNights,
                MinGuests = x.MinGuests,
                MaxGuests = x.MaxGuests,
                CountryCode = x.CountryCode,
                Province = x.Province,
                City = x.City,
                CurrencyCode = x.CurrencyCode,
                IsFeatured = x.IsFeatured,
                IsFeaturedOnHome = x.IsFeaturedOnHome,
                IsPrivateTourSupported = x.IsPrivateTourSupported,
                IsInstantConfirm = x.IsInstantConfirm,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CoverImageUrl = x.CoverImageUrl,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new ToursAdminPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ToursAdminDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        IQueryable<Tour> query = includeDeleted
            ? _db.Tours.IgnoreQueryFilters()
            : _db.Tours.Where(x => !x.IsDeleted);

        var entity = await query
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour not found." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<ToursAdminCreateResponse>> Create(
        [FromBody] ToursAdminCreateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireWriteTenant();
        await ValidateCreateAsync(tenantId, req, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = new Tour
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProviderId = NormalizeGuid(req.ProviderId),
            PrimaryLocationId = NormalizeGuid(req.PrimaryLocationId),
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            Slug = req.Slug.Trim(),
            Type = req.Type,
            Status = req.Status,
            Difficulty = req.Difficulty,
            DurationDays = req.DurationDays,
            DurationNights = req.DurationNights,
            MinGuests = req.MinGuests,
            MaxGuests = req.MaxGuests,
            MinAge = req.MinAge,
            MaxAge = req.MaxAge,
            IsFeatured = req.IsFeatured ?? false,
            IsFeaturedOnHome = req.IsFeaturedOnHome ?? false,
            IsPrivateTourSupported = req.IsPrivateTourSupported ?? false,
            IsInstantConfirm = req.IsInstantConfirm ?? false,
            CountryCode = NullIfWhiteSpace(req.CountryCode),
            Province = NullIfWhiteSpace(req.Province),
            City = NullIfWhiteSpace(req.City),
            MeetingPointSummary = NullIfWhiteSpace(req.MeetingPointSummary),
            ShortDescription = NullIfWhiteSpace(req.ShortDescription),
            DescriptionMarkdown = NullIfWhiteSpace(req.DescriptionMarkdown),
            DescriptionHtml = NullIfWhiteSpace(req.DescriptionHtml),
            HighlightsJson = NullIfWhiteSpace(req.HighlightsJson),
            IncludesJson = NullIfWhiteSpace(req.IncludesJson),
            ExcludesJson = NullIfWhiteSpace(req.ExcludesJson),
            TermsJson = NullIfWhiteSpace(req.TermsJson),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            CoverImageUrl = NullIfWhiteSpace(req.CoverImageUrl),
            CoverMediaAssetId = NormalizeGuid(req.CoverMediaAssetId),
            CurrencyCode = req.CurrencyCode.Trim(),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.Tours.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new ToursAdminCreateResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] ToursAdminUpdateRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.Tours.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour not found in current switched tenant." });

        if (!string.IsNullOrWhiteSpace(req.RowVersionBase64))
        {
            try
            {
                _db.Entry(entity).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(req.RowVersionBase64);
            }
            catch
            {
                return BadRequest(new { message = "RowVersionBase64 is invalid." });
            }
        }

        await ValidateUpdateAsync(tenantId, id, req, ct);

        if (req.ProviderId.HasValue) entity.ProviderId = NormalizeGuid(req.ProviderId);
        if (req.PrimaryLocationId.HasValue) entity.PrimaryLocationId = NormalizeGuid(req.PrimaryLocationId);

        if (req.Code is not null) entity.Code = req.Code.Trim();
        if (req.Name is not null) entity.Name = req.Name.Trim();
        if (req.Slug is not null) entity.Slug = req.Slug.Trim();

        if (req.Type.HasValue) entity.Type = req.Type.Value;
        if (req.Status.HasValue) entity.Status = req.Status.Value;
        if (req.Difficulty.HasValue) entity.Difficulty = req.Difficulty.Value;

        if (req.DurationDays.HasValue) entity.DurationDays = req.DurationDays.Value;
        if (req.DurationNights.HasValue) entity.DurationNights = req.DurationNights.Value;

        if (req.MinGuests.HasValue) entity.MinGuests = req.MinGuests;
        if (req.MaxGuests.HasValue) entity.MaxGuests = req.MaxGuests;
        if (req.MinAge.HasValue) entity.MinAge = req.MinAge;
        if (req.MaxAge.HasValue) entity.MaxAge = req.MaxAge;

        if (req.IsFeatured.HasValue) entity.IsFeatured = req.IsFeatured.Value;
        if (req.IsFeaturedOnHome.HasValue) entity.IsFeaturedOnHome = req.IsFeaturedOnHome.Value;
        if (req.IsPrivateTourSupported.HasValue) entity.IsPrivateTourSupported = req.IsPrivateTourSupported.Value;
        if (req.IsInstantConfirm.HasValue) entity.IsInstantConfirm = req.IsInstantConfirm.Value;

        if (req.CountryCode is not null) entity.CountryCode = NullIfWhiteSpace(req.CountryCode);
        if (req.Province is not null) entity.Province = NullIfWhiteSpace(req.Province);
        if (req.City is not null) entity.City = NullIfWhiteSpace(req.City);
        if (req.MeetingPointSummary is not null) entity.MeetingPointSummary = NullIfWhiteSpace(req.MeetingPointSummary);
        if (req.ShortDescription is not null) entity.ShortDescription = NullIfWhiteSpace(req.ShortDescription);
        if (req.DescriptionMarkdown is not null) entity.DescriptionMarkdown = NullIfWhiteSpace(req.DescriptionMarkdown);
        if (req.DescriptionHtml is not null) entity.DescriptionHtml = NullIfWhiteSpace(req.DescriptionHtml);
        if (req.HighlightsJson is not null) entity.HighlightsJson = NullIfWhiteSpace(req.HighlightsJson);
        if (req.IncludesJson is not null) entity.IncludesJson = NullIfWhiteSpace(req.IncludesJson);
        if (req.ExcludesJson is not null) entity.ExcludesJson = NullIfWhiteSpace(req.ExcludesJson);
        if (req.TermsJson is not null) entity.TermsJson = NullIfWhiteSpace(req.TermsJson);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.CoverImageUrl is not null) entity.CoverImageUrl = NullIfWhiteSpace(req.CoverImageUrl);
        if (req.CoverMediaAssetId.HasValue) entity.CoverMediaAssetId = NormalizeGuid(req.CoverMediaAssetId);

        if (req.CurrencyCode is not null) entity.CurrencyCode = req.CurrencyCode.Trim();

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
            return Conflict(new { message = "Tour was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        => await ToggleDeleted(id, true, ct);

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
        => await ToggleDeleted(id, false, ct);

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct = default)
        => await ToggleActive(id, true, ct);

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
        => await ToggleActive(id, false, ct);

    [HttpPost("{id:guid}/feature")]
    public async Task<IActionResult> Feature(Guid id, CancellationToken ct = default)
        => await ToggleFeature(id, isFeatured: true, isFeaturedOnHome: null, ct);

    [HttpPost("{id:guid}/unfeature")]
    public async Task<IActionResult> Unfeature(Guid id, CancellationToken ct = default)
        => await ToggleFeature(id, isFeatured: false, isFeaturedOnHome: null, ct);

    [HttpPost("{id:guid}/feature-home")]
    public async Task<IActionResult> FeatureHome(Guid id, CancellationToken ct = default)
        => await ToggleFeature(id, isFeatured: null, isFeaturedOnHome: true, ct);

    [HttpPost("{id:guid}/unfeature-home")]
    public async Task<IActionResult> UnfeatureHome(Guid id, CancellationToken ct = default)
        => await ToggleFeature(id, isFeatured: null, isFeaturedOnHome: false, ct);

    private async Task<IActionResult> ToggleDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.Tours.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid id, bool isActive, CancellationToken ct)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.Tours.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour not found in current switched tenant." });

        entity.IsActive = isActive;

        if (isActive && entity.Status == TourStatus.Inactive)
            entity.Status = TourStatus.Active;

        if (!isActive && entity.Status == TourStatus.Active)
            entity.Status = TourStatus.Inactive;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleFeature(
        Guid id,
        bool? isFeatured,
        bool? isFeaturedOnHome,
        CancellationToken ct)
    {
        var tenantId = RequireWriteTenant();

        var entity = await _db.Tours.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour not found in current switched tenant." });

        if (isFeatured.HasValue) entity.IsFeatured = isFeatured.Value;
        if (isFeaturedOnHome.HasValue) entity.IsFeaturedOnHome = isFeaturedOnHome.Value;

        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task ValidateCreateAsync(Guid tenantId, ToursAdminCreateRequest req, CancellationToken ct)
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

        ValidateBusinessRules(
            req.DurationDays,
            req.DurationNights,
            req.MinGuests,
            req.MaxGuests,
            req.MinAge,
            req.MaxAge,
            req.CurrencyCode);

        var code = req.Code.Trim();
        var slug = req.Slug.Trim();

        var duplicatedCode = await _db.Tours.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (duplicatedCode)
            throw new ArgumentException("Code already exists in current switched tenant.");

        var duplicatedSlug = await _db.Tours.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Slug == slug, ct);

        if (duplicatedSlug)
            throw new ArgumentException("Slug already exists in current switched tenant.");
    }

    private async Task ValidateUpdateAsync(Guid tenantId, Guid currentId, ToursAdminUpdateRequest req, CancellationToken ct)
    {
        if (req.Code is not null && string.IsNullOrWhiteSpace(req.Code))
            throw new ArgumentException("Code cannot be empty.");

        if (req.Name is not null && string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name cannot be empty.");

        if (req.Slug is not null && string.IsNullOrWhiteSpace(req.Slug))
            throw new ArgumentException("Slug cannot be empty.");

        if (req.Code is not null && req.Code.Length > 50)
            throw new ArgumentException("Code max length is 50.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

        if (req.Slug is not null && req.Slug.Length > 300)
            throw new ArgumentException("Slug max length is 300.");

        var current = await _db.Tours.IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(x => x.TenantId == tenantId && x.Id == currentId, ct);

        ValidateBusinessRules(
            req.DurationDays ?? current.DurationDays,
            req.DurationNights ?? current.DurationNights,
            req.MinGuests ?? current.MinGuests,
            req.MaxGuests ?? current.MaxGuests,
            req.MinAge ?? current.MinAge,
            req.MaxAge ?? current.MaxAge,
            req.CurrencyCode ?? current.CurrencyCode);

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var duplicatedCode = await _db.Tours.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Code == req.Code.Trim() && x.Id != currentId, ct);

            if (duplicatedCode)
                throw new ArgumentException("Code already exists in current switched tenant.");
        }

        if (!string.IsNullOrWhiteSpace(req.Slug))
        {
            var duplicatedSlug = await _db.Tours.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Slug == req.Slug.Trim() && x.Id != currentId, ct);

            if (duplicatedSlug)
                throw new ArgumentException("Slug already exists in current switched tenant.");
        }
    }

    private static void ValidateBusinessRules(
        int durationDays,
        int durationNights,
        int? minGuests,
        int? maxGuests,
        int? minAge,
        int? maxAge,
        string? currencyCode)
    {
        if (durationDays <= 0)
            throw new ArgumentException("DurationDays must be greater than 0.");

        if (durationNights < 0)
            throw new ArgumentException("DurationNights cannot be negative.");

        if (durationDays < durationNights)
            throw new ArgumentException("DurationDays cannot be less than DurationNights.");

        if (minGuests.HasValue && minGuests <= 0)
            throw new ArgumentException("MinGuests must be greater than 0.");

        if (maxGuests.HasValue && maxGuests <= 0)
            throw new ArgumentException("MaxGuests must be greater than 0.");

        if (minGuests.HasValue && maxGuests.HasValue && minGuests > maxGuests)
            throw new ArgumentException("MinGuests cannot be greater than MaxGuests.");

        if (minAge.HasValue && minAge < 0)
            throw new ArgumentException("MinAge cannot be negative.");

        if (maxAge.HasValue && maxAge < 0)
            throw new ArgumentException("MaxAge cannot be negative.");

        if (minAge.HasValue && maxAge.HasValue && minAge > maxAge)
            throw new ArgumentException("MinAge cannot be greater than MaxAge.");

        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("CurrencyCode is required.");

        if (currencyCode.Trim().Length > 10)
            throw new ArgumentException("CurrencyCode max length is 10.");
    }

    private Guid RequireWriteTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue || _tenant.TenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Admin write operations require switched tenant context.");

        return _tenant.TenantId.Value;
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static Guid? NormalizeGuid(Guid? value)
        => value.HasValue && value.Value != Guid.Empty ? value.Value : null;

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ToursAdminDetailDto MapDetail(Tour x)
    {
        return new ToursAdminDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            ProviderId = x.ProviderId,
            PrimaryLocationId = x.PrimaryLocationId,
            Code = x.Code,
            Name = x.Name,
            Slug = x.Slug,
            Type = x.Type,
            Status = x.Status,
            Difficulty = x.Difficulty,
            DurationDays = x.DurationDays,
            DurationNights = x.DurationNights,
            MinGuests = x.MinGuests,
            MaxGuests = x.MaxGuests,
            MinAge = x.MinAge,
            MaxAge = x.MaxAge,
            IsFeatured = x.IsFeatured,
            IsFeaturedOnHome = x.IsFeaturedOnHome,
            IsPrivateTourSupported = x.IsPrivateTourSupported,
            IsInstantConfirm = x.IsInstantConfirm,
            CountryCode = x.CountryCode,
            Province = x.Province,
            City = x.City,
            MeetingPointSummary = x.MeetingPointSummary,
            ShortDescription = x.ShortDescription,
            DescriptionMarkdown = x.DescriptionMarkdown,
            DescriptionHtml = x.DescriptionHtml,
            HighlightsJson = x.HighlightsJson,
            IncludesJson = x.IncludesJson,
            ExcludesJson = x.ExcludesJson,
            TermsJson = x.TermsJson,
            MetadataJson = x.MetadataJson,
            CoverImageUrl = x.CoverImageUrl,
            CoverMediaAssetId = x.CoverMediaAssetId,
            CurrencyCode = x.CurrencyCode,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            CreatedByUserId = x.CreatedByUserId,
            UpdatedAt = x.UpdatedAt,
            UpdatedByUserId = x.UpdatedByUserId,
            RowVersionBase64 = x.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
        };
    }
}

public sealed class ToursAdminPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<ToursAdminListItemDto> Items { get; set; } = new();
}

public sealed class ToursAdminListItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? PrimaryLocationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public TourType Type { get; set; }
    public TourStatus Status { get; set; }
    public TourDifficulty Difficulty { get; set; }
    public int DurationDays { get; set; }
    public int DurationNights { get; set; }
    public int? MinGuests { get; set; }
    public int? MaxGuests { get; set; }
    public string? CountryCode { get; set; }
    public string? Province { get; set; }
    public string? City { get; set; }
    public string CurrencyCode { get; set; } = "";
    public bool IsFeatured { get; set; }
    public bool IsFeaturedOnHome { get; set; }
    public bool IsPrivateTourSupported { get; set; }
    public bool IsInstantConfirm { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class ToursAdminDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? PrimaryLocationId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public TourType Type { get; set; }
    public TourStatus Status { get; set; }
    public TourDifficulty Difficulty { get; set; }
    public int DurationDays { get; set; }
    public int DurationNights { get; set; }
    public int? MinGuests { get; set; }
    public int? MaxGuests { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsFeaturedOnHome { get; set; }
    public bool IsPrivateTourSupported { get; set; }
    public bool IsInstantConfirm { get; set; }
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
    public string? MetadataJson { get; set; }
    public string? CoverImageUrl { get; set; }
    public Guid? CoverMediaAssetId { get; set; }
    public string CurrencyCode { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class ToursAdminCreateRequest
{
    public Guid? ProviderId { get; set; }
    public Guid? PrimaryLocationId { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";

    public TourType Type { get; set; } = TourType.Domestic;
    public TourStatus Status { get; set; } = TourStatus.Draft;
    public TourDifficulty Difficulty { get; set; } = TourDifficulty.Easy;

    public int DurationDays { get; set; }
    public int DurationNights { get; set; }

    public int? MinGuests { get; set; }
    public int? MaxGuests { get; set; }

    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    public bool? IsFeatured { get; set; }
    public bool? IsFeaturedOnHome { get; set; }
    public bool? IsPrivateTourSupported { get; set; }
    public bool? IsInstantConfirm { get; set; }

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
    public string? MetadataJson { get; set; }

    public string? CoverImageUrl { get; set; }
    public Guid? CoverMediaAssetId { get; set; }

    public string CurrencyCode { get; set; } = "VND";
    public bool? IsActive { get; set; }
}

public sealed class ToursAdminUpdateRequest
{
    public Guid? ProviderId { get; set; }
    public Guid? PrimaryLocationId { get; set; }

    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }

    public TourType? Type { get; set; }
    public TourStatus? Status { get; set; }
    public TourDifficulty? Difficulty { get; set; }

    public int? DurationDays { get; set; }
    public int? DurationNights { get; set; }

    public int? MinGuests { get; set; }
    public int? MaxGuests { get; set; }

    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }

    public bool? IsFeatured { get; set; }
    public bool? IsFeaturedOnHome { get; set; }
    public bool? IsPrivateTourSupported { get; set; }
    public bool? IsInstantConfirm { get; set; }

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
    public string? MetadataJson { get; set; }

    public string? CoverImageUrl { get; set; }
    public Guid? CoverMediaAssetId { get; set; }

    public string? CurrencyCode { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }

    public string? RowVersionBase64 { get; set; }
}

public sealed class ToursAdminCreateResponse
{
    public Guid Id { get; set; }
}
