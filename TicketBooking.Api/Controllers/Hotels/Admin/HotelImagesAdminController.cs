// FILE #218: TicketBooking.Api/Controllers/Hotels/HotelImagesAdminController.cs
using System.Security.Claims;
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
[Route("api/v{version:apiVersion}/admin/hotel-images")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class HotelImagesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public HotelImagesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<AdminHotelImagePagedResponse>> List(
        [FromQuery] Guid? hotelId = null,
        [FromQuery] string? q = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool includeInactive = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        IQueryable<HotelImage> query = includeDeleted
            ? _db.HotelImages.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.HotelImages.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (hotelId.HasValue)
            query = query.Where(x => x.HotelId == hotelId.Value);

        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                (x.Title != null && x.Title.Contains(qq)) ||
                (x.AltText != null && x.AltText.Contains(qq)) ||
                (x.ImageUrl != null && x.ImageUrl.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var rows = await query.AsNoTracking()
            .OrderByDescending(x => x.Kind == ImageKind.Cover)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = rows
            .Select(MapListItem)
            .ToList();

        return Ok(new AdminHotelImagePagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminHotelImageDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        IQueryable<HotelImage> query = includeDeleted
            ? _db.HotelImages.IgnoreQueryFilters()
            : _db.HotelImages;

        query = query.Where(x => x.TenantId == tenantId);

        if (!includeDeleted)
            query = query.Where(x => !x.IsDeleted);

        var entity = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Hotel image not found in current switched tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<AdminCreateHotelImageResponse>> Create(
        [FromBody] AdminCreateHotelImageRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateCreate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var hotelExists = await _db.Hotels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.HotelId && !x.IsDeleted, ct);

        if (!hotelExists)
            return BadRequest(new { message = "Hotel not found in current switched tenant." });

        var entity = new HotelImage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HotelId = req.HotelId,
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        entity.MediaAssetId = req.MediaAssetId;
        entity.ImageUrl = NullIfWhiteSpace(req.ImageUrl);
        entity.Title = FirstNonBlank(req.Title, req.Caption);
        entity.AltText = NullIfWhiteSpace(req.AltText);
        entity.SortOrder = req.SortOrder ?? 0;
        entity.Kind = req.IsPrimary == true ? ImageKind.Cover : ImageKind.Gallery;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        _db.HotelImages.Add(entity);
        await _db.SaveChangesAsync(ct);

        await EnsurePrimaryImageAsync(tenantId, entity.HotelId, null, userId, ct);
        await tx.CommitAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new AdminCreateHotelImageResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] AdminUpdateHotelImageRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateUpdate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();
        Guid previousHotelId;

        var entity = await _db.HotelImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel image not found in current switched tenant." });

        previousHotelId = entity.HotelId;

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

        if (req.HotelId.HasValue && req.HotelId.Value != entity.HotelId)
        {
            var hotelExists = await _db.Hotels.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == req.HotelId.Value && !x.IsDeleted, ct);

            if (!hotelExists)
                return BadRequest(new { message = "Target hotel not found in current switched tenant." });

            entity.HotelId = req.HotelId.Value;
        }

        if (req.MediaAssetId.HasValue)
            entity.MediaAssetId = req.MediaAssetId;

        if (req.ImageUrl is not null)
            entity.ImageUrl = NullIfWhiteSpace(req.ImageUrl);

        if (req.Title is not null)
            entity.Title = NullIfWhiteSpace(req.Title);
        else if (req.Caption is not null)
            entity.Title = NullIfWhiteSpace(req.Caption);

        if (req.AltText is not null)
            entity.AltText = NullIfWhiteSpace(req.AltText);

        if (req.SortOrder.HasValue)
            entity.SortOrder = req.SortOrder.Value;

        if (req.IsPrimary.HasValue)
            entity.Kind = req.IsPrimary.Value
                ? ImageKind.Cover
                : DemoteHotelImageKind(entity.Kind);

        if (req.IsActive.HasValue)
            entity.IsActive = req.IsActive.Value;

        if (req.IsDeleted.HasValue)
            entity.IsDeleted = req.IsDeleted.Value;

        if (entity.Kind == ImageKind.Cover && (!entity.IsActive || entity.IsDeleted))
            entity.Kind = DemoteHotelImageKind(entity.Kind);

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            await _db.SaveChangesAsync(ct);
            if (previousHotelId != entity.HotelId)
                await EnsurePrimaryImageAsync(tenantId, previousHotelId, null, userId, ct);

            await EnsurePrimaryImageAsync(tenantId, entity.HotelId, null, userId, ct);
            await tx.CommitAsync(ct);

            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Data was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("{id:guid}/set-primary")]
    public async Task<IActionResult> SetPrimary(Guid id, CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        var entity = await _db.HotelImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && !x.IsDeleted, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel image not found in current switched tenant." });

        if (!entity.IsActive)
            return BadRequest(new { message = "Inactive hotel image cannot be set as primary." });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await EnsurePrimaryImageAsync(tenantId, entity.HotelId, entity.Id, GetCurrentUserId(), ct);
        await tx.CommitAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        return await ToggleDeleted(id, true, ct);
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        return await ToggleDeleted(id, false, ct);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct = default)
    {
        return await ToggleActive(id, true, ct);
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct = default)
    {
        return await ToggleActive(id, false, ct);
    }

    private async Task<IActionResult> ToggleDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.HotelImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel image not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        if (!isDeleted) entity.IsActive = true;
        if (isDeleted)
            entity.Kind = DemoteHotelImageKind(entity.Kind);
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await _db.SaveChangesAsync(ct);
        await EnsurePrimaryImageAsync(tenantId, entity.HotelId, null, userId, ct);
        await tx.CommitAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid id, bool isActive, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.HotelImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel image not found in current switched tenant." });

        entity.IsActive = isActive;
        if (!isActive)
            entity.Kind = DemoteHotelImageKind(entity.Kind);
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        await _db.SaveChangesAsync(ct);
        await EnsurePrimaryImageAsync(tenantId, entity.HotelId, null, userId, ct);
        await tx.CommitAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task SetPrimaryInternalAsync(HotelImage entity, CancellationToken ct)
    {
        await EnsurePrimaryImageAsync(entity.TenantId, entity.HotelId, entity.Id, GetCurrentUserId(), ct);
    }

    private async Task EnsurePrimaryImageAsync(
        Guid tenantId,
        Guid hotelId,
        Guid? preferredImageId,
        Guid? userId,
        CancellationToken ct)
    {
        var now = DateTimeOffset.Now;
        var images = await _db.HotelImages.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.HotelId == hotelId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(ct);

        var candidates = images
            .Where(x => !x.IsDeleted && x.IsActive)
            .ToList();

        HotelImage? selected = null;
        if (preferredImageId.HasValue)
            selected = candidates.FirstOrDefault(x => x.Id == preferredImageId.Value);

        selected ??= candidates.FirstOrDefault(x => x.Kind == ImageKind.Cover);
        selected ??= candidates.FirstOrDefault();

        var hasChanges = false;

        foreach (var image in images)
        {
            var desiredKind = selected is not null && image.Id == selected.Id
                ? ImageKind.Cover
                : DemoteHotelImageKind(image.Kind);

            if (image.Kind == desiredKind)
                continue;

            image.Kind = desiredKind;
            image.UpdatedAt = now;
            image.UpdatedByUserId = userId;
            hasChanges = true;
        }

        if (hasChanges)
            await _db.SaveChangesAsync(ct);
    }

    private void RequireAdminWriteTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Admin write requires switched tenant context (X-TenantId).");
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static void ValidateCreate(AdminCreateHotelImageRequest req)
    {
        if (req.HotelId == Guid.Empty)
            throw new ArgumentException("HotelId is required.");

        if (req.MediaAssetId is null && string.IsNullOrWhiteSpace(req.ImageUrl))
            throw new ArgumentException("Either MediaAssetId or ImageUrl is required.");

        if (req.Caption is not null && req.Caption.Length > 500)
            throw new ArgumentException("Caption max length is 500.");

        if (req.AltText is not null && req.AltText.Length > 500)
            throw new ArgumentException("AltText max length is 500.");

        if (req.Title is not null && req.Title.Length > 500)
            throw new ArgumentException("Title max length is 500.");
    }

    private static void ValidateUpdate(AdminUpdateHotelImageRequest req)
    {
        if (req.Caption is not null && req.Caption.Length > 500)
            throw new ArgumentException("Caption max length is 500.");

        if (req.AltText is not null && req.AltText.Length > 500)
            throw new ArgumentException("AltText max length is 500.");

        if (req.Title is not null && req.Title.Length > 500)
            throw new ArgumentException("Title max length is 500.");
    }

    private static AdminHotelImageListItemDto MapListItem(HotelImage x)
    {
        return new AdminHotelImageListItemDto
        {
            Id = x.Id,
            HotelId = x.HotelId,
            ImageUrl = x.ImageUrl,
            Caption = x.Title,
            AltText = x.AltText,
            Title = x.Title,
            MediaAssetId = x.MediaAssetId,
            IsPrimary = x.Kind == ImageKind.Cover,
            SortOrder = x.SortOrder,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }

    private static AdminHotelImageDetailDto MapDetail(HotelImage x)
    {
        return new AdminHotelImageDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            HotelId = x.HotelId,
            ImageUrl = x.ImageUrl,
            Caption = x.Title,
            AltText = x.AltText,
            Title = x.Title,
            MediaAssetId = x.MediaAssetId,
            IsPrimary = x.Kind == ImageKind.Cover,
            SortOrder = x.SortOrder,
            MetadataJson = null,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            CreatedByUserId = x.CreatedByUserId,
            UpdatedAt = x.UpdatedAt,
            UpdatedByUserId = x.UpdatedByUserId,
            RowVersionBase64 = x.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
        };
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? FirstNonBlank(string? primary, string? fallback)
        => NullIfWhiteSpace(primary) ?? NullIfWhiteSpace(fallback);

    private static ImageKind DemoteHotelImageKind(ImageKind kind)
        => kind == ImageKind.Cover ? ImageKind.Gallery : kind;
}

public sealed class AdminHotelImagePagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminHotelImageListItemDto> Items { get; set; } = new();
}

public sealed class AdminHotelImageListItemDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public Guid? MediaAssetId { get; set; }
    public bool? IsPrimary { get; set; }
    public int? SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AdminHotelImageDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public Guid? MediaAssetId { get; set; }
    public bool? IsPrimary { get; set; }
    public int? SortOrder { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminCreateHotelImageRequest
{
    public Guid HotelId { get; set; }
    public Guid? MediaAssetId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public bool? IsPrimary { get; set; }
    public int? SortOrder { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AdminUpdateHotelImageRequest
{
    public Guid? HotelId { get; set; }
    public Guid? MediaAssetId { get; set; }
    public string? ImageUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public string? Title { get; set; }
    public bool? IsPrimary { get; set; }
    public int? SortOrder { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminCreateHotelImageResponse
{
    public Guid Id { get; set; }
}
