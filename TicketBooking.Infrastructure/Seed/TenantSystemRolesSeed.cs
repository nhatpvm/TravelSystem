using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketBooking.Domain.Auth;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Infrastructure.Seed;

public static class TenantSystemRolesSeed
{
    private sealed record TenantRoleTemplate(
        string Code,
        string Name,
        string Description,
        string StorageRoleName,
        IReadOnlyList<string> PermissionCodes);

    public static async Task SeedAsync(
        AppDbContext db,
        UserManager<AppUser> userManager,
        ILogger logger,
        CancellationToken ct = default)
    {
        var now = DateTimeOffset.Now;

        var tenants = await db.Tenants.IgnoreQueryFilters()
            .Where(x => !x.IsDeleted && x.Type != TenantType.Platform)
            .OrderBy(x => x.Code)
            .ToListAsync(ct);

        if (tenants.Count == 0)
        {
            logger.LogInformation("Tenant system role seed skipped: no active business tenants found.");
            return;
        }

        foreach (var tenant in tenants)
        {
            var templates = BuildTemplates(tenant.Type);
            var permissionMap = new Dictionary<string, Permission>(StringComparer.OrdinalIgnoreCase);

            foreach (var permissionCode in templates.SelectMany(x => x.PermissionCodes).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                permissionMap[permissionCode] = await EnsurePermissionAsync(db, permissionCode, now, logger, ct);
            }

            var tenantRoleMap = new Dictionary<string, TenantRole>(StringComparer.OrdinalIgnoreCase);
            foreach (var template in templates)
            {
                var tenantRole = await EnsureTenantRoleAsync(db, tenant, template, now, logger, ct);
                tenantRoleMap[template.Code] = tenantRole;
                await EnsureTenantRolePermissionsAsync(db, tenant.Id, tenantRole, template, permissionMap, now, logger, ct);
            }

            var links = await db.TenantUsers.IgnoreQueryFilters()
                .Where(x => x.TenantId == tenant.Id && !x.IsDeleted)
                .OrderByDescending(x => x.IsOwner)
                .ThenBy(x => x.CreatedAt)
                .ToListAsync(ct);

            foreach (var link in links)
            {
                var roleCode = ResolveSystemRoleCode(link);
                if (!tenantRoleMap.TryGetValue(roleCode, out var tenantRole))
                {
                    continue;
                }

                if (!string.Equals(link.RoleName, tenantRole.Name, StringComparison.OrdinalIgnoreCase))
                {
                    link.RoleName = tenantRole.Name;
                    link.UpdatedAt = now;
                }

                await EnsureTenantUserRoleAsync(
                    db,
                    tenant.Id,
                    link.UserId,
                    tenantRole,
                    templates.Select(x => x.Code).ToArray(),
                    now,
                    logger,
                    ct);
            }
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Tenant system role seed completed.");
    }

    private static TenantRoleTemplate[] BuildTemplates(TenantType tenantType)
    {
        var modulePermission = ResolveModulePermissionCode(tenantType);
        var ticketPermissions = modulePermission is null
            ? new[] { "tenant.bookings.read" }
            : new[] { "tenant.bookings.read", modulePermission, "ticket.scan" };

        return
        [
            new TenantRoleTemplate(
                "MANAGER",
                "Quản lý",
                "System-managed manager role for tenant portal.",
                "Manager",
                BuildDistinctPermissions(
                    modulePermission,
                    "cms.posts.read",
                    "cms.posts.write",
                    "cms.posts.publish",
                    "cms.media.manage",
                    "cms.taxonomy.manage",
                    "cms.redirects.manage",
                    "cms.site-settings.manage",
                    "cms.seo.audit",
                    "tenant.dashboard.read",
                    "tenant.bookings.read",
                    "tenant.reviews.read",
                    "tenant.staff.manage",
                    "tenant.finance.read",
                    "tenant.reports.read",
                    "tenant.settings.read")),
            new TenantRoleTemplate(
                "ACCOUNTANT",
                "Kế toán",
                "System-managed accountant role for tenant portal.",
                "Accountant",
                BuildDistinctPermissions(
                    "tenant.finance.read",
                    "tenant.reports.read",
                    "tenant.settings.read")),
            new TenantRoleTemplate(
                "OPS",
                "Vận hành",
                "System-managed operations role for tenant portal.",
                "Ops",
                BuildDistinctPermissions(
                    modulePermission,
                    "tenant.dashboard.read",
                    "tenant.bookings.read")),
            new TenantRoleTemplate(
                "SUPPORT",
                "CSKH",
                "System-managed support role for tenant portal.",
                "Support",
                BuildDistinctPermissions(
                    "tenant.bookings.read",
                    "tenant.reviews.read")),
            new TenantRoleTemplate(
                "TICKET",
                "Đại lý vé",
                "System-managed ticket role for tenant portal.",
                "Ticket",
                BuildDistinctPermissions(ticketPermissions))
        ];
    }

    private static IReadOnlyList<string> BuildDistinctPermissions(params string?[] values)
        => values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string? ResolveModulePermissionCode(TenantType tenantType)
        => tenantType switch
        {
            TenantType.Bus => "bus.trips.read",
            TenantType.Train => "train.trips.read",
            TenantType.Flight => "flight.inventory.read",
            TenantType.Hotel => "hotel.inventory.read",
            TenantType.Tour => "tour.inventory.read",
            _ => null
        };

    private static string ResolveSystemRoleCode(TenantUser link)
    {
        if (link.IsOwner)
        {
            return "MANAGER";
        }

        var normalized = (link.RoleName ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return "MANAGER";
        }

        if (normalized.StartsWith("QL", StringComparison.OrdinalIgnoreCase))
        {
            return "MANAGER";
        }

        if (normalized.Contains("ACCOUNT", StringComparison.OrdinalIgnoreCase))
        {
            return "ACCOUNTANT";
        }

        if (normalized.Contains("SUPPORT", StringComparison.OrdinalIgnoreCase))
        {
            return "SUPPORT";
        }

        if (normalized.Contains("TICKET", StringComparison.OrdinalIgnoreCase))
        {
            return "TICKET";
        }

        if (normalized.Contains("OPS", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("VAN HANH", StringComparison.OrdinalIgnoreCase))
        {
            return "OPS";
        }

        return "MANAGER";
    }

    private static async Task<Permission> EnsurePermissionAsync(
        AppDbContext db,
        string code,
        DateTimeOffset now,
        ILogger logger,
        CancellationToken ct)
    {
        var existing = await db.Permissions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Code == code, ct);

        if (existing is not null)
        {
            var changed = false;
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }
            if (changed)
            {
                existing.UpdatedAt = now;
            }

            return existing;
        }

        var category = code.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "tenant";
        var entity = new Permission
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code.Replace('.', ' '),
            Category = category,
            SortOrder = 0,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now
        };

        db.Permissions.Add(entity);
        logger.LogInformation("Created missing tenant permission {PermissionCode}.", code);
        return entity;
    }

    private static async Task<TenantRole> EnsureTenantRoleAsync(
        AppDbContext db,
        Tenant tenant,
        TenantRoleTemplate template,
        DateTimeOffset now,
        ILogger logger,
        CancellationToken ct)
    {
        var existing = await db.TenantRoles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenant.Id &&
                     x.Code.ToUpper() == template.Code.ToUpper(),
                ct);

        if (existing is null)
        {
            existing = new TenantRole
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Code = template.Code,
                Name = template.Name,
                Description = template.Description,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now
            };

            db.TenantRoles.Add(existing);
            logger.LogInformation("Created tenant role {RoleCode} for tenant {TenantCode}.", template.Code, tenant.Code);
            return existing;
        }

        var changed = false;
        if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }
        if (!existing.IsActive) { existing.IsActive = true; changed = true; }
        if (!string.Equals(existing.Name, template.Name, StringComparison.Ordinal)) { existing.Name = template.Name; changed = true; }
        if (!string.Equals(existing.Description, template.Description, StringComparison.Ordinal)) { existing.Description = template.Description; changed = true; }
        if (!string.Equals(existing.Code, template.Code, StringComparison.Ordinal)) { existing.Code = template.Code; changed = true; }

        if (changed)
        {
            existing.UpdatedAt = now;
        }

        return existing;
    }

    private static async Task EnsureTenantRolePermissionsAsync(
        AppDbContext db,
        Guid tenantId,
        TenantRole tenantRole,
        TenantRoleTemplate template,
        IReadOnlyDictionary<string, Permission> permissionMap,
        DateTimeOffset now,
        ILogger logger,
        CancellationToken ct)
    {
        var existingLinks = await db.TenantRolePermissions.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TenantRoleId == tenantRole.Id)
            .ToListAsync(ct);

        var targetPermissionIds = template.PermissionCodes
            .Where(permissionMap.ContainsKey)
            .Select(x => permissionMap[x].Id)
            .ToHashSet();

        foreach (var permissionId in targetPermissionIds)
        {
            var current = existingLinks.FirstOrDefault(x => x.PermissionId == permissionId);
            if (current is null)
            {
                db.TenantRolePermissions.Add(new TenantRolePermission
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    TenantRoleId = tenantRole.Id,
                    PermissionId = permissionId,
                    IsDeleted = false,
                    CreatedAt = now
                });

                logger.LogInformation("Granted tenant role {RoleCode} permission {PermissionId}.", tenantRole.Code, permissionId);
                continue;
            }

            if (current.IsDeleted)
            {
                current.IsDeleted = false;
                current.UpdatedAt = now;
            }
        }

        foreach (var stale in existingLinks.Where(x => !x.IsDeleted && !targetPermissionIds.Contains(x.PermissionId)))
        {
            stale.IsDeleted = true;
            stale.UpdatedAt = now;
        }
    }

    private static async Task EnsureTenantUserRoleAsync(
        AppDbContext db,
        Guid tenantId,
        Guid userId,
        TenantRole targetRole,
        IReadOnlyCollection<string> systemRoleCodes,
        DateTimeOffset now,
        ILogger logger,
        CancellationToken ct)
    {
        var existingLinks = await (
            from tur in db.TenantUserRoles.IgnoreQueryFilters()
            join tr in db.TenantRoles.IgnoreQueryFilters() on tur.TenantRoleId equals tr.Id
            where tur.TenantId == tenantId &&
                  tur.UserId == userId &&
                  systemRoleCodes.Contains(tr.Code)
            select new
            {
                UserRole = tur,
                RoleCode = tr.Code
            }
        ).ToListAsync(ct);

        foreach (var row in existingLinks)
        {
            var shouldBeActive = row.UserRole.TenantRoleId == targetRole.Id;
            if (shouldBeActive)
            {
                if (row.UserRole.IsDeleted)
                {
                    row.UserRole.IsDeleted = false;
                    row.UserRole.UpdatedAt = now;
                }

                return;
            }

            if (!row.UserRole.IsDeleted)
            {
                row.UserRole.IsDeleted = true;
                row.UserRole.UpdatedAt = now;
            }
        }

        db.TenantUserRoles.Add(new TenantUserRole
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantRoleId = targetRole.Id,
            UserId = userId,
            IsDeleted = false,
            CreatedAt = now
        });

        logger.LogInformation("Assigned system tenant role {RoleCode} to user {UserId} for tenant {TenantId}.", targetRole.Code, userId, tenantId);
    }
}
