//FILE: TicketBooking.Api/Controllers/FlightSearchController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Contracts.Flight;
using TicketBooking.Api.Services.Flight;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/search/flights")]
public sealed class FlightSearchController : ControllerBase
{
    private readonly IFlightPublicQueryService _flightPublicQueryService;

    public FlightSearchController(IFlightPublicQueryService flightPublicQueryService)
    {
        _flightPublicQueryService = flightPublicQueryService;
    }

    /// <summary>
    /// Public flight search.
    /// Supports:
    /// - scoped mode with X-TenantId
    /// - public/global mode without X-TenantId (tenant resolved internally)
    ///
    /// Example:
    /// GET /api/v1/search/flights?from=SGN&amp;to=HAN&amp;date=2026-03-03
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FlightSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] FlightSearchRequest request,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.From) || string.IsNullOrWhiteSpace(request.To))
                return BadRequest(new { message = "Query 'from' and 'to' are required." });

            if (request.From.Trim().Equals(request.To.Trim(), StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "'from' and 'to' must be different." });

            var response = await _flightPublicQueryService.SearchAsync(request, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Public airport lookup for search dropdown/autocomplete.
    /// Supports:
    /// - scoped mode with X-TenantId
    /// - public/global mode without X-TenantId (tenant(s) resolved internally)
    ///
    /// Example:
    /// GET /api/v1/search/flights/airports?q=SGN
    /// GET /api/v1/search/flights/airports?q=Tan Son Nhat
    /// </summary>
    [HttpGet("airports")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FlightAirportLookupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Airports(
        [FromQuery] FlightAirportLookupRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _flightPublicQueryService.LookupAirportsAsync(request, ct);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
