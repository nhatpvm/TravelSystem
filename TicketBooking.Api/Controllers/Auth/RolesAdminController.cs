// FILE #112: TicketBooking.Api/Controllers/Admin/RolesAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Auth;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
namespace TicketBooking.Api.Controllers.Admin
{
    /// <summary>
    /// Admin Roles management (Identity roles - dbo.AspNetRoles):
    /// - List roles
    /// - Get role details
    /// - Create role (optional)
    /// - Delete role (optional)
    /// - Assign/Remove role for a user
    ///
    /// Notes:
    /// - For this project, system roles are usually seeded (Admin/QL*/Customer).
    /// - Still useful to have admin endpoints for completeness + testing.
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/admin/roles")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class RolesAdminController : ControllerBase
    {
        private readonly RoleManager<AppRole> _roleManager;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _db;
        public RolesAdminController(RoleManager<AppRole> roleManager, UserManager<AppUser> userManager, AppDbContext db)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _db = db;
        }
        public sealed class CreateRoleRequest
        {
            public string Name { get; set; } = "";
        }
        public sealed class UpdateRoleRequest
        {
            public string Name { get; set; } = "";
        }
        public sealed class SetUserRoleRequest
        {
            /// <summary>UserName or Email</summary>
            public string UserNameOrEmail { get; set; } = "";
        }
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? q, CancellationToken ct = default)
        {
            IQueryable<AppRole> query = _roleManager.Roles.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim().ToUpperInvariant();
                query = query.Where(x => x.Name != null && x.Name.ToUpper().Contains(key));
            }
            var items = await query
                .OrderBy(x => x.Name)
                .ToListAsync(ct);

            var roleIds = items.Select(x => x.Id).ToArray();
            var permissionsByRoleId = await _db.RolePermissions.AsNoTracking()
                .Where(x => roleIds.Contains(x.RoleId) && !x.IsDeleted)
                .GroupBy(x => x.RoleId)
                .Select(x => new { RoleId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.RoleId, x => x.Count, ct);

            var result = new List<object>(items.Count);
            foreach (var x in items)
            {
                var usersCount = string.IsNullOrWhiteSpace(x.Name)
                    ? 0
                    : (await _userManager.GetUsersInRoleAsync(x.Name)).Count;

                result.Add(new
                {
                    x.Id,
                    x.Name,
                    x.NormalizedName,
                    UsersCount = usersCount,
                    PermissionsCount = permissionsByRoleId.TryGetValue(x.Id, out var permissionsCount) ? permissionsCount : 0,
                    IsProtected = IsProtectedRole(x.Name)
                });
            }
            return Ok(new { count = items.Count, items = result });
        }
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct = default)
        {
            var role = await _roleManager.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (role is null)
                return NotFound(new { message = "Role not found." });

            var usersCount = string.IsNullOrWhiteSpace(role.Name)
                ? 0
                : (await _userManager.GetUsersInRoleAsync(role.Name)).Count;

            var permissionsCount = await _db.RolePermissions.AsNoTracking()
                .CountAsync(x => x.RoleId == role.Id && !x.IsDeleted, ct);

            return Ok(new
            {
                role.Id,
                role.Name,
                role.NormalizedName,
                UsersCount = usersCount,
                PermissionsCount = permissionsCount,
                IsProtected = IsProtectedRole(role.Name)
            });
        }
        /// <summary>
        /// Rename a custom role.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateRoleRequest req, CancellationToken ct = default)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role is null)
                return NotFound(new { message = "Role not found." });

            var name = (req.Name ?? "").Trim();
            if (name.Length == 0)
                return BadRequest(new { message = "Role name is required." });

            if (IsProtectedRole(role.Name) &&
                !string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Cannot rename protected system role." });
            }

            var existing = await _roleManager.FindByNameAsync(name);
            if (existing is not null && existing.Id != role.Id)
                return Conflict(new { message = "Role already exists." });

            role.Name = name;
            role.NormalizedName = name.ToUpperInvariant();

            var res = await _roleManager.UpdateAsync(role);
            if (!res.Succeeded)
                return BadRequest(new { message = "Update role failed.", errors = res.Errors.Select(e => e.Description).ToArray() });

            return Ok(new { ok = true, role.Id, role.Name });
        }
        /// <summary>
        /// Create a new role (optional). You can keep roles seeded only.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoleRequest req, CancellationToken ct = default)
        {
            var name = (req.Name ?? "").Trim();
            if (name.Length == 0)
                return BadRequest(new { message = "Role name is required." });
            if (await _roleManager.RoleExistsAsync(name))
                return Conflict(new { message = "Role already exists." });
            var role = new AppRole
            {
                Id = Guid.NewGuid(),
                Name = name,
                NormalizedName = name.ToUpperInvariant()
            };
            var res = await _roleManager.CreateAsync(role);
            if (!res.Succeeded)
                return BadRequest(new { message = "Create role failed.", errors = res.Errors.Select(e => e.Description).ToArray() });
            return CreatedAtAction(nameof(Get), new { version = "1.0", id = role.Id }, new { role.Id, role.Name });
        }
        /// <summary>
        /// Delete a role (optional). Usually avoid deleting seeded roles.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct = default)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role is null)
                return NotFound(new { message = "Role not found." });
            // Protect critical roles
            var protectedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                RoleNames.Admin,
                RoleNames.Customer,
                RoleNames.QLNX,
                RoleNames.QLVT,
                RoleNames.QLVMM,
                RoleNames.QLKS,
                RoleNames.QLTour
            };
            if (!string.IsNullOrWhiteSpace(role.Name) && protectedRoles.Contains(role.Name))
                return BadRequest(new { message = "Cannot delete protected system role." });

            if (!string.IsNullOrWhiteSpace(role.Name))
            {
                var assignedUsers = await _userManager.GetUsersInRoleAsync(role.Name);
                if (assignedUsers.Count > 0)
                    return BadRequest(new { message = "Cannot delete a role that is still assigned to users." });
            }

            await _db.RolePermissions
                .IgnoreQueryFilters()
                .Where(x => x.RoleId == role.Id)
                .ExecuteDeleteAsync(ct);

            var res = await _roleManager.DeleteAsync(role);
            if (!res.Succeeded)
                return BadRequest(new { message = "Delete role failed.", errors = res.Errors.Select(e => e.Description).ToArray() });
            return Ok(new { ok = true });
        }
        /// <summary>
        /// Add a user to a role.
        /// </summary>
        [HttpPost("{roleName}/users")]
        public async Task<IActionResult> AddUserToRole([FromRoute] string roleName, [FromBody] SetUserRoleRequest req, CancellationToken ct = default)
        {
            roleName = (roleName ?? "").Trim();
            if (roleName.Length == 0)
                return BadRequest(new { message = "RoleName is required." });
            if (!await _roleManager.RoleExistsAsync(roleName))
                return NotFound(new { message = "Role not found." });
            var key = (req.UserNameOrEmail ?? "").Trim();
            if (key.Length == 0)
                return BadRequest(new { message = "UserNameOrEmail is required." });
            var user = await FindUserAsync(key, ct);
            if (user is null)
                return NotFound(new { message = "User not found." });
            var already = await _userManager.IsInRoleAsync(user, roleName);
            if (already)
                return Ok(new { ok = true, changed = false });
            var res = await _userManager.AddToRoleAsync(user, roleName);
            if (!res.Succeeded)
                return BadRequest(new { message = "Add role failed.", errors = res.Errors.Select(e => e.Description).ToArray() });
            user.UpdatedAt = DateTimeOffset.Now;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            return Ok(new { ok = true, changed = true });
        }
        /// <summary>
        /// Remove a user from a role.
        /// </summary>
        [HttpDelete("{roleName}/users")]
        public async Task<IActionResult> RemoveUserFromRole([FromRoute] string roleName, [FromBody] SetUserRoleRequest req, CancellationToken ct = default)
        {
            roleName = (roleName ?? "").Trim();
            if (roleName.Length == 0)
                return BadRequest(new { message = "RoleName is required." });
            if (!await _roleManager.RoleExistsAsync(roleName))
                return NotFound(new { message = "Role not found." });
            var key = (req.UserNameOrEmail ?? "").Trim();
            if (key.Length == 0)
                return BadRequest(new { message = "UserNameOrEmail is required." });
            var user = await FindUserAsync(key, ct);
            if (user is null)
                return NotFound(new { message = "User not found." });
            var inRole = await _userManager.IsInRoleAsync(user, roleName);
            if (!inRole)
                return Ok(new { ok = true, changed = false });
            // Prevent removing last Admin
            if (string.Equals(roleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase))
            {
                var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
                if (admins.Count <= 1 && admins.Any(x => x.Id == user.Id))
                    return BadRequest(new { message = "Cannot remove the last Admin user." });
            }
            var res = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!res.Succeeded)
                return BadRequest(new { message = "Remove role failed.", errors = res.Errors.Select(e => e.Description).ToArray() });
            user.UpdatedAt = DateTimeOffset.Now;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            return Ok(new { ok = true, changed = true });
        }
        private async Task<AppUser?> FindUserAsync(string userNameOrEmail, CancellationToken ct)
        {
            var key = userNameOrEmail.Trim();
            if (key.Contains('@'))
            {
                var email = key.ToLowerInvariant();
                var byEmail = await _userManager.FindByEmailAsync(email);
                if (byEmail is not null) return byEmail;
            }
            var byName = await _userManager.FindByNameAsync(key);
            if (byName is not null) return byName;
            // fallback by email raw
            var byEmail2 = await _userManager.FindByEmailAsync(key);
            return byEmail2;
        }
        private static bool IsProtectedRole(string? roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return false;

            return roleName.Equals(RoleNames.Admin, StringComparison.OrdinalIgnoreCase) ||
                   roleName.Equals(RoleNames.Customer, StringComparison.OrdinalIgnoreCase) ||
                   roleName.Equals(RoleNames.QLNX, StringComparison.OrdinalIgnoreCase) ||
                   roleName.Equals(RoleNames.QLVT, StringComparison.OrdinalIgnoreCase) ||
                   roleName.Equals(RoleNames.QLVMM, StringComparison.OrdinalIgnoreCase) ||
                   roleName.Equals(RoleNames.QLKS, StringComparison.OrdinalIgnoreCase) ||
                   roleName.Equals(RoleNames.QLTour, StringComparison.OrdinalIgnoreCase);
        }
    }
}


