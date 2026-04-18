// FILE #224: TicketBooking.Api/Controllers/Hotels/BedTypesAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/bed-types")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class BedTypesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public BedTypesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // =========================================================
    // DICTIONARY CRUD
    // =========================================================

    [HttpGet]
    public async Task<ActionResult<AdminBedTypePagedResponse>> List(
        [FromQuery] string? q = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        IQueryable<BedType> query = includeDeleted
            ? _db.BedTypes.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.BedTypes.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(qq) ||
                x.Name.Contains(qq) ||
                (x.Description != null && x.Description.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminBedTypeListItemDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Category = null,
                SortOrder = null,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new AdminBedTypePagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminBedTypeDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        IQueryable<BedType> query = includeDeleted
            ? _db.BedTypes.IgnoreQueryFilters()
            : _db.BedTypes;

        query = query.Where(x => x.TenantId == tenantId);

        if (!includeDeleted)
            query = query.Where(x => !x.IsDeleted);

        var entity = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Bed type not found in current switched tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<AdminCreateBedTypeResponse>> Create(
        [FromBody] AdminCreateBedTypeRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateCreate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var code = req.Code!.Trim();

        var exists = await _db.BedTypes.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (exists)
            return Conflict(new { message = "Bed type code already exists in current switched tenant." });

        var entity = new BedType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code,
            Name = req.Name!.Trim(),
            Description = NullIfWhiteSpace(req.Description),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.BedTypes.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new AdminCreateBedTypeResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] AdminUpdateBedTypeRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateUpdate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.BedTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Bed type not found in current switched tenant." });

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
            var code = req.Code.Trim();

            var exists = await _db.BedTypes.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Code == code && x.Id != id, ct);

            if (exists)
                return Conflict(new { message = "Bed type code already exists in current switched tenant." });

            entity.Code = code;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
            entity.Name = req.Name.Trim();

        if (req.Description is not null)
            entity.Description = NullIfWhiteSpace(req.Description);

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

    // =========================================================
    // ROOM TYPE LINKING
    // =========================================================

    [HttpGet("room-types/{roomTypeId:guid}/links")]
    public async Task<ActionResult<List<AdminRoomTypeBedLinkDto>>> GetRoomTypeLinks(
        Guid roomTypeId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        var roomTypeExists = await _db.RoomTypes.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == roomTypeId && !x.IsDeleted, ct);

        if (!roomTypeExists)
            return NotFound(new { message = "Room type not found in current switched tenant." });

        IQueryable<RoomTypeBed> query = includeDeleted
            ? _db.RoomTypeBeds.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.RoomTypeBeds.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        var links = await query
            .Where(x => x.RoomTypeId == roomTypeId)
            .ToListAsync(ct);

        var bedTypeIds = links.Select(x => x.BedTypeId).Distinct().ToList();

        var bedTypes = await _db.BedTypes.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && bedTypeIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var result = links
            .OrderBy(x => bedTypes.TryGetValue(x.BedTypeId, out var b) ? b.Name : string.Empty)
            .Select(x =>
            {
                bedTypes.TryGetValue(x.BedTypeId, out var bedType);

                return new AdminRoomTypeBedLinkDto
                {
                    Id = x.Id,
                    RoomTypeId = x.RoomTypeId,
                    BedTypeId = x.BedTypeId,
                    BedTypeCode = bedType?.Code,
                    BedTypeName = bedType?.Name,
                    Quantity = x.Quantity,
                    IsDeleted = x.IsDeleted
                };
            })
            .ToList();

        return Ok(result);
    }

    [HttpPut("room-types/{roomTypeId:guid}/links")]
    public async Task<IActionResult> ReplaceRoomTypeLinks(
        Guid roomTypeId,
        [FromBody] AdminReplaceRoomTypeBedLinksRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var roomTypeExists = await _db.RoomTypes.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == roomTypeId && !x.IsDeleted, ct);

        if (!roomTypeExists)
            return NotFound(new { message = "Room type not found in current switched tenant." });

        var bedTypeIds = req.Items.Select(x => x.BedTypeId).Distinct().ToList();

        if (bedTypeIds.Count > 0)
        {
            var validIds = await _db.BedTypes.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted && bedTypeIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(ct);

            var invalidIds = bedTypeIds.Except(validIds).ToList();
            if (invalidIds.Count > 0)
                return BadRequest(new { message = "One or more BedTypeId values are invalid in current switched tenant." });
        }

        var existingLinks = await _db.RoomTypeBeds.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.RoomTypeId == roomTypeId)
            .ToListAsync(ct);

        var requestedLinks = req.Items
            .GroupBy(x => x.BedTypeId)
            .Select(g => new
            {
                BedTypeId = g.Key,
                Quantity = g.First().Quantity
            })
            .ToList();

        var existingByBedTypeId = existingLinks.ToDictionary(x => x.BedTypeId);
        var requestedBedTypeIds = requestedLinks.Select(x => x.BedTypeId).ToHashSet();

        foreach (var item in requestedLinks)
        {
            if (existingByBedTypeId.TryGetValue(item.BedTypeId, out var existing))
            {
                existing.Quantity = item.Quantity;
                existing.IsDeleted = false;
                existing.UpdatedAt = now;
                existing.UpdatedByUserId = userId;
                continue;
            }

            _db.RoomTypeBeds.Add(new RoomTypeBed
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RoomTypeId = roomTypeId,
                BedTypeId = item.BedTypeId,
                Quantity = item.Quantity,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId,
                UpdatedAt = now,
                UpdatedByUserId = userId
            });
        }

        foreach (var existing in existingLinks)
        {
            if (requestedBedTypeIds.Contains(existing.BedTypeId) || existing.IsDeleted)
                continue;

            existing.IsDeleted = true;
            existing.UpdatedAt = now;
            existing.UpdatedByUserId = userId;
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.BedTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Bed type not found in current switched tenant." });

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

        var entity = await _db.BedTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Bed type not found in current switched tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
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

    private static void ValidateCreate(AdminCreateBedTypeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Code))
            throw new ArgumentException("Code is required.");

        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");

        if (req.Code.Length > 80)
            throw new ArgumentException("Code max length is 80.");

        if (req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

    }

    private static void ValidateUpdate(AdminUpdateBedTypeRequest req)
    {
        if (req.Code is not null && req.Code.Length > 80)
            throw new ArgumentException("Code max length is 80.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

    }

    private static AdminBedTypeListItemDto MapListItem(BedType x)
    {
        return new AdminBedTypeListItemDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Category = null,
            SortOrder = null,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }

    private static AdminBedTypeDetailDto MapDetail(BedType x)
    {
        return new AdminBedTypeDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            Category = null,
            SortOrder = null,
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
}

public sealed class AdminBedTypePagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminBedTypeListItemDto> Items { get; set; } = new();
}

public sealed class AdminBedTypeListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public int? SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AdminBedTypeDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
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

public sealed class AdminRoomTypeBedLinkDto
{
    public Guid Id { get; set; }
    public Guid RoomTypeId { get; set; }
    public Guid BedTypeId { get; set; }
    public string? BedTypeCode { get; set; }
    public string? BedTypeName { get; set; }
    public int Quantity { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class AdminCreateBedTypeRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int? SortOrder { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AdminUpdateBedTypeRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public int? SortOrder { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminReplaceRoomTypeBedLinksRequest
{
    public List<AdminReplaceRoomTypeBedLinkItem> Items { get; set; } = new();
}

public sealed class AdminReplaceRoomTypeBedLinkItem
{
    public Guid BedTypeId { get; set; }
    public int Quantity { get; set; }
}

public sealed class AdminCreateBedTypeResponse
{
    public Guid Id { get; set; }
}
