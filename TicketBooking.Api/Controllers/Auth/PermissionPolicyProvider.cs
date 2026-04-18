// FILE #030: TicketBooking.Api/Auth/PermissionPolicyProvider.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using TicketBooking.Infrastructure.Auth;

namespace TicketBooking.Api.Auth;

/// <summary>
/// Allows dynamic policies like "perm:bus.trips.read".
/// </summary>
public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith("perm:", StringComparison.OrdinalIgnoreCase))
        {
            var code = policyName.Substring("perm:".Length).Trim();
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(code))
                .RequireAuthenticatedUser()
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return base.GetPolicyAsync(policyName);
    }
}