// FILE #040 (RE-SEND): TicketBooking.Infrastructure/Seed/PermissionsSeed.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketBooking.Domain.Auth;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Infrastructure.Seed
{
    /// <summary>
    /// Phase 4 seed: create a minimal permission catalog and grant Admin role permissions.
    /// Phase 5: uses IgnoreQueryFilters() so idempotent seed works even if soft-deleted.
    /// </summary>
    public static class PermissionsSeed
    {
        public static async Task SeedAsync(
            AppDbContext db,
            RoleManager<AppRole> roleManager,
            ILogger logger,
            CancellationToken ct = default)
        {
            var now = DateTimeOffset.Now;

            // Core permissions
            var p1 = await EnsurePermissionAsync(db, "bus.trips.read", "Bus trips read", "bus", 10, now, logger, ct);
            var p2 = await EnsurePermissionAsync(db, "bus.trips.write", "Bus trips write", "bus", 11, now, logger, ct);
            var p3 = await EnsurePermissionAsync(db, "tenants.manage", "Tenants manage", "tenants", 1, now, logger, ct);
            var p4 = await EnsurePermissionAsync(db, "cms.posts.read", "CMS read posts", "cms", 1, now, logger, ct);
            var p5 = await EnsurePermissionAsync(db, "cms.posts.write", "CMS write posts", "cms", 2, now, logger, ct);
            var p6 = await EnsurePermissionAsync(db, "cms.posts.publish", "CMS publish posts", "cms", 3, now, logger, ct);
            var p7 = await EnsurePermissionAsync(db, "cms.media.manage", "CMS manage media", "cms", 4, now, logger, ct);
            var p8 = await EnsurePermissionAsync(db, "cms.taxonomy.manage", "CMS manage categories and tags", "cms", 5, now, logger, ct);
            var p9 = await EnsurePermissionAsync(db, "cms.redirects.manage", "CMS manage redirects", "cms", 6, now, logger, ct);
            var p10 = await EnsurePermissionAsync(db, "cms.site-settings.manage", "CMS manage site settings", "cms", 7, now, logger, ct);
            var p11 = await EnsurePermissionAsync(db, "cms.seo.audit", "CMS run SEO audit", "cms", 8, now, logger, ct);
            var p12 = await EnsurePermissionAsync(db, "ticket.scan", "Ticket scan", "ticketing", 1, now, logger, ct);
            var p13 = await EnsurePermissionAsync(db, "tenant.dashboard.read", "Tenant dashboard read", "tenant", 1, now, logger, ct);
            var p14 = await EnsurePermissionAsync(db, "tenant.bookings.read", "Tenant bookings read", "tenant", 2, now, logger, ct);
            var p15 = await EnsurePermissionAsync(db, "tenant.reviews.read", "Tenant reviews read", "tenant", 3, now, logger, ct);
            var p16 = await EnsurePermissionAsync(db, "tenant.staff.manage", "Tenant staff manage", "tenant", 4, now, logger, ct);
            var p17 = await EnsurePermissionAsync(db, "tenant.finance.read", "Tenant finance read", "tenant", 5, now, logger, ct);
            var p18 = await EnsurePermissionAsync(db, "tenant.reports.read", "Tenant reports read", "tenant", 6, now, logger, ct);
            var p19 = await EnsurePermissionAsync(db, "tenant.settings.read", "Tenant settings read", "tenant", 7, now, logger, ct);
            var p20 = await EnsurePermissionAsync(db, "train.trips.read", "Train trips read", "train", 10, now, logger, ct);
            var p21 = await EnsurePermissionAsync(db, "flight.inventory.read", "Flight inventory read", "flight", 10, now, logger, ct);
            var p22 = await EnsurePermissionAsync(db, "hotel.inventory.read", "Hotel inventory read", "hotel", 10, now, logger, ct);
            var p23 = await EnsurePermissionAsync(db, "tour.inventory.read", "Tour inventory read", "tour", 10, now, logger, ct);

            await db.SaveChangesAsync(ct);

            // Grant Admin role these permissions
            var adminRole = await roleManager.FindByNameAsync(RoleNames.Admin);
            if (adminRole is null)
            {
                logger.LogWarning("Admin role not found; skip RolePermissions seed.");
                return;
            }

            await EnsureRolePermissionAsync(db, adminRole.Id, p1.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p2.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p3.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p4.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p5.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p6.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p7.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p8.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p9.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p10.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p11.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p12.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p13.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p14.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p15.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p16.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p17.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p18.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p19.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p20.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p21.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p22.Id, now, logger, ct);
            await EnsureRolePermissionAsync(db, adminRole.Id, p23.Id, now, logger, ct);

            await db.SaveChangesAsync(ct);

            logger.LogInformation("Permissions seed completed.");
        }

        private static async Task<Permission> EnsurePermissionAsync(
            AppDbContext db,
            string code,
            string name,
            string category,
            int sortOrder,
            DateTimeOffset now,
            ILogger logger,
            CancellationToken ct)
        {
            var existing = await db.Permissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Code == code, ct);

            if (existing is null)
            {
                var p = new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = name,
                    Category = category,
                    SortOrder = sortOrder,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = now
                };

                db.Permissions.Add(p);
                logger.LogInformation("Created permission {Code}.", code);
                return p;
            }

            var changed = false;
            if (existing.Name != name) { existing.Name = name; changed = true; }
            if (existing.Category != category) { existing.Category = category; changed = true; }
            if (existing.SortOrder != sortOrder) { existing.SortOrder = sortOrder; changed = true; }
            if (!existing.IsActive) { existing.IsActive = true; changed = true; }
            if (existing.IsDeleted) { existing.IsDeleted = false; changed = true; }

            if (changed)
            {
                existing.UpdatedAt = now;
                logger.LogInformation("Updated permission {Code}.", code);
            }

            return existing;
        }

        private static async Task EnsureRolePermissionAsync(
            AppDbContext db,
            Guid roleId,
            Guid permissionId,
            DateTimeOffset now,
            ILogger logger,
            CancellationToken ct)
        {
            var exists = await db.RolePermissions.IgnoreQueryFilters()
                .AnyAsync(x => x.RoleId == roleId && x.PermissionId == permissionId && !x.IsDeleted, ct);

            if (exists) return;

            db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                PermissionId = permissionId,
                IsDeleted = false,
                CreatedAt = now
            });

            logger.LogInformation("Granted role {RoleId} permission {PermissionId}.", roleId, permissionId);
        }
    }
}
