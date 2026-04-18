using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Services.Tenancy;

namespace TicketBooking.Api.Controllers.Admin;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/admin/tenant-onboarding")]
[Authorize(Roles = "Admin")]
public sealed class AdminTenantOnboardingController : ControllerBase
{
    private readonly PartnerOnboardingStore _store;

    public AdminTenantOnboardingController(PartnerOnboardingStore store)
    {
        _store = store;
    }

    public sealed class ReviewTenantOnboardingRequest
    {
        public string Status { get; set; } = "";
        public string? ReviewerNote { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? q,
        CancellationToken ct = default)
    {
        var items = await _store.ListAsync(status, q, ct);
        var normalizedStatus = PartnerOnboardingStore.NormalizeStatus(status);

        return Ok(new
        {
            total = items.Count,
            pending = items.Count(x => string.Equals(x.Status, "PendingReview", StringComparison.OrdinalIgnoreCase)),
            items = items.Select(x => new
            {
                x.TrackingCode,
                x.ServiceType,
                x.BusinessName,
                x.TaxCode,
                x.Address,
                x.ContactEmail,
                x.ContactPhone,
                x.Status,
                x.SubmittedAt,
                x.ReviewedAt,
                x.ReviewedBy,
                x.ReviewerNote,
                LegalDocument = new
                {
                    x.LegalDocument.OriginalFileName,
                    x.LegalDocument.ContentType,
                    x.LegalDocument.SizeBytes
                }
            }),
            filters = new
            {
                status = normalizedStatus,
                q
            }
        });
    }

    [HttpGet("{trackingCode}")]
    public async Task<IActionResult> GetById([FromRoute] string trackingCode, CancellationToken ct = default)
    {
        var item = await _store.GetAsync(trackingCode, ct);
        if (item is null)
        {
            return NotFound(new { message = "Onboarding request not found." });
        }

        return Ok(item);
    }

    [HttpPost("{trackingCode}/review")]
    public async Task<IActionResult> Review(
        [FromRoute] string trackingCode,
        [FromBody] ReviewTenantOnboardingRequest req,
        CancellationToken ct = default)
    {
        var status = PartnerOnboardingStore.NormalizeStatus(req.Status);
        if (status is null)
        {
            return BadRequest(new { message = "Status is invalid. Use PendingReview, Approved, Rejected or NeedsMoreInfo." });
        }

        var reviewer = User?.Identity?.Name
            ?? User?.Claims.FirstOrDefault(x => x.Type == System.Security.Claims.ClaimTypes.Email)?.Value
            ?? User?.Claims.FirstOrDefault(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var updated = await _store.ReviewAsync(trackingCode, status, reviewer, req.ReviewerNote, ct);
        if (updated is null)
        {
            return NotFound(new { message = "Onboarding request not found." });
        }

        return Ok(new
        {
            updated.TrackingCode,
            updated.Status,
            updated.ReviewedAt,
            updated.ReviewedBy,
            updated.ReviewerNote
        });
    }
}
