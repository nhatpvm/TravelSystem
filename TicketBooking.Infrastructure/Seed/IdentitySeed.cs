// FILE #006: TicketBooking.Infrastructure/Seed/IdentitySeed.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TicketBooking.Infrastructure.Identity;

namespace TicketBooking.Infrastructure.Seed
{
    /// <summary>
    /// Phase 2 seed:
    /// - Ensure roles exist (PascalCase first letter).
    /// - Ensure demo admin + demo users exist.
    /// Login supports username OR email (handled in AuthController later).
    /// </summary>
    public static class IdentitySeed
    {
        public static async Task SeedAsync(
            RoleManager<AppRole> roleManager,
            UserManager<AppUser> userManager,
            ILogger logger,
            CancellationToken ct = default)
        {
            // 1) Roles
            foreach (var roleName in RoleNames.All)
            {
                if (await roleManager.RoleExistsAsync(roleName))
                    continue;

                var role = new AppRole(roleName);
                var created = await roleManager.CreateAsync(role);
                if (!created.Succeeded)
                {
                    var err = string.Join("; ", created.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create role '{roleName}': {err}");
                }
            }

            // 2) Admin user (demo)
            const string adminUserName = "admin";
            const string adminEmail = "admin@ticketbooking.local";
            const string adminPassword = "Admin@12345"; // demo password (change later)

            var admin = await userManager.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == adminUserName.ToUpper(), ct);
            if (admin is null)
            {
                admin = new AppUser
                {
                    Id = Guid.NewGuid(),
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = false,
                    IsActive = true,
                    FullName = "System Admin"
                };

                var created = await userManager.CreateAsync(admin, adminPassword);
                if (!created.Succeeded)
                {
                    var err = string.Join("; ", created.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create admin user: {err}");
                }
            }

            // Ensure Admin role
            if (!await userManager.IsInRoleAsync(admin, RoleNames.Admin))
            {
                var added = await userManager.AddToRoleAsync(admin, RoleNames.Admin);
                if (!added.Succeeded)
                {
                    var err = string.Join("; ", added.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to add admin role: {err}");
                }
            }

            // 3) Demo tenant managers (optional for Phase 2; helpful for later)
            await EnsureUserWithRoleAsync(userManager, "qlnx", "qlnx@ticketbooking.local", "QlNx@12345", "Quản lý nhà xe", RoleNames.QLNX, logger, ct);
            await EnsureUserWithRoleAsync(userManager, "qlks", "qlks@ticketbooking.local", "QlKs@12345", "Quản lý khách sạn", RoleNames.QLKS, logger, ct);
            await EnsureUserWithRoleAsync(userManager, "qlvt", "qlvt@ticketbooking.local", "QlVt@12345", "Quản lý vé tàu", RoleNames.QLVT, logger, ct);
            await EnsureUserWithRoleAsync(userManager, "qlvmm", "qlvmm@ticketbooking.local", "QlVmm@12345", "Quản lý vé máy bay", RoleNames.QLVMM, logger, ct);
            await EnsureUserWithRoleAsync(userManager, "qltour", "qltour@ticketbooking.local", "QlTour@12345", "Quản lý tour", RoleNames.QLTour, logger, ct);
            await EnsureUserWithRoleAsync(userManager, "scanner_nx", "scanner_nx@ticketbooking.local", "ScannerNx@12345", "NX Ticket Scanner", RoleNames.Customer, logger, ct);
            await EnsureUserWithRoleAsync(userManager, "customer", "customer@ticketbooking.local", "Customer@12345", "Nguyễn Minh Anh", RoleNames.Customer, logger, ct);

            logger.LogInformation("Identity seed completed.");
        }

        private static async Task EnsureUserWithRoleAsync(
            UserManager<AppUser> userManager,
            string userName,
            string email,
            string password,
            string fullName,
            string roleName,
            ILogger logger,
            CancellationToken ct)
        {
            var normalizedUserName = userName.ToUpperInvariant();
            var user = await userManager.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, ct);

            if (user is null)
            {
                user = new AppUser
                {
                    Id = Guid.NewGuid(),
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true,
                    IsActive = true,
                    FullName = fullName
                };

                var created = await userManager.CreateAsync(user, password);
                if (!created.Succeeded)
                {
                    var err = string.Join("; ", created.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create user '{userName}': {err}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, roleName))
            {
                var added = await userManager.AddToRoleAsync(user, roleName);
                if (!added.Succeeded)
                {
                    var err = string.Join("; ", added.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to add role '{roleName}' to '{userName}': {err}");
                }
            }

            logger.LogInformation("Ensured user '{UserName}' with role '{RoleName}'.", userName, roleName);
        }
    }
}
