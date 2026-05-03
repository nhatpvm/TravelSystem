// FILE: TicketBooking.Api/Middlewares/TenantContextMiddleware.cs
// FILE #020 (FIX)

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Middlewares
{
    /// <summary>
    /// Phase 3: TenantContext middleware
    /// Rules:
    /// - Header used: X-TenantId (Guid)
    /// - Admin:
    ///   - Read requests: can omit tenant (TenantId null => cross-tenant read allowed later).
    ///   - Write requests (POST/PUT/PATCH/DELETE): MUST provide X-TenantId.
    /// - Non-admin authenticated:
    ///   - User must belong to >= 1 tenant via tenants.TenantUsers.
    ///   - If user belongs to exactly 1 tenant -> auto select it unless header provided.
    ///   - If user belongs to many tenants -> header X-TenantId is REQUIRED and must be one of user's tenant ids.
    /// - Unauthenticated:
    ///   - TenantId stays null.
    ///
    /// IMPORTANT FIX:
    /// - Auth self-service routes are GLOBAL and must NOT require tenant:
    ///   /api/v{version}/auth/*
    ///   Examples:
    ///   - /auth/login
    ///   - /auth/register
    ///   - /auth/me
    ///   - /auth/change-password
    ///   - /auth/forgot-password
    ///   - /auth/reset-password
    ///   - /auth/logout
    /// </summary>
    public sealed class TenantContextMiddleware : IMiddleware
    {
        public const string HeaderTenantId = "X-TenantId";

        private readonly ITenantContext _tenantContext;
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMemoryCache _cache;

        public TenantContextMiddleware(
            ITenantContext tenantContext,
            AppDbContext db,
            UserManager<AppUser> userManager,
            IMemoryCache cache)
        {
            _tenantContext = tenantContext;
            _db = db;
            _userManager = userManager;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            _tenantContext.Clear();

            var path = context.Request.Path;
            var isPublicTenantInferredRoute = IsPublicTenantInferredRoute(path);
            var isTenantBootstrapRoute = IsTenantBootstrapRoute(path);

            if (IsGlobalBypassRoute(path))
            {
                await next(context);
                return;
            }


            var isWrite = HttpMethodHelper.IsWriteMethod(context.Request.Method);
            _tenantContext.SetRequiresTenantForWrite(isWrite);

            // If not authenticated, do nothing (TenantId stays null)
            var userId = GetUserId(context.User);
            if (!userId.HasValue)
            {
                await next(context);
                return;
            }

            var userAccess = await GetCachedUserAccessAsync(context, userId.Value);
            if (userAccess is null || !userAccess.IsActive)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "User not found or inactive." });
                return;
            }

            var isAdmin = userAccess.Roles.Any(r => string.Equals(r, RoleNames.Admin, StringComparison.OrdinalIgnoreCase));
            var userTenantIds = userAccess.TenantIds;

            _tenantContext.SetUser(userAccess.UserId, isAdmin, userTenantIds);

            if (isTenantBootstrapRoute)
            {
                _tenantContext.SetTenant(null);
                await next(context);
                return;
            }

            // Public BUS/TRAIN product routes:
            // - still keep authenticated user in context
            // - but DO NOT bind tenant from user/header here
            // - controller will infer seller tenant from trip when needed
            if (isPublicTenantInferredRoute)
            {
                _tenantContext.SetTenant(null);
                await next(context);
                return;
            }

            // Parse tenant header (if any)
            var headerTenantId = TryParseTenantIdHeader(context.Request.Headers[HeaderTenantId]);


            if (isAdmin)
            {
                // Admin: for write, must supply X-TenantId
                if (isWrite && headerTenantId is null)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await context.Response.WriteAsJsonAsync(new { message = "X-TenantId is required for admin write requests." });
                    return;
                }

                _tenantContext.SetTenant(headerTenantId); // can be null for reads
                await next(context);
                return;
            }

            // Non-admin: must belong to at least one tenant
            if (userTenantIds.Count == 0)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "User is not assigned to any tenant." });
                return;
            }

            // If user belongs to only one tenant and header not provided -> auto select
            if (userTenantIds.Count == 1 && headerTenantId is null)
            {
                _tenantContext.SetTenant(userTenantIds.First());
                await next(context);
                return;
            }

            // If multiple tenants -> require header
            if (headerTenantId is null)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { message = "X-TenantId is required when user belongs to multiple tenants." });
                return;
            }

            // Validate membership
            if (!userTenantIds.Contains(headerTenantId.Value))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { message = "You are not allowed to access this tenant." });
                return;
            }

            _tenantContext.SetTenant(headerTenantId);
            await next(context);
        }

        private static bool IsGlobalBypassRoute(PathString path)
        {
            // Geo sync/read admin tool is global
            if (IsVersionedApiRoute(path, "admin", "geo"))
                return true;

            // All auth routes are global self-service routes
            if (IsApiAuthRoute(path))
                return true;

            // Global admin identity/auth management routes
            if (IsVersionedApiRoute(path, "admin", "users"))
                return true;

            if (IsVersionedApiRoute(path, "admin", "roles"))
                return true;

            if (IsVersionedApiRoute(path, "admin", "auth", "permissions"))
                return true;

            if (IsVersionedApiRoute(path, "admin", "auth", "role-permissions"))
                return true;

            if (IsVersionedApiRoute(path, "admin", "auth", "user-permissions"))
                return true;

            if (IsVersionedApiRoute(path, "admin", "tenants"))
                return true;

            if (IsVersionedApiRoute(path, "admin", "tenant-onboarding"))
                return true;

            if (IsVersionedApiRoute(path, "admin", "commerce", "settlements"))
                return true;

            if (IsVersionedApiRoute(path, "admin", "promotions"))
                return true;

            if (IsVersionedApiRoute(path, "admin", "uploads"))
                return true;

            // Customer commerce/account flows are global user flows and do not require tenant membership.
            if (IsVersionedApiRoute(path, "customer", "orders"))
                return true;

            if (IsVersionedApiRoute(path, "customer", "account"))
                return true;

            if (IsVersionedApiRoute(path, "customer", "uploads"))
                return true;

            if (IsVersionedApiRoute(path, "payments", "sepay"))
                return true;

            if (IsVersionedApiRoute(path, "promotions"))
                return true;

            return false;
        }


        private static bool IsApiAuthRoute(PathString path)
        {
            var value = path.Value;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var segments = value
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Expected:
            // [0] = api
            // [1] = v1 / v1.0 / v2 ...
            // [2] = auth
            if (segments.Length < 3)
                return false;

            if (!string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!segments[1].StartsWith("v", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!string.Equals(segments[2], "auth", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }
        private static bool IsTenantBootstrapRoute(PathString path)
        {
            return IsVersionedApiRoute(path, "tenancy", "memberships")
                || IsVersionedApiRoute(path, "tenancy", "whoami");
        }

        private static bool IsVersionedApiRoute(PathString path, params string[] routeSegments)
        {
            var value = path.Value;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var segments = value
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (segments.Length < routeSegments.Length + 2)
                return false;

            if (!string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!segments[1].StartsWith("v", StringComparison.OrdinalIgnoreCase))
                return false;

            for (var i = 0; i < routeSegments.Length; i++)
            {
                if (!string.Equals(segments[i + 2], routeSegments[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
        private static bool IsPublicTenantInferredRoute(PathString path)
        {
            var value = path.Value;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var segments = value
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            // Expected:
            // [0] = api
            // [1] = v1 / v1.0 / v2 ...
            // [2] = bus | train
            if (segments.Length < 4)
                return false;

            if (!string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!segments[1].StartsWith("v", StringComparison.OrdinalIgnoreCase))
                return false;

            var module = segments[2];

            // BUS public routes:
            // GET    /api/v*/bus/search/*
            // GET    /api/v*/bus/trips/{tripId}
            // GET    /api/v*/bus/trips/{tripId}/seats
            // POST   /api/v*/bus/seat-holds
            // DELETE /api/v*/bus/seat-holds/{holdToken}
            if (string.Equals(module, "bus", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(segments[3], "search", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (string.Equals(segments[3], "seat-holds", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (segments.Length == 5 &&
                    string.Equals(segments[3], "trips", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (segments.Length >= 5 &&
                    string.Equals(segments[3], "trips", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(segments[^1], "seats", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            // TRAIN public routes:
            // GET    /api/v*/train/search/*
            // GET    /api/v*/train/trips/{tripId}/seats
            // POST   /api/v*/train/seat-holds
            // DELETE /api/v*/train/seat-holds/{holdToken}
            if (string.Equals(module, "train", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(segments[3], "search", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (string.Equals(segments[3], "seat-holds", StringComparison.OrdinalIgnoreCase))
                    return true;

                if (segments.Length >= 5 &&
                    string.Equals(segments[3], "trips", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(segments[^1], "seats", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static Guid? GetUserId(ClaimsPrincipal principal)
        {
            var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(sub, out var id) ? id : null;
        }

        private async Task<CachedUserAccess?> GetCachedUserAccessAsync(HttpContext context, Guid userId)
        {
            var securityStamp = context.User.FindFirstValue("security_stamp") ?? "";
            var cacheKey = $"tenant-access:{userId:N}:{securityStamp}";
            if (_cache.TryGetValue(cacheKey, out CachedUserAccess? cached))
                return cached;

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return null;

            var roles = await _userManager.GetRolesAsync(user);
            var tenantIds = await _db.TenantUsers
                .AsNoTracking()
                .Where(x => x.UserId == user.Id && !x.IsDeleted)
                .Select(x => x.TenantId)
                .Distinct()
                .ToListAsync(context.RequestAborted);

            var access = new CachedUserAccess(
                user.Id,
                user.IsActive,
                roles.ToArray(),
                tenantIds);

            _cache.Set(
                cacheKey,
                access,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30),
                    SlidingExpiration = TimeSpan.FromSeconds(10),
                    Size = 1
                });

            return access;
        }

        private static Guid? TryParseTenantIdHeader(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Guid.TryParse(value.Trim(), out var id) ? id : null;
        }
    }

    internal sealed record CachedUserAccess(
        Guid UserId,
        bool IsActive,
        IReadOnlyCollection<string> Roles,
        IReadOnlyCollection<Guid> TenantIds);
}
