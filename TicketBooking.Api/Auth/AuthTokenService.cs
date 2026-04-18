using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Auth;

public interface IAuthTokenService
{
    Task<AuthTokenBundle> IssueTokenPairAsync(AppUser user, IList<string>? roles = null, CancellationToken ct = default);
    Task<AuthTokenBundle> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task<int> RevokeAllRefreshTokensAsync(Guid userId, CancellationToken ct = default);
    Task<bool> RevokeRefreshTokenSessionAsync(Guid userId, string sessionId, CancellationToken ct = default);
    Task<bool> IsSessionActiveAsync(Guid userId, string sessionId, CancellationToken ct = default);
    Task<IReadOnlyList<AuthSessionInfo>> GetSessionsAsync(Guid userId, CancellationToken ct = default);
}

public sealed class AuthTokenBundle
{
    public string AccessToken { get; init; } = "";
    public DateTimeOffset AccessTokenExpiresAt { get; init; }
    public string RefreshToken { get; init; } = "";
    public DateTimeOffset RefreshTokenExpiresAt { get; init; }
    public string SessionId { get; init; } = "";
    public AppUser User { get; init; } = default!;
    public IList<string> Roles { get; init; } = Array.Empty<string>();
}

public sealed class AuthSessionInfo
{
    public string SessionId { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastRefreshedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
}

public sealed class AuthTokenService : IAuthTokenService
{
    private const string RefreshTokenProvider = "TicketBooking.RefreshTokens";
    private const string RefreshTokenNamePrefix = "refresh:";

    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthTokenService(
        IConfiguration config,
        AppDbContext db,
        UserManager<AppUser> userManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _config = config;
        _db = db;
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AuthTokenBundle> IssueTokenPairAsync(AppUser user, IList<string>? roles = null, CancellationToken ct = default)
    {
        roles ??= await _userManager.GetRolesAsync(user);

        var refreshTokenExpiresAt = DateTimeOffset.Now.AddDays(GetRefreshTokenDays());
        var sessionId = Guid.NewGuid().ToString("N");
        var (accessToken, accessTokenExpiresAt) = CreateAccessToken(user, roles, sessionId);
        var refreshSecret = GenerateRefreshSecret();
        var securityStamp = await _userManager.GetSecurityStampAsync(user) ?? "";

        var payload = new StoredRefreshToken
        {
            TokenHash = EncodeHash(ComputeHashBytes(refreshSecret)),
            SecurityStamp = securityStamp,
            ExpiresAt = refreshTokenExpiresAt,
            CreatedAt = DateTimeOffset.Now,
            LastRefreshedAt = null,
            IpAddress = TrimOrNull(GetClientIp(), 100),
            UserAgent = TrimOrNull(GetUserAgent(), 500)
        };

        var storeResult = await SaveRefreshTokenAsync(user.Id, sessionId, payload, ct);

        if (!storeResult)
            throw new InvalidOperationException("Could not persist refresh token.");

        return new AuthTokenBundle
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshToken = BuildRefreshToken(sessionId, refreshSecret),
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            SessionId = sessionId,
            User = user,
            Roles = roles
        };
    }

    public async Task<AuthTokenBundle> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var parsed = ParseRefreshToken(refreshToken);
        if (parsed is null)
            throw new UnauthorizedAccessException("Refresh token is invalid.");

        var tokenRow = await _db.Set<IdentityUserToken<Guid>>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.LoginProvider == RefreshTokenProvider &&
                x.Name == BuildRefreshTokenName(parsed.Value.SessionId), ct);

        if (tokenRow is null)
            throw new UnauthorizedAccessException("Refresh token is invalid.");

        var stored = DeserializeStoredRefreshToken(tokenRow.Value);
        if (stored is null)
        {
            await DeleteRefreshTokenBySessionIdAsync(parsed.Value.SessionId, ct);
            throw new UnauthorizedAccessException("Refresh token is invalid.");
        }

        if (stored.ExpiresAt <= DateTimeOffset.Now)
        {
            await DeleteRefreshTokenBySessionIdAsync(parsed.Value.SessionId, ct);
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        if (!VerifySecret(parsed.Value.Secret, stored.TokenHash))
            throw new UnauthorizedAccessException("Refresh token is invalid.");

        var user = await _userManager.FindByIdAsync(tokenRow.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            await DeleteRefreshTokenBySessionIdAsync(parsed.Value.SessionId, ct);
            throw new UnauthorizedAccessException("User not found or inactive.");
        }

        var currentSecurityStamp = await _userManager.GetSecurityStampAsync(user) ?? "";
        if (!string.Equals(currentSecurityStamp, stored.SecurityStamp, StringComparison.Ordinal))
        {
            await DeleteRefreshTokenBySessionIdAsync(parsed.Value.SessionId, ct);
            throw new UnauthorizedAccessException("Refresh token has been revoked.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var (accessToken, accessTokenExpiresAt) = CreateAccessToken(user, roles, parsed.Value.SessionId);
        var nextRefreshSecret = GenerateRefreshSecret();
        var nextRefreshExpiresAt = DateTimeOffset.Now.AddDays(GetRefreshTokenDays());

        stored.TokenHash = EncodeHash(ComputeHashBytes(nextRefreshSecret));
        stored.SecurityStamp = currentSecurityStamp;
        stored.ExpiresAt = nextRefreshExpiresAt;
        stored.LastRefreshedAt = DateTimeOffset.Now;

        var saveResult = await SaveRefreshTokenAsync(user.Id, parsed.Value.SessionId, stored, ct);

        if (!saveResult)
            throw new InvalidOperationException("Could not rotate refresh token.");

        return new AuthTokenBundle
        {
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshToken = BuildRefreshToken(parsed.Value.SessionId, nextRefreshSecret),
            RefreshTokenExpiresAt = nextRefreshExpiresAt,
            SessionId = parsed.Value.SessionId,
            User = user,
            Roles = roles
        };
    }

    public async Task<int> RevokeAllRefreshTokensAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.Set<IdentityUserToken<Guid>>()
            .Where(x => x.UserId == userId &&
                        x.LoginProvider == RefreshTokenProvider &&
                        x.Name.StartsWith(RefreshTokenNamePrefix))
            .ExecuteDeleteAsync(ct);
    }

    public async Task<bool> RevokeRefreshTokenSessionAsync(Guid userId, string sessionId, CancellationToken ct = default)
    {
        sessionId = NormalizeSessionId(sessionId);
        if (sessionId.Length == 0)
            return false;

        var deleted = await _db.Set<IdentityUserToken<Guid>>()
            .Where(x => x.UserId == userId &&
                        x.LoginProvider == RefreshTokenProvider &&
                        x.Name == BuildRefreshTokenName(sessionId))
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }

    public async Task<bool> IsSessionActiveAsync(Guid userId, string sessionId, CancellationToken ct = default)
    {
        sessionId = NormalizeSessionId(sessionId);
        if (sessionId.Length == 0)
            return false;

        var row = await _db.Set<IdentityUserToken<Guid>>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.LoginProvider == RefreshTokenProvider &&
                x.Name == BuildRefreshTokenName(sessionId), ct);

        if (row is null)
            return false;

        var stored = DeserializeStoredRefreshToken(row.Value);
        return stored is not null && stored.ExpiresAt > DateTimeOffset.Now;
    }

    public async Task<IReadOnlyList<AuthSessionInfo>> GetSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _db.Set<IdentityUserToken<Guid>>()
            .AsNoTracking()
            .Where(x => x.UserId == userId &&
                        x.LoginProvider == RefreshTokenProvider &&
                        x.Name.StartsWith(RefreshTokenNamePrefix))
            .ToListAsync(ct);

        var items = rows
            .Select(x => new
            {
                SessionId = ExtractSessionIdFromTokenName(x.Name),
                Stored = DeserializeStoredRefreshToken(x.Value)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.SessionId) && x.Stored is not null)
            .Select(x => new AuthSessionInfo
            {
                SessionId = x.SessionId!,
                CreatedAt = x.Stored!.CreatedAt,
                LastRefreshedAt = x.Stored.LastRefreshedAt,
                ExpiresAt = x.Stored.ExpiresAt,
                IpAddress = x.Stored.IpAddress,
                UserAgent = x.Stored.UserAgent
            })
            .OrderByDescending(x => x.LastRefreshedAt ?? x.CreatedAt)
            .ThenByDescending(x => x.ExpiresAt)
            .ToList();

        return items;
    }

    private (string Token, DateTimeOffset ExpiresAt) CreateAccessToken(AppUser user, IList<string> roles, string? sessionId)
    {
        var issuer = _config["Jwt:Issuer"] ?? "TicketBooking.V3";
        var audience = _config["Jwt:Audience"] ?? "TicketBooking.V3";
        var signingKey = _config["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey))
            throw new InvalidOperationException("Jwt:SigningKey is missing.");

        var accessMinutes = 60;
        var minutesStr = _config["Jwt:AccessTokenMinutes"];
        if (!string.IsNullOrWhiteSpace(minutesStr) && int.TryParse(minutesStr, out var m) && m > 0)
            accessMinutes = m;

        var now = DateTimeOffset.Now;
        var expires = now.AddMinutes(accessMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? ""),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (!string.IsNullOrWhiteSpace(user.FullName))
            claims.Add(new Claim("full_name", user.FullName));

        if (!string.IsNullOrWhiteSpace(user.SecurityStamp))
            claims.Add(new Claim("security_stamp", user.SecurityStamp));

        if (!string.IsNullOrWhiteSpace(sessionId))
            claims.Add(new Claim("session_id", sessionId));

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    private async Task DeleteRefreshTokenBySessionIdAsync(string sessionId, CancellationToken ct)
    {
        await _db.Set<IdentityUserToken<Guid>>()
            .Where(x => x.LoginProvider == RefreshTokenProvider && x.Name == BuildRefreshTokenName(sessionId))
            .ExecuteDeleteAsync(ct);
    }

    private async Task<bool> SaveRefreshTokenAsync(Guid userId, string sessionId, StoredRefreshToken stored, CancellationToken ct)
    {
        var tokenName = BuildRefreshTokenName(sessionId);
        var serialized = JsonSerializer.Serialize(stored);

        var row = await _db.Set<IdentityUserToken<Guid>>()
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.LoginProvider == RefreshTokenProvider &&
                x.Name == tokenName, ct);

        if (row is null)
        {
            _db.Set<IdentityUserToken<Guid>>().Add(new IdentityUserToken<Guid>
            {
                UserId = userId,
                LoginProvider = RefreshTokenProvider,
                Name = tokenName,
                Value = serialized
            });
        }
        else
        {
            row.Value = serialized;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    private int GetRefreshTokenDays()
    {
        var refreshDays = 7;
        var daysStr = _config["Jwt:RefreshTokenDays"];
        if (!string.IsNullOrWhiteSpace(daysStr) && int.TryParse(daysStr, out var d) && d > 0)
            refreshDays = d;

        return refreshDays;
    }

    private static string BuildRefreshToken(string sessionId, string secret)
        => $"{sessionId}.{secret}";

    private static string BuildRefreshTokenName(string sessionId)
        => $"{RefreshTokenNamePrefix}{sessionId}";

    private static string? ExtractSessionIdFromTokenName(string? tokenName)
    {
        if (string.IsNullOrWhiteSpace(tokenName) || !tokenName.StartsWith(RefreshTokenNamePrefix, StringComparison.Ordinal))
            return null;

        var sessionId = tokenName[RefreshTokenNamePrefix.Length..];
        return Guid.TryParseExact(sessionId, "N", out _) ? sessionId : null;
    }

    private static (string SessionId, string Secret)? ParseRefreshToken(string? refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var parts = refreshToken.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return null;

        if (!Guid.TryParseExact(parts[0], "N", out _))
            return null;

        if (string.IsNullOrWhiteSpace(parts[1]))
            return null;

        return (parts[0], parts[1]);
    }

    private static string NormalizeSessionId(string? sessionId)
    {
        var value = (sessionId ?? "").Trim();
        return Guid.TryParseExact(value, "N", out _) ? value : "";
    }

    private static StoredRefreshToken? DeserializeStoredRefreshToken(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        try
        {
            return JsonSerializer.Deserialize<StoredRefreshToken>(raw);
        }
        catch
        {
            return null;
        }
    }

    private static string GenerateRefreshSecret()
        => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

    private static byte[] ComputeHashBytes(string raw)
        => SHA256.HashData(Encoding.UTF8.GetBytes(raw));

    private static string EncodeHash(byte[] bytes)
        => Convert.ToBase64String(bytes);

    private static bool VerifySecret(string secret, string storedHash)
    {
        byte[] expected;
        try
        {
            expected = Convert.FromBase64String(storedHash);
        }
        catch
        {
            return false;
        }

        var actual = ComputeHashBytes(secret);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private string? GetClientIp()
        => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent()
        => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    private static string? TrimOrNull(string? input, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        var value = input.Trim();
        if (value.Length > maxLen)
            value = value[..maxLen];

        return value;
    }

    private sealed class StoredRefreshToken
    {
        public string TokenHash { get; set; } = "";
        public string SecurityStamp { get; set; } = "";
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastRefreshedAt { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
