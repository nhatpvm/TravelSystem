// FILE #255: TicketBooking.Api/Controllers/Tours/QlTourTourPickupDropoffController.cs
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
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/pickup-dropoff")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class QlTourTourPickupDropoffController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QlTourTourPickupDropoffController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // =========================================================
    // PICKUP POINTS
    // =========================================================

    [HttpGet("pickup-points")]
    public async Task<ActionResult<QlTourPickupPointPagedResponse>> ListPickupPoints(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] bool? isDefault = null,
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

        IQueryable<TourPickupPoint> query = includeDeleted
            ? _db.TourPickupPoints.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourPickupPoints.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        if (isDefault.HasValue)
            query = query.Where(x => x.IsDefault == isDefault.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(qq) ||
                x.Name.Contains(qq) ||
                (x.AddressLine != null && x.AddressLine.Contains(qq)) ||
                (x.Ward != null && x.Ward.Contains(qq)) ||
                (x.District != null && x.District.Contains(qq)) ||
                (x.Province != null && x.Province.Contains(qq)) ||
                (x.Notes != null && x.Notes.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QlTourPickupPointListItemDto
            {
                Id = x.Id,
                TourId = x.TourId,
                Code = x.Code,
                Name = x.Name,
                AddressLine = x.AddressLine,
                Ward = x.Ward,
                District = x.District,
                Province = x.Province,
                CountryCode = x.CountryCode,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                PickupTime = x.PickupTime,
                IsDefault = x.IsDefault,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new QlTourPickupPointPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("pickup-points/{id:guid}")]
    public async Task<ActionResult<QlTourPickupPointDetailDto>> GetPickupPointById(
        Guid tourId,
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        IQueryable<TourPickupPoint> query = includeDeleted
            ? _db.TourPickupPoints.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourPickupPoints.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        var entity = await query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Tour pickup point not found in current tenant." });

        return Ok(MapPickupDetail(entity));
    }

    [HttpPost("pickup-points")]
    public async Task<ActionResult<QlTourCreatePickupPointResponse>> CreatePickupPoint(
        Guid tourId,
        [FromBody] QlTourCreatePickupPointRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await ValidateCreatePickupAsync(tenantId, tourId, req, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        if (req.IsDefault == true)
        {
            var defaults = await _db.TourPickupPoints.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.IsDefault)
                .ToListAsync(ct);

            foreach (var old in defaults)
            {
                old.IsDefault = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }
        }

        var entity = new TourPickupPoint
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tourId,
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            AddressLine = NullIfWhiteSpace(req.AddressLine),
            Ward = NullIfWhiteSpace(req.Ward),
            District = NullIfWhiteSpace(req.District),
            Province = NullIfWhiteSpace(req.Province),
            CountryCode = NullIfWhiteSpace(req.CountryCode),
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            PickupTime = req.PickupTime,
            Notes = NullIfWhiteSpace(req.Notes),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsDefault = req.IsDefault ?? false,
            SortOrder = req.SortOrder ?? 0,
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.TourPickupPoints.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetPickupPointById),
            new { version = "1.0", tourId, id = entity.Id },
            new QlTourCreatePickupPointResponse { Id = entity.Id });
    }

    [HttpPut("pickup-points/{id:guid}")]
    public async Task<IActionResult> UpdatePickupPoint(
        Guid tourId,
        Guid id,
        [FromBody] QlTourUpdatePickupPointRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourPickupPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour pickup point not found in current tenant." });

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

        await ValidateUpdatePickupAsync(tenantId, tourId, id, req, entity, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();
        var nextIsDefault = req.IsDefault ?? entity.IsDefault;

        if (nextIsDefault)
        {
            var defaults = await _db.TourPickupPoints.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.Id != id && x.IsDefault)
                .ToListAsync(ct);

            foreach (var old in defaults)
            {
                old.IsDefault = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }
        }

        if (req.Code is not null) entity.Code = req.Code.Trim();
        if (req.Name is not null) entity.Name = req.Name.Trim();
        if (req.AddressLine is not null) entity.AddressLine = NullIfWhiteSpace(req.AddressLine);
        if (req.Ward is not null) entity.Ward = NullIfWhiteSpace(req.Ward);
        if (req.District is not null) entity.District = NullIfWhiteSpace(req.District);
        if (req.Province is not null) entity.Province = NullIfWhiteSpace(req.Province);
        if (req.CountryCode is not null) entity.CountryCode = NullIfWhiteSpace(req.CountryCode);
        if (req.Latitude.HasValue) entity.Latitude = req.Latitude;
        if (req.Longitude.HasValue) entity.Longitude = req.Longitude;
        if (req.PickupTime.HasValue) entity.PickupTime = req.PickupTime;
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsDefault.HasValue) entity.IsDefault = req.IsDefault.Value;
        if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;
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
            return Conflict(new { message = "Tour pickup point was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("pickup-points/{id:guid}/delete")]
    public async Task<IActionResult> DeletePickupPoint(Guid tourId, Guid id, CancellationToken ct = default)
        => await TogglePickupDeleted(tourId, id, true, ct);

    [HttpPost("pickup-points/{id:guid}/restore")]
    public async Task<IActionResult> RestorePickupPoint(Guid tourId, Guid id, CancellationToken ct = default)
        => await TogglePickupDeleted(tourId, id, false, ct);

    [HttpPost("pickup-points/{id:guid}/activate")]
    public async Task<IActionResult> ActivatePickupPoint(Guid tourId, Guid id, CancellationToken ct = default)
        => await TogglePickupActive(tourId, id, true, ct);

    [HttpPost("pickup-points/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivatePickupPoint(Guid tourId, Guid id, CancellationToken ct = default)
        => await TogglePickupActive(tourId, id, false, ct);

    [HttpPost("pickup-points/{id:guid}/set-default")]
    public async Task<IActionResult> SetDefaultPickupPoint(Guid tourId, Guid id, CancellationToken ct = default)
        => await TogglePickupDefault(tourId, id, true, ct);

    [HttpPost("pickup-points/{id:guid}/unset-default")]
    public async Task<IActionResult> UnsetDefaultPickupPoint(Guid tourId, Guid id, CancellationToken ct = default)
        => await TogglePickupDefault(tourId, id, false, ct);

    // =========================================================
    // DROPOFF POINTS
    // =========================================================

    [HttpGet("dropoff-points")]
    public async Task<ActionResult<QlTourDropoffPointPagedResponse>> ListDropoffPoints(
        Guid tourId,
        [FromQuery] string? q = null,
        [FromQuery] bool? isDefault = null,
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

        IQueryable<TourDropoffPoint> query = includeDeleted
            ? _db.TourDropoffPoints.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourDropoffPoints.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        if (isDefault.HasValue)
            query = query.Where(x => x.IsDefault == isDefault.Value);

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x =>
                x.Code.Contains(qq) ||
                x.Name.Contains(qq) ||
                (x.AddressLine != null && x.AddressLine.Contains(qq)) ||
                (x.Ward != null && x.Ward.Contains(qq)) ||
                (x.District != null && x.District.Contains(qq)) ||
                (x.Province != null && x.Province.Contains(qq)) ||
                (x.Notes != null && x.Notes.Contains(qq)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new QlTourDropoffPointListItemDto
            {
                Id = x.Id,
                TourId = x.TourId,
                Code = x.Code,
                Name = x.Name,
                AddressLine = x.AddressLine,
                Ward = x.Ward,
                District = x.District,
                Province = x.Province,
                CountryCode = x.CountryCode,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                DropoffTime = x.DropoffTime,
                IsDefault = x.IsDefault,
                SortOrder = x.SortOrder,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new QlTourDropoffPointPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("dropoff-points/{id:guid}")]
    public async Task<ActionResult<QlTourDropoffPointDetailDto>> GetDropoffPointById(
        Guid tourId,
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        IQueryable<TourDropoffPoint> query = includeDeleted
            ? _db.TourDropoffPoints.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && x.TourId == tourId)
            : _db.TourDropoffPoints.Where(x => x.TenantId == tenantId && x.TourId == tourId && !x.IsDeleted);

        var entity = await query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Tour dropoff point not found in current tenant." });

        return Ok(MapDropoffDetail(entity));
    }

    [HttpPost("dropoff-points")]
    public async Task<ActionResult<QlTourCreateDropoffPointResponse>> CreateDropoffPoint(
        Guid tourId,
        [FromBody] QlTourCreateDropoffPointRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);
        await ValidateCreateDropoffAsync(tenantId, tourId, req, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        if (req.IsDefault == true)
        {
            var defaults = await _db.TourDropoffPoints.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.IsDefault)
                .ToListAsync(ct);

            foreach (var old in defaults)
            {
                old.IsDefault = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }
        }

        var entity = new TourDropoffPoint
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TourId = tourId,
            Code = req.Code.Trim(),
            Name = req.Name.Trim(),
            AddressLine = NullIfWhiteSpace(req.AddressLine),
            Ward = NullIfWhiteSpace(req.Ward),
            District = NullIfWhiteSpace(req.District),
            Province = NullIfWhiteSpace(req.Province),
            CountryCode = NullIfWhiteSpace(req.CountryCode),
            Latitude = req.Latitude,
            Longitude = req.Longitude,
            DropoffTime = req.DropoffTime,
            Notes = NullIfWhiteSpace(req.Notes),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsDefault = req.IsDefault ?? false,
            SortOrder = req.SortOrder ?? 0,
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.TourDropoffPoints.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetDropoffPointById),
            new { version = "1.0", tourId, id = entity.Id },
            new QlTourCreateDropoffPointResponse { Id = entity.Id });
    }

    [HttpPut("dropoff-points/{id:guid}")]
    public async Task<IActionResult> UpdateDropoffPoint(
        Guid tourId,
        Guid id,
        [FromBody] QlTourUpdateDropoffPointRequest req,
        CancellationToken ct = default)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourDropoffPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour dropoff point not found in current tenant." });

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

        await ValidateUpdateDropoffAsync(tenantId, tourId, id, req, entity, ct);

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();
        var nextIsDefault = req.IsDefault ?? entity.IsDefault;

        if (nextIsDefault)
        {
            var defaults = await _db.TourDropoffPoints.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.Id != id && x.IsDefault)
                .ToListAsync(ct);

            foreach (var old in defaults)
            {
                old.IsDefault = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }
        }

        if (req.Code is not null) entity.Code = req.Code.Trim();
        if (req.Name is not null) entity.Name = req.Name.Trim();
        if (req.AddressLine is not null) entity.AddressLine = NullIfWhiteSpace(req.AddressLine);
        if (req.Ward is not null) entity.Ward = NullIfWhiteSpace(req.Ward);
        if (req.District is not null) entity.District = NullIfWhiteSpace(req.District);
        if (req.Province is not null) entity.Province = NullIfWhiteSpace(req.Province);
        if (req.CountryCode is not null) entity.CountryCode = NullIfWhiteSpace(req.CountryCode);
        if (req.Latitude.HasValue) entity.Latitude = req.Latitude;
        if (req.Longitude.HasValue) entity.Longitude = req.Longitude;
        if (req.DropoffTime.HasValue) entity.DropoffTime = req.DropoffTime;
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsDefault.HasValue) entity.IsDefault = req.IsDefault.Value;
        if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;
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
            return Conflict(new { message = "Tour dropoff point was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("dropoff-points/{id:guid}/delete")]
    public async Task<IActionResult> DeleteDropoffPoint(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDropoffDeleted(tourId, id, true, ct);

    [HttpPost("dropoff-points/{id:guid}/restore")]
    public async Task<IActionResult> RestoreDropoffPoint(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDropoffDeleted(tourId, id, false, ct);

    [HttpPost("dropoff-points/{id:guid}/activate")]
    public async Task<IActionResult> ActivateDropoffPoint(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDropoffActive(tourId, id, true, ct);

    [HttpPost("dropoff-points/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateDropoffPoint(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDropoffActive(tourId, id, false, ct);

    [HttpPost("dropoff-points/{id:guid}/set-default")]
    public async Task<IActionResult> SetDefaultDropoffPoint(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDropoffDefault(tourId, id, true, ct);

    [HttpPost("dropoff-points/{id:guid}/unset-default")]
    public async Task<IActionResult> UnsetDefaultDropoffPoint(Guid tourId, Guid id, CancellationToken ct = default)
        => await ToggleDropoffDefault(tourId, id, false, ct);

    // =========================================================
    // HELPERS - PICKUP
    // =========================================================

    private async Task<IActionResult> TogglePickupDeleted(Guid tourId, Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourPickupPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour pickup point not found in current tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> TogglePickupActive(Guid tourId, Guid id, bool isActive, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourPickupPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour pickup point not found in current tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> TogglePickupDefault(Guid tourId, Guid id, bool isDefault, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourPickupPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour pickup point not found in current tenant." });

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        if (isDefault)
        {
            var defaults = await _db.TourPickupPoints.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.Id != id && x.IsDefault)
                .ToListAsync(ct);

            foreach (var old in defaults)
            {
                old.IsDefault = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }

            entity.IsActive = true;
        }

        entity.IsDefault = isDefault;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    // =========================================================
    // HELPERS - DROPOFF
    // =========================================================

    private async Task<IActionResult> ToggleDropoffDeleted(Guid tourId, Guid id, bool isDeleted, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourDropoffPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour dropoff point not found in current tenant." });

        entity.IsDeleted = isDeleted;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleDropoffActive(Guid tourId, Guid id, bool isActive, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourDropoffPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour dropoff point not found in current tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = GetCurrentUserId();

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleDropoffDefault(Guid tourId, Guid id, bool isDefault, CancellationToken ct)
    {
        var tenantId = RequireTenant();
        await EnsureTourExistsAsync(tenantId, tourId, ct);

        var entity = await _db.TourDropoffPoints.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Tour dropoff point not found in current tenant." });

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        if (isDefault)
        {
            var defaults = await _db.TourDropoffPoints.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.TourId == tourId && x.Id != id && x.IsDefault)
                .ToListAsync(ct);

            foreach (var old in defaults)
            {
                old.IsDefault = false;
                old.UpdatedAt = now;
                old.UpdatedByUserId = userId;
            }

            entity.IsActive = true;
        }

        entity.IsDefault = isDefault;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    // =========================================================
    // SHARED VALIDATION
    // =========================================================

    private async Task EnsureTourExistsAsync(Guid tenantId, Guid tourId, CancellationToken ct)
    {
        var exists = await _db.Tours.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == tourId && !x.IsDeleted, ct);

        if (!exists)
            throw new KeyNotFoundException("Tour not found in current tenant.");
    }

    private async Task ValidateCreatePickupAsync(Guid tenantId, Guid tourId, QlTourCreatePickupPointRequest req, CancellationToken ct)
    {
        ValidatePointPayload(
            req.Code,
            req.Name,
            req.AddressLine,
            req.Ward,
            req.District,
            req.Province,
            req.CountryCode,
            req.Latitude,
            req.Longitude,
            req.Notes);

        var duplicatedCode = await _db.TourPickupPoints.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Code == req.Code.Trim(), ct);

        if (duplicatedCode)
            throw new ArgumentException("Pickup point code already exists in current tour.");
    }

    private async Task ValidateUpdatePickupAsync(
        Guid tenantId,
        Guid tourId,
        Guid currentId,
        QlTourUpdatePickupPointRequest req,
        TourPickupPoint current,
        CancellationToken ct)
    {
        ValidatePointPayload(
            req.Code ?? current.Code,
            req.Name ?? current.Name,
            req.AddressLine,
            req.Ward,
            req.District,
            req.Province,
            req.CountryCode,
            req.Latitude ?? current.Latitude,
            req.Longitude ?? current.Longitude,
            req.Notes);

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var duplicatedCode = await _db.TourPickupPoints.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Code == req.Code.Trim() && x.Id != currentId, ct);

            if (duplicatedCode)
                throw new ArgumentException("Pickup point code already exists in current tour.");
        }
    }

    private async Task ValidateCreateDropoffAsync(Guid tenantId, Guid tourId, QlTourCreateDropoffPointRequest req, CancellationToken ct)
    {
        ValidatePointPayload(
            req.Code,
            req.Name,
            req.AddressLine,
            req.Ward,
            req.District,
            req.Province,
            req.CountryCode,
            req.Latitude,
            req.Longitude,
            req.Notes);

        var duplicatedCode = await _db.TourDropoffPoints.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Code == req.Code.Trim(), ct);

        if (duplicatedCode)
            throw new ArgumentException("Dropoff point code already exists in current tour.");
    }

    private async Task ValidateUpdateDropoffAsync(
        Guid tenantId,
        Guid tourId,
        Guid currentId,
        QlTourUpdateDropoffPointRequest req,
        TourDropoffPoint current,
        CancellationToken ct)
    {
        ValidatePointPayload(
            req.Code ?? current.Code,
            req.Name ?? current.Name,
            req.AddressLine,
            req.Ward,
            req.District,
            req.Province,
            req.CountryCode,
            req.Latitude ?? current.Latitude,
            req.Longitude ?? current.Longitude,
            req.Notes);

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var duplicatedCode = await _db.TourDropoffPoints.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.TourId == tourId && x.Code == req.Code.Trim() && x.Id != currentId, ct);

            if (duplicatedCode)
                throw new ArgumentException("Dropoff point code already exists in current tour.");
        }
    }

    private static void ValidatePointPayload(
        string code,
        string name,
        string? addressLine,
        string? ward,
        string? district,
        string? province,
        string? countryCode,
        decimal? latitude,
        decimal? longitude,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.");

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.");

        if (code.Trim().Length > 50)
            throw new ArgumentException("Code max length is 50.");

        if (name.Trim().Length > 300)
            throw new ArgumentException("Name max length is 300.");

        if (addressLine is not null && addressLine.Length > 500)
            throw new ArgumentException("AddressLine max length is 500.");

        if (ward is not null && ward.Length > 200)
            throw new ArgumentException("Ward max length is 200.");

        if (district is not null && district.Length > 200)
            throw new ArgumentException("District max length is 200.");

        if (province is not null && province.Length > 200)
            throw new ArgumentException("Province max length is 200.");

        if (countryCode is not null && countryCode.Length > 10)
            throw new ArgumentException("CountryCode max length is 10.");

        if (notes is not null && notes.Length > 2000)
            throw new ArgumentException("Notes max length is 2000.");

        if (latitude.HasValue && (latitude < -90 || latitude > 90))
            throw new ArgumentException("Latitude must be between -90 and 90.");

        if (longitude.HasValue && (longitude < -180 || longitude > 180))
            throw new ArgumentException("Longitude must be between -180 and 180.");
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

    private static QlTourPickupPointDetailDto MapPickupDetail(TourPickupPoint x)
    {
        return new QlTourPickupPointDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            TourId = x.TourId,
            Code = x.Code,
            Name = x.Name,
            AddressLine = x.AddressLine,
            Ward = x.Ward,
            District = x.District,
            Province = x.Province,
            CountryCode = x.CountryCode,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            PickupTime = x.PickupTime,
            Notes = x.Notes,
            MetadataJson = x.MetadataJson,
            IsDefault = x.IsDefault,
            SortOrder = x.SortOrder,
            IsActive = x.IsActive,
            IsDeleted = x.IsDeleted,
            CreatedAt = x.CreatedAt,
            CreatedByUserId = x.CreatedByUserId,
            UpdatedAt = x.UpdatedAt,
            UpdatedByUserId = x.UpdatedByUserId,
            RowVersionBase64 = x.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
        };
    }

    private static QlTourDropoffPointDetailDto MapDropoffDetail(TourDropoffPoint x)
    {
        return new QlTourDropoffPointDetailDto
        {
            Id = x.Id,
            TenantId = x.TenantId,
            TourId = x.TourId,
            Code = x.Code,
            Name = x.Name,
            AddressLine = x.AddressLine,
            Ward = x.Ward,
            District = x.District,
            Province = x.Province,
            CountryCode = x.CountryCode,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            DropoffTime = x.DropoffTime,
            Notes = x.Notes,
            MetadataJson = x.MetadataJson,
            IsDefault = x.IsDefault,
            SortOrder = x.SortOrder,
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

public sealed class QlTourPickupPointPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<QlTourPickupPointListItemDto> Items { get; set; } = new();
}

public sealed class QlTourPickupPointListItemDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public TimeOnly? PickupTime { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class QlTourPickupPointDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public TimeOnly? PickupTime { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreatePickupPointRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public TimeOnly? PickupTime { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsDefault { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class QlTourUpdatePickupPointRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public TimeOnly? PickupTime { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsDefault { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreatePickupPointResponse
{
    public Guid Id { get; set; }
}

public sealed class QlTourDropoffPointPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<QlTourDropoffPointListItemDto> Items { get; set; } = new();
}

public sealed class QlTourDropoffPointListItemDto
{
    public Guid Id { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public TimeOnly? DropoffTime { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class QlTourDropoffPointDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public TimeOnly? DropoffTime { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreateDropoffPointRequest
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public TimeOnly? DropoffTime { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsDefault { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class QlTourUpdateDropoffPointRequest
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? AddressLine { get; set; }
    public string? Ward { get; set; }
    public string? District { get; set; }
    public string? Province { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public TimeOnly? DropoffTime { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsDefault { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class QlTourCreateDropoffPointResponse
{
    public Guid Id { get; set; }
}
