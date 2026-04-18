// FILE #110 (UPDATE - Production): TicketBooking.Api/Controllers/Auth/AuthAccountController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TicketBooking.Api.Auth;
using TicketBooking.Infrastructure.Identity;

namespace TicketBooking.Api.Controllers.Auth
{
    /// <summary>
    /// Production-ready account endpoints:
    /// - Register (Customer)
    /// - Forgot password (anti-enumeration + rate limit + logs token in DEV only)
    /// - Reset password (security stamp update)
    /// - Change password (security stamp update)
    ///
    /// Notes:
    /// - Keeps your existing AuthController (login/me). This file is only account flows.
    /// - For graduation demo: DEV mode returns token in response to speed up testing.
    /// - In production: token is never returned; integrate Notify/Email later.
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/auth")]
    public sealed class AuthAccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<AuthAccountController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private readonly IAuthTokenService _authTokenService;

        // Rate limit settings (simple, in-memory). Good enough for graduation + local dev.
        private const int ForgotLimitCount = 5;
        private static readonly TimeSpan ForgotWindow = TimeSpan.FromMinutes(10);

        private const int ResetLimitCount = 5;
        private static readonly TimeSpan ResetWindow = TimeSpan.FromMinutes(10);

        public AuthAccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            ILogger<AuthAccountController> logger,
            IWebHostEnvironment env,
            IMemoryCache cache,
            IAuthTokenService authTokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _env = env;
            _cache = cache;
            _authTokenService = authTokenService;
        }

        // ---------------------------
        // DTOs
        // ---------------------------

        public sealed class RegisterRequest
        {
            public string? UserName { get; set; }
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
            public string FullName { get; set; } = "";
            public string? PhoneNumber { get; set; }
        }

        public sealed class RegisterResponse
        {
            public Guid UserId { get; set; }
            public string UserName { get; set; } = "";
            public string Email { get; set; } = "";
            public string? PhoneNumber { get; set; }
        }

        public sealed class ForgotPasswordRequest
        {
            /// <summary>Allow input email OR username.</summary>
            public string EmailOrUserName { get; set; } = "";
        }

        public sealed class ResetPasswordRequest
        {
            public string EmailOrUserName { get; set; } = "";
            public string Token { get; set; } = "";
            public string NewPassword { get; set; } = "";
        }

        public sealed class ChangePasswordRequest
        {
            public string CurrentPassword { get; set; } = "";
            public string NewPassword { get; set; } = "";
        }

        public sealed class DeactivateAccountRequest
        {
            public string? Reason { get; set; }
        }

        // ---------------------------
        // Endpoints
        // ---------------------------

        /// <summary>
        /// Register a new Customer account.
        /// Production-ready:
        /// - Username regex validation
        /// - Email normalization
        /// - Prevent trivial password == username/email
        /// - Returns minimal info
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req, CancellationToken ct = default)
        {
            var rawEmail = (req.Email ?? "").Trim();
            var password = req.Password ?? "";
            var rawFullName = (req.FullName ?? "").Trim();
            var phoneNumber = NullIfWhiteSpace(req.PhoneNumber);

            var email = NormalizeEmail(rawEmail);
            if (email is null)
                return BadRequest(new { message = "Email is invalid." });

            if (string.IsNullOrWhiteSpace(password))
                return BadRequest(new { message = "Password is required." });

            var fallbackName = email.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Traveler";
            var fullName = rawFullName.Length == 0 ? ToTitleCase(fallbackName) : ToTitleCase(rawFullName);
            if (fullName.Length > 200)
                return BadRequest(new { message = "FullName max length is 200." });

            if (phoneNumber is not null && phoneNumber.Length > 30)
                return BadRequest(new { message = "PhoneNumber max length is 30." });

            var userName = await ResolveAvailableUserNameAsync(req.UserName, fullName, email, ct);
            if (!IsValidUserName(userName))
                return BadRequest(new { message = "UserName is invalid. Use 3-32 chars: a-z, 0-9, '.', '_', '-'." });

            if (IsTrivialPassword(password, userName, email))
                return BadRequest(new { message = "Password is too weak (cannot equal username/email)." });

            // Ensure unique username/email
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
                EmailConfirmed = true, // For this project: no SMTP in Phase 1-10
                FullName = fullName,
                PhoneNumber = phoneNumber,
                IsActive = true,
                CreatedAt = now
            };

            var create = await _userManager.CreateAsync(user, password);
            if (!create.Succeeded)
                return BadRequest(new { message = "Register failed.", errors = create.Errors.Select(e => e.Description).ToArray() });

            var addRole = await _userManager.AddToRoleAsync(user, RoleNames.Customer);
            if (!addRole.Succeeded)
                return BadRequest(new { message = "Register succeeded but assigning role failed.", errors = addRole.Errors.Select(e => e.Description).ToArray() });

            _logger.LogInformation("Registered new customer: {UserName} ({Email})", user.UserName, user.Email);

            return CreatedAtAction(nameof(Register), new RegisterResponse
            {
                UserId = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber
            });
        }

        /// <summary>
        /// Forgot password:
        /// - Anti user-enumeration (always 200)
        /// - Rate limited per IP+key
        /// - Generates token if user exists + active
        /// - DEV: returns token to speed up testing; PROD: never returns token
        /// </summary>
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req, CancellationToken ct = default)
        {
            var key = (req.EmailOrUserName ?? "").Trim();
            var ip = GetClientIp();

            // Rate limit even if key is empty to prevent abuse
            if (!TryConsumeRateLimit($"fp:{ip}:{key}", ForgotLimitCount, ForgotWindow, out var retryAfterSeconds))
            {
                // Still do not reveal existence; but return 429 for caller to slow down
                Response.Headers["Retry-After"] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Too many requests. Please try again later." });
            }

            if (key.Length == 0)
                return Ok(new { ok = true });

            var user = await FindByEmailOrUserNameAsync(key, ct);
            if (user is null)
                return Ok(new { ok = true });

            if (!user.IsActive)
                return Ok(new { ok = true });

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Log token (useful for DEV demo). Token must be URL-encoded when used in a link.
            _logger.LogWarning("PASSWORD RESET TOKEN generated for {UserName}/{Email}. (DEV should read token from response/logs)",
                user.UserName, user.Email);

            if (_env.IsDevelopment())
            {
                _logger.LogWarning("PASSWORD RESET TOKEN (DEV): {Token}", token);
                return Ok(new
                {
                    ok = true,
                    message = "Reset token generated (DEV).",
                    devToken = token
                });
            }

            // Production: integrate notify/email later
            return Ok(new { ok = true, message = "If the account exists, a reset instruction will be sent." });
        }

        /// <summary>
        /// Reset password using token from forgot-password.
        /// - Rate limited per IP+key
        /// - Updates security stamp to invalidate existing sign-ins (important for refresh tokens/sessions later)
        /// </summary>
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req, CancellationToken ct = default)
        {
            var key = (req.EmailOrUserName ?? "").Trim();
            var token = req.Token ?? "";
            var newPassword = req.NewPassword ?? "";
            var ip = GetClientIp();

            if (!TryConsumeRateLimit($"rp:{ip}:{key}", ResetLimitCount, ResetWindow, out var retryAfterSeconds))
            {
                Response.Headers["Retry-After"] = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);
                return StatusCode(StatusCodes.Status429TooManyRequests, new { message = "Too many requests. Please try again later." });
            }

            if (key.Length == 0)
                return BadRequest(new { message = "EmailOrUserName is required." });

            if (string.IsNullOrWhiteSpace(token))
                return BadRequest(new { message = "Token is required." });

            if (string.IsNullOrWhiteSpace(newPassword))
                return BadRequest(new { message = "NewPassword is required." });

            var user = await FindByEmailOrUserNameAsync(key, ct);
            if (user is null)
                return BadRequest(new { message = "Invalid token or user." });

            if (!user.IsActive)
                return BadRequest(new { message = "User is inactive." });

            if (IsTrivialPassword(newPassword, user.UserName ?? "", user.Email ?? ""))
                return BadRequest(new { message = "NewPassword is too weak (cannot equal username/email)." });

            var reset = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!reset.Succeeded)
                return BadRequest(new { message = "Reset password failed.", errors = reset.Errors.Select(e => e.Description).ToArray() });

            // Invalidate other sign-ins (especially important once you add refresh tokens/sessions)
            await _userManager.UpdateSecurityStampAsync(user);
            await _authTokenService.RevokeAllRefreshTokensAsync(user.Id, ct);

            _logger.LogInformation("Password reset succeeded for {UserName} ({Email})", user.UserName, user.Email);

            return Ok(new { ok = true });
        }

        /// <summary>
        /// Change password for current user.
        /// - Validates new != current
        /// - Updates security stamp
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req, CancellationToken ct = default)
        {
            var currentPassword = req.CurrentPassword ?? "";
            var newPassword = req.NewPassword ?? "";

            if (string.IsNullOrWhiteSpace(currentPassword))
                return BadRequest(new { message = "CurrentPassword is required." });

            if (string.IsNullOrWhiteSpace(newPassword))
                return BadRequest(new { message = "NewPassword is required." });

            if (newPassword == currentPassword)
                return BadRequest(new { message = "NewPassword must be different from CurrentPassword." });

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId) || userId == Guid.Empty)
                return Unauthorized(new { message = "Invalid user identity." });

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Unauthorized(new { message = "User not found." });

            if (!user.IsActive)
                return Forbid();

            if (IsTrivialPassword(newPassword, user.UserName ?? "", user.Email ?? ""))
                return BadRequest(new { message = "NewPassword is too weak (cannot equal username/email)." });

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
                return BadRequest(new { message = "Change password failed.", errors = result.Errors.Select(e => e.Description).ToArray() });

            await _userManager.UpdateSecurityStampAsync(user);
            await _authTokenService.RevokeAllRefreshTokensAsync(user.Id, ct);

            _logger.LogInformation("Password changed for {UserName} ({Email})", user.UserName, user.Email);

            return Ok(new { ok = true });
        }

        [HttpPost("deactivate-account")]
        [Authorize]
        public async Task<IActionResult> DeactivateAccount([FromBody] DeactivateAccountRequest? req, CancellationToken ct = default)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId) || userId == Guid.Empty)
                return Unauthorized(new { message = "Invalid user identity." });

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Unauthorized(new { message = "User not found." });

            var isLastAdmin = await _userManager.IsInRoleAsync(user, RoleNames.Admin) &&
                              (await _userManager.GetUsersInRoleAsync(RoleNames.Admin))
                                  .Count(x => x.IsActive) <= 1;
            if (isLastAdmin)
                return BadRequest(new { message = "Cannot deactivate the last active Admin account." });

            if (!user.IsActive)
                return Ok(new { ok = true, changed = false });

            user.IsActive = false;
            user.UpdatedAt = DateTimeOffset.Now;
            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
                return BadRequest(new { message = "Deactivate account failed.", errors = update.Errors.Select(e => e.Description).ToArray() });

            await _userManager.UpdateSecurityStampAsync(user);
            await _authTokenService.RevokeAllRefreshTokensAsync(user.Id, ct);

            _logger.LogInformation("Account deactivated by user {UserName} ({Email}). Reason: {Reason}",
                user.UserName,
                user.Email,
                string.IsNullOrWhiteSpace(req?.Reason) ? "N/A" : req!.Reason);

            return Ok(new { ok = true, changed = true });
        }

        // ---------------------------
        // Helpers
        // ---------------------------

        private async Task<AppUser?> FindByEmailOrUserNameAsync(string emailOrUserName, CancellationToken ct)
        {
            var key = emailOrUserName.Trim();

            // Email first if it looks like email
            if (key.Contains('@'))
            {
                var normalized = NormalizeEmail(key);
                if (normalized is not null)
                {
                    var byEmail = await _userManager.FindByEmailAsync(normalized);
                    if (byEmail is not null) return byEmail;
                }
            }

            // Username
            var byUserName = await _userManager.FindByNameAsync(key);
            if (byUserName is not null) return byUserName;

            // Fallback: try email raw
            var byEmail2 = await _userManager.FindByEmailAsync(key);
            return byEmail2;
        }

        private static bool IsValidUserName(string userName)
        {
            // 3-32 chars: a-z, 0-9, '.', '_', '-'
            if (string.IsNullOrWhiteSpace(userName)) return false;
            if (userName.Length < 3 || userName.Length > 32) return false;
            return Regex.IsMatch(userName, "^[a-zA-Z0-9._-]+$");
        }

        private static string? NormalizeEmail(string email)
        {
            email = (email ?? "").Trim();
            if (email.Length < 5) return null;
            if (!email.Contains('@')) return null;
            // Basic normalization (lowercase). Identity still stores NormalizedEmail uppercase.
            return email.ToLowerInvariant();
        }

        private async Task<string> ResolveAvailableUserNameAsync(string? requestedUserName, string fullName, string email, CancellationToken ct)
        {
            var preferred = NullIfWhiteSpace(requestedUserName);
            if (!string.IsNullOrWhiteSpace(preferred))
                return preferred;

            var baseUserName = BuildBaseUserName(fullName, email);
            var candidate = baseUserName;
            var suffix = 0;

            while (await _userManager.Users.AnyAsync(x => x.NormalizedUserName == candidate.ToUpperInvariant(), ct))
            {
                suffix++;
                var suffixText = suffix.ToString(CultureInfo.InvariantCulture);
                var maxBaseLength = Math.Max(3, 32 - suffixText.Length);
                candidate = baseUserName.Length > maxBaseLength
                    ? baseUserName[..maxBaseLength] + suffixText
                    : baseUserName + suffixText;
            }

            return candidate;
        }

        private static string BuildBaseUserName(string fullName, string email)
        {
            var localPart = email.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "traveler";
            var source = string.IsNullOrWhiteSpace(fullName) ? localPart : fullName;

            var normalized = source.Trim().ToLowerInvariant();
            var buffer = new List<char>(normalized.Length);

            foreach (var ch in normalized)
            {
                if (ch is >= 'a' and <= 'z' || ch is >= '0' and <= '9')
                {
                    buffer.Add(ch);
                    continue;
                }

                if ((char.IsWhiteSpace(ch) || ch is '.' or '_' or '-') &&
                    buffer.Count > 0 &&
                    buffer[^1] != '.')
                {
                    buffer.Add('.');
                }
            }

            var collapsed = new string(buffer.ToArray()).Trim('.');

            if (collapsed.Length < 3)
                collapsed = (localPart + "001").ToLowerInvariant();

            if (collapsed.Length > 32)
                collapsed = collapsed[..32];

            return collapsed;
        }

        private static bool IsTrivialPassword(string password, string userName, string email)
        {
            if (string.IsNullOrWhiteSpace(password)) return true;

            var p = password.Trim();
            var u = (userName ?? "").Trim();
            var e = (email ?? "").Trim();

            if (u.Length > 0 && string.Equals(p, u, StringComparison.OrdinalIgnoreCase)) return true;
            if (e.Length > 0 && string.Equals(p, e, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private static string ToTitleCase(string input)
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

        private string GetClientIp()
        {
            // If behind proxy, you may need X-Forwarded-For later; for now direct connection.
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// Simple in-memory rate limiter:
        /// - key window stored in MemoryCache
        /// - returns false when limit exceeded
        /// </summary>
        private bool TryConsumeRateLimit(string key, int limit, TimeSpan window, out int retryAfterSeconds)
        {
            retryAfterSeconds = (int)window.TotalSeconds;

            var cacheKey = $"rl:{key}";
            var now = DateTimeOffset.UtcNow;

            var entry = _cache.GetOrCreate(cacheKey, e =>
            {
                e.AbsoluteExpirationRelativeToNow = window;
                return new RateLimitEntry { Count = 0, FirstAtUtc = now };
            });

            // entry can be null only if cache misbehaves
            entry ??= new RateLimitEntry { Count = 0, FirstAtUtc = now };

            if (entry.Count >= limit)
            {
                var elapsed = now - entry.FirstAtUtc;
                var remain = window - elapsed;
                retryAfterSeconds = (int)Math.Max(1, remain.TotalSeconds);
                return false;
            }

            entry.Count++;
            _cache.Set(cacheKey, entry, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = window
            });

            return true;
        }

        private sealed class RateLimitEntry
        {
            public int Count { get; set; }
            public DateTimeOffset FirstAtUtc { get; set; }
        }
    }
}
