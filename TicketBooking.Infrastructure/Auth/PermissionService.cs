// FILE #028: TicketBooking.Infrastructure/Auth/PermissionService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Auth;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Infrastructure.Auth
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(Guid userId, string permissionCode, Guid? tenantId, CancellationToken ct = default);
        Task<IReadOnlyList<EffectivePermissionInfo>> GetEffectivePermissionsAsync(Guid userId, Guid? tenantId, CancellationToken ct = default);
    }

    public sealed class EffectivePermissionInfo
    {
        public Guid PermissionId { get; init; }
        public string Code { get; init; } = "";
        public string Name { get; init; } = "";
        public string? Category { get; init; }
        public bool IsGranted { get; init; }
        public string Resolution { get; init; } = "";
        public string[] Sources { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Permission resolution order (most specific -> least):
    /// 1) auth.UserPermissions (TenantId match first; then global TenantId null)
    ///    - Deny wins immediately.
    ///    - Allow returns true if no deny.
    /// 2) tenants.TenantRolePermissions via tenants.TenantUserRoles (within the tenantId)
    /// 3) auth.RolePermissions via Identity roles (global)
    /// Default: false.
    /// </summary>
    public sealed class PermissionService : IPermissionService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public PermissionService(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, Guid? tenantId, CancellationToken ct = default)
        {
            permissionCode = (permissionCode ?? "").Trim();
            if (string.IsNullOrWhiteSpace(permissionCode))
                return false;

            // Resolve permission id
            var perm = await _db.Permissions
                .Where(x => x.Code == permissionCode && !x.IsDeleted && x.IsActive)
                .Select(x => new { x.Id })
                .FirstOrDefaultAsync(ct);

            if (perm is null)
                return false;

            var permId = perm.Id;

            // 1) UserPermissions (tenant-scoped overrides first)
            // Deny wins immediately.
            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            {
                var tenantOverride = await _db.UserPermissions
                    .Where(x => x.UserId == userId && x.PermissionId == permId && x.TenantId == tenantId && !x.IsDeleted)
                    .Select(x => new { x.Effect })
                    .FirstOrDefaultAsync(ct);

                if (tenantOverride is not null)
                {
                    return tenantOverride.Effect == PermissionEffect.Allow;
                }
            }

            // Global override (TenantId null)
            var globalOverride = await _db.UserPermissions
                .Where(x => x.UserId == userId && x.PermissionId == permId && x.TenantId == null && !x.IsDeleted)
                .Select(x => new { x.Effect })
                .FirstOrDefaultAsync(ct);

            if (globalOverride is not null)
            {
                return globalOverride.Effect == PermissionEffect.Allow;
            }

            // 2) TenantRolePermissions (requires tenantId)
            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            {
                var hasTenantRolePerm = await (
                    from tur in _db.TenantUserRoles
                    join trp in _db.TenantRolePermissions on tur.TenantRoleId equals trp.TenantRoleId
                    where !tur.IsDeleted
                          && !trp.IsDeleted
                          && tur.UserId == userId
                          && tur.TenantId == tenantId.Value
                          && trp.TenantId == tenantId.Value
                          && trp.PermissionId == permId
                    select tur.Id
                ).AnyAsync(ct);

                if (hasTenantRolePerm)
                    return true;
            }

            // 3) RolePermissions via Identity roles
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return false;

            var roleNames = await _userManager.GetRolesAsync(user);
            if (roleNames.Count == 0)
                return false;

            var roleIds = await _db.Roles
                .Where(r => roleNames.Contains(r.Name!))
                .Select(r => r.Id)
                .ToListAsync(ct);

            if (roleIds.Count == 0)
                return false;

            var hasRolePerm = await _db.RolePermissions
                .Where(x => roleIds.Contains(x.RoleId) && x.PermissionId == permId && !x.IsDeleted)
                .AnyAsync(ct);

            return hasRolePerm;
        }

        public async Task<IReadOnlyList<EffectivePermissionInfo>> GetEffectivePermissionsAsync(Guid userId, Guid? tenantId, CancellationToken ct = default)
        {
            var permissions = await _db.Permissions
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive)
                .OrderBy(x => x.Category)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.Name,
                    x.Category
                })
                .ToListAsync(ct);

            var relevantOverrides = await _db.UserPermissions
                .AsNoTracking()
                .Where(x => x.UserId == userId &&
                            !x.IsDeleted &&
                            (x.TenantId == null || (tenantId.HasValue && x.TenantId == tenantId.Value)))
                .Select(x => new
                {
                    x.PermissionId,
                    x.TenantId,
                    x.Effect
                })
                .ToListAsync(ct);

            var tenantOverrideMap = relevantOverrides
                .Where(x => tenantId.HasValue && tenantId.Value != Guid.Empty && x.TenantId == tenantId.Value)
                .ToDictionary(x => x.PermissionId, x => x.Effect);

            var globalOverrideMap = relevantOverrides
                .Where(x => x.TenantId == null)
                .ToDictionary(x => x.PermissionId, x => x.Effect);

            var tenantRoleSourcesByPermissionId = new Dictionary<Guid, List<string>>();
            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
            {
                var tenantRoleRows = await (
                    from tur in _db.TenantUserRoles.AsNoTracking()
                    join tr in _db.TenantRoles.AsNoTracking() on tur.TenantRoleId equals tr.Id
                    join trp in _db.TenantRolePermissions.AsNoTracking() on tur.TenantRoleId equals trp.TenantRoleId
                    join p in _db.Permissions.AsNoTracking() on trp.PermissionId equals p.Id
                    where !tur.IsDeleted
                          && !tr.IsDeleted
                          && !trp.IsDeleted
                          && !p.IsDeleted
                          && p.IsActive
                          && tur.UserId == userId
                          && tur.TenantId == tenantId.Value
                          && tr.TenantId == tenantId.Value
                          && trp.TenantId == tenantId.Value
                    select new
                    {
                        trp.PermissionId,
                        RoleCode = tr.Code,
                        RoleName = tr.Name
                    }
                ).ToListAsync(ct);

                tenantRoleSourcesByPermissionId = tenantRoleRows
                    .GroupBy(x => x.PermissionId)
                    .ToDictionary(
                        x => x.Key,
                        x => x
                            .Select(y => string.IsNullOrWhiteSpace(y.RoleCode) ? y.RoleName : y.RoleCode)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList());
            }

            var user = await _userManager.FindByIdAsync(userId.ToString());
            var globalRoleSourcesByPermissionId = new Dictionary<Guid, List<string>>();

            if (user is not null)
            {
                var roleNames = await _userManager.GetRolesAsync(user);
                if (roleNames.Count > 0)
                {
                    var globalRoleRows = await (
                        from r in _db.Roles.AsNoTracking()
                        join rp in _db.RolePermissions.AsNoTracking() on r.Id equals rp.RoleId
                        join p in _db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                        where r.Name != null
                              && roleNames.Contains(r.Name)
                              && !rp.IsDeleted
                              && !p.IsDeleted
                              && p.IsActive
                        select new
                        {
                            rp.PermissionId,
                            RoleName = r.Name!
                        }
                    ).ToListAsync(ct);

                    globalRoleSourcesByPermissionId = globalRoleRows
                        .GroupBy(x => x.PermissionId)
                        .ToDictionary(
                            x => x.Key,
                            x => x.Select(y => y.RoleName).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
                }
            }

            var items = new List<EffectivePermissionInfo>(permissions.Count);

            foreach (var permission in permissions)
            {
                if (tenantOverrideMap.TryGetValue(permission.Id, out var tenantEffect))
                {
                    items.Add(new EffectivePermissionInfo
                    {
                        PermissionId = permission.Id,
                        Code = permission.Code,
                        Name = permission.Name,
                        Category = permission.Category,
                        IsGranted = tenantEffect == PermissionEffect.Allow,
                        Resolution = tenantEffect == PermissionEffect.Allow ? "user-override-tenant-allow" : "user-override-tenant-deny",
                        Sources = new[] { $"tenant-user-override:{tenantEffect}" }
                    });
                    continue;
                }

                if (globalOverrideMap.TryGetValue(permission.Id, out var globalEffect))
                {
                    items.Add(new EffectivePermissionInfo
                    {
                        PermissionId = permission.Id,
                        Code = permission.Code,
                        Name = permission.Name,
                        Category = permission.Category,
                        IsGranted = globalEffect == PermissionEffect.Allow,
                        Resolution = globalEffect == PermissionEffect.Allow ? "user-override-global-allow" : "user-override-global-deny",
                        Sources = new[] { $"global-user-override:{globalEffect}" }
                    });
                    continue;
                }

                if (tenantRoleSourcesByPermissionId.TryGetValue(permission.Id, out var tenantRoleSources) && tenantRoleSources.Count > 0)
                {
                    items.Add(new EffectivePermissionInfo
                    {
                        PermissionId = permission.Id,
                        Code = permission.Code,
                        Name = permission.Name,
                        Category = permission.Category,
                        IsGranted = true,
                        Resolution = "tenant-role",
                        Sources = tenantRoleSources.ToArray()
                    });
                    continue;
                }

                if (globalRoleSourcesByPermissionId.TryGetValue(permission.Id, out var globalRoleSources) && globalRoleSources.Count > 0)
                {
                    items.Add(new EffectivePermissionInfo
                    {
                        PermissionId = permission.Id,
                        Code = permission.Code,
                        Name = permission.Name,
                        Category = permission.Category,
                        IsGranted = true,
                        Resolution = "global-role",
                        Sources = globalRoleSources.ToArray()
                    });
                    continue;
                }

                items.Add(new EffectivePermissionInfo
                {
                    PermissionId = permission.Id,
                    Code = permission.Code,
                    Name = permission.Name,
                    Category = permission.Category,
                    IsGranted = false,
                    Resolution = "none",
                    Sources = Array.Empty<string>()
                });
            }

            return items;
        }
    }
}
