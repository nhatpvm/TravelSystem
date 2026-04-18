// FILE #029: TicketBooking.Infrastructure/Auth/PermissionAuthorizationHandler.cs
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Infrastructure.Auth
{
    /// <summary>
    /// Requirement: user must have the given permission code (string).
    /// Usage: [Authorize(Policy = "perm:bus.trips.read")]
    /// </summary>
    public sealed class PermissionRequirement : IAuthorizationRequirement
    {
        public string PermissionCode { get; }
        public PermissionRequirement(string permissionCode) => PermissionCode = permissionCode;
    }

    /// <summary>
    /// Authorization handler that checks permission using IPermissionService.
    /// TenantId is taken from ITenantContext (resolved by middleware via X-TenantId / auto tenant).
    /// </summary>
    public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly ITenantContext _tenantContext;
        private readonly IPermissionService _permissionService;

        public PermissionAuthorizationHandler(ITenantContext tenantContext, IPermissionService permissionService)
        {
            _tenantContext = tenantContext;
            _permissionService = permissionService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            var userId = GetUserId(context.User);
            if (!userId.HasValue)
                return;

            // tenantId can be null for admin reads; permissions can still be checked globally/role-based
            var tenantId = _tenantContext.TenantId;

            var ok = await _permissionService.HasPermissionAsync(userId.Value, requirement.PermissionCode, tenantId);
            if (ok)
                context.Succeed(requirement);
        }

        private static Guid? GetUserId(ClaimsPrincipal principal)
        {
            var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }
}