//FILE: TicketBooking.Api/Controllers/FlightOfferAncillariesController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Contracts.Flight;
using TicketBooking.Api.Services.Flight;
using TicketBooking.Domain.Flight;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/flight/offers")]
public sealed class FlightOfferAncillariesController : ControllerBase
{
    private readonly IFlightOfferAncillaryQueryService _flightOfferAncillaryQueryService;

    public FlightOfferAncillariesController(
        IFlightOfferAncillaryQueryService flightOfferAncillaryQueryService)
    {
        _flightOfferAncillaryQueryService = flightOfferAncillaryQueryService;
    }

    /// <summary>
    /// Public ancillary options for a specific flight offer.
    /// Examples:
    /// GET /api/v1/flight/offers/{offerId}/ancillaries
    /// GET /api/v1/flight/offers/{offerId}/ancillaries?type=Meal
    /// GET /api/v1/flight/offers/{offerId}/ancillaries?includeInactive=true
    /// GET /api/v1/flight/offers/{offerId}/ancillaries?includeExpired=true
    /// </summary>
    [HttpGet("{offerId:guid}/ancillaries")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FlightOfferAncillaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByOffer(
        [FromRoute] Guid offerId,
        [FromQuery] AncillaryType? type,
        [FromQuery] bool includeInactive = false,
        [FromQuery] bool includeExpired = false,
        CancellationToken ct = default)
    {
        try
        {
            if (offerId == Guid.Empty)
                return BadRequest(new { message = "offerId is required." });

            var request = new FlightOfferAncillaryRequest
            {
                OfferId = offerId,
                Type = type,
                IncludeInactive = includeInactive,
                IncludeExpired = includeExpired
            };

            var response = await _flightOfferAncillaryQueryService.GetByOfferAsync(request, ct);

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
