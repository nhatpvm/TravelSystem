// FILE #009: TicketBooking.Api/Controllers/AuthController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TicketBooking.Api.Auth;
using TicketBooking.Infrastructure.Auth;
using TicketBooking.Infrastructure.Identity;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IAuthTokenService _authTokenService;
    private readonly IPermissionService _permissionService;
    private readonly IMemoryCache _cache;

    private const string TwoFactorChallengeCachePrefix = "auth:2fa:challenge:";
    private static readonly TimeSpan TwoFactorChallengeLifetime = TimeSpan.FromMinutes(5);
    private const string TwoFactorIssuer = "TicketBooking.V3";

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        IAuthTokenService authTokenService,
        IPermissionService permissionService,
        IMemoryCache cache)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _authTokenService = authTokenService;
        _permissionService = permissionService;
        _cache = cache;
    }

    public sealed class LoginRequest
    {
        public string UsernameOrEmail { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public sealed class LoginResponse
    {
        public string AccessToken { get; set; } = "";
        public DateTimeOffset ExpiresAt { get; set; }
        public string RefreshToken { get; set; } = "";
        public DateTimeOffset RefreshTokenExpiresAt { get; set; }
        public string SessionId { get; set; } = "";
        public bool RequiresTwoFactor { get; set; }
        public string ChallengeToken { get; set; } = "";
        public DateTimeOffset? ChallengeExpiresAt { get; set; }
        public UserMeResponse User { get; set; } = new();
    }

    public class TwoFactorStatusResponse
    {
        public bool IsEnabled { get; set; }
        public bool HasAuthenticator { get; set; }
        public int RecoveryCodesLeft { get; set; }
    }

    public sealed class TwoFactorSetupResponse : TwoFactorStatusResponse
    {
        public string SharedKey { get; set; } = "";
        public string AuthenticatorUri { get; set; } = "";
        public string? AccessToken { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
        public string? SessionId { get; set; }
        public UserMeResponse? User { get; set; }
    }

    public sealed class EnableTwoFactorRequest
    {
        public string Code { get; set; } = "";
    }

    public sealed class VerifyTwoFactorLoginRequest
    {
        public string ChallengeToken { get; set; } = "";
        public string? Code { get; set; }
        public string? RecoveryCode { get; set; }
    }

    private sealed class TwoFactorLoginChallenge
    {
        public Guid UserId { get; set; }
        public string SecurityStamp { get; set; } = "";
        public DateTimeOffset ExpiresAt { get; set; }
    }

    public sealed class RefreshRequest
    {
        public string RefreshToken { get; set; } = "";
    }

    public sealed class UserMeResponse
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public bool EmailConfirmed { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string[] Roles { get; set; } = Array.Empty<string>();
    }

    public sealed class PermissionSummaryResponse
    {
        public Guid PermissionId { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Category { get; set; }
        public bool IsGranted { get; set; }
        public string Resolution { get; set; } = "";
        public string[] Sources { get; set; } = Array.Empty<string>();
    }

    public sealed class UpdateMeRequest
    {
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UsernameOrEmail) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "UsernameOrEmail and Password are required." });

        var input = req.UsernameOrEmail.Trim();

        // Allow login by username OR email:
        // 1) try username
        // 2) if not found, try email
        AppUser? user = await _userManager.FindByNameAsync(input);

        if (user is null)
            user = await _userManager.FindByEmailAsync(input);

        // If still null and the input isn't email-looking, try as email anyway (safe fallback)
        if (user is null && input.Contains('@'))
            user = await _userManager.FindByEmailAsync(input);

        if (user is null)
            return Unauthorized(new { message = "Invalid credentials." });

        if (!user.IsActive)
            return StatusCode(403, new { message = "User is inactive." });

        var signIn = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: true);
        if (!signIn.Succeeded)
        {
            if (signIn.IsLockedOut)
                return StatusCode(423, new { message = "User is locked out. Please try again later." });

            return Unauthorized(new { message = "Invalid credentials." });
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (await _userManager.GetTwoFactorEnabledAsync(user))
        {
            var challengeToken = Guid.NewGuid().ToString("N");
            var expiresAt = DateTimeOffset.UtcNow.Add(TwoFactorChallengeLifetime);

            _cache.Set(
                GetTwoFactorChallengeCacheKey(challengeToken),
                new TwoFactorLoginChallenge
                {
                    UserId = user.Id,
                    SecurityStamp = user.SecurityStamp ?? "",
                    ExpiresAt = expiresAt,
                },
                expiresAt);

            return Ok(new LoginResponse
            {
                RequiresTwoFactor = true,
                ChallengeToken = challengeToken,
                ChallengeExpiresAt = expiresAt,
                User = BuildUserMeResponse(user, roles)
            });
        }

        var tokenResult = await _authTokenService.IssueTokenPairAsync(user);

        return Ok(new LoginResponse
        {
            AccessToken = tokenResult.AccessToken,
            ExpiresAt = tokenResult.AccessTokenExpiresAt,
            RefreshToken = tokenResult.RefreshToken,
            RefreshTokenExpiresAt = tokenResult.RefreshTokenExpiresAt,
            SessionId = tokenResult.SessionId,
            User = BuildUserMeResponse(tokenResult.User, tokenResult.Roles)
        });
    }

    [HttpPost("2fa/verify-login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> VerifyTwoFactorLogin([FromBody] VerifyTwoFactorLoginRequest req, CancellationToken ct = default)
    {
        var challengeToken = NullIfWhiteSpace(req.ChallengeToken);
        if (challengeToken is null)
            return BadRequest(new { message = "ChallengeToken is required." });

        if (!_cache.TryGetValue<TwoFactorLoginChallenge>(GetTwoFactorChallengeCacheKey(challengeToken), out var challenge)
            || challenge is null
            || challenge.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Unauthorized(new { message = "The two-factor verification session has expired. Please login again." });
        }

        var user = await _userManager.FindByIdAsync(challenge.UserId.ToString());
        if (user is null || !user.IsActive)
            return Unauthorized(new { message = "User not found or inactive." });

        if (!string.Equals(user.SecurityStamp, challenge.SecurityStamp, StringComparison.Ordinal))
            return Unauthorized(new { message = "The two-factor verification session is no longer valid. Please login again." });

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            return StatusCode(423, new { message = "User is locked out. Please try again later." });

        var authenticatorCode = NormalizeTwoFactorCode(req.Code);
        var recoveryCode = NormalizeRecoveryCode(req.RecoveryCode);
        var succeeded = false;

        if (!string.IsNullOrWhiteSpace(recoveryCode))
        {
            var redeemResult = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, recoveryCode);
            succeeded = redeemResult.Succeeded;
        }
        else if (!string.IsNullOrWhiteSpace(authenticatorCode))
        {
            succeeded = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                authenticatorCode);
        }
        else
        {
            return BadRequest(new { message = "A two-factor code or recovery code is required." });
        }

        if (!succeeded)
            return Unauthorized(new { message = "The two-factor code is invalid." });

        _cache.Remove(GetTwoFactorChallengeCacheKey(challengeToken));

        var tokenResult = await _authTokenService.IssueTokenPairAsync(user, ct: ct);

        return Ok(new LoginResponse
        {
            AccessToken = tokenResult.AccessToken,
            ExpiresAt = tokenResult.AccessTokenExpiresAt,
            RefreshToken = tokenResult.RefreshToken,
            RefreshTokenExpiresAt = tokenResult.RefreshTokenExpiresAt,
            SessionId = tokenResult.SessionId,
            User = BuildUserMeResponse(tokenResult.User, tokenResult.Roles)
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshRequest req, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            return BadRequest(new { message = "RefreshToken is required." });

        try
        {
            var tokenResult = await _authTokenService.RefreshAsync(req.RefreshToken, ct);
            return Ok(new LoginResponse
            {
                AccessToken = tokenResult.AccessToken,
                ExpiresAt = tokenResult.AccessTokenExpiresAt,
                RefreshToken = tokenResult.RefreshToken,
                RefreshTokenExpiresAt = tokenResult.RefreshTokenExpiresAt,
                SessionId = tokenResult.SessionId,
                User = BuildUserMeResponse(tokenResult.User, tokenResult.Roles)
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserMeResponse>> Me()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Invalid token subject." });

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Unauthorized(new { message = "User not found." });

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(BuildUserMeResponse(user, roles));
    }

    [HttpGet("me/permissions")]
    [Authorize]
    public async Task<ActionResult<object>> MePermissions([FromQuery] Guid? tenantId, [FromQuery] bool grantedOnly = false, [FromQuery] string? q = null, CancellationToken ct = default)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Invalid token subject." });

        var items = await _permissionService.GetEffectivePermissionsAsync(userId, tenantId, ct);
        var filtered = items
            .Where(x => !grantedOnly || x.IsGranted)
            .Where(x => string.IsNullOrWhiteSpace(q)
                || x.Code.Contains(q, StringComparison.OrdinalIgnoreCase)
                || x.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (x.Category?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(x => new PermissionSummaryResponse
            {
                PermissionId = x.PermissionId,
                Code = x.Code,
                Name = x.Name,
                Category = x.Category,
                IsGranted = x.IsGranted,
                Resolution = x.Resolution,
                Sources = x.Sources
            })
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Code)
            .ToList();

        return Ok(new
        {
            userId,
            tenantId,
            total = filtered.Count,
            items = filtered
        });
    }

    [HttpGet("2fa/status")]
    [Authorize]
    public async Task<ActionResult<TwoFactorStatusResponse>> GetTwoFactorStatus()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Unauthorized(new { message = "User not found." });

        return Ok(await BuildTwoFactorStatusResponseAsync(user));
    }

    [HttpPost("2fa/setup")]
    [Authorize]
    public async Task<ActionResult<TwoFactorSetupResponse>> SetupTwoFactor()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Unauthorized(new { message = "User not found." });

        var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
        AuthTokenBundle? tokenResult = null;
        if (string.IsNullOrWhiteSpace(authenticatorKey))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
            tokenResult = await _authTokenService.IssueTokenPairAsync(user);
        }

        if (string.IsNullOrWhiteSpace(authenticatorKey))
            return BadRequest(new { message = "Unable to initialize the authenticator key." });

        var status = await BuildTwoFactorStatusResponseAsync(user);
        return Ok(new TwoFactorSetupResponse
        {
            IsEnabled = status.IsEnabled,
            HasAuthenticator = true,
            RecoveryCodesLeft = status.RecoveryCodesLeft,
            SharedKey = FormatAuthenticatorKey(authenticatorKey),
            AuthenticatorUri = GenerateAuthenticatorUri(user, authenticatorKey),
            AccessToken = tokenResult?.AccessToken,
            ExpiresAt = tokenResult?.AccessTokenExpiresAt,
            RefreshToken = tokenResult?.RefreshToken,
            RefreshTokenExpiresAt = tokenResult?.RefreshTokenExpiresAt,
            SessionId = tokenResult?.SessionId,
            User = tokenResult is null ? null : BuildUserMeResponse(tokenResult.User, tokenResult.Roles)
        });
    }

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<ActionResult<object>> EnableTwoFactor([FromBody] EnableTwoFactorRequest req)
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Unauthorized(new { message = "User not found." });

        var code = NormalizeTwoFactorCode(req.Code);
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { message = "The authenticator code is required." });

        var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(authenticatorKey))
            return BadRequest(new { message = "Please create an authenticator key before enabling two-factor authentication." });

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        if (!isValid)
            return BadRequest(new { message = "The authenticator code is invalid." });

        var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
        if (!result.Succeeded)
            return BadRequest(new { message = "Unable to enable two-factor authentication.", errors = result.Errors.Select(x => x.Description).ToArray() });

        var recoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))?.ToArray() ?? Array.Empty<string>();
        return Ok(new
        {
            ok = true,
            message = "Two-factor authentication has been enabled.",
            recoveryCodes,
            status = await BuildTwoFactorStatusResponseAsync(user)
        });
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<ActionResult<object>> DisableTwoFactor()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Unauthorized(new { message = "User not found." });

        var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
            return BadRequest(new { message = "Unable to disable two-factor authentication.", errors = result.Errors.Select(x => x.Description).ToArray() });

        return Ok(new
        {
            ok = true,
            message = "Two-factor authentication has been disabled.",
            status = await BuildTwoFactorStatusResponseAsync(user)
        });
    }

    [HttpPost("2fa/recovery-codes")]
    [Authorize]
    public async Task<ActionResult<object>> RegenerateRecoveryCodes()
    {
        var user = await GetCurrentUserAsync();
        if (user is null)
            return Unauthorized(new { message = "User not found." });

        if (!await _userManager.GetTwoFactorEnabledAsync(user))
            return BadRequest(new { message = "Enable two-factor authentication before regenerating recovery codes." });

        var recoveryCodes = (await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10))?.ToArray() ?? Array.Empty<string>();
        return Ok(new
        {
            ok = true,
            message = "Recovery codes have been regenerated.",
            recoveryCodes,
            status = await BuildTwoFactorStatusResponseAsync(user)
        });
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<UserMeResponse>> UpdateMe([FromBody] UpdateMeRequest req)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Invalid token subject." });

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Unauthorized(new { message = "User not found." });

        var email = NormalizeEmail(req.Email);
        if (email is null)
            return BadRequest(new { message = "Email is invalid." });

        var fullName = ToTitleCase(req.FullName);
        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { message = "FullName is required." });
        if (fullName.Length > 200)
            return BadRequest(new { message = "FullName max length is 200." });

        var phoneNumber = NullIfWhiteSpace(req.PhoneNumber);
        if (phoneNumber is not null && phoneNumber.Length > 30)
            return BadRequest(new { message = "PhoneNumber max length is 30." });

        var avatarUrl = NullIfWhiteSpace(req.AvatarUrl);
        if (avatarUrl is not null && avatarUrl.Length > 500)
            return BadRequest(new { message = "AvatarUrl max length is 500." });

        var emailOwner = await _userManager.FindByEmailAsync(email);
        if (emailOwner is not null && emailOwner.Id != user.Id)
            return Conflict(new { message = "Email already exists." });

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = email;
            user.NormalizedEmail = email.ToUpperInvariant();
            user.EmailConfirmed = false;
        }

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        user.AvatarUrl = avatarUrl;
        user.UpdatedAt = DateTimeOffset.Now;

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
            return BadRequest(new { message = "Update profile failed.", errors = update.Errors.Select(e => e.Description).ToArray() });

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(BuildUserMeResponse(user, roles));
    }

    private static UserMeResponse BuildUserMeResponse(AppUser user, IList<string> roles)
        => new()
        {
            UserId = user.Id,
            UserName = user.UserName ?? "",
            Email = user.Email ?? "",
            EmailConfirmed = user.EmailConfirmed,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToArray()
        };

    private static string? NormalizeEmail(string? email)
    {
        email = (email ?? "").Trim();
        if (email.Length < 5 || !email.Contains('@'))
            return null;

        return email.ToLowerInvariant();
    }

    private static string ToTitleCase(string? input)
    {
        input = (input ?? "").Trim();
        if (input.Length == 0)
            return "";

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            var first = char.ToUpperInvariant(p[0]);
            var rest = p.Length > 1 ? p[1..].ToLowerInvariant() : "";
            parts[i] = first + rest;
        }

        return string.Join(' ', parts);
    }

    private static string? NullIfWhiteSpace(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<AppUser?> GetCurrentUserAsync()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userIdStr, out var userId))
            return null;

        return await _userManager.FindByIdAsync(userId.ToString());
    }

    private async Task<TwoFactorStatusResponse> BuildTwoFactorStatusResponseAsync(AppUser user)
    {
        var authenticatorKey = await _userManager.GetAuthenticatorKeyAsync(user);
        var isEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        var recoveryCodesLeft = isEnabled ? await _userManager.CountRecoveryCodesAsync(user) : 0;

        return new TwoFactorStatusResponse
        {
            IsEnabled = isEnabled,
            HasAuthenticator = !string.IsNullOrWhiteSpace(authenticatorKey),
            RecoveryCodesLeft = recoveryCodesLeft
        };
    }

    private static string GetTwoFactorChallengeCacheKey(string token)
        => $"{TwoFactorChallengeCachePrefix}{token}";

    private static string NormalizeTwoFactorCode(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Replace(" ", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Trim();

    private static string NormalizeRecoveryCode(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? ""
            : value.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();

    private static string FormatAuthenticatorKey(string rawKey)
        => string.Join(' ', rawKey.ToUpperInvariant().Chunk(4).Select(chunk => new string(chunk)));

    private static string GenerateAuthenticatorUri(AppUser user, string rawKey)
    {
        var accountName = Uri.EscapeDataString(user.Email ?? user.UserName ?? user.Id.ToString());
        var issuer = Uri.EscapeDataString(TwoFactorIssuer);
        return $"otpauth://totp/{issuer}:{accountName}?secret={rawKey}&issuer={issuer}&digits=6";
    }
}
