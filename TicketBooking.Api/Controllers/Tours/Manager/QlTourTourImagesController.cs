// FILE #252: TicketBooking.Api/Controllers/Tours/QlTourTourImagesController.cs
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
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/images")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class QlTourTourImagesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QlTourTourImagesController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<QlTourImagePagedResponse>> List(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] bool? primary = null,
        [FromQuery] bool? cover = null,
        [FromQuery] bool? featured = null,
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

        IQueryable<TourImage> query = includeDeleted
            ? _db.TourImages.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourImages.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        if (primary.HasValue)
            query = query.Where(x => x.IsPrimary == primary.Value);

        if (cover.HasValue)
            query = query.Where(x => x.IsCover == cover.Value);

        if (featured.HasValue)
            query = query.Where(x => x.IsFeatured == featured.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                (x.ImageUrl != null && x.ImageUrl.Contains(qq)) ||
                (x.Caption != null && x.Caption.Contains(qq)) ||
                (x.AltText != null && x.AltText.Contains(qq)) ||
                (x.Title != null && x.Title.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(x => x.IsCover)
            .ThenByDescending(x => x.IsPrimary)
            .ThenByDescending(x => x.IsFeatured)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QlTourImageListItemDto
            {
                Id = x.Id,
                TourId = x.TourId,
                MediaAssetId = x.MediaAssetId,
                ImageUrl = x.ImageUrl,
                Caption = x.Caption,
                AltText = x.AltText,
                Title = x.Title,
                IsPrimary = x.IsPrimary,
                IsCover = x.IsCover,
                IsFeatured = x.IsFeatured,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new QlTourImagePagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QlTourImageDetailDto>> GetById(
        Guid tourId,
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        IQueryable<TourImage> query = includeDeleted
            ? _db.TourImages.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourImages.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        var entity = await query
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour image not found in current tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<QlTourCreateImageResponse>> Create(
        Guid tourId,
        [FromBody] QlTourCreateImageRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        ValidatePayload(req.ImageUrl, req.MediaAssetId, req.Caption, req.AltText, req.Title);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        if (req.IsPrimary == true)
        {
            var currentPrimary = await _db.TourImages.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.IsPrimary)
                .ToListAsync(ct);

            foreach (var old in currentPrimary)
            {
                old.IsPrimary = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }
        }

        if (req.IsCover == true)
        {
            var currentCover = await _db.TourImages.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.IsCover)
                .ToListAsync(ct);

            foreach (var old in currentCover)
            {
                old.IsCover = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }
        }

        var entity = new TourImage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tourId,
            MediaAssetId = NormalizeGuid(req.MediaAssetId),
            ImageUrl = NullIfWhiteSpace(req.ImageUrl),
            Caption = NullIfWhiteSpace(req.Caption),
            AltText = NullIfWhiteSpace(req.AltText),
            Title = NullIfWhiteSpace(req.Title),
            IsPrimary = req.IsPrimary ?? false,
            IsCover = req.IsCover ?? false,
            IsFeatured = req.IsFeatured ?? false,
            SortOrder = req.SortOrder ?? 0,
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.TourImages.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", tourId, id = entity.Id },
            new QlTourCreateImageResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid tourId,
        Guid id,
        [FromBody] QlTourUpdateImageRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour image not found in current tenant." });

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

        ValidatePayload(
            req.ImageUrl ?? entity.ImageUrl,
            req.MediaAssetId ?? entity.MediaAssetId,
            req.Caption,
            req.AltText,
            req.Title);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var nextIsPrimary = req.IsPrimary ?? entity.IsPrimary;
        var nextIsCover = req.IsCover ?? entity.IsCover;

        if (nextIsPrimary)
        {
            var currentPrimary = await _db.TourImages.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.Id != id && x.IsPrimary)
                .ToListAsync(ct);

            foreach (var old in currentPrimary)
            {
                old.IsPrimary = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }
        }

        if (nextIsCover)
        {
            var currentCover = await _db.TourImages.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.Id != id && x.IsCover)
                .ToListAsync(ct);

            foreach (var old in currentCover)
            {
                old.IsCover = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }
        }

        if (req.MediaAssetId.HasValue)
            entity.MediaAssetId = NormalizeGuid(req.MediaAssetId);

        if (req.ImageUrl is not null)
            entity.ImageUrl = NullIfWhiteSpace(req.ImageUrl);

        if (req.Caption is not null)
            entity.Caption = NullIfWhiteSpace(req.Caption);

        if (req.AltText is not null)
            entity.AltText = NullIfWhiteSpace(req.AltText);

        if (req.Title is not null)
            entity.Title = NullIfWhiteSpace(req.Title);

        if (req.IsPrimary.HasValue)
            entity.IsPrimary = req.IsPrimary.Value;

        if (req.IsCover.HasValue)
            entity.IsCover = req.IsCover.Value;

        if (req.IsFeatured.HasValue)
            entity.IsFeatured = req.IsFeatured.Value;

        if (req.SortOrder.HasValue)
            entity.SortOrder = req.SortOrder.Value;

        if (req.MetadataJson is not null)
            entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);

        if (req.IsActive.HasValue)
            entity.IsActive = req.IsActive.Value;

        if (req.IsDeleted.HasValue)
            entity.IsDeleted = req.IsDeleted.Value;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        try
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Tour image was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, id, true, ct);

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDeleted(tourId, id, false, ct);

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, id, true, ct);

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleActive(tourId, id, false, ct);

    [HttpPost("{id:guid}/set-primary")]
    public async Task<IActionResult> SetPrimary(Guid tourId, Guid id, CancellationToken ct = default)
        => await TogglePrimary(tourId, id, true, ct);

    [HttpPost("{id:guid}/unset-primary")]
    public async Task<IActionResult> UnsetPrimary(Guid tourId, Guid id, CancellationToken ct = default)
        => await TogglePrimary(tourId, id, false, ct);

    [HttpPost("{id:guid}/set-cover")]
    public async Task<IActionResult> SetCover(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleCover(tourId, id, true, ct);

    [HttpPost("{id:guid}/unset-cover")]
    public async Task<IActionResult> UnsetCover(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleCover(tourId, id, false, ct);

    [HttpPost("{id:guid}/feature")]
    public async Task<IActionResult> Feature(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleFeatured(tourId, id, true, ct);

    [HttpPost("{id:guid}/unfeature")]
    public async Task<IActionResult> Unfeature(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleFeatured(tourId, id, false, ct);

    private async Task<IActionResult> ToggleDeleted(Guid tourId, Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour image not found in current tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid tourId, Guid id, bool isActive, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour image not found in current tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> TogglePrimary(Guid tourId, Guid id, bool isPrimary, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour image not found in current tenant." });

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        if (isPrimary)
        {
            var currentPrimary = await _db.TourImages.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.Id != id && x.IsPrimary)
                .ToListAsync(ct);

            foreach (var old in currentPrimary)
            {
                old.IsPrimary = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }

            entity.IsActive = true;
        }

        entity.IsPrimary = isPrimary;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleCover(Guid tourId, Guid id, bool isCover, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour image not found in current tenant." });

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        if (isCover)
        {
            var currentCover = await _db.TourImages.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.Id != id && x.IsCover)
                .ToListAsync(ct);

            foreach (var old in currentCover)
            {
                old.IsCover = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }

            entity.IsActive = true;
        }

        entity.IsCover = isCover;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleFeatured(Guid tourId, Guid id, bool isFeatured, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour image not found in current tenant." });

        entity.IsFeatured = isFeatured;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        if (isFeatured)
            entity.IsActive = true;

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

    private static void ValidatePayload(
        string? imageUrl,
        Guid? mediaAssetId,
        string? caption,
        string? altText,
        string? title)
    {
        var normalizedUrl = NullIfWhiteSpace(imageUrl);
        var normalizedMedia = NormalizeGuid(mediaAssetId);

        if (string.IsNullOrWhiteSpace(normalizedUrl) && !normalizedMedia.HasValue)
            throw new ArgumentException("ImageUrl or MediaAssetId is required.");

        if (normalizedUrl is not null && normalizedUrl.Length > 1000)
            throw new ArgumentException("ImageUrl max length is 1000.");

        if (caption is not null && caption.Length > 500)
            throw new ArgumentException("Caption max length is 500.");

        if (altText is not null && altText.Length > 500)
            throw new ArgumentException("AltText max length is 500.");

        if (title is not null && title.Length > 500)
            throw new ArgumentException("Title max length is 500.");
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

    private static Guid? NormalizeGuid(Guid? value)
        => value.HasValue && value.Value != Guid.Empty ? value.Value : null;

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static QlTourImageDetailDto MapDetail(TourImage x)
    {
        return new QlTourImageDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            TourId = x.TourId,
            MediaAssetId = x.MediaAssetId,
            ImageUrl = x.ImageUrl,
            Caption = x.Caption,
            AltText = x.AltText,
            Title = x.Title,
            IsPrimary = x.IsPrimary,
            IsCover = x.IsCover,
            IsFeatured = x.IsFeatured,
            SortOrder = x.SortOrder,
            MetadataJson = x.MetadataJson,
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

public sealed class QlTourImagePagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<QlTourImageListItemDto> Items { get; set; } = new();
}

public sealed class QlTourImageListItemDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public Guid? MediaAssetId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsCover { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class QlTourImageDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public Guid? MediaAssetId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsCover { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreateImageRequest
{
    public Guid? MediaAssetId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public bool? IsPrimary { get; set; }
    public bool? IsCover { get; set; }
    public bool? IsFeatured { get; set; }
    public int? SortOrder { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class QlTourUpdateImageRequest
{
    public Guid? MediaAssetId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public bool? IsPrimary { get; set; }
    public bool? IsCover { get; set; }
    public bool? IsFeatured { get; set; }
    public int? SortOrder { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreateImageResponse
{
    public Guid Id { get; set; }
}
