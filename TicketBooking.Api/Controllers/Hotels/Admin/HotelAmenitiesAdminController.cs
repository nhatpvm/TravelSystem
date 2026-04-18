// FILE #222: TicketBooking.Api/Controllers/Hotels/HotelAmenitiesAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/hotel-amenities")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class HotelAmenitiesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public HotelAmenitiesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // =========================================================
    // DICTIONARY CRUD
    // =========================================================

    [HttpGet]
    public async Task<ActionResult<AdminHotelAmenityPagedResponse>> List(
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

        IQueryable<HotelAmenity> query = includeDeleted
            ? _db.HotelAmenities.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.HotelAmenities.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(qq) ||
                x.Name.Contains(qq) ||
                (x.Description != null && x.Description.Contains(qq)) ||
                (x.IconKey != null && x.IconKey.Contains(qq)) ||
                (x.MetadataJson != null && x.MetadataJson.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminHotelAmenityListItemDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Category = x.Kind.ToString(),
                IconUrl = x.IconKey,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new AdminHotelAmenityPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminHotelAmenityDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        IQueryable<HotelAmenity> query = includeDeleted
            ? _db.HotelAmenities.IgnoreQueryFilters()
            : _db.HotelAmenities;

        query = query.Where(x => x.TenantId == tenantId);

        if (!includeDeleted)
            query = query.Where(x => !x.IsDeleted);

        var entity = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Hotel amenity not found in current switched tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<AdminCreateHotelAmenityResponse>> Create(
        [FromBody] AdminCreateHotelAmenityRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateCreate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var code = req.Code!.Trim();

        var exists = await _db.HotelAmenities.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, ct);

        if (exists)
            return Conflict(new { message = "Hotel amenity code already exists in current switched tenant." });

        var entity = new HotelAmenity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Scope = AmenityScope.Hotel,
            Kind = ParseAmenityKind(req.Category),
            Code = code,
            Name = req.Name!.Trim(),
            Description = NullIfWhiteSpace(req.Description),
            SortOrder = req.SortOrder ?? 0,
            IconKey = NullIfWhiteSpace(req.IconUrl),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.HotelAmenities.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new AdminCreateHotelAmenityResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] AdminUpdateHotelAmenityRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateUpdate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.HotelAmenities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel amenity not found in current switched tenant." });

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
            var exists = await _db.HotelAmenities.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Code == code && x.Id != id, ct);

            if (exists)
                return Conflict(new { message = "Hotel amenity code already exists in current switched tenant." });

            entity.Code = code;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
            entity.Name = req.Name.Trim();

        if (req.Description is not null) entity.Description = NullIfWhiteSpace(req.Description);
        if (req.Category is not null) entity.Kind = ParseAmenityKind(req.Category, entity.Kind);
        if (req.IconUrl is not null) entity.IconKey = NullIfWhiteSpace(req.IconUrl);
        if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;
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
    // HOTEL LINKING
    // =========================================================

    [HttpGet("hotels/{hotelId:guid}/links")]
    public async Task<ActionResult<List<AdminHotelAmenityLinkDto>>> GetHotelLinks(
        Guid hotelId,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        var hotelExists = await _db.Hotels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == hotelId && !x.IsDeleted, ct);

        if (!hotelExists)
            return NotFound(new { message = "Hotel not found in current switched tenant." });

        IQueryable<HotelAmenityLink> query = includeDeleted
            ? _db.HotelAmenityLinks.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.HotelAmenityLinks.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        var links = await query
            .Where(x => x.HotelId == hotelId)
            .ToListAsync(ct);

        var amenityIds = links.Select(x => x.AmenityId).Distinct().ToList();

        var amenities = await _db.HotelAmenities.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && amenityIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, ct);

        var result = links
            .OrderByDescending(x => x.IsHighlighted)
            .ThenBy(x => amenities.TryGetValue(x.AmenityId, out var a) ? a.Name : string.Empty)
            .Select(x =>
            {
                amenities.TryGetValue(x.AmenityId, out var amenity);

                return new AdminHotelAmenityLinkDto
                {
                    Id = x.Id,
                    HotelId = x.HotelId,
                    AmenityId = x.AmenityId,
                    AmenityCode = amenity?.Code,
                    AmenityName = amenity?.Name,
                    IsHighlighted = x.IsHighlighted,
                    SortOrder = null,
                    Notes = x.Notes,
                    IsDeleted = x.IsDeleted
                };
            })
            .ToList();

        return Ok(result);
    }

    [HttpPut("hotels/{hotelId:guid}/links")]
    public async Task<IActionResult> ReplaceHotelLinks(
        Guid hotelId,
        [FromBody] AdminReplaceHotelAmenityLinksRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var hotelExists = await _db.Hotels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == hotelId && !x.IsDeleted, ct);

        if (!hotelExists)
            return NotFound(new { message = "Hotel not found in current switched tenant." });

        var amenityIds = req.Items.Select(x => x.AmenityId).Distinct().ToList();

        if (amenityIds.Count > 0)
        {
            var validIds = await _db.HotelAmenities.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && !x.IsDeleted && amenityIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(ct);

            var invalidIds = amenityIds.Except(validIds).ToList();
            if (invalidIds.Count > 0)
                return BadRequest(new { message = "One or more AmenityId values are invalid in current switched tenant." });
        }

        var existingLinks = await _db.HotelAmenityLinks.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.HotelId == hotelId)
            .ToListAsync(ct);

        var requestedLinks = req.Items
            .GroupBy(x => x.AmenityId)
            .Select(g => new
            {
                AmenityId = g.Key,
                IsHighlighted = g.First().IsHighlighted ?? false,
                Notes = NullIfWhiteSpace(g.First().Notes)
            })
            .ToList();

        var existingByAmenityId = existingLinks.ToDictionary(x => x.AmenityId);
        var requestedAmenityIds = requestedLinks.Select(x => x.AmenityId).ToHashSet();

        foreach (var item in requestedLinks)
        {
            if (existingByAmenityId.TryGetValue(item.AmenityId, out var existing))
            {
                existing.IsHighlighted = item.IsHighlighted;
                existing.Notes = item.Notes;
                existing.IsDeleted = false;
                existing.UpdatedAt = now;
                existing.UpdatedByUserId = userId;
                continue;
            }

            _db.HotelAmenityLinks.Add(new HotelAmenityLink
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                HotelId = hotelId,
                AmenityId = item.AmenityId,
                IsHighlighted = item.IsHighlighted,
                Notes = item.Notes,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId,
                UpdatedAt = now,
                UpdatedByUserId = userId
            });
        }

        foreach (var existing in existingLinks)
        {
            if (requestedAmenityIds.Contains(existing.AmenityId) || existing.IsDeleted)
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

        var entity = await _db.HotelAmenities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel amenity not found in current switched tenant." });

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

        var entity = await _db.HotelAmenities.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Hotel amenity not found in current switched tenant." });

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

    private static void ValidateCreate(AdminCreateHotelAmenityRequest req)
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

    private static void ValidateUpdate(AdminUpdateHotelAmenityRequest req)
    {
        if (req.Code is not null && req.Code.Length > 80)
            throw new ArgumentException("Code max length is 80.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");
    }

    private static AdminHotelAmenityListItemDto MapListItem(HotelAmenity x)
    {
        return new AdminHotelAmenityListItemDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Category = x.Kind.ToString(),
            IconUrl = x.IconKey,
            SortOrder = x.SortOrder,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        };
    }

    private static AdminHotelAmenityDetailDto MapDetail(HotelAmenity x)
    {
        return new AdminHotelAmenityDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            Code = x.Code,
            Name = x.Name,
            Description = x.Description,
            Category = x.Kind.ToString(),
            IconUrl = x.IconKey,
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

    private static AmenityKind ParseAmenityKind(string? rawValue, AmenityKind fallback = AmenityKind.General)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return fallback;

        if (Enum.TryParse<AmenityKind>(rawValue.Trim(), true, out var kind))
            return kind;

        throw new ArgumentException("Category must match a valid AmenityKind value.");
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class AdminHotelAmenityPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminHotelAmenityListItemDto> Items { get; set; } = new();
}

public sealed class AdminHotelAmenityListItemDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string? IconUrl { get; set; }
    public int? SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AdminHotelAmenityDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? IconUrl { get; set; }
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

public sealed class AdminHotelAmenityLinkDto
{
    public Guid Id { get; set; }
    public Guid HotelId { get; set; }
    public Guid AmenityId { get; set; }
    public string? AmenityCode { get; set; }
    public string? AmenityName { get; set; }
    public bool? IsHighlighted { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
}

public sealed class AdminCreateHotelAmenityRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? IconUrl { get; set; }
    public int? SortOrder { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AdminUpdateHotelAmenityRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? IconUrl { get; set; }
    public int? SortOrder { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminReplaceHotelAmenityLinksRequest
{
    public List<AdminReplaceHotelAmenityLinkItem> Items { get; set; } = new();
}

public sealed class AdminReplaceHotelAmenityLinkItem
{
    public Guid AmenityId { get; set; }
    public bool? IsHighlighted { get; set; }
    public int? SortOrder { get; set; }
    public string? Notes { get; set; }
}

public sealed class AdminCreateHotelAmenityResponse
{
    public Guid Id { get; set; }
}
