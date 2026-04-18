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
[Route("api/v{version:apiVersion}/admin/room-type-policies")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class RoomTypePoliciesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public RoomTypePoliciesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<AdminRoomTypePolicyPagedResponse>> List(
        [FromQuery] Guid? roomTypeId = null,
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

        IQueryable<RoomTypePolicy> query = includeDeleted
            ? _db.RoomTypePolicies.IgnoreQueryFilters().Where(x => x.TenantId == tenantId)
            : _db.RoomTypePolicies.Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (roomTypeId.HasValue)
            query = query.Where(x => x.RoomTypeId == roomTypeId.Value);

        if (hotelId.HasValue)
        {
            var roomTypeIds = await _db.RoomTypes.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.HotelId == hotelId.Value && !x.IsDeleted)
                .Select(x => x.Id)
                .ToListAsync(ct);

            query = query.Where(x => roomTypeIds.Contains(x.RoomTypeId));
        }

        if (activeOnly)
            query = query.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var qq = q.Trim();
            query = query.Where(x => x.PolicyJson != null && x.PolicyJson.Contains(qq));
        }

        var total = await query.CountAsync(ct);
        var items = await query.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AdminRoomTypePolicyListItemDto
            {
                Id = x.Id,
                RoomTypeId = x.RoomTypeId,
                PolicyJson = x.PolicyJson,
                IsActive = x.IsActive,
                IsDeleted = x.IsDeleted,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new AdminRoomTypePolicyPagedResponse
        {
            Page = page,
            PageSize = pageSize,
            Total = total,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminRoomTypePolicyDetailDto>> GetById(
        Guid id,
        [FromQuery] bool includeDeleted = true,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;

        IQueryable<RoomTypePolicy> query = includeDeleted
            ? _db.RoomTypePolicies.IgnoreQueryFilters()
            : _db.RoomTypePolicies;

        var entity = await query.AsNoTracking()
            .Where(x => x.TenantId == tenantId && (includeDeleted || !x.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Room type policy not found in current switched tenant." });

        return Ok(MapDetail(entity));
    }

    [HttpPost]
    public async Task<ActionResult<AdminCreateRoomTypePolicyResponse>> Create(
        [FromBody] AdminCreateRoomTypePolicyRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        if (req.RoomTypeId == Guid.Empty)
            return BadRequest(new { message = "RoomTypeId is required." });

        if (string.IsNullOrWhiteSpace(req.PolicyJson))
            return BadRequest(new { message = "PolicyJson is required." });

        var tenantId = _tenant.TenantId!.Value;
        var roomTypeExists = await _db.RoomTypes.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Id == req.RoomTypeId && !x.IsDeleted, ct);

        if (!roomTypeExists)
            return BadRequest(new { message = "RoomTypeId not found in current switched tenant." });

        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = new RoomTypePolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RoomTypeId = req.RoomTypeId,
            PolicyJson = req.PolicyJson.Trim(),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId,
            UpdatedAt = now,
            UpdatedByUserId = userId
        };

        _db.RoomTypePolicies.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetById),
            new { version = "1.0", id = entity.Id },
            new AdminCreateRoomTypePolicyResponse { Id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] AdminUpdateRoomTypePolicyRequest req,
        CancellationToken ct = default)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var now = DateTimeOffset.Now;
        var userId = GetCurrentUserId();

        var entity = await _db.RoomTypePolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Room type policy not found in current switched tenant." });

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
                return BadRequest(new { message = "RoomTypeId not found in current switched tenant." });

            entity.RoomTypeId = req.RoomTypeId.Value;
        }

        if (req.PolicyJson is not null)
        {
            if (string.IsNullOrWhiteSpace(req.PolicyJson))
                return BadRequest(new { message = "PolicyJson is required." });

            entity.PolicyJson = req.PolicyJson.Trim();
        }

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

    private async Task<IActionResult> ToggleDeleted(Guid id, bool isDeleted, CancellationToken ct)
    {
        RequireAdminWriteTenant();

        var tenantId = _tenant.TenantId!.Value;
        var userId = GetCurrentUserId();

        var entity = await _db.RoomTypePolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Room type policy not found in current switched tenant." });

        entity.IsDeleted = isDeleted;
        if (!isDeleted)
            entity.IsActive = true;

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

        var entity = await _db.RoomTypePolicies.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == id, ct);

        if (entity is null)
            return NotFound(new { message = "Room type policy not found in current switched tenant." });

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

    private static AdminRoomTypePolicyDetailDto MapDetail(RoomTypePolicy x) => new()
    {
        Id = x.Id,
        TenantId = x.TenantId,
        RoomTypeId = x.RoomTypeId,
        PolicyJson = x.PolicyJson,
        IsActive = x.IsActive,
        IsDeleted = x.IsDeleted,
        CreatedAt = x.CreatedAt,
        CreatedByUserId = x.CreatedByUserId,
        UpdatedAt = x.UpdatedAt,
        UpdatedByUserId = x.UpdatedByUserId,
        RowVersionBase64 = x.RowVersion is { Length: > 0 } rv ? Convert.ToBase64String(rv) : null
    };
}

public sealed class AdminRoomTypePolicyPagedResponse
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public List<AdminRoomTypePolicyListItemDto> Items { get; set; } = new();
}

public sealed class AdminRoomTypePolicyListItemDto
{
    public Guid Id { get; set; }
    public Guid RoomTypeId { get; set; }
    public string? PolicyJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class AdminRoomTypePolicyDetailDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RoomTypeId { get; set; }
    public string? PolicyJson { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminCreateRoomTypePolicyRequest
{
    public Guid RoomTypeId { get; set; }
    public string? PolicyJson { get; set; }
    public bool? IsActive { get; set; }
}

public sealed class AdminUpdateRoomTypePolicyRequest
{
    public Guid? RoomTypeId { get; set; }
    public string? PolicyJson { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsDeleted { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public sealed class AdminCreateRoomTypePolicyResponse
{
    public Guid Id { get; set; }
}
