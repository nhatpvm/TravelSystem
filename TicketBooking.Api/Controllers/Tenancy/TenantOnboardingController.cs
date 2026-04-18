using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Services.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/tenancy/onboarding")]
public sealed class TenantOnboardingController : ControllerBase
{
    private readonly PartnerOnboardingStore _store;

    public TenantOnboardingController(PartnerOnboardingStore store)
    {
        _store = store;
    }

    public sealed class SubmitTenantOnboardingRequest
    {
        [FromForm(Name = "serviceType")]
        public string ServiceType { get; set; } = "";

        [FromForm(Name = "businessName")]
        public string BusinessName { get; set; } = "";

        [FromForm(Name = "taxCode")]
        public string TaxCode { get; set; } = "";

        [FromForm(Name = "address")]
        public string Address { get; set; } = "";

        [FromForm(Name = "contactEmail")]
        public string? ContactEmail { get; set; }

        [FromForm(Name = "contactPhone")]
        public string? ContactPhone { get; set; }

        [FromForm(Name = "legalDocument")]
        public IFormFile? LegalDocument { get; set; }
    }

    [HttpPost]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 6_000_000)]
    public async Task<IActionResult> Submit([FromForm] SubmitTenantOnboardingRequest req, CancellationToken ct = default)
    {
        var serviceType = PartnerOnboardingStore.NormalizeServiceType(req.ServiceType);
        if (serviceType is null)
            return BadRequest(new { message = "ServiceType is invalid. Use bus, train, flight, hotel or tour." });

        var businessName = NormalizeRequired(req.BusinessName, 200);
        if (businessName is null)
            return BadRequest(new { message = "BusinessName is required." });

        var taxCode = NormalizeRequired(req.TaxCode, 32);
        if (taxCode is null)
            return BadRequest(new { message = "TaxCode is required." });

        var address = NormalizeRequired(req.Address, 500);
        if (address is null)
            return BadRequest(new { message = "Address is required." });

        if (req.LegalDocument is null)
            return BadRequest(new { message = "LegalDocument is required." });

        if (req.LegalDocument.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "LegalDocument must be 5MB or smaller." });

        if (!PartnerOnboardingStore.IsAllowedDocument(req.LegalDocument))
            return BadRequest(new { message = "LegalDocument must be PDF, JPG or PNG." });

        var result = await _store.SaveAsync(new PartnerOnboardingSubmission
        {
            ServiceType = serviceType,
            BusinessName = businessName,
            TaxCode = taxCode,
            Address = address,
            ContactEmail = NormalizeOptional(req.ContactEmail, 200),
            ContactPhone = NormalizeOptional(req.ContactPhone, 30)
        }, req.LegalDocument, ct);

        return CreatedAtAction(nameof(GetStatus), new { version = "1.0", trackingCode = result.TrackingCode }, result);
    }

    [HttpGet("{trackingCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatus([FromRoute] string trackingCode, CancellationToken ct = default)
    {
        var item = await _store.GetAsync(trackingCode, ct);
        if (item is null)
            return NotFound(new { message = "Onboarding request not found." });

        return Ok(new
        {
            item.TrackingCode,
            item.ServiceType,
            item.BusinessName,
            item.TaxCode,
            item.Address,
            item.ContactEmail,
            item.ContactPhone,
            item.Status,
            item.SubmittedAt,
            LegalDocument = new
            {
                item.LegalDocument.OriginalFileName,
                item.LegalDocument.ContentType,
                item.LegalDocument.SizeBytes
            }
        });
    }

    private static string? NormalizeRequired(string? value, int maxLength)
    {
        var normalized = NormalizeOptional(value, maxLength);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            normalized = normalized[..maxLength];

        return normalized;
    }
}
