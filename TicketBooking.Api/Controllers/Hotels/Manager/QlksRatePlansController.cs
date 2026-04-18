// FILE #203: TicketBooking.Api/Controllers/Hotels/QlksRatePlansController.cs
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
[Route("api/v{version:apiVersion}/qlks/rate-plans")]
[Authorize(Roles = RoleNames.QLKS + "," + RoleNames.Admin)]
public sealed class QlksRatePlansController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public QlksRatePlansController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<RatePlanPagedResponse<RatePlanListItemDto>>> List(
        [FromQuery] Guid? hotelId = null,
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

        IQueryable<RatePlan> query = includeDeleted
            ? _db.RatePlans.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.RatePlans.Where(x => x.TenantId == tenantId && !x.IsDeleted);

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
                (x.Description != null && x.Description.Contains(q)));
        }

        var total = await query.CountAsync(ct);

        var items = await query.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RatePlanListItemDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                HotelId = x.HotelId,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                Status = x.Status,
                Refundable = x.Refundable,
                BreakfastIncluded = x.BreakfastIncluded,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new RatePlanPagedResponse<RatePlanListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RatePlanDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "Tenant context is required." });

        var tenantId = _tenant.TenantId.Value;

        IQueryable<RatePlan> query = includeDeleted
            ? _db.RatePlans.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.RatePlans.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        var entity = await query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Rate plan not found." });

        var roomTypeMappings = await _db.RatePlanRoomTypes.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.RatePlanId == id && !x.IsDeleted)
            .Join(
                _db.RoomTypes.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && !x.IsDeleted),
                m => m.RoomTypeId,
                r => r.Id,
                (m, r) => new RatePlanRoomTypeDto
                {
                    Id = m.Id,
                    RoomTypeId = m.RoomTypeId,
                    RoomTypeCode = r.Code,
                    RoomTypeName = r.Name,
                    BasePrice = m.BasePrice,
                    CurrencyCode = m.CurrencyCode,
                    IsActive = m.IsActive
                })
            .OrderBy(x => x.RoomTypeCode)
            .ToListAsync(ct);

        var policy = await _db.RatePlanPolicies.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.RatePlanId == id && !x.IsDeleted)
            .Select(x => new RatePlanPolicyDto
            {
                Id = x.Id,
                PolicyJson = x.PolicyJson,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync(ct);

        return Ok(new RatePlanDetailDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            HotelId = entity.HotelId,
            Code = entity.Code,
            Name = entity.Name,
            Description = entity.Description,
            Type = entity.Type,
            Status = entity.Status,
            CancellationPolicyId = entity.CancellationPolicyId,
            CheckInOutRuleId = entity.CheckInOutRuleId,
            PropertyPolicyId = entity.PropertyPolicyId,
            Refundable = entity.Refundable,
            BreakfastIncluded = entity.BreakfastIncluded,
            MinNights = entity.MinNights,
            MaxNights = entity.MaxNights,
            MinAdvanceDays = entity.MinAdvanceDays,
            MaxAdvanceDays = entity.MaxAdvanceDays,
            RequiresGuarantee = entity.RequiresGuarantee,
            MetadataJson = entity.MetadataJson,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId,
            RowVersionBase64 = entity.RowVersion is { Length: > 0 } ? Convert.ToBase64String(entity.RowVersion) : null,
            RoomTypes = roomTypeMappings,
            Policy = policy
        });
    }

    [HttpPost]
    public async Task<ActionResult<CreateRatePlanResponse>> Create(
        [FromBody] CreateRatePlanRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "Tenant context is required." });

        ValidateCreate(req);

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var hotelExists = await _db.Hotels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.HotelId && !x.IsDeleted, ct);

        if (!hotelExists)
            return BadRequest(new { message = "Hotel not found in current tenant." });

        if (req.CancellationPolicyId.HasValue)
        {
            var ok = await _db.CancellationPolicies.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == req.CancellationPolicyId.Value && !x.IsDeleted, ct);
            if (!ok) return BadRequest(new { message = "CancellationPolicyId is invalid in current tenant." });
        }

        if (req.CheckInOutRuleId.HasValue)
        {
            var ok = await _db.CheckInOutRules.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == req.CheckInOutRuleId.Value && !x.IsDeleted, ct);
            if (!ok) return BadRequest(new { message = "CheckInOutRuleId is invalid in current tenant." });
        }

        if (req.PropertyPolicyId.HasValue)
        {
            var ok = await _db.PropertyPolicies.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == req.PropertyPolicyId.Value && !x.IsDeleted, ct);
            if (!ok) return BadRequest(new { message = "PropertyPolicyId is invalid in current tenant." });
        }

        var code = req.Code!.Trim();

        var codeExists = await _db.RatePlans.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.HotelId == req.HotelId && x.Code == code, ct);

        if (codeExists)
            return Conflict(new { message = "Rate plan code already exists in this hotel." });

        var entity = new RatePlan
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HotelId = req.HotelId,
            Code = code,
            Name = req.Name!.Trim(),
            Description = NullIfWhiteSpace(req.Description),
            Type = req.Type ?? RatePlanType.Public,
            Status = req.Status ?? RatePlanStatus.Active,
            CancellationPolicyId = req.CancellationPolicyId,
            CheckInOutRuleId = req.CheckInOutRuleId,
            PropertyPolicyId = req.PropertyPolicyId,
            Refundable = req.Refundable ?? true,
            BreakfastIncluded = req.BreakfastIncluded ?? false,
            MinNights = req.MinNights,
            MaxNights = req.MaxNights,
            MinAdvanceDays = req.MinAdvanceDays,
            MaxAdvanceDays = req.MaxAdvanceDays,
            RequiresGuarantee = req.RequiresGuarantee ?? false,
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.RatePlans.Add(entity);
        await _db.SaveChangesAsync(ct);

        if (req.RoomTypes is { Count: > 0 })
            await ReplaceRoomTypeMappingsAsync(tenantId, entity.Id, req.RoomTypes, now, userId, ct);

        if (req.Policy is not null)
            await UpsertPolicyAsync(tenantId, entity.Id, req.Policy, now, userId, ct);

        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new CreateRatePlanResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRatePlanRequest req,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "Tenant context is required." });

        ValidateUpdate(req);

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.RatePlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Rate plan not found." });

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
                return BadRequest(new { message = "Target hotel not found in current tenant." });

            var movedCode = req.Code?.Trim() ?? entity.Code;
            var codeExists = await _db.RatePlans.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.HotelId == req.HotelId.Value && x.Code == movedCode && x.Id != id, ct);

            if (codeExists)
                return Conflict(new { message = "Rate plan code already exists in target hotel." });

            entity.HotelId = req.HotelId.Value;

            if (req.RoomTypes is null)
            {
                var hasForeignMappings = await _db.RatePlanRoomTypes.IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenantId && x.RatePlanId == entity.Id && !x.IsDeleted)
                    .Join(
                        _db.RoomTypes.IgnoreQueryFilters().Where(x => x.TenantId == tenantId && !x.IsDeleted),
                        mapping => mapping.RoomTypeId,
                        roomType => roomType.Id,
                        (mapping, roomType) => roomType.HotelId)
                    .AnyAsync(x => x != entity.HotelId, ct);

                if (hasForeignMappings)
                    return BadRequest(new { message = "Existing room type mappings do not belong to the target hotel. Provide RoomTypes to remap them." });
            }
        }

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var newCode = req.Code.Trim();
            var codeExists = await _db.RatePlans.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.HotelId == entity.HotelId && x.Code == newCode && x.Id != id, ct);

            if (codeExists)
                return Conflict(new { message = "Rate plan code already exists in this hotel." });

            entity.Code = newCode;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
            entity.Name = req.Name.Trim();

        if (req.Description is not null) entity.Description = NullIfWhiteSpace(req.Description);
        if (req.Type.HasValue) entity.Type = req.Type.Value;
        if (req.Status.HasValue) entity.Status = req.Status.Value;

        if (req.CancellationPolicyId.HasValue)
        {
            var ok = await _db.CancellationPolicies.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == req.CancellationPolicyId.Value && !x.IsDeleted, ct);
            if (!ok) return BadRequest(new { message = "CancellationPolicyId is invalid in current tenant." });

            entity.CancellationPolicyId = req.CancellationPolicyId.Value;
        }

        if (req.CheckInOutRuleId.HasValue)
        {
            var ok = await _db.CheckInOutRules.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == req.CheckInOutRuleId.Value && !x.IsDeleted, ct);
            if (!ok) return BadRequest(new { message = "CheckInOutRuleId is invalid in current tenant." });

            entity.CheckInOutRuleId = req.CheckInOutRuleId.Value;
        }

        if (req.PropertyPolicyId.HasValue)
        {
            var ok = await _db.PropertyPolicies.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.Id == req.PropertyPolicyId.Value && !x.IsDeleted, ct);
            if (!ok) return BadRequest(new { message = "PropertyPolicyId is invalid in current tenant." });

            entity.PropertyPolicyId = req.PropertyPolicyId.Value;
        }

        if (req.Refundable.HasValue) entity.Refundable = req.Refundable.Value;
        if (req.BreakfastIncluded.HasValue) entity.BreakfastIncluded = req.BreakfastIncluded.Value;
        if (req.MinNights.HasValue) entity.MinNights = req.MinNights;
        if (req.MaxNights.HasValue) entity.MaxNights = req.MaxNights;
        if (req.MinAdvanceDays.HasValue) entity.MinAdvanceDays = req.MinAdvanceDays;
        if (req.MaxAdvanceDays.HasValue) entity.MaxAdvanceDays = req.MaxAdvanceDays;
        if (req.RequiresGuarantee.HasValue) entity.RequiresGuarantee = req.RequiresGuarantee.Value;
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        if (req.RoomTypes is not null)
            await ReplaceRoomTypeMappingsAsync(tenantId, entity.Id, req.RoomTypes, now, userId, ct);

        if (req.Policy is not null)
            await UpsertPolicyAsync(tenantId, entity.Id, req.Policy, now, userId, ct);

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

        var entity = await _db.RatePlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Rate plan not found." });

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

        var entity = await _db.RatePlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Rate plan not found." });

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

        var entity = await _db.RatePlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Rate plan not found." });

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

        var entity = await _db.RatePlans.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Rate plan not found." });

        if (entity.IsActive)
        {
            entity.IsActive = false;
            entity.UpdatedAt = DateTimeOffset.Now;
            entity.UpdatedByUserId = userId;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    private async Task ReplaceRoomTypeMappingsAsync(
        Guid tenantId,
        Guid ratePlanId,
        List<RatePlanRoomTypeUpsertDto> items,
        DateTimeOffset now,
        Guid? userId,
        CancellationToken ct)
    {
        var hotelId = await _db.RatePlans.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.Id == ratePlanId && !x.IsDeleted)
            .Select(x => (Guid?)x.HotelId)
            .FirstOrDefaultAsync(ct);

        if (!hotelId.HasValue)
            throw new ArgumentException("RatePlanId is invalid in current tenant.");

        var existingMappings = await _db.RatePlanRoomTypes.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.RatePlanId == ratePlanId)
            .ToListAsync(ct);

        var roomTypeIds = items.Select(x => x.RoomTypeId).Distinct().ToList();

        var validRoomTypeIds = await _db.RoomTypes.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.HotelId == hotelId.Value && !x.IsDeleted && roomTypeIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(ct);

        var invalidIds = roomTypeIds.Except(validRoomTypeIds).ToList();
        if (invalidIds.Count > 0)
            throw new ArgumentException("One or more RoomTypeId values are invalid for this hotel.");

        var requestedMappings = items
            .GroupBy(x => x.RoomTypeId)
            .Select(g => new
            {
                RoomTypeId = g.Key,
                BasePrice = g.First().BasePrice,
                CurrencyCode = string.IsNullOrWhiteSpace(g.First().CurrencyCode) ? "VND" : g.First().CurrencyCode!.Trim(),
                IsActive = g.First().IsActive ?? true
            })
            .ToList();

        var existingByRoomTypeId = existingMappings.ToDictionary(x => x.RoomTypeId);
        var requestedRoomTypeIds = requestedMappings.Select(x => x.RoomTypeId).ToHashSet();

        foreach (var item in requestedMappings)
        {
            if (existingByRoomTypeId.TryGetValue(item.RoomTypeId, out var existing))
            {
                existing.BasePrice = item.BasePrice;
                existing.CurrencyCode = item.CurrencyCode;
                existing.IsActive = item.IsActive;
                existing.IsDeleted = false;
                existing.UpdatedAt = now;
                existing.UpdatedByUserId = userId;
                continue;
            }

            _db.RatePlanRoomTypes.Add(new RatePlanRoomType
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RatePlanId = ratePlanId,
                RoomTypeId = item.RoomTypeId,
                BasePrice = item.BasePrice,
                CurrencyCode = item.CurrencyCode,
                IsActive = item.IsActive,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId,
                UpdatedAt = now,
                UpdatedByUserId = userId
            });
        }

        foreach (var existing in existingMappings)
        {
            if (requestedRoomTypeIds.Contains(existing.RoomTypeId) || existing.IsDeleted)
                continue;

            existing.IsActive = false;
            existing.IsDeleted = true;
            existing.UpdatedAt = now;
            existing.UpdatedByUserId = userId;
        }
    }

    private async Task UpsertPolicyAsync(
        Guid tenantId,
        Guid ratePlanId,
        RatePlanPolicyUpsertDto dto,
        DateTimeOffset now,
        Guid? userId,
        CancellationToken ct)
    {
        var existing = await _db.RatePlanPolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.RatePlanId == ratePlanId, ct);

        if (existing is null)
        {
            existing = new RatePlanPolicy
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                RatePlanId = ratePlanId,
                PolicyJson = NullIfWhiteSpace(dto.PolicyJson),
                IsActive = dto.IsActive ?? true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId,
                UpdatedAt = now,
                UpdatedByUserId = userId
            };

            _db.RatePlanPolicies.Add(existing);
            return;
        }

        existing.PolicyJson = NullIfWhiteSpace(dto.PolicyJson);
        if (dto.IsActive.HasValue) existing.IsActive = dto.IsActive.Value;
        existing.IsDeleted = false;
        existing.UpdatedAt = now;
        existing.UpdatedByUserId = userId;
    }

    private static void ValidateCreate(CreateRatePlanRequest req)
    {
        if (req.HotelId == Guid.Empty)
            throw new ArgumentException("HotelId is required.");

        if (string.IsNullOrWhiteSpace(req.Code))
            throw new ArgumentException("Code is required.");

        if (string.IsNullOrWhiteSpace(req.Name))
            throw new ArgumentException("Name is required.");

        if (req.Code.Length > 80)
            throw new ArgumentException("Code max length is 80.");

        if (req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

        if (req.MinNights.HasValue && req.MinNights < 0)
            throw new ArgumentException("MinNights must be >= 0.");

        if (req.MaxNights.HasValue && req.MaxNights < 0)
            throw new ArgumentException("MaxNights must be >= 0.");

        if (req.MinAdvanceDays.HasValue && req.MinAdvanceDays < 0)
            throw new ArgumentException("MinAdvanceDays must be >= 0.");

        if (req.MaxAdvanceDays.HasValue && req.MaxAdvanceDays < 0)
            throw new ArgumentException("MaxAdvanceDays must be >= 0.");
    }

    private static void ValidateUpdate(UpdateRatePlanRequest req)
    {
        if (req.Code is not null && req.Code.Length > 80)
            throw new ArgumentException("Code max length is 80.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

        if (req.MinNights.HasValue && req.MinNights < 0)
            throw new ArgumentException("MinNights must be >= 0.");

        if (req.MaxNights.HasValue && req.MaxNights < 0)
            throw new ArgumentException("MaxNights must be >= 0.");

        if (req.MinAdvanceDays.HasValue && req.MinAdvanceDays < 0)
            throw new ArgumentException("MinAdvanceDays must be >= 0.");

        if (req.MaxAdvanceDays.HasValue && req.MaxAdvanceDays < 0)
            throw new ArgumentException("MaxAdvanceDays must be >= 0.");
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class RatePlanPagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}

public sealed class RatePlanListItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public RatePlanType Type { get; set; }
    public RatePlanStatus Status { get; set; }
    public bool Refundable { get; set; }
    public bool BreakfastIncluded { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class RatePlanDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }

    public RatePlanType Type { get; set; }
    public RatePlanStatus Status { get; set; }

    public Guid? CancellationPolicyId { get; set; }
    public Guid? CheckInOutRuleId { get; set; }
    public Guid? PropertyPolicyId { get; set; }

    public bool Refundable { get; set; }
    public bool BreakfastIncluded { get; set; }

    public int? MinNights { get; set; }
    public int? MaxNights { get; set; }
    public int? MinAdvanceDays { get; set; }
    public int? MaxAdvanceDays { get; set; }
    public bool RequiresGuarantee { get; set; }

    public string? MetadataJson { get; set; }

    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }

    public string? RowVersionBase64 { get; set; }

    public List<RatePlanRoomTypeDto> RoomTypes { get; set; } = new();
    public RatePlanPolicyDto? Policy { get; set; }
}

public sealed class RatePlanRoomTypeDto
{
    public Guid Id { get; set; }
    public Guid RoomTypeId { get; set; }
    public string RoomTypeCode { get; set; } = "";
    public string RoomTypeName { get; set; } = "";
    public decimal? BasePrice { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public bool IsActive { get; set; }
}

public sealed class RatePlanPolicyDto
{
    public Guid Id { get; set; }
    public string? PolicyJson { get; set; }
    public bool IsActive { get; set; }
}

public sealed class CreateRatePlanRequest
{
    public Guid HotelId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }

    public RatePlanType? Type { get; set; }
    public RatePlanStatus? Status { get; set; }

    public Guid? CancellationPolicyId { get; set; }
    public Guid? CheckInOutRuleId { get; set; }
    public Guid? PropertyPolicyId { get; set; }

    public bool? Refundable { get; set; }
    public bool? BreakfastIncluded { get; set; }

    public int? MinNights { get; set; }
    public int? MaxNights { get; set; }
    public int? MinAdvanceDays { get; set; }
    public int? MaxAdvanceDays { get; set; }
    public bool? RequiresGuarantee { get; set; }

    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }

    public List<RatePlanRoomTypeUpsertDto>? RoomTypes { get; set; }
    public RatePlanPolicyUpsertDto? Policy { get; set; }
}

public sealed class UpdateRatePlanRequest
{
    public Guid? HotelId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }

    public RatePlanType? Type { get; set; }
    public RatePlanStatus? Status { get; set; }

    public Guid? CancellationPolicyId { get; set; }
    public Guid? CheckInOutRuleId { get; set; }
    public Guid? PropertyPolicyId { get; set; }

    public bool? Refundable { get; set; }
    public bool? BreakfastIncluded { get; set; }

    public int? MinNights { get; set; }
    public int? MaxNights { get; set; }
    public int? MinAdvanceDays { get; set; }
    public int? MaxAdvanceDays { get; set; }
    public bool? RequiresGuarantee { get; set; }

    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }

    public List<RatePlanRoomTypeUpsertDto>? RoomTypes { get; set; }
    public RatePlanPolicyUpsertDto? Policy { get; set; }

    public string? RowVersionBase64 { get; set; }
}

public sealed class RatePlanRoomTypeUpsertDto
{
    public Guid RoomTypeId { get; set; }
    public decimal? BasePrice { get; set; }
    public string? CurrencyCode { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class RatePlanPolicyUpsertDto
{
    public string? PolicyJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class CreateRatePlanResponse
{
    public Guid Id { get; set; }
}

