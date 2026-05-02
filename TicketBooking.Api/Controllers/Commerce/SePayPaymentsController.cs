using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Services.Commerce;

namespace TicketBooking.Api.Controllers.Commerce;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments/sepay")]
public sealed class SePayPaymentsController : ControllerBase
{
    private readonly CustomerOrderService _customerOrderService;

    public SePayPaymentsController(CustomerOrderService customerOrderService)
    {
        _customerOrderService = customerOrderService;
    }

    [HttpPost("webhook")]
    [HttpPost("ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(CancellationToken ct = default)
    {
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(ct);
        var authorization = Request.Headers.Authorization.ToString();
        var secret = Request.Headers["X-Secret-Key"].ToString();
        var signature = Request.Headers["X-SePay-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signature))
            signature = Request.Headers["X-Signature"].ToString();

        var handled = await _customerOrderService.HandleGatewayWebhookAsync(rawBody, authorization, secret, signature, ct);
        if (!handled)
            return Unauthorized(new { success = false, message = "Webhook is invalid or unauthorized." });

        return Ok(new { success = true });
    }
}
