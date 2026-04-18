// FILE #041 (RE-SEND): TicketBooking.Infrastructure/Seed/TenantRolesSeed.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketBooking.Domain.Auth;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Infrastructure.Seed
{
    /// <summary>
    /// Seed tenant roles + permissions for NX001 (Bus tenant).
    /// Phase 5: uses IgnoreQueryFilters() so idempotent seed works even if soft-deleted.
    /// </summary>
    public static class TenantRolesSeed
    {
        public static async Task SeedForNx001Async(
            AppDbContext db,
            UserManager<AppUser> userManager,
            ILogger logger,
            CancellationToken ct = default)
        {
            var now = DateTimeOffset.Now;

            var nx = await db.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == "NX001", ct);

            if (nx is null)
            {
                logger.LogWarning("Tenant NX001 not found. Skip TenantRolesSeed.");
                return;
            }

            if (nx.IsDeleted)
            {
                nx.IsDeleted = false;
                nx.UpdatedAt = now;
            }

            // Ensure permissions exist
            var busTripsRead = await EnsurePermissionExistsAsync(db, "bus.trips.read", "Bus trips read", "bus", now, logger, ct);
            var ticketScan = await EnsurePermissionExistsAsync(db, "ticket.scan", "Ticket scan", "ticketing", now, logger, ct);

            await db.SaveChangesAsync(ct);

            // Create tenant roles
            var nxStaff = await EnsureTenantRoleAsync(db, nx.Id, "NX_Staff", "NX Staff", "General staff role for NX001", now, logger, ct);
            var nxScanner = await EnsureTenantRoleAsync(db, nx.Id, "NX_TicketScanner", "NX Ticket Scanner", "Can scan tickets in NX001", now, logger, ct);

            await db.SaveChangesAsync(ct);

            // Map permissions to tenant roles
            await EnsureTenantRolePermissionAsync(db, nx.Id, nxStaff.Id, busTripsRead.Id, now, logger, ct);
            await EnsureTenantRolePermissionAsync(db, nx.Id, nxScanner.Id, ticketScan.Id, now, logger, ct);

            await db.SaveChangesAsync(ct);

            // Ensure users belong to NX001 + roles
            await EnsureTenantUserLinkAsync(db, userManager, nx.Id, "customer", roleName: RoleNames.Customer, isOwner: false, now, logger, ct);
            await db.SaveChangesAsync(ct);
            await EnsureTenantUserRoleAsync(db, userManager, nx.Id, nxStaff.Id, "customer", now, logger, ct);
            await db.SaveChangesAsync(ct);

            await EnsureTenantUserLinkAsync(db, userManager, nx.Id, "scanner_nx", roleName: "Staff", isOwner: false, now, logger, ct);
            await db.SaveChangesAsync(ct);
            await EnsureTenantUserRoleAsync(db, userManager, nx.Id, nxScanner.Id, "scanner_nx", now, logger, ct);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("TenantRoles seed completed for NX001.");
        }

        private static async Task<Permission> EnsurePermissionExistsAsync(
            AppDbContext db,
            string code,
            string name,
            string category,
            DateTimeOffset now,
            ILogger logger,
            CancellationToken ct)
        {
            var p = await db.Permissions.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Code == code, ct);
            if (p is not null)
            {
                var changed = false;
                if (p.IsDeleted) { p.IsDeleted = false; changed = true; }
                if (!p.IsActive) { p.IsActive = true; changed = true; }
                if (p.Name != name) { p.Name = name; changed = true; }
                if (p.Category != category) { p.Category = category; changed = true; }
                if (changed) p.UpdatedAt = now;
                return p;
            }

            p = new Permission
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                Category = category,
                SortOrder = 0,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now
            };

            db.Permissions.Add(p);
            logger.LogInformation("Created permission {Code}.", code);
            return p;
        }

        private static async Task<TenantRole> EnsureTenantRoleAsync(
            AppDbContext db,
            Guid tenantId,
            string code,
            string name,
            string? description,
            DateTimeOffset now,
            ILogger logger,
            CancellationToken ct)
        {
            var role = await db.TenantRoles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == code, ct);

            if (role is null)
            {
                role = new TenantRole
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Code = code,
                    Name = name,
                    Description = description,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now
                };

                db.TenantRoles.Add(role);
                logger.LogInformation("Created TenantRole {Code} for tenant {TenantId}.", code, tenantId);
                return role;
            }

            var changed = false;
            if (role.IsDeleted) { role.IsDeleted = false; changed = true; }
            if (!role.IsActive) { role.IsActive = true; changed = true; }
            if (role.Name != name) { role.Name = name; changed = true; }
            if (role.Description != description) { role.Description = description; changed = true; }

            if (changed)
            {
                role.UpdatedAt = now;
                logger.LogInformation("Updated TenantRole {Code} for tenant {TenantId}.", code, tenantId);
            }

            return role;
        }

        private static async Task EnsureTenantRolePermissionAsync(
            AppDbContext db,
            Guid tenantId,
            Guid tenantRoleId,
            Guid permissionId,
            DateTimeOffset now,
            ILogger logger,
            CancellationToken ct)
        {
            var exists = await db.TenantRolePermissions.IgnoreQueryFilters().AnyAsync(x =>
                x.TenantId == tenantId &&
                x.TenantRoleId == tenantRoleId &&
                x.PermissionId == permissionId &&
                !x.IsDeleted, ct);

            if (exists) return;

            db.TenantRolePermissions.Add(new TenantRolePermission
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TenantRoleId = tenantRoleId,
                PermissionId = permissionId,
                IsDeleted = false,
                CreatedAt = now
            });

            logger.LogInformation("Granted TenantRole {TenantRoleId} permission {PermissionId}.", tenantRoleId, permissionId);
        }

        private static async Task EnsureTenantUserLinkAsync(
            AppDbContext db,
            UserManager<AppUser> userManager,
            Guid tenantId,
            string userName,
            string roleName,
            bool isOwner,
            DateTimeOffset now,
            ILogger logger,
            CancellationToken ct)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user is null)
            {
                logger.LogWarning("User '{UserName}' not found. Skip TenantUser link.", userName);
                return;
            }

            var link = await db.TenantUsers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == user.Id, ct);

            if (link is null)
            {
                db.TenantUsers.Add(new TenantUser
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UserId = user.Id,
                    RoleName = roleName,
                    IsOwner = isOwner,
                    IsDeleted = false,
                    CreatedAt = now
                });

                logger.LogInformation("Linked user '{UserName}' into tenant {TenantId}.", userName, tenantId);
                return;
            }

            var changed = false;
            if (link.IsDeleted) { link.IsDeleted = false; changed = true; }
            if (link.RoleName != roleName) { link.RoleName = roleName; changed = true; }
            if (link.IsOwner != isOwner) { link.IsOwner = isOwner; changed = true; }

            if (changed)
            {
                link.UpdatedAt = now;
                logger.LogInformation("Updated TenantUser link '{UserName}' -> tenant {TenantId}.", userName, tenantId);
            }
        }

        private static async Task EnsureTenantUserRoleAsync(
            AppDbContext db,
            UserManager<AppUser> userManager,
            Guid tenantId,
            Guid tenantRoleId,
            string userName,
            DateTimeOffset now,
            ILogger logger,
            CancellationToken ct)
        {
            var user = await userManager.FindByNameAsync(userName);
            if (user is null)
            {
                logger.LogWarning("User '{UserName}' not found. Skip TenantUserRole.", userName);
                return;
            }

            var exists = await db.TenantUserRoles.IgnoreQueryFilters().AnyAsync(x =>
                x.TenantId == tenantId &&
                x.TenantRoleId == tenantRoleId &&
                x.UserId == user.Id &&
                !x.IsDeleted, ct);

            if (exists) return;

            db.TenantUserRoles.Add(new TenantUserRole
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TenantRoleId = tenantRoleId,
                UserId = user.Id,
                IsDeleted = false,
                CreatedAt = now
            });

            logger.LogInformation("Assigned TenantRole {TenantRoleId} to user '{UserName}' in tenant {TenantId}.", tenantRoleId, userName, tenantId);
        }
    }
}