using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Services.Uploads;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers.Uploads;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/manager/uploads")]
[Authorize(Roles = $"{RoleNames.Admin},{RoleNames.QLNX},{RoleNames.QLVT},{RoleNames.QLVMM},{RoleNames.QLKS},{RoleNames.QLTour}")]
public sealed class ManagerUploadsController : ControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly PortalImageUploadService _uploadService;

    public ManagerUploadsController(
        ITenantContext tenantContext,
        PortalImageUploadService uploadService)
    {
        _tenantContext = tenantContext;
        _uploadService = uploadService;
    }

    public sealed class UploadManagerImageRequest
    {
        public IFormFile? File { get; set; }
        public string? Scope { get; set; }
    }

    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(
        [FromForm] UploadManagerImageRequest request,
        CancellationToken ct = default)
    {
        var normalizedScope = PortalImageUploadService.NormalizeManagerScope(request.Scope);
        if (normalizedScope is null)
        {
            return BadRequest(new { message = "Upload scope is invalid." });
        }

        if (!PortalImageUploadService.IsAllowedImage(request.File))
        {
            return BadRequest(new { message = "Only .jpg, .jpeg, .png, .webp images up to 10MB are supported." });
        }

        var stored = await _uploadService.SaveManagerImageAsync(
            request.File!,
            normalizedScope,
            _tenantContext.TenantId?.ToString(),
            ct);

        var url = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{stored.RelativeUrl}";

        return Ok(new
        {
            url,
            stored.RelativeUrl,
            stored.StorageKey,
            stored.FileName,
            stored.ContentType,
            stored.SizeBytes
        });
    }
}
