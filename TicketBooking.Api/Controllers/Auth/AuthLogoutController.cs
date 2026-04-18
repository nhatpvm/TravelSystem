// FILE #113: TicketBooking.Api/Controllers/Auth/AuthLogoutController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TicketBooking.Api.Auth;
using TicketBooking.Infrastructure.Identity;

namespace TicketBooking.Api.Controllers.Auth
{
    /// <summary>
    /// Logout endpoint for production readiness.
    ///
    /// IMPORTANT:
    /// - With pure JWT (no refresh token/session tables yet), logout is client-side:
    ///   the client deletes access token. This endpoint still returns 200 for UX consistency.
    /// - When you implement auth.RefreshTokens + auth.UserSessions later, this endpoint will:
    ///   - revoke refresh token / session
    ///   - update security stamp / session status
    ///
    /// For now (Phase 1-10), we keep it safe and simple.
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/auth")]
    public sealed class AuthLogoutController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IAuthTokenService _authTokenService;

        public AuthLogoutController(UserManager<AppUser> userManager, IAuthTokenService authTokenService)
        {
            _userManager = userManager;
            _authTokenService = authTokenService;
        }

        /// <summary>
        /// POST /api/v1/auth/logout
        /// Invalidates the current JWT by rotating the user's security stamp,
        /// then the client should also delete access_token locally.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(CancellationToken ct = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var parsedUserId) || parsedUserId == Guid.Empty)
                return Unauthorized(new { message = "Invalid user identity." });

            var user = await _userManager.FindByIdAsync(parsedUserId.ToString());
            if (user is null)
                return Unauthorized(new { message = "User not found." });

            var sessionId = User.FindFirstValue("session_id");
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                await _userManager.UpdateSecurityStampAsync(user);
                var revokedAll = await _authTokenService.RevokeAllRefreshTokensAsync(parsedUserId, ct);
                return Ok(new { ok = true, userId = parsedUserId, revokedSessions = revokedAll, scope = "all" });
            }

            var revoked = await _authTokenService.RevokeRefreshTokenSessionAsync(parsedUserId, sessionId, ct);
            return Ok(new { ok = true, userId = parsedUserId, revoked, sessionId, scope = "current" });
        }

        /// <summary>
        /// POST /api/v1/auth/logout-all
        /// Explicitly revokes every refresh token and invalidates all current JWTs by rotating security stamp.
        /// </summary>
        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll(CancellationToken ct = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var parsedUserId) || parsedUserId == Guid.Empty)
                return Unauthorized(new { message = "Invalid user identity." });

            var user = await _userManager.FindByIdAsync(parsedUserId.ToString());
            if (user is null)
                return Unauthorized(new { message = "User not found." });

            await _userManager.UpdateSecurityStampAsync(user);
            var revokedSessions = await _authTokenService.RevokeAllRefreshTokensAsync(parsedUserId, ct);
            return Ok(new { ok = true, userId = parsedUserId, revokedSessions });
        }

        [HttpGet("sessions")]
        [Authorize]
        public async Task<IActionResult> Sessions(CancellationToken ct = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var parsedUserId) || parsedUserId == Guid.Empty)
                return Unauthorized(new { message = "Invalid user identity." });

            var currentSessionId = User.FindFirstValue("session_id");
            var sessions = await _authTokenService.GetSessionsAsync(parsedUserId, ct);

            return Ok(new
            {
                userId = parsedUserId,
                currentSessionId,
                total = sessions.Count,
                items = sessions.Select(x => new
                {
                    x.SessionId,
                    x.CreatedAt,
                    x.LastRefreshedAt,
                    x.ExpiresAt,
                    x.IpAddress,
                    x.UserAgent,
                    IsCurrent = !string.IsNullOrWhiteSpace(currentSessionId) && string.Equals(currentSessionId, x.SessionId, StringComparison.Ordinal)
                })
            });
        }

        [HttpDelete("sessions/{sessionId}")]
        [Authorize]
        public async Task<IActionResult> RevokeSession([FromRoute] string sessionId, CancellationToken ct = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var parsedUserId) || parsedUserId == Guid.Empty)
                return Unauthorized(new { message = "Invalid user identity." });

            var revoked = await _authTokenService.RevokeRefreshTokenSessionAsync(parsedUserId, sessionId, ct);
            if (!revoked)
                return NotFound(new { message = "Session not found." });

            return Ok(new { ok = true, sessionId });
        }
    }
}

