// FILE #111: TicketBooking.Api/Controllers/Admin/UsersAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TicketBooking.Infrastructure.Auth;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
namespace TicketBooking.Api.Controllers.Admin
{
    /// <summary>
    /// Admin Users management (production-ready for graduation project):
    /// - List/search users (paging)
    /// - Get details (roles)
    /// - Create user (optional) + assign roles
    /// - Set roles (replace)
    /// - Activate/Deactivate (IsActive)
    /// - Lock/Unlock (Identity Lockout)
    /// - Reset password (admin)
    ///
    /// Notes:
    /// - This is global (dbo.AspNetUsers) and NOT tenant-owned.
    /// - Soft delete is not applied to users; use IsActive + Lockout.
    /// - Requires Role: Admin
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/admin/users")]
    [Authorize(Roles = RoleNames.Admin)]
    public sealed class UsersAdminController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly AppDbContext _db;
        private readonly IPermissionService _permissionService;
        public UsersAdminController(
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            AppDbContext db,
            IPermissionService permissionService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
            _permissionService = permissionService;
        }
        // ---------------------------
        // DTOs
        // ---------------------------
        public sealed class CreateUserRequest
        {
            public string UserName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
            public string FullName { get; set; } = "";
            public string? PhoneNumber { get; set; }
            public string? AvatarUrl { get; set; }
            public bool EmailConfirmed { get; set; } = true;
            public bool IsActive { get; set; } = true;
            public string[] Roles { get; set; } = Array.Empty<string>();
        }
        public sealed class UpdateUserRequest
        {
            public string UserName { get; set; } = "";
            public string Email { get; set; } = "";
            public string FullName { get; set; } = "";
            public string? PhoneNumber { get; set; }
            public string? AvatarUrl { get; set; }
            public bool EmailConfirmed { get; set; } = true;
            public bool IsActive { get; set; } = true;
        }
        public sealed class ResetPasswordRequest
        {
            public string NewPassword { get; set; } = "";
        }
        public sealed class SetRolesRequest
        {
            public string[] Roles { get; set; } = Array.Empty<string>();
        }
        public sealed class LockRequest
        {
            /// <summary>Minutes to lock. If null -> 15 minutes.</summary>
            public int? Minutes { get; set; }
        }
        // ---------------------------
        // Endpoints
        // ---------------------------
        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? q,
            [FromQuery] bool includeInactive = true,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : (pageSize > 200 ? 200 : pageSize);
            IQueryable<AppUser> query = _userManager.Users.AsNoTracking();
            if (!includeInactive)
                query = query.Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim();
                var upper = key.ToUpperInvariant();
                query = query.Where(x =>
                    (x.UserName != null && x.UserName.ToUpper().Contains(upper)) ||
                    (x.Email != null && x.Email.ToUpper().Contains(upper)) ||
                    (x.FullName != null && x.FullName.ToUpper().Contains(upper)));
            }
            var total = await query.CountAsync(ct);
            var rows = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var items = new List<object>(rows.Count);
            foreach (var x in rows)
            {
                var roles = await _userManager.GetRolesAsync(x);
                items.Add(new
                {
                    x.Id,
                    x.UserName,
                    x.Email,
                    x.FullName,
                    x.PhoneNumber,
                    x.AvatarUrl,
                    x.IsActive,
                    x.EmailConfirmed,
                    x.LockoutEnabled,
                    x.LockoutEnd,
                    x.AccessFailedCount,
                    Roles = roles,
                    x.CreatedAt,
                    x.UpdatedAt
                });
            }

            return Ok(new { page, pageSize, total, items });
        }
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> Get([FromRoute] Guid id, CancellationToken ct = default)
        {
            var user = await _userManager.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (user is null)
                return NotFound(new { message = "User not found." });
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                user.AvatarUrl,
                user.IsActive,
                user.EmailConfirmed,
                user.LockoutEnabled,
                user.LockoutEnd,
                user.AccessFailedCount,
                user.CreatedAt,
                user.UpdatedAt,
                roles
            });
        }
        /// <summary>
        /// Create a user (admin tool). For Customer self-signup use /auth/register.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest req, CancellationToken ct = default)
        {
            var userName = (req.UserName ?? "").Trim();
            var email = NormalizeEmail(req.Email);
            var password = req.Password ?? "";
            var fullName = ToTitleCase(req.FullName);
            var phoneNumber = NullIfWhiteSpace(req.PhoneNumber);
            var avatarUrl = NullIfWhiteSpace(req.AvatarUrl);
            if (!IsValidUserName(userName))
                return BadRequest(new { message = "UserName is invalid. Use 3-32 chars: a-z, 0-9, '.', '_', '-'." });
            if (email is null)
                return BadRequest(new { message = "Email is invalid." });
            if (string.IsNullOrWhiteSpace(password))
                return BadRequest(new { message = "Password is required." });
            if (phoneNumber is not null && phoneNumber.Length > 30)
                return BadRequest(new { message = "PhoneNumber max length is 30." });
            if (avatarUrl is not null && avatarUrl.Length > 500)
                return BadRequest(new { message = "AvatarUrl max length is 500." });
            if (await _userManager.FindByNameAsync(userName) is not null)
                return Conflict(new { message = "UserName already exists." });
            if (await _userManager.FindByEmailAsync(email) is not null)
                return Conflict(new { message = "Email already exists." });
            var now = DateTimeOffset.Now;
            var user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = req.EmailConfirmed,
                FullName = string.IsNullOrWhiteSpace(fullName) ? ToTitleCase(userName) : fullName,
                PhoneNumber = phoneNumber,
                AvatarUrl = avatarUrl,
                IsActive = req.IsActive,
                CreatedAt = now
            };
            var create = await _userManager.CreateAsync(user, password);
            if (!create.Succeeded)
                return BadRequest(new { message = "Create user failed.", errors = create.Errors.Select(e => e.Description).ToArray() });
            var roles = NormalizeRoles(req.Roles);
            // Validate roles exist
            foreach (var r in roles)
            {
                if (!await _roleManager.RoleExistsAsync(r))
                    return BadRequest(new { message = $"Role '{r}' does not exist." });
            }
            if (roles.Length > 0)
            {
                var addRoles = await _userManager.AddToRolesAsync(user, roles);
                if (!addRoles.Succeeded)
                    return BadRequest(new { message = "Assign roles failed.", errors = addRoles.Errors.Select(e => e.Description).ToArray() });
            }
            return CreatedAtAction(nameof(Get), new { version = "1.0", id = user.Id }, new { user.Id });
        }
        /// <summary>
        /// Update profile fields of a user.
        /// </summary>
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateUserRequest req, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return NotFound(new { message = "User not found." });

            var userName = (req.UserName ?? "").Trim();
            var email = NormalizeEmail(req.Email);
            var fullName = ToTitleCase(req.FullName);
            var phoneNumber = NullIfWhiteSpace(req.PhoneNumber);
            var avatarUrl = NullIfWhiteSpace(req.AvatarUrl);

            if (!IsValidUserName(userName))
                return BadRequest(new { message = "UserName is invalid. Use 3-32 chars: a-z, 0-9, '.', '_', '-'." });
            if (email is null)
                return BadRequest(new { message = "Email is invalid." });
            if (string.IsNullOrWhiteSpace(fullName))
                return BadRequest(new { message = "FullName is required." });
            if (fullName.Length > 200)
                return BadRequest(new { message = "FullName max length is 200." });
            if (phoneNumber is not null && phoneNumber.Length > 30)
                return BadRequest(new { message = "PhoneNumber max length is 30." });
            if (avatarUrl is not null && avatarUrl.Length > 500)
                return BadRequest(new { message = "AvatarUrl max length is 500." });

            var userNameOwner = await _userManager.FindByNameAsync(userName);
            if (userNameOwner is not null && userNameOwner.Id != user.Id)
                return Conflict(new { message = "UserName already exists." });

            var emailOwner = await _userManager.FindByEmailAsync(email);
            if (emailOwner is not null && emailOwner.Id != user.Id)
                return Conflict(new { message = "Email already exists." });

            var shouldRevokeSessions = false;

            if (!string.Equals(user.UserName, userName, StringComparison.OrdinalIgnoreCase))
            {
                user.UserName = userName;
                user.NormalizedUserName = userName.ToUpperInvariant();
            }

            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                user.Email = email;
                user.NormalizedEmail = email.ToUpperInvariant();
                shouldRevokeSessions = true;
            }

            if (user.IsActive != req.IsActive)
            {
                user.IsActive = req.IsActive;
                shouldRevokeSessions = true;
            }

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.AvatarUrl = avatarUrl;
            user.EmailConfirmed = req.EmailConfirmed;
            user.UpdatedAt = DateTimeOffset.Now;

            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
                return BadRequest(new { message = "Update user failed.", errors = update.Errors.Select(e => e.Description).ToArray() });

            if (shouldRevokeSessions)
                await _userManager.UpdateSecurityStampAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new
            {
                user.Id,
                user.UserName,
                user.Email,
                user.FullName,
                user.PhoneNumber,
                user.AvatarUrl,
                user.IsActive,
                user.EmailConfirmed,
                Roles = roles,
                user.CreatedAt,
                user.UpdatedAt
            });
        }
        /// <summary>
        /// Replace roles of a user (set exact roles).
        /// </summary>
        [HttpPut("{id:guid}/roles")]
        public async Task<IActionResult> SetRoles([FromRoute] Guid id, [FromBody] SetRolesRequest req, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return NotFound(new { message = "User not found." });
            var targetRoles = NormalizeRoles(req.Roles);
            foreach (var r in targetRoles)
            {
                if (!await _roleManager.RoleExistsAsync(r))
                    return BadRequest(new { message = $"Role '{r}' does not exist." });
            }
            var current = await _userManager.GetRolesAsync(user);
            if (current.Contains(RoleNames.Admin, StringComparer.OrdinalIgnoreCase) &&
                !targetRoles.Contains(RoleNames.Admin, StringComparer.OrdinalIgnoreCase) &&
                await IsLastAdminAsync(user.Id))
            {
                return BadRequest(new { message = "Cannot remove the last Admin user." });
            }
            // Remove roles not in target
            var toRemove = current.Where(x => !targetRoles.Contains(x, StringComparer.OrdinalIgnoreCase)).ToArray();
            if (toRemove.Length > 0)
            {
                var rm = await _userManager.RemoveFromRolesAsync(user, toRemove);
                if (!rm.Succeeded)
                    return BadRequest(new { message = "Remove roles failed.", errors = rm.Errors.Select(e => e.Description).ToArray() });
            }
            // Add roles missing
            var toAdd = targetRoles.Where(x => !current.Contains(x, StringComparer.OrdinalIgnoreCase)).ToArray();
            if (toAdd.Length > 0)
            {
                var add = await _userManager.AddToRolesAsync(user, toAdd);
                if (!add.Succeeded)
                    return BadRequest(new { message = "Add roles failed.", errors = add.Errors.Select(e => e.Description).ToArray() });
            }
            user.UpdatedAt = DateTimeOffset.Now;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            var finalRoles = await _userManager.GetRolesAsync(user);
            return Ok(new { ok = true, roles = finalRoles });
        }
        /// <summary>
        /// Activate or deactivate a user (IsActive).
        /// </summary>
        [HttpPut("{id:guid}/active")]
        public async Task<IActionResult> SetActive([FromRoute] Guid id, [FromQuery] bool isActive, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return NotFound(new { message = "User not found." });
            if (!isActive && await IsCurrentUserAsync(user.Id))
                return BadRequest(new { message = "You cannot deactivate your own account." });
            if (!isActive &&
                await _userManager.IsInRoleAsync(user, RoleNames.Admin) &&
                await IsLastAdminAsync(user.Id))
            {
                return BadRequest(new { message = "Cannot deactivate the last Admin user." });
            }
            if (user.IsActive == isActive)
                return Ok(new { ok = true, changed = false });
            user.IsActive = isActive;
            user.UpdatedAt = DateTimeOffset.Now;
            var upd = await _userManager.UpdateAsync(user);
            if (!upd.Succeeded)
                return BadRequest(new { message = "Update failed.", errors = upd.Errors.Select(e => e.Description).ToArray() });
            // Invalidate tokens/sessions (when you have refresh tokens later)
            await _userManager.UpdateSecurityStampAsync(user);
            return Ok(new { ok = true, changed = true, user.IsActive });
        }
        /// <summary>
        /// Explain effective permissions for a user within an optional tenant scope.
        /// </summary>
        [HttpGet("{id:guid}/effective-permissions")]
        public async Task<IActionResult> EffectivePermissions(
            [FromRoute] Guid id,
            [FromQuery] Guid? tenantId,
            [FromQuery] bool grantedOnly = false,
            [FromQuery] string? q = null,
            CancellationToken ct = default)
        {
            var user = await _userManager.Users.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new { x.Id, x.UserName, x.Email, x.FullName, x.IsActive })
                .FirstOrDefaultAsync(ct);

            if (user is null)
                return NotFound(new { message = "User not found." });

            var tenant = tenantId.HasValue && tenantId.Value != Guid.Empty
                ? await _db.Tenants.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => x.Id == tenantId.Value && !x.IsDeleted)
                    .Select(x => new { x.Id, x.Code, x.Name })
                    .FirstOrDefaultAsync(ct)
                : null;

            if (tenantId.HasValue && tenantId.Value != Guid.Empty && tenant is null)
                return BadRequest(new { message = "TenantId not found." });

            var items = await _permissionService.GetEffectivePermissionsAsync(id, tenantId, ct);
            var filtered = items
                .Where(x => !grantedOnly || x.IsGranted)
                .Where(x => string.IsNullOrWhiteSpace(q)
                    || x.Code.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || x.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || (x.Category?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(x => new
                {
                    x.PermissionId,
                    x.Code,
                    x.Name,
                    x.Category,
                    x.IsGranted,
                    x.Resolution,
                    x.Sources
                })
                .OrderBy(x => x.Category)
                .ThenBy(x => x.Code)
                .ToList();

            return Ok(new
            {
                user,
                tenant,
                total = filtered.Count,
                items = filtered
            });
        }
        /// <summary>
        /// Lock a user for N minutes (default 15).
        /// </summary>
        [HttpPost("{id:guid}/lock")]
        public async Task<IActionResult> Lock([FromRoute] Guid id, [FromBody] LockRequest req, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return NotFound(new { message = "User not found." });
            if (await IsCurrentUserAsync(user.Id))
                return BadRequest(new { message = "You cannot lock your own account." });
            if (await _userManager.IsInRoleAsync(user, RoleNames.Admin) &&
                await IsLastAdminAsync(user.Id))
            {
                return BadRequest(new { message = "Cannot lock the last Admin user." });
            }
            if (!user.LockoutEnabled)
            {
                user.LockoutEnabled = true;
                await _userManager.UpdateAsync(user);
            }
            var minutes = req.Minutes.GetValueOrDefault(15);
            if (minutes < 1) minutes = 1;
            if (minutes > 43200) minutes = 43200; // 30 days cap
            var lockoutEnd = DateTimeOffset.Now.AddMinutes(minutes);
            var res = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);
            if (!res.Succeeded)
                return BadRequest(new { message = "Lock failed.", errors = res.Errors.Select(e => e.Description).ToArray() });
            user.UpdatedAt = DateTimeOffset.Now;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            return Ok(new { ok = true, lockoutEnd });
        }
        /// <summary>
        /// Unlock a user immediately.
        /// </summary>
        [HttpPost("{id:guid}/unlock")]
        public async Task<IActionResult> Unlock([FromRoute] Guid id, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return NotFound(new { message = "User not found." });
            var res = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!res.Succeeded)
                return BadRequest(new { message = "Unlock failed.", errors = res.Errors.Select(e => e.Description).ToArray() });
            await _userManager.ResetAccessFailedCountAsync(user);
            user.UpdatedAt = DateTimeOffset.Now;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            return Ok(new { ok = true });
        }
        /// <summary>
        /// Admin reset password (no need current password).
        /// </summary>
        [HttpPost("{id:guid}/reset-password")]
        public async Task<IActionResult> ResetPassword([FromRoute] Guid id, [FromBody] ResetPasswordRequest req, CancellationToken ct = default)
        {
            var newPassword = req.NewPassword ?? "";
            if (string.IsNullOrWhiteSpace(newPassword))
                return BadRequest(new { message = "NewPassword is required." });
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user is null)
                return NotFound(new { message = "User not found." });
            // Generate reset token and reset
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var res = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!res.Succeeded)
                return BadRequest(new { message = "Reset password failed.", errors = res.Errors.Select(e => e.Description).ToArray() });
            user.UpdatedAt = DateTimeOffset.Now;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
            return Ok(new { ok = true });
        }
        // ---------------------------
        // Helpers
        // ---------------------------
        private static bool IsValidUserName(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName)) return false;
            if (userName.Length < 3 || userName.Length > 32) return false;
            return Regex.IsMatch(userName, "^[a-zA-Z0-9._-]+$");
        }
        private static string? NormalizeEmail(string? email)
        {
            email = (email ?? "").Trim();
            if (email.Length < 5) return null;
            if (!email.Contains('@')) return null;
            return email.ToLowerInvariant();
        }
        private static string[] NormalizeRoles(string[]? roles)
        {
            roles ??= Array.Empty<string>();
            return roles
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        private static string ToTitleCase(string? input)
        {
            input = (input ?? "").Trim();
            if (input.Length == 0) return "";
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                if (p.Length == 0) continue;
                var first = char.ToUpperInvariant(p[0]);
                var rest = p.Length > 1 ? p.Substring(1).ToLowerInvariant() : "";
                parts[i] = first + rest;
            }
            return string.Join(' ', parts);
        }
        private static string? NullIfWhiteSpace(string? input)
            => string.IsNullOrWhiteSpace(input) ? null : input.Trim();

        private async Task<bool> IsLastAdminAsync(Guid targetUserId)
        {
            var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
            return admins.Count(x => x.IsActive) <= 1 && admins.Any(x => x.Id == targetUserId && x.IsActive);
        }

        private Task<bool> IsCurrentUserAsync(Guid userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Task.FromResult(Guid.TryParse(currentUserId, out var parsed) && parsed == userId);
        }
    }
}

