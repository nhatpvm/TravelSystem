using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Services.Commerce;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Commerce;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/tenant/commerce")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX},{RoleNames.QLVT},{RoleNames.QLVMM},{RoleNames.QLKS},{RoleNames.QLTour}")]
public sealed class TenantCommerceController : ControllerBase
{
    private readonly CommerceBackofficeService _service;
    private readonly ITenantContext _tenantContext;

    public TenantCommerceController(
        CommerceBackofficeService service,
        ITenantContext tenantContext)
    {
        _service = service;
        _tenantContext = tenantContext;
    }

    [HttpGet("finance")]
    public async Task<ActionResult<TenantFinanceDashboardDto>> GetFinance(CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Can gui X-TenantId de xem tai chinh tenant." });

        return Ok(await _service.GetTenantFinanceDashboardAsync(_tenantContext.TenantId!.Value, ct));
    }

    [HttpPut("payout-account")]
    public async Task<ActionResult<TenantPayoutAccountDto>> UpsertPayoutAccount(
        [FromBody] UpsertTenantPayoutAccountRequest request,
        CancellationToken ct = default)
    {
        if (!_tenantContext.HasTenant)
            return BadRequest(new { message = "Can gui X-TenantId de cap nhat tai khoan doi soat." });

        return Ok(await _service.UpsertTenantPayoutAccountAsync(_tenantContext.TenantId!.Value, request, GetCurrentUserId(), ct));
    }

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
