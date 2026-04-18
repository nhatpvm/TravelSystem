// FILE #208: TicketBooking.Api/Controllers/Hotels/RoomTypesAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/room-types")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class RoomTypesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public RoomTypesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<AdminRoomTypePagedResponse<AdminRoomTypeListItemDto>>> List(
        [FromQuery] Guid? tenantId = null,
        [FromQuery] string? tenantCode = null,
        [FromQuery] Guid? hotelId = null,
        [FromQuery] string? q = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] bool activeOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 100 ? 100 : pageSize;

        var resolvedTenantId = tenantId;

        if (!resolvedTenantId.HasValue && !string.IsNullOrWhiteSpace(tenantCode))
        {
            resolvedTenantId = await _db.Tenants.IgnoreQueryFilters()
                .Where(x => x.Code == tenantCode && !x.IsDeleted)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(ct);

            if (!resolvedTenantId.HasValue)
            {
                return Ok(new AdminRoomTypePagedResponse<AdminRoomTypeListItemDto>
                {
                    Page = page,
                    PageSize = pageSize,
                    Total = 0,
                    Items = new()
                });
            }
        }

        if (!resolvedTenantId.HasValue && _tenant.HasTenant && _tenant.TenantId.HasValue)
            resolvedTenantId = _tenant.TenantId.Value;

        IQueryable<RoomType> query = includeDeleted
            ? _db.RoomTypes.IgnoreQueryFilters()
            : _db.RoomTypes.Where(x => !x.IsDeleted);

        if (resolvedTenantId.HasValue)
            query = query.Where(x => x.TenantId == resolvedTenantId.Value);

        if (hotelId.HasValue)
            query = query.Where(x => x.HotelId == hotelId.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(q) ||
                x.Name.Contains(q) ||
                (x.DescriptionMarkdown != null && x.DescriptionMarkdown.Contains(q)) ||
                (x.DescriptionHtml != null && x.DescriptionHtml.Contains(q)));
        }

        var total = await query.CountAsync(ct);

        var items = await query.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminRoomTypeListItemDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                HotelId = x.HotelId,
                Code = x.Code,
                Name = x.Name,
                SortOrder = x.SortOrder,
                AreaSquareMeters = x.AreaSquareMeters,
                DefaultAdults = x.DefaultAdults,
                DefaultChildren = x.DefaultChildren,
                MaxAdults = x.MaxAdults,
                MaxChildren = x.MaxChildren,
                MaxGuests = x.MaxGuests,
                TotalUnits = x.TotalUnits,
                Status = x.Status,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new AdminRoomTypePagedResponse<AdminRoomTypeListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminRoomTypeDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        IQueryable<RoomType> query = includeDeleted
            ? _db.RoomTypes.IgnoreQueryFilters()
            : _db.RoomTypes.Where(x => !x.IsDeleted);

        var switchedTenantId = _tenant.HasTenant && _tenant.TenantId.HasValue
            ? _tenant.TenantId.Value
            : (Guid?)null;

        var entity = await query.AsNoTracking().FirstOrDefaultAsync(x =>
            x.Id == id &&
            (!switchedTenantId.HasValue || x.TenantId == switchedTenantId.Value), ct);
        if (entity is null)
            return NotFound(new { message = "Room type not found." });

        var tenantCode = await _db.Tenants.IgnoreQueryFilters()
            .Where(x => x.Id == entity.TenantId)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(ct);

        var bedLinks = await _db.RoomTypeBeds.IgnoreQueryFilters()
            .Where(x => x.TenantId == entity.TenantId && x.RoomTypeId == id && !x.IsDeleted)
            .Join(
                _db.BedTypes.IgnoreQueryFilters().Where(x => x.TenantId == entity.TenantId && !x.IsDeleted),
                l => l.BedTypeId,
                b => b.Id,
                (l, b) => new AdminRoomTypeBedDto
                {
                    BedTypeId = b.Id,
                    BedTypeCode = b.Code,
                    BedTypeName = b.Name,
                    Quantity = l.Quantity
                })
            .ToListAsync(ct);

        var amenityLinks = await _db.RoomAmenityLinks.IgnoreQueryFilters()
            .Where(x => x.TenantId == entity.TenantId && x.RoomTypeId == id && !x.IsDeleted)
            .Join(
                _db.RoomAmenities.IgnoreQueryFilters().Where(x => x.TenantId == entity.TenantId && !x.IsDeleted),
                l => l.AmenityId,
                a => a.Id,
                (l, a) => new AdminRoomTypeAmenityDto
                {
                    AmenityId = a.Id,
                    AmenityCode = a.Code,
                    AmenityName = a.Name,
                    IsHighlighted = l.IsHighlighted,
                    Notes = l.Notes
                })
            .ToListAsync(ct);

        var occupancyRules = await _db.RoomTypeOccupancyRules.IgnoreQueryFilters()
            .Where(x => x.TenantId == entity.TenantId && x.RoomTypeId == id && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new AdminRoomTypeOccupancyRuleDto
            {
                Id = x.Id,
                MinAdults = x.MinAdults,
                MaxAdults = x.MaxAdults,
                MinChildren = x.MinChildren,
                MaxChildren = x.MaxChildren,
                MinGuests = x.MinGuests,
                MaxGuests = x.MaxGuests,
                AllowsInfants = x.AllowsInfants,
                MaxInfants = x.MaxInfants,
                Notes = x.Notes,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

        return Ok(new AdminRoomTypeDetailDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            TenantCode = tenantCode,
            HotelId = entity.HotelId,
            Code = entity.Code,
            Name = entity.Name,
            DescriptionMarkdown = entity.DescriptionMarkdown,
            DescriptionHtml = entity.DescriptionHtml,
            SortOrder = entity.SortOrder,
            AreaSquareMeters = entity.AreaSquareMeters,
            HasBalcony = entity.HasBalcony,
            HasWindow = entity.HasWindow,
            SmokingAllowed = entity.SmokingAllowed,
            DefaultAdults = entity.DefaultAdults,
            DefaultChildren = entity.DefaultChildren,
            MaxAdults = entity.MaxAdults,
            MaxChildren = entity.MaxChildren,
            MaxGuests = entity.MaxGuests,
            TotalUnits = entity.TotalUnits,
            CoverMediaAssetId = entity.CoverMediaAssetId,
            CoverImageUrl = entity.CoverImageUrl,
            Status = entity.Status,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            MetadataJson = entity.MetadataJson,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId,
            RowVersionBase64 = entity.RowVersion is { Length: > 0 } ? Convert.ToBase64String(entity.RowVersion) : null,
            Beds = bedLinks,
            Amenities = amenityLinks,
            OccupancyRules = occupancyRules
        });
    }

    [HttpPost]
    public async Task<ActionResult<AdminCreateRoomTypeResponse>> Create(
        [FromBody] AdminCreateRoomTypeRequest req,
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

        var code = req.Code!.Trim();

        var codeExists = await _db.RoomTypes.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.HotelId == req.HotelId && x.Code == code, ct);

        if (codeExists)
            return Conflict(new { message = "Room type code already exists in this hotel." });

        var entity = new RoomType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HotelId = req.HotelId,
            Code = code,
            Name = req.Name!.Trim(),
            DescriptionMarkdown = NullIfWhiteSpace(req.DescriptionMarkdown),
            DescriptionHtml = NullIfWhiteSpace(req.DescriptionHtml),
            SortOrder = req.SortOrder ?? 0,
            AreaSquareMeters = req.AreaSquareMeters,
            HasBalcony = req.HasBalcony,
            HasWindow = req.HasWindow,
            SmokingAllowed = req.SmokingAllowed,
            DefaultAdults = req.DefaultAdults ?? 1,
            DefaultChildren = req.DefaultChildren ?? 0,
            MaxAdults = req.MaxAdults ?? 1,
            MaxChildren = req.MaxChildren ?? 0,
            MaxGuests = req.MaxGuests ?? (req.MaxAdults ?? 1),
            TotalUnits = req.TotalUnits ?? 0,
            CoverMediaAssetId = req.CoverMediaAssetId,
            CoverImageUrl = NullIfWhiteSpace(req.CoverImageUrl),
            Status = req.Status ?? RoomTypeStatus.Active,
            IsActive = req.IsActive ?? true,
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.RoomTypes.Add(entity);
        await _db.SaveChangesAsync(ct);

        if (req.Beds is { Count: > 0 })
            await ReplaceBedsAsync(tenantId, entity.Id, req.Beds, now, userId, ct);

        if (req.Amenities is { Count: > 0 })
            await ReplaceAmenitiesAsync(tenantId, entity.Id, req.Amenities, now, userId, ct);

        if (req.OccupancyRules is { Count: > 0 })
            await ReplaceOccupancyRulesAsync(tenantId, entity.Id, req.OccupancyRules, now, userId, ct);

        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new AdminCreateRoomTypeResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] AdminUpdateRoomTypeRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateUpdate(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.RoomTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Room type not found in current switched tenant." });

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

            var movedCode = req.Code?.Trim() ?? entity.Code;
            var codeExists = await _db.RoomTypes.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.HotelId == req.HotelId.Value && x.Code == movedCode && x.Id != id, ct);

            if (codeExists)
                return Conflict(new { message = "Room type code already exists in target hotel." });

            entity.HotelId = req.HotelId.Value;
        }

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var newCode = req.Code.Trim();
            var codeExists = await _db.RoomTypes.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.HotelId == entity.HotelId && x.Code == newCode && x.Id != id, ct);

            if (codeExists)
                return Conflict(new { message = "Room type code already exists in this hotel." });

            entity.Code = newCode;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
            entity.Name = req.Name.Trim();

        if (req.DescriptionMarkdown is not null) entity.DescriptionMarkdown = NullIfWhiteSpace(req.DescriptionMarkdown);
        if (req.DescriptionHtml is not null) entity.DescriptionHtml = NullIfWhiteSpace(req.DescriptionHtml);
        if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;
        if (req.AreaSquareMeters.HasValue) entity.AreaSquareMeters = req.AreaSquareMeters;
        if (req.HasBalcony.HasValue) entity.HasBalcony = req.HasBalcony;
        if (req.HasWindow.HasValue) entity.HasWindow = req.HasWindow;
        if (req.SmokingAllowed.HasValue) entity.SmokingAllowed = req.SmokingAllowed;
        if (req.DefaultAdults.HasValue) entity.DefaultAdults = req.DefaultAdults.Value;
        if (req.DefaultChildren.HasValue) entity.DefaultChildren = req.DefaultChildren.Value;
        if (req.MaxAdults.HasValue) entity.MaxAdults = req.MaxAdults.Value;
        if (req.MaxChildren.HasValue) entity.MaxChildren = req.MaxChildren.Value;
        if (req.MaxGuests.HasValue) entity.MaxGuests = req.MaxGuests.Value;
        if (req.TotalUnits.HasValue) entity.TotalUnits = req.TotalUnits.Value;
        if (req.CoverMediaAssetId.HasValue) entity.CoverMediaAssetId = req.CoverMediaAssetId;
        if (req.CoverImageUrl is not null) entity.CoverImageUrl = NullIfWhiteSpace(req.CoverImageUrl);
        if (req.Status.HasValue) entity.Status = req.Status.Value;
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        if (req.Beds is not null)
            await ReplaceBedsAsync(tenantId, entity.Id, req.Beds, now, userId, ct);

        if (req.Amenities is not null)
            await ReplaceAmenitiesAsync(tenantId, entity.Id, req.Amenities, now, userId, ct);

        if (req.OccupancyRules is not null)
            await ReplaceOccupancyRulesAsync(tenantId, entity.Id, req.OccupancyRules, now, userId, ct);

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
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.RoomTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Room type not found in current switched tenant." });

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
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.RoomTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Room type not found in current switched tenant." });

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
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.RoomTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Room type not found in current switched tenant." });

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
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.RoomTypes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Room type not found in current switched tenant." });

        if (entity.IsActive)
        {
            entity.IsActive = false;
            entity.UpdatedAt = DateTimeOffset.Now;
            entity.UpdatedByUserId = userId;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    private void RequireAdminWriteTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Admin write requires switched tenant context (X-TenantId).");
    }

    private async Task ReplaceBedsAsync(
        Guid tenantId,
        Guid roomTypeId,
        List<AdminRoomTypeBedUpsertDto> items,
        DateTimeOffset now,
        Guid? userId,
        CancellationToken ct)
    {
        var existingRows = await _db.RoomTypeBeds.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.RoomTypeId == roomTypeId)
            .ToListAsync(ct);

        var bedTypeIds = items.Select(x => x.BedTypeId).Distinct().ToList();

        var validBedTypeIds = await _db.BedTypes.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && bedTypeIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);

        var invalidIds = bedTypeIds.Except(validBedTypeIds).ToList();
        if (invalidIds.Count > 0)
            throw new ArgumentException("One or more BedTypeId values are invalid in current tenant.");

        var requestedRows = items
            .GroupBy(x => x.BedTypeId)
            .Select(g => new
            {
                BedTypeId = g.Key,
                Quantity = g.First().Quantity
            })
            .ToList();

        var existingByBedTypeId = existingRows.ToDictionary(x => x.BedTypeId);
        var requestedBedTypeIds = requestedRows.Select(x => x.BedTypeId).ToHashSet();

        foreach (var item in requestedRows)
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

        foreach (var existing in existingRows)
        {
            if (requestedBedTypeIds.Contains(existing.BedTypeId) || existing.IsDeleted)
                continue;

            existing.IsDeleted = true;
            existing.UpdatedAt = now;
            existing.UpdatedByUserId = userId;
        }
    }

    private async Task ReplaceAmenitiesAsync(
        Guid tenantId,
        Guid roomTypeId,
        List<AdminRoomTypeAmenityUpsertDto> items,
        DateTimeOffset now,
        Guid? userId,
        CancellationToken ct)
    {
        var existingRows = await _db.RoomAmenityLinks.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.RoomTypeId == roomTypeId)
            .ToListAsync(ct);

        var amenityIds = items.Select(x => x.AmenityId).Distinct().ToList();

        var validAmenityIds = await _db.RoomAmenities.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && amenityIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);

        var invalidIds = amenityIds.Except(validAmenityIds).ToList();
        if (invalidIds.Count > 0)
            throw new ArgumentException("One or more AmenityId values are invalid in current tenant.");

        var requestedRows = items
            .GroupBy(x => x.AmenityId)
            .Select(g => new
            {
                AmenityId = g.Key,
                IsHighlighted = g.First().IsHighlighted ?? false,
                Notes = NullIfWhiteSpace(g.First().Notes)
            })
            .ToList();

        var existingByAmenityId = existingRows.ToDictionary(x => x.AmenityId);
        var requestedAmenityIds = requestedRows.Select(x => x.AmenityId).ToHashSet();

        foreach (var item in requestedRows)
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

            _db.RoomAmenityLinks.Add(new RoomAmenityLink
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RoomTypeId = roomTypeId,
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

        foreach (var existing in existingRows)
        {
            if (requestedAmenityIds.Contains(existing.AmenityId) || existing.IsDeleted)
                continue;

            existing.IsDeleted = true;
            existing.UpdatedAt = now;
            existing.UpdatedByUserId = userId;
        }
    }

    private async Task ReplaceOccupancyRulesAsync(
        Guid tenantId,
        Guid roomTypeId,
        List<AdminRoomTypeOccupancyRuleUpsertDto> items,
        DateTimeOffset now,
        Guid? userId,
        CancellationToken ct)
    {
        var existingRows = await _db.RoomTypeOccupancyRules.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.RoomTypeId == roomTypeId)
            .ToListAsync(ct);

        _db.RoomTypeOccupancyRules.RemoveRange(existingRows);

        if (items.Count == 0) return;

        var rows = items.Select(x => new RoomTypeOccupancyRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RoomTypeId = roomTypeId,
            MinAdults = x.MinAdults,
            MaxAdults = x.MaxAdults,
            MinChildren = x.MinChildren,
            MaxChildren = x.MaxChildren,
            MinGuests = x.MinGuests,
            MaxGuests = x.MaxGuests,
            AllowsInfants = x.AllowsInfants ?? true,
            MaxInfants = x.MaxInfants,
            Notes = NullIfWhiteSpace(x.Notes),
            IsActive = x.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        }).ToList();

        _db.RoomTypeOccupancyRules.AddRange(rows);
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static void ValidateCreate(AdminCreateRoomTypeRequest req)
    {
        if (req.HotelId == Guid.Empty)
            throw new ArgumentException("HotelId is required.");

        if (string.IsNullOrWhiteSpace(req.Code))
            throw new ArgumentException("Code is required.");

        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");

        if (req.Code.Length > 50)
            throw new ArgumentException("Code max length is 50.");

        if (req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

        if (req.AreaSquareMeters.HasValue && req.AreaSquareMeters < 0)
            throw new ArgumentException("AreaSquareMeters must be >= 0.");

        if (req.TotalUnits.HasValue && req.TotalUnits < 0)
            throw new ArgumentException("TotalUnits must be >= 0.");
    }

    private static void ValidateUpdate(AdminUpdateRoomTypeRequest req)
    {
        if (req.Code is not null && req.Code.Length > 50)
            throw new ArgumentException("Code max length is 50.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

        if (req.AreaSquareMeters.HasValue && req.AreaSquareMeters < 0)
            throw new ArgumentException("AreaSquareMeters must be >= 0.");

        if (req.TotalUnits.HasValue && req.TotalUnits < 0)
            throw new ArgumentException("TotalUnits must be >= 0.");
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class AdminRoomTypePagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}

public sealed class AdminRoomTypeListItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public int? AreaSquareMeters { get; set; }
    public int DefaultAdults { get; set; }
    public int DefaultChildren { get; set; }
    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }
    public int MaxGuests { get; set; }
    public int TotalUnits { get; set; }
    public RoomTypeStatus Status { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AdminRoomTypeDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? TenantCode { get; set; }
    public Guid HotelId { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }

    public int SortOrder { get; set; }
    public int? AreaSquareMeters { get; set; }
    public bool? HasBalcony { get; set; }
    public bool? HasWindow { get; set; }
    public bool? SmokingAllowed { get; set; }

    public int DefaultAdults { get; set; }
    public int DefaultChildren { get; set; }
    public int MaxAdults { get; set; }
    public int MaxChildren { get; set; }
    public int MaxGuests { get; set; }
    public int TotalUnits { get; set; }

    public Guid? CoverMediaAssetId { get; set; }
    public string? CoverImageUrl { get; set; }

    public RoomTypeStatus Status { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public string? MetadataJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public string? RowVersionBase64 { get; set; }

    public List<AdminRoomTypeBedDto> Beds { get; set; } = new();
    public List<AdminRoomTypeAmenityDto> Amenities { get; set; } = new();
    public List<AdminRoomTypeOccupancyRuleDto> OccupancyRules { get; set; } = new();
}

public sealed class AdminRoomTypeBedDto
{
    public Guid BedTypeId { get; set; }
    public string BedTypeCode { get; set; } = "";
    public string BedTypeName { get; set; } = "";
    public int Quantity { get; set; }
}

public sealed class AdminRoomTypeAmenityDto
{
    public Guid AmenityId { get; set; }
    public string AmenityCode { get; set; } = "";
    public string AmenityName { get; set; } = "";
    public bool IsHighlighted { get; set; }
    public string? Notes { get; set; }
}

public sealed class AdminRoomTypeOccupancyRuleDto
{
    public Guid Id { get; set; }
    public int MinAdults { get; set; }
    public int MaxAdults { get; set; }
    public int MinChildren { get; set; }
    public int MaxChildren { get; set; }
    public int MinGuests { get; set; }
    public int MaxGuests { get; set; }
    public bool AllowsInfants { get; set; }
    public int? MaxInfants { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

public sealed class AdminCreateRoomTypeRequest
{
    public Guid HotelId { get; set; }

    public string? Code { get; set; }
    public string? Name { get; set; }

    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }

    public int? SortOrder { get; set; }
    public int? AreaSquareMeters { get; set; }
    public bool? HasBalcony { get; set; }
    public bool? HasWindow { get; set; }
    public bool? SmokingAllowed { get; set; }

    public int? DefaultAdults { get; set; }
    public int? DefaultChildren { get; set; }
    public int? MaxAdults { get; set; }
    public int? MaxChildren { get; set; }
    public int? MaxGuests { get; set; }
    public int? TotalUnits { get; set; }

    public Guid? CoverMediaAssetId { get; set; }
    public string? CoverImageUrl { get; set; }

    public RoomTypeStatus? Status { get; set; }
    public bool? IsActive { get; set; }
    public string? MetadataJson { get; set; }

    public List<AdminRoomTypeBedUpsertDto>? Beds { get; set; }
    public List<AdminRoomTypeAmenityUpsertDto>? Amenities { get; set; }
    public List<AdminRoomTypeOccupancyRuleUpsertDto>? OccupancyRules { get; set; }
}

public sealed class AdminUpdateRoomTypeRequest
{
    public Guid? HotelId { get; set; }

    public string? Code { get; set; }
    public string? Name { get; set; }

    public string? DescriptionMarkdown { get; set; }
    public string? DescriptionHtml { get; set; }

    public int? SortOrder { get; set; }
    public int? AreaSquareMeters { get; set; }
    public bool? HasBalcony { get; set; }
    public bool? HasWindow { get; set; }
    public bool? SmokingAllowed { get; set; }

    public int? DefaultAdults { get; set; }
    public int? DefaultChildren { get; set; }
    public int? MaxAdults { get; set; }
    public int? MaxChildren { get; set; }
    public int? MaxGuests { get; set; }
    public int? TotalUnits { get; set; }

    public Guid? CoverMediaAssetId { get; set; }
    public string? CoverImageUrl { get; set; }

    public RoomTypeStatus? Status { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? MetadataJson { get; set; }

    public List<AdminRoomTypeBedUpsertDto>? Beds { get; set; }
    public List<AdminRoomTypeAmenityUpsertDto>? Amenities { get; set; }
    public List<AdminRoomTypeOccupancyRuleUpsertDto>? OccupancyRules { get; set; }

    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminRoomTypeBedUpsertDto
{
    public Guid BedTypeId { get; set; }
    public int Quantity { get; set; }
}

public sealed class AdminRoomTypeAmenityUpsertDto
{
    public Guid AmenityId { get; set; }
    public bool? IsHighlighted { get; set; }
    public string? Notes { get; set; }
}

public sealed class AdminRoomTypeOccupancyRuleUpsertDto
{
    public int MinAdults { get; set; }
    public int MaxAdults { get; set; }
    public int MinChildren { get; set; }
    public int MaxChildren { get; set; }
    public int MinGuests { get; set; }
    public int MaxGuests { get; set; }
    public bool? AllowsInfants { get; set; }
    public int? MaxInfants { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AdminCreateRoomTypeResponse
{
    public Guid Id { get; set; }
}

