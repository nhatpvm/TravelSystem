//FILE: TicketBooking.Api/Controllers/FlightOfferDetailsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Contracts.Flight;
using TicketBooking.Api.Services.Flight;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/flight/offers")]
public sealed class FlightOfferDetailsController : ControllerBase
{
    private readonly IFlightPublicQueryService _flightPublicQueryService;

    public FlightOfferDetailsController(IFlightPublicQueryService flightPublicQueryService)
    {
        _flightPublicQueryService = flightPublicQueryService;
    }

    /// <summary>
    /// Public offer details for customer flow.
    /// Supports:
    /// - scoped mode with X-TenantId
    /// - public/global mode without X-TenantId (tenant resolved internally)
    ///
    /// Example:
    /// GET /api/v1/flight/offers/{offerId}
    /// GET /api/v1/flight/offers/{offerId}?includeExpired=true
    /// </summary>
    [HttpGet("{offerId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FlightOfferDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOfferDetails(
        [FromRoute] Guid offerId,
        [FromQuery] bool includeExpired = false,
        CancellationToken ct = default)
    {
        try
        {
            if (offerId == Guid.Empty)
                return BadRequest(new { message = "offerId is required." });

            var request = new FlightOfferDetailsRequest
            {
                OfferId = offerId,
                IncludeExpired = includeExpired
            };

            var response = await _flightPublicQueryService.GetOfferDetailsAsync(request, ct);

            if (response is null)
                return NotFound(new { message = "Offer not found or expired." });

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
