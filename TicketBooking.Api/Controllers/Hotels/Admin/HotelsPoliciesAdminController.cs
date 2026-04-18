// FILE #211: TicketBooking.Api/Controllers/Hotels/HotelsPoliciesAdminController.cs
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
[Route("api/v{version:apiVersion}/admin/hotel-policies")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class HotelsPoliciesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public HotelsPoliciesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // =========================================================
    // CANCELLATION POLICIES
    // =========================================================

    [HttpGet("cancellation-policies")]
    public async Task<ActionResult<AdminCancellationPolicyPagedResponse<AdminCancellationPolicyListItemDto>>> ListCancellationPolicies(
        [FromQuery] Guid? hotelId = null,
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

        IQueryable<CancellationPolicy> query = includeDeleted
            ? _db.CancellationPolicies.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.CancellationPolicies.Where(x => x.TenantId == tenantId && !x.IsDeleted);

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
            .Select(x => new AdminCancellationPolicyListItemDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                HotelId = x.HotelId,
                Code = x.Code,
                Name = x.Name,
                Type = x.Type,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new AdminCancellationPolicyPagedResponse<AdminCancellationPolicyListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("cancellation-policies/{id:guid}")]
    public async Task<ActionResult<AdminCancellationPolicyDetailDto>> GetCancellationPolicyById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        IQueryable<CancellationPolicy> query = includeDeleted
            ? _db.CancellationPolicies.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.CancellationPolicies.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        var entity = await query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Cancellation policy not found in current switched tenant." });

        var rules = await _db.CancellationPolicyRules.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.CancellationPolicyId == id && !x.IsDeleted)
            .OrderBy(x => x.Priority)
            .Select(x => new AdminCancellationPolicyRuleDto
            {
                Id = x.Id,
                CancelBeforeHours = x.CancelBeforeHours,
                CancelBeforeDays = x.CancelBeforeDays,
                ChargeType = x.ChargeType,
                ChargeValue = x.ChargeValue,
                CurrencyCode = x.CurrencyCode,
                Notes = x.Notes,
                Priority = x.Priority,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);

        return Ok(new AdminCancellationPolicyDetailDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            HotelId = entity.HotelId,
            Code = entity.Code,
            Name = entity.Name,
            Type = entity.Type,
            Description = entity.Description,
            MetadataJson = entity.MetadataJson,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId,
            RowVersionBase64 = entity.RowVersion is { Length: > 0 } ? Convert.ToBase64String(entity.RowVersion) : null,
            Rules = rules
        });
    }

    [HttpPost("cancellation-policies")]
    public async Task<ActionResult<AdminCreateCancellationPolicyResponse>> CreateCancellationPolicy(
        [FromBody] AdminCreateCancellationPolicyRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateCreateCancellation(req);
        if (req.Rules is not null)
            ValidateCancellationRules(req.Rules);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var hotelExists = await _db.Hotels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.HotelId && !x.IsDeleted, ct);

        if (!hotelExists)
            return BadRequest(new { message = "Hotel not found in current switched tenant." });

        var code = req.Code!.Trim();

        var codeExists = await _db.CancellationPolicies.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.HotelId == req.HotelId && x.Code == code, ct);

        if (codeExists)
            return Conflict(new { message = "Cancellation policy code already exists in this hotel." });

        var entity = new CancellationPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HotelId = req.HotelId,
            Code = code,
            Name = req.Name!.Trim(),
            Type = req.Type ?? CancellationPolicyType.Custom,
            Description = NullIfWhiteSpace(req.Description),
            MetadataJson = NullIfWhiteSpace(req.MetadataJson),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        _db.CancellationPolicies.Add(entity);
        await _db.SaveChangesAsync(ct);

        if (req.Rules is { Count: > 0 })
            await ReplaceCancellationRulesAsync(tenantId, entity.Id, req.Rules, now, userId, ct);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return CreatedAtAction(
            nameof(GetCancellationPolicyById),
            new { version = "1.0", id = entity.Id },
            new AdminCreateCancellationPolicyResponse { Id = entity.Id });
    }

    [HttpPut("cancellation-policies/{id:guid}")]
    public async Task<IActionResult> UpdateCancellationPolicy(
        Guid id,
        [FromBody] AdminUpdateCancellationPolicyRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateUpdateCancellation(req);
        if (req.Rules is not null)
            ValidateCancellationRules(req.Rules);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.CancellationPolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Cancellation policy not found in current switched tenant." });

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
            var codeExists = await _db.CancellationPolicies.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.HotelId == req.HotelId.Value && x.Code == movedCode && x.Id != id, ct);

            if (codeExists)
                return Conflict(new { message = "Cancellation policy code already exists in target hotel." });

            entity.HotelId = req.HotelId.Value;
        }

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var newCode = req.Code.Trim();
            var codeExists = await _db.CancellationPolicies.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.HotelId == entity.HotelId && x.Code == newCode && x.Id != id, ct);

            if (codeExists)
                return Conflict(new { message = "Cancellation policy code already exists in this hotel." });

            entity.Code = newCode;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
            entity.Name = req.Name.Trim();

        if (req.Type.HasValue) entity.Type = req.Type.Value;
        if (req.Description is not null) entity.Description = NullIfWhiteSpace(req.Description);
        if (req.MetadataJson is not null) entity.MetadataJson = NullIfWhiteSpace(req.MetadataJson);
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            if (req.Rules is not null)
                await ReplaceCancellationRulesAsync(tenantId, entity.Id, req.Rules, now, userId, ct);

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Ok(new { ok = true });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Data was changed by another user. Please reload and try again." });
        }
    }

    [HttpPost("cancellation-policies/{id:guid}/delete")]
    public async Task<IActionResult> DeleteCancellationPolicy(Guid id, CancellationToken ct = default)
    {
        return await ToggleCancellationDeleted(id, true, ct);
    }

    [HttpPost("cancellation-policies/{id:guid}/restore")]
    public async Task<IActionResult> RestoreCancellationPolicy(Guid id, CancellationToken ct = default)
    {
        return await ToggleCancellationDeleted(id, false, ct);
    }

    [HttpPost("cancellation-policies/{id:guid}/activate")]
    public async Task<IActionResult> ActivateCancellationPolicy(Guid id, CancellationToken ct = default)
    {
        return await ToggleCancellationActive(id, true, ct);
    }

    [HttpPost("cancellation-policies/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateCancellationPolicy(Guid id, CancellationToken ct = default)
    {
        return await ToggleCancellationActive(id, false, ct);
    }

    // =========================================================
    // CHECK IN / OUT RULES
    // =========================================================

    [HttpGet("check-in-out-rules")]
    public async Task<ActionResult<AdminCheckInOutRulePagedResponse<AdminCheckInOutRuleListItemDto>>> ListCheckInOutRules(
        [FromQuery] Guid? hotelId = null,
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

        IQueryable<CheckInOutRule> query = includeDeleted
            ? _db.CheckInOutRules.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.CheckInOutRules.Where(x => x.TenantId == tenantId && !x.IsDeleted);

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
                (x.Notes != null && x.Notes.Contains(q)));
        }

        var total = await query.CountAsync(ct);

        var items = await query.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminCheckInOutRuleListItemDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                HotelId = x.HotelId,
                Code = x.Code,
                Name = x.Name,
                CheckInFrom = x.CheckInFrom,
                CheckInTo = x.CheckInTo,
                CheckOutFrom = x.CheckOutFrom,
                CheckOutTo = x.CheckOutTo,
                AllowsEarlyCheckIn = x.AllowsEarlyCheckIn,
                AllowsLateCheckOut = x.AllowsLateCheckOut,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new AdminCheckInOutRulePagedResponse<AdminCheckInOutRuleListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("check-in-out-rules/{id:guid}")]
    public async Task<ActionResult<AdminCheckInOutRuleDetailDto>> GetCheckInOutRuleById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        IQueryable<CheckInOutRule> query = includeDeleted
            ? _db.CheckInOutRules.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.CheckInOutRules.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        var entity = await query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Check-in/out rule not found in current switched tenant." });

        return Ok(new AdminCheckInOutRuleDetailDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            HotelId = entity.HotelId,
            Code = entity.Code,
            Name = entity.Name,
            CheckInFrom = entity.CheckInFrom,
            CheckInTo = entity.CheckInTo,
            CheckOutFrom = entity.CheckOutFrom,
            CheckOutTo = entity.CheckOutTo,
            AllowsEarlyCheckIn = entity.AllowsEarlyCheckIn,
            AllowsLateCheckOut = entity.AllowsLateCheckOut,
            Notes = entity.Notes,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId,
            RowVersionBase64 = entity.RowVersion is { Length: > 0 } ? Convert.ToBase64String(entity.RowVersion) : null
        });
    }

    [HttpPost("check-in-out-rules")]
    public async Task<ActionResult<AdminCreateCheckInOutRuleResponse>> CreateCheckInOutRule(
        [FromBody] AdminCreateCheckInOutRuleRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateCreateCheckInOut(req);
        ValidateCheckInOutWindow(req.CheckInFrom, req.CheckInTo, req.CheckOutFrom, req.CheckOutTo);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var hotelExists = await _db.Hotels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.HotelId && !x.IsDeleted, ct);

        if (!hotelExists)
            return BadRequest(new { message = "Hotel not found in current switched tenant." });

        var code = req.Code!.Trim();

        var codeExists = await _db.CheckInOutRules.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.HotelId == req.HotelId && x.Code == code, ct);

        if (codeExists)
            return Conflict(new { message = "Check-in/out rule code already exists in this hotel." });

        var entity = new CheckInOutRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HotelId = req.HotelId,
            Code = code,
            Name = req.Name!.Trim(),
            CheckInFrom = req.CheckInFrom,
            CheckInTo = req.CheckInTo,
            CheckOutFrom = req.CheckOutFrom,
            CheckOutTo = req.CheckOutTo,
            AllowsEarlyCheckIn = req.AllowsEarlyCheckIn ?? false,
            AllowsLateCheckOut = req.AllowsLateCheckOut ?? false,
            Notes = NullIfWhiteSpace(req.Notes),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.CheckInOutRules.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetCheckInOutRuleById),
            new { version = "1.0", id = entity.Id },
            new AdminCreateCheckInOutRuleResponse { Id = entity.Id });
    }

    [HttpPut("check-in-out-rules/{id:guid}")]
    public async Task<IActionResult> UpdateCheckInOutRule(
        Guid id,
        [FromBody] AdminUpdateCheckInOutRuleRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateUpdateCheckInOut(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.CheckInOutRules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Check-in/out rule not found in current switched tenant." });

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
            var codeExists = await _db.CheckInOutRules.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.HotelId == req.HotelId.Value && x.Code == movedCode && x.Id != id, ct);

            if (codeExists)
                return Conflict(new { message = "Check-in/out rule code already exists in target hotel." });

            entity.HotelId = req.HotelId.Value;
        }

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var newCode = req.Code.Trim();
            var codeExists = await _db.CheckInOutRules.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.HotelId == entity.HotelId && x.Code == newCode && x.Id != id, ct);

            if (codeExists)
                return Conflict(new { message = "Check-in/out rule code already exists in this hotel." });

            entity.Code = newCode;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
            entity.Name = req.Name.Trim();

        if (req.CheckInFrom.HasValue) entity.CheckInFrom = req.CheckInFrom.Value;
        if (req.CheckInTo.HasValue) entity.CheckInTo = req.CheckInTo.Value;
        if (req.CheckOutFrom.HasValue) entity.CheckOutFrom = req.CheckOutFrom.Value;
        if (req.CheckOutTo.HasValue) entity.CheckOutTo = req.CheckOutTo.Value;
        if (req.AllowsEarlyCheckIn.HasValue) entity.AllowsEarlyCheckIn = req.AllowsEarlyCheckIn.Value;
        if (req.AllowsLateCheckOut.HasValue) entity.AllowsLateCheckOut = req.AllowsLateCheckOut.Value;
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;
        if (req.IsDeleted.HasValue) entity.IsDeleted = req.IsDeleted.Value;

        ValidateCheckInOutWindow(entity.CheckInFrom, entity.CheckInTo, entity.CheckOutFrom, entity.CheckOutTo);

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

    [HttpPost("check-in-out-rules/{id:guid}/delete")]
    public async Task<IActionResult> DeleteCheckInOutRule(Guid id, CancellationToken ct = default)
    {
        return await ToggleCheckInOutDeleted(id, true, ct);
    }

    [HttpPost("check-in-out-rules/{id:guid}/restore")]
    public async Task<IActionResult> RestoreCheckInOutRule(Guid id, CancellationToken ct = default)
    {
        return await ToggleCheckInOutDeleted(id, false, ct);
    }

    [HttpPost("check-in-out-rules/{id:guid}/activate")]
    public async Task<IActionResult> ActivateCheckInOutRule(Guid id, CancellationToken ct = default)
    {
        return await ToggleCheckInOutActive(id, true, ct);
    }

    [HttpPost("check-in-out-rules/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateCheckInOutRule(Guid id, CancellationToken ct = default)
    {
        return await ToggleCheckInOutActive(id, false, ct);
    }

    // =========================================================
    // PROPERTY POLICIES
    // =========================================================

    [HttpGet("property-policies")]
    public async Task<ActionResult<AdminPropertyPolicyPagedResponse<AdminPropertyPolicyListItemDto>>> ListPropertyPolicies(
        [FromQuery] Guid? hotelId = null,
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

        IQueryable<PropertyPolicy> query = includeDeleted
            ? _db.PropertyPolicies.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.PropertyPolicies.Where(x => x.TenantId == tenantId && !x.IsDeleted);

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
                (x.Notes != null && x.Notes.Contains(q)) ||
                (x.PolicyJson != null && x.PolicyJson.Contains(q)));
        }

        var total = await query.CountAsync(ct);

        var items = await query.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminPropertyPolicyListItemDto
            {
                Id = x.Id,
                TenantId = x.TenantId,
                HotelId = x.HotelId,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new AdminPropertyPolicyPagedResponse<AdminPropertyPolicyListItemDto>
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("property-policies/{id:guid}")]
    public async Task<ActionResult<AdminPropertyPolicyDetailDto>> GetPropertyPolicyById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        IQueryable<PropertyPolicy> query = includeDeleted
            ? _db.PropertyPolicies.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.PropertyPolicies.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        var entity = await query.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound(new { message = "Property policy not found in current switched tenant." });

        return Ok(new AdminPropertyPolicyDetailDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            HotelId = entity.HotelId,
            Code = entity.Code,
            Name = entity.Name,
            PolicyJson = entity.PolicyJson,
            Notes = entity.Notes,
            IsActive = entity.IsActive,
            IsDeleted = entity.IsDeleted,
            CreatedAt = entity.CreatedAt,
            CreatedByUserId = entity.CreatedByUserId,
            UpdatedAt = entity.UpdatedAt,
            UpdatedByUserId = entity.UpdatedByUserId,
            RowVersionBase64 = entity.RowVersion is { Length: > 0 } ? Convert.ToBase64String(entity.RowVersion) : null
        });
    }

    [HttpPost("property-policies")]
    public async Task<ActionResult<AdminCreatePropertyPolicyResponse>> CreatePropertyPolicy(
        [FromBody] AdminCreatePropertyPolicyRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateCreateProperty(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var hotelExists = await _db.Hotels.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.HotelId && !x.IsDeleted, ct);

        if (!hotelExists)
            return BadRequest(new { message = "Hotel not found in current switched tenant." });

        var code = req.Code!.Trim();

        var codeExists = await _db.PropertyPolicies.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.HotelId == req.HotelId && x.Code == code, ct);

        if (codeExists)
            return Conflict(new { message = "Property policy code already exists in this hotel." });

        var entity = new PropertyPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            HotelId = req.HotelId,
            Code = code,
            Name = req.Name!.Trim(),
            PolicyJson = NullIfWhiteSpace(req.PolicyJson),
            Notes = NullIfWhiteSpace(req.Notes),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.PropertyPolicies.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetPropertyPolicyById),
            new { version = "1.0", id = entity.Id },
            new AdminCreatePropertyPolicyResponse { Id = entity.Id });
    }

    [HttpPut("property-policies/{id:guid}")]
    public async Task<IActionResult> UpdatePropertyPolicy(
        Guid id,
        [FromBody] AdminUpdatePropertyPolicyRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();
        ValidateUpdateProperty(req);

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.PropertyPolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Property policy not found in current switched tenant." });

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
            var codeExists = await _db.PropertyPolicies.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.HotelId == req.HotelId.Value && x.Code == movedCode && x.Id != id, ct);

            if (codeExists)
                return Conflict(new { message = "Property policy code already exists in target hotel." });

            entity.HotelId = req.HotelId.Value;
        }

        if (!string.IsNullOrWhiteSpace(req.Code))
        {
            var newCode = req.Code.Trim();
            var codeExists = await _db.PropertyPolicies.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == tenantId && x.HotelId == entity.HotelId && x.Code == newCode && x.Id != id, ct);

            if (codeExists)
                return Conflict(new { message = "Property policy code already exists in this hotel." });

            entity.Code = newCode;
        }

        if (!string.IsNullOrWhiteSpace(req.Name))
            entity.Name = req.Name.Trim();

        if (req.PolicyJson is not null) entity.PolicyJson = NullIfWhiteSpace(req.PolicyJson);
        if (req.Notes is not null) entity.Notes = NullIfWhiteSpace(req.Notes);
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

    [HttpPost("property-policies/{id:guid}/delete")]
    public async Task<IActionResult> DeletePropertyPolicy(Guid id, CancellationToken ct = default)
    {
        return await TogglePropertyDeleted(id, true, ct);
    }

    [HttpPost("property-policies/{id:guid}/restore")]
    public async Task<IActionResult> RestorePropertyPolicy(Guid id, CancellationToken ct = default)
    {
        return await TogglePropertyDeleted(id, false, ct);
    }

    [HttpPost("property-policies/{id:guid}/activate")]
    public async Task<IActionResult> ActivatePropertyPolicy(Guid id, CancellationToken ct = default)
    {
        return await TogglePropertyActive(id, true, ct);
    }

    [HttpPost("property-policies/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivatePropertyPolicy(Guid id, CancellationToken ct = default)
    {
        return await TogglePropertyActive(id, false, ct);
    }

    // =========================================================
    // Toggle helpers
    // =========================================================

    private async Task<IActionResult> ToggleCancellationDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.CancellationPolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Cancellation policy not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        if (!isDeleted) entity.IsActive = true;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleCancellationActive(Guid id, bool isActive, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.CancellationPolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Cancellation policy not found in current switched tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleCheckInOutDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.CheckInOutRules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Check-in/out rule not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        if (!isDeleted) entity.IsActive = true;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> ToggleCheckInOutActive(Guid id, bool isActive, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.CheckInOutRules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Check-in/out rule not found in current switched tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> TogglePropertyDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.PropertyPolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Property policy not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        if (!isDeleted) entity.IsActive = true;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task<IActionResult> TogglePropertyActive(Guid id, bool isActive, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.PropertyPolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Property policy not found in current switched tenant." });

        entity.IsActive = isActive;
        entity.UpdatedAt = DateTimeOffset.Now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private async Task ReplaceCancellationRulesAsync(
        Guid tenantId,
        Guid cancellationPolicyId,
        List<AdminCancellationPolicyRuleUpsertDto> items,
        DateTimeOffset now,
        Guid? userId,
        CancellationToken ct)
    {
        var existingRows = await _db.CancellationPolicyRules.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.CancellationPolicyId == cancellationPolicyId)
            .ToListAsync(ct);

        _db.CancellationPolicyRules.RemoveRange(existingRows);

        if (items.Count == 0) return;

        var rows = items
            .OrderBy(x => x.Priority)
            .Select(x => new CancellationPolicyRule
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CancellationPolicyId = cancellationPolicyId,
                CancelBeforeHours = x.CancelBeforeHours,
                CancelBeforeDays = x.CancelBeforeDays,
                ChargeType = x.ChargeType,
                ChargeValue = x.ChargeValue,
                CurrencyCode = string.IsNullOrWhiteSpace(x.CurrencyCode) ? "VND" : x.CurrencyCode!.Trim(),
                Notes = NullIfWhiteSpace(x.Notes),
                Priority = x.Priority,
                IsActive = x.IsActive ?? true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = userId,
                UpdatedAt = now,
                UpdatedByUserId = userId
            })
            .ToList();

        _db.CancellationPolicyRules.AddRange(rows);
    }

    private void RequireAdminWriteTenant()
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            throw new InvalidOperationException("Admin write requires switched tenant context (X-TenantId).");
    }

    private static void ValidateCreateCancellation(AdminCreateCancellationPolicyRequest req)
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

        if (req.Type.HasValue && !Enum.IsDefined(req.Type.Value))
            throw new ArgumentException("Type is invalid.");
    }

    private static void ValidateUpdateCancellation(AdminUpdateCancellationPolicyRequest req)
    {
        if (req.Code is not null && req.Code.Length > 80)
            throw new ArgumentException("Code max length is 80.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");

        if (req.Type.HasValue && !Enum.IsDefined(req.Type.Value))
            throw new ArgumentException("Type is invalid.");
    }

    private static void ValidateCreateCheckInOut(AdminCreateCheckInOutRuleRequest req)
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
    }

    private static void ValidateUpdateCheckInOut(AdminUpdateCheckInOutRuleRequest req)
    {
        if (req.Code is not null && req.Code.Length > 80)
            throw new ArgumentException("Code max length is 80.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");
    }

    private static void ValidateCancellationRules(List<AdminCancellationPolicyRuleUpsertDto> items)
    {
        var duplicatePriority = items
            .GroupBy(x => x.Priority)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicatePriority is not null)
            throw new ArgumentException($"Cancellation policy rule priority '{duplicatePriority.Key}' is duplicated.");

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];

            if (!item.CancelBeforeHours.HasValue && !item.CancelBeforeDays.HasValue)
                throw new ArgumentException($"Rules[{i}] requires CancelBeforeHours or CancelBeforeDays.");

            if (item.CancelBeforeHours.HasValue && item.CancelBeforeHours.Value < 0)
                throw new ArgumentException($"Rules[{i}].CancelBeforeHours must be >= 0.");

            if (item.CancelBeforeDays.HasValue && item.CancelBeforeDays.Value < 0)
                throw new ArgumentException($"Rules[{i}].CancelBeforeDays must be >= 0.");

            if (!Enum.IsDefined(item.ChargeType))
                throw new ArgumentException($"Rules[{i}].ChargeType is invalid.");

            if (item.ChargeValue < 0)
                throw new ArgumentException($"Rules[{i}].ChargeValue must be >= 0.");

            if ((item.ChargeType == PenaltyChargeType.PercentOfNight ||
                 item.ChargeType == PenaltyChargeType.PercentOfTotal) &&
                item.ChargeValue > 100)
            {
                throw new ArgumentException($"Rules[{i}].ChargeValue must be between 0 and 100 for percentage charge types.");
            }

            if (item.Priority < 0)
                throw new ArgumentException($"Rules[{i}].Priority must be >= 0.");

            if (item.CurrencyCode is not null && item.CurrencyCode.Length > 10)
                throw new ArgumentException($"Rules[{i}].CurrencyCode max length is 10.");
        }
    }

    private static void ValidateCheckInOutWindow(
        TimeOnly checkInFrom,
        TimeOnly checkInTo,
        TimeOnly checkOutFrom,
        TimeOnly checkOutTo)
    {
        if (checkInFrom >= checkInTo)
            throw new ArgumentException("CheckInFrom must be earlier than CheckInTo.");

        if (checkOutFrom >= checkOutTo)
            throw new ArgumentException("CheckOutFrom must be earlier than CheckOutTo.");
    }

    private static void ValidateCreateProperty(AdminCreatePropertyPolicyRequest req)
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
    }

    private static void ValidateUpdateProperty(AdminUpdatePropertyPolicyRequest req)
    {
        if (req.Code is not null && req.Code.Length > 80)
            throw new ArgumentException("Code max length is 80.");

        if (req.Name is not null && req.Name.Length > 300)
            throw new ArgumentException("Name max length is 300.");
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class AdminCancellationPolicyPagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}

public sealed class AdminCancellationPolicyListItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public CancellationPolicyType Type { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AdminCancellationPolicyDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public CancellationPolicyType Type { get; set; }
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
    public List<AdminCancellationPolicyRuleDto> Rules { get; set; } = new();
}

public sealed class AdminCancellationPolicyRuleDto
{
    public Guid Id { get; set; }
    public int? CancelBeforeHours { get; set; }
    public int? CancelBeforeDays { get; set; }
    public PenaltyChargeType ChargeType { get; set; }
    public decimal ChargeValue { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public string? Notes { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}

public sealed class AdminCreateCancellationPolicyRequest
{
    public Guid HotelId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public CancellationPolicyType? Type { get; set; }
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public List<AdminCancellationPolicyRuleUpsertDto>? Rules { get; set; }
}

public sealed class AdminUpdateCancellationPolicyRequest
{
    public Guid? HotelId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public CancellationPolicyType? Type { get; set; }
    public string? Description { get; set; }
    public string? MetadataJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public List<AdminCancellationPolicyRuleUpsertDto>? Rules { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminCancellationPolicyRuleUpsertDto
{
    public int? CancelBeforeHours { get; set; }
    public int? CancelBeforeDays { get; set; }
    public PenaltyChargeType ChargeType { get; set; }
    public decimal ChargeValue { get; set; }
    public string? CurrencyCode { get; set; }
    public string? Notes { get; set; }
    public int Priority { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AdminCreateCancellationPolicyResponse
{
    public Guid Id { get; set; }
}

public sealed class AdminCheckInOutRulePagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}

public sealed class AdminCheckInOutRuleListItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TimeOnly CheckInFrom { get; set; }
    public TimeOnly CheckInTo { get; set; }
    public TimeOnly CheckOutFrom { get; set; }
    public TimeOnly CheckOutTo { get; set; }
    public bool AllowsEarlyCheckIn { get; set; }
    public bool AllowsLateCheckOut { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AdminCheckInOutRuleDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public TimeOnly CheckInFrom { get; set; }
    public TimeOnly CheckInTo { get; set; }
    public TimeOnly CheckOutFrom { get; set; }
    public TimeOnly CheckOutTo { get; set; }
    public bool AllowsEarlyCheckIn { get; set; }
    public bool AllowsLateCheckOut { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminCreateCheckInOutRuleRequest
{
    public Guid HotelId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public TimeOnly CheckInFrom { get; set; }
    public TimeOnly CheckInTo { get; set; }
    public TimeOnly CheckOutFrom { get; set; }
    public TimeOnly CheckOutTo { get; set; }
    public bool? AllowsEarlyCheckIn { get; set; }
    public bool? AllowsLateCheckOut { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AdminUpdateCheckInOutRuleRequest
{
    public Guid? HotelId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public TimeOnly? CheckInFrom { get; set; }
    public TimeOnly? CheckInTo { get; set; }
    public TimeOnly? CheckOutFrom { get; set; }
    public TimeOnly? CheckOutTo { get; set; }
    public bool? AllowsEarlyCheckIn { get; set; }
    public bool? AllowsLateCheckOut { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminCreateCheckInOutRuleResponse
{
    public Guid Id { get; set; }
}

public sealed class AdminPropertyPolicyPagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<T> Items { get; set; } = new();
}

public sealed class AdminPropertyPolicyListItemDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AdminPropertyPolicyDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid HotelId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? PolicyJson { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminCreatePropertyPolicyRequest
{
    public Guid HotelId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? PolicyJson { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AdminUpdatePropertyPolicyRequest
{
    public Guid? HotelId { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? PolicyJson { get; set; }
    public string? Notes { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminCreatePropertyPolicyResponse
{
    public Guid Id { get; set; }
}

