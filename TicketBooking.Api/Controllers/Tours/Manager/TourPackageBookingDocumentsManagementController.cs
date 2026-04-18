using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Services.Tours;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Tours;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ql-tour/tours/{tourId:guid}/package-bookings/{bookingId:guid}")]
[Route("api/v{version:apiVersion}/admin/tours/{tourId:guid}/package-bookings/{bookingId:guid}")]
[Authorize(Roles = $"{RoleNames.QLTour},{RoleNames.Admin}")]
public sealed class TourPackageBookingDocumentsManagementController : ControllerBase
{
    private readonly TourPackageDocumentService _documentService;
    private readonly ITenantContext _tenantContext;

    public TourPackageBookingDocumentsManagementController(
        TourPackageDocumentService documentService,
        ITenantContext tenantContext)
    {
        _documentService = documentService;
        _tenantContext = tenantContext;
    }

    [HttpGet("itinerary")]
    public async Task<ActionResult<TourPackageCustomerItineraryView>> GetItinerary(
        Guid tourId,
        Guid bookingId,
        CancellationToken ct = default)
    {
        var result = await _documentService.GetItineraryAsync(
            tourId,
            bookingId,
            GetCurrentUserId() ?? _tenantContext.UserId,
            true,
            ct);

        return Ok(result);
    }

    [HttpGet("voucher")]
    public async Task<ActionResult<TourPackageCustomerVoucherView>> GetVoucher(
        Guid tourId,
        Guid bookingId,
        CancellationToken ct = default)
    {
        var result = await _documentService.GetVoucherAsync(
            tourId,
            bookingId,
            GetCurrentUserId() ?? _tenantContext.UserId,
            true,
            ct);

        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
