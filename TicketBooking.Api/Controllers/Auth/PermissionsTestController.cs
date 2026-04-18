// FILE #031: TicketBooking.Api/Controllers/PermissionsTestController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/perm-test")]
public sealed class PermissionsTestController : ControllerBase
{
    /// <summary>
    /// Requires permission: bus.trips.read
    /// Call:
    /// - Login -> Authorize (Bearer token)
    /// - For non-admin multi-tenant users, provide X-TenantId if needed
    /// </summary>
    [HttpGet("bus-trips-read")]
    [Authorize(Policy = "perm:bus.trips.read")]
    public IActionResult BusTripsRead()
    {
        return Ok(new { ok = true, permission = "bus.trips.read" });
    }

    /// <summary>
    /// Requires permission: tenants.manage
    /// </summary>
    [HttpGet("tenants-manage")]
    [Authorize(Policy = "perm:tenants.manage")]
    public IActionResult TenantsManage()
    {
        return Ok(new { ok = true, permission = "tenants.manage" });
    }

    /// <summary>
    /// Requires permission: cms.posts.publish
    /// </summary>
    [HttpGet("cms-posts-publish")]
    [Authorize(Policy = "perm:cms.posts.publish")]
    public IActionResult CmsPostsPublish()
    {
        return Ok(new { ok = true, permission = "cms.posts.publish" });
    }
}