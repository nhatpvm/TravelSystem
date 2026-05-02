using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Services.Admin;
using TicketBooking.Infrastructure.Identity;

namespace TicketBooking.Api.Controllers.Admin;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/ops")]
[Authorize(Policy = "perm:tenants.manage")]
public sealed class AdminOpsController : ControllerBase
{
    private readonly AdminOpsService _service;

    public AdminOpsController(AdminOpsService service)
    {
        _service = service;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AdminOpsOverviewDto>> GetOverview(CancellationToken ct = default)
    {
        return Ok(await _service.GetOverviewAsync(ct));
    }

    [HttpGet("audit-events")]
    public async Task<ActionResult<AdminOpsAuditEventListResponse>> ListAuditEvents(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken ct = default)
    {
        return Ok(await _service.ListAuditEventsAsync(q, page, pageSize, ct));
    }

    [HttpGet("outbox-messages")]
    public async Task<ActionResult<AdminOpsOutboxListResponse>> ListOutboxMessages(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken ct = default)
    {
        return Ok(await _service.ListOutboxMessagesAsync(q, page, pageSize, ct));
    }

    [HttpGet("promo-readiness")]
    public async Task<ActionResult<AdminOpsPromoReadinessDto>> GetPromoReadiness(CancellationToken ct = default)
    {
        return Ok(await _service.GetPromoReadinessAsync(ct));
    }
}
