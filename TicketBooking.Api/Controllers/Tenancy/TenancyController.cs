// FILE #021: TicketBooking.Api/Controllers/TenancyController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/tenancy")]
public sealed class TenancyController : ControllerBase
{
    public sealed class UpdateCurrentTenantSettingsRequest
    {
        public string? Name { get; set; }
        public int HoldMinutes { get; set; } = 5;
    }

    private sealed class MembershipTenantRoleItem
    {
        public Guid Id { get; init; }
        public string Code { get; init; } = "";
        public string Name { get; init; } = "";
        public bool IsActive { get; init; }
    }

    private readonly ITenantContext _tenantContext;
    private readonly AppDbContext _db;

    public TenancyController(ITenantContext tenantContext, AppDbContext db)
    {
        _tenantContext = tenantContext;
        _db = db;
    }

    /// <summary>
    /// Debug endpoint: shows current tenant context resolved by middleware.
    /// - Non-admin (single tenant) => auto TenantId
    /// - Non-admin (multi-tenant) => requires X-TenantId
    /// - Admin => TenantId can be null for read requests
    /// </summary>
    [HttpGet("whoami")]
    public IActionResult WhoAmI()
    {
        return Ok(new
        {
            tenantId = _tenantContext.TenantId,
            hasTenant = _tenantContext.HasTenant,
            userId = _tenantContext.UserId,
            isAuthenticated = _tenantContext.IsAuthenticated,
            isAdmin = _tenantContext.IsAdmin,
            requiresTenantForWrite = _tenantContext.RequiresTenantForWrite,
            userTenantIds = _tenantContext.UserTenantIds
        });
    }

    /// <summary>
    /// Returns tenant memberships of the current authenticated user.
    /// Useful for tenant portal bootstrapping and tenant switcher UI.
    /// </summary>
    [HttpGet("memberships")]
    public async Task<IActionResult> Memberships(CancellationToken ct = default)
    {
        if (!_tenantContext.UserId.HasValue || _tenantContext.UserId.Value == Guid.Empty)
            return Unauthorized(new { message = "Invalid user identity." });

        var userId = _tenantContext.UserId.Value;

        var tenantLinks = await (
            from tu in _db.TenantUsers.AsNoTracking()
            join t in _db.Tenants.IgnoreQueryFilters().AsNoTracking() on tu.TenantId equals t.Id
            where tu.UserId == userId && !tu.IsDeleted && !t.IsDeleted
            select new
            {
                tu.TenantId,
                TenantCode = t.Code,
                TenantName = t.Name,
                TenantType = t.Type.ToString(),
                TenantStatus = t.Status.ToString(),
                tu.RoleName,
                tu.IsOwner
            }
        ).ToListAsync(ct);

        var tenantIds = tenantLinks.Select(x => x.TenantId).Distinct().ToArray();

        var tenantRoleRows = tenantIds.Length == 0
            ? new List<(Guid TenantId, MembershipTenantRoleItem Role)>()
            : (await (
                from tur in _db.TenantUserRoles.AsNoTracking()
                join tr in _db.TenantRoles.IgnoreQueryFilters().AsNoTracking() on tur.TenantRoleId equals tr.Id
                where tur.UserId == userId
                      && !tur.IsDeleted
                      && !tr.IsDeleted
                      && tenantIds.Contains(tur.TenantId)
                select new
                {
                    tur.TenantId,
                    Role = new MembershipTenantRoleItem
                    {
                        Id = tr.Id,
                        Code = tr.Code,
                        Name = tr.Name,
                        IsActive = tr.IsActive
                    }
                }
            ).ToListAsync(ct))
            .Select(x => (x.TenantId, x.Role))
            .ToList();

        var roleMap = tenantRoleRows
            .GroupBy(x => x.TenantId)
            .ToDictionary(
                x => x.Key,
                x => x.Select(y => y.Role).ToList());

        var items = tenantLinks
            .Select(x => new
            {
                x.TenantId,
                Code = x.TenantCode,
                Name = x.TenantName,
                Type = x.TenantType,
                Status = x.TenantStatus,
                x.RoleName,
                x.IsOwner,
                TenantRoles = roleMap.TryGetValue(x.TenantId, out var roles) ? roles.ToArray() : Array.Empty<MembershipTenantRoleItem>()
            })
            .OrderBy(x => x.Code)
            .ToList();

        return Ok(new
        {
            userId,
            currentTenantId = _tenantContext.TenantId,
            total = items.Count,
            items
        });
    }

    [HttpGet("current-tenant/settings")]
    [Authorize(Policy = "perm:tenant.settings.read")]
    public async Task<IActionResult> CurrentTenantSettings(CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Tenant context is required." });

        var tenantId = _tenantContext.TenantId!.Value;
        var tenant = await _db.Tenants.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.Id == tenantId && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                Type = x.Type.ToString(),
                Status = x.Status.ToString(),
                x.HoldMinutes,
                x.CreatedAt,
                x.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (tenant is null)
            return NotFound(new { message = "Tenant not found." });

        var membership = _tenantContext.UserId.HasValue
            ? await _db.TenantUsers.AsNoTracking()
                .Where(x => x.TenantId == tenantId && x.UserId == _tenantContext.UserId.Value && !x.IsDeleted)
                .Select(x => new
                {
                    x.RoleName,
                    x.IsOwner,
                    x.CreatedAt
                })
                .FirstOrDefaultAsync(ct)
            : null;

        return Ok(new
        {
            tenant,
            membership
        });
    }

    [HttpPut("current-tenant/settings")]
    [Authorize(Policy = "perm:tenant.settings.read")]
    public async Task<IActionResult> UpdateCurrentTenantSettings(
        [FromBody] UpdateCurrentTenantSettingsRequest request,
        CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Tenant context is required." });

        var tenantId = _tenantContext.TenantId!.Value;
        var tenant = await _db.Tenants.FirstOrDefaultAsync(x => x.Id == tenantId && !x.IsDeleted, ct);
        if (tenant is null)
            return NotFound(new { message = "Tenant not found." });

        var name = (request.Name ?? "").Trim();
        if (name.Length == 0)
            return BadRequest(new { message = "Tenant name is required." });

        tenant.Name = name.Length > 200 ? name[..200] : name;
        tenant.HoldMinutes = Math.Clamp(request.HoldMinutes, 1, 60);
        tenant.UpdatedAt = DateTimeOffset.Now;
        tenant.UpdatedByUserId = _tenantContext.UserId;

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            tenant.Id,
            tenant.Code,
            tenant.Name,
            Type = tenant.Type.ToString(),
            Status = tenant.Status.ToString(),
            tenant.HoldMinutes,
            tenant.UpdatedAt
        });
    }

    /// <summary>
    /// Write test endpoint:
    /// - Admin MUST send X-TenantId (middleware enforces)
    /// - Non-admin must pass tenant rules as usual
    /// </summary>
    [HttpPost("write-test")]
    public IActionResult WriteTest()
    {
        return Ok(new
        {
            ok = true,
            tenantId = _tenantContext.TenantId,
            message = "Write request passed tenant enforcement."
        });
    }
}
