// FILE #219: TicketBooking.Api/Controllers/Hotels/RoomTypeImagesAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/room-type-images")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class RoomTypeImagesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public RoomTypeImagesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<AdminRoomTypeImagePagedResponse>> List(
        [FromQuery] Guid? roomTypeId = null,
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

        IQueryable<RoomTypeImage> query = includeDeleted
            ? _db.RoomTypeImages.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.RoomTypeImages.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (roomTypeId.HasValue)
            query = query.Where(x => x.RoomTypeId == roomTypeId.Value);

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

        return Ok(new AdminRoomTypeImagePagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminRoomTypeImageDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        IQueryable<RoomTypeImage> query = includeDeleted
            ? _db.RoomTypeImages.IgnoreQueryFilters()
            : _db.RoomTypeImages;

        query = query.Where(x => x.TenantId == tenantId);

        if (!includeDeleted)
            query = query.Where(x => !x.IsDeleted);

        var entity = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Room type image not found in current switched tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<AdminCreateRoomTypeImageResponse>> Create(
        [FromBody] AdminCreateRoomTypeImageRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateCreate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var roomTypeExists = await _db.RoomTypes.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.RoomTypeId && !x.IsDeleted, ct);

        if (!roomTypeExists)
            return BadRequest(new { message = "Room type not found in current switched tenant." });

        var entity = new RoomTypeImage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RoomTypeId = req.RoomTypeId,
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
        entity.Kind = req.IsPrimary == true ? ImageKind.Cover : ImageKind.Room;

        if (req.IsPrimary == true && !entity.IsActive)
            return BadRequest(new { message = "Inactive room type image cannot be set as primary." });

        _db.RoomTypeImages.Add(entity);
        await _db.SaveChangesAsync(ct);

        if (req.IsPrimary == true)
            await SetPrimaryInternalAsync(entity, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new AdminCreateRoomTypeImageResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] AdminUpdateRoomTypeImageRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateUpdate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.RoomTypeImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Room type image not found in current switched tenant." });

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

        if (req.RoomTypeId.HasValue && req.RoomTypeId.Value != entity.RoomTypeId)
        {
            var roomTypeExists = await _db.RoomTypes.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == req.RoomTypeId.Value && !x.IsDeleted, ct);

            if (!roomTypeExists)
                return BadRequest(new { message = "Target room type not found in current switched tenant." });

            entity.RoomTypeId = req.RoomTypeId.Value;
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
                : DemoteRoomTypeImageKind(entity.Kind);

        if (req.IsActive.HasValue)
            entity.IsActive = req.IsActive.Value;

        if (req.IsDeleted.HasValue)
            entity.IsDeleted = req.IsDeleted.Value;

        if (entity.Kind == ImageKind.Cover && (!entity.IsActive || entity.IsDeleted))
            entity.Kind = DemoteRoomTypeImageKind(entity.Kind);

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        try
        {
            await _db.SaveChangesAsync(ct);

            if (req.IsPrimary == true)
                await SetPrimaryInternalAsync(entity, ct);

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

        var entity = await _db.RoomTypeImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id && !x.IsDeleted, ct);

        if (entity is null)
            return NotFound(new { message = "Room type image not found in current switched tenant." });

        if (!entity.IsActive)
            return BadRequest(new { message = "Inactive room type image cannot be set as primary." });

        await SetPrimaryInternalAsync(entity, ct);
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

        var entity = await _db.RoomTypeImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Room type image not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        if (!isDeleted) entity.IsActive = true;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleActive(Guid id, bool isActive, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.RoomTypeImages.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Room type image not found in current switched tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task SetPrimaryInternalAsync(RoomTypeImage entity, CancellationToken ct)
    {
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var others = await _db.RoomTypeImages.IgnoreQueryFilters()
            .Where(x =>
                x.TenantId == entity.TenantId &&
                x.RoomTypeId == entity.RoomTypeId &&
                x.Id != entity.Id &&
                !x.IsDeleted)
            .ToListAsync(ct);

        foreach (var other in others)
        {
            other.Kind = DemoteRoomTypeImageKind(other.Kind);
            other.UpdatedAt = now;
            other.UpdatedByUserId = userId;
        }

        entity.Kind = ImageKind.Cover;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

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

    private static void ValidateCreate(AdminCreateRoomTypeImageRequest req)
    {
        if (req.RoomTypeId == Guid.Empty)
            throw new ArgumentException("RoomTypeId is required.");

        if (req.MediaAssetId is null && string.IsNullOrWhiteSpace(req.ImageUrl))
            throw new ArgumentException("Either MediaAssetId or ImageUrl is required.");

        if (req.Caption is not null && req.Caption.Length > 500)
            throw new ArgumentException("Caption max length is 500.");

        if (req.AltText is not null && req.AltText.Length > 500)
            throw new ArgumentException("AltText max length is 500.");

        if (req.Title is not null && req.Title.Length > 500)
            throw new ArgumentException("Title max length is 500.");
    }

    private static void ValidateUpdate(AdminUpdateRoomTypeImageRequest req)
    {
        if (req.Caption is not null && req.Caption.Length > 500)
            throw new ArgumentException("Caption max length is 500.");

        if (req.AltText is not null && req.AltText.Length > 500)
            throw new ArgumentException("AltText max length is 500.");

        if (req.Title is not null && req.Title.Length > 500)
            throw new ArgumentException("Title max length is 500.");
    }

    private static AdminRoomTypeImageListItemDto MapListItem(RoomTypeImage x)
    {
        return new AdminRoomTypeImageListItemDto
        {
            Id = x.Id,
            RoomTypeId = x.RoomTypeId,
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

    private static AdminRoomTypeImageDetailDto MapDetail(RoomTypeImage x)
    {
        return new AdminRoomTypeImageDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            RoomTypeId = x.RoomTypeId,
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

    private static ImageKind DemoteRoomTypeImageKind(ImageKind kind)
        => kind == ImageKind.Cover ? ImageKind.Room : kind;
}

public sealed class AdminRoomTypeImagePagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminRoomTypeImageListItemDto> Items { get; set; } = new();
}

public sealed class AdminRoomTypeImageListItemDto
{
    public Guid Id { get; set; }
    public Guid RoomTypeId { get; set; }
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

public sealed class AdminRoomTypeImageDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RoomTypeId { get; set; }
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

public sealed class AdminCreateRoomTypeImageRequest
{
    public Guid RoomTypeId { get; set; }
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

public sealed class AdminUpdateRoomTypeImageRequest
{
    public Guid? RoomTypeId { get; set; }
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

public sealed class AdminCreateRoomTypeImageResponse
{
    public Guid Id { get; set; }
}
