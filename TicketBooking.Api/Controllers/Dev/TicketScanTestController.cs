// FILE #034: TicketBooking.Api/Controllers/TicketScanTestController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize]
[Route("api/v{version:apiVersion}/ticket-scan-test")]
public sealed class TicketScanTestController : ControllerBase
{
    /// <summary>
    /// Requires permission: ticket.scan
    /// Used to verify tenant role NX_TicketScanner works.
    /// </summary>
    [HttpPost("scan")]
    [Authorize(Policy = "perm:ticket.scan")]
    public IActionResult Scan()
    {
        return Ok(new { ok = true, permission = "ticket.scan", message = "Scan permitted." });
    }
}