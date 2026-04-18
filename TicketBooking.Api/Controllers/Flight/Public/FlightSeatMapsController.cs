//FILE: TicketBooking.Api/Controllers/FlightSeatMapsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Contracts.Flight;
using TicketBooking.Api.Services.Flight;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/flight")]
public sealed class FlightSeatMapsController : ControllerBase
{
    private readonly IFlightSeatMapPublicQueryService _flightSeatMapPublicQueryService;

    public FlightSeatMapsController(IFlightSeatMapPublicQueryService flightSeatMapPublicQueryService)
    {
        _flightSeatMapPublicQueryService = flightSeatMapPublicQueryService;
    }

    /// <summary>
    /// Public seat-map by offer for customer flow.
    /// Supports:
    /// - scoped mode with X-TenantId
    /// - public/global mode without X-TenantId (tenant resolved internally)
    ///
    /// Resolution strategy is handled by service:
    /// Offer -> OfferSegment.CabinSeatMapId
    /// fallback:
    /// Offer -> Flight -> Aircraft -> AircraftModel + FareClass.CabinClass -> CabinSeatMap
    /// </summary>
    [HttpGet("offers/{offerId:guid}/seat-map")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FlightSeatMapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSeatMapByOffer(
        [FromRoute] Guid offerId,
        CancellationToken ct = default)
    {
        try
        {
            if (offerId == Guid.Empty)
                return BadRequest(new { message = "offerId is required." });

            var request = new FlightSeatMapByOfferRequest
            {
                OfferId = offerId
            };

            var response = await _flightSeatMapPublicQueryService.GetByOfferAsync(request, ct);

            if (response is null)
                return NotFound(new { message = "Seat-map not found for this offer." });

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Public seat-map by CabinSeatMapId.
    /// Supports:
    /// - scoped mode with X-TenantId
    /// - public/global mode without X-TenantId when the seat map can be resolved
    /// </summary>
    [HttpGet("cabin-seat-maps/{cabinSeatMapId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FlightSeatMapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSeatMapById(
        [FromRoute] Guid cabinSeatMapId,
        CancellationToken ct = default)
    {
        try
        {
            if (cabinSeatMapId == Guid.Empty)
                return BadRequest(new { message = "cabinSeatMapId is required." });

            var request = new FlightSeatMapByIdRequest
            {
                CabinSeatMapId = cabinSeatMapId
            };

            var response = await _flightSeatMapPublicQueryService.GetByIdAsync(request, ct);

            if (response is null)
                return NotFound(new { message = "Cabin seat-map not found." });

            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
