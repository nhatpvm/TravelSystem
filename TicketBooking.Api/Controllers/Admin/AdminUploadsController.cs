using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketBooking.Api.Services.Admin;
using TicketBooking.Infrastructure.Identity;

namespace TicketBooking.Api.Controllers.Admin;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/admin/uploads")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminUploadsController : ControllerBase
{
    private readonly AdminImageUploadService _uploadService;

    public AdminUploadsController(AdminImageUploadService uploadService)
    {
        _uploadService = uploadService;
    }

    public sealed class UploadAdminImageRequest
    {
        public IFormFile? File { get; set; }
        public string? Scope { get; set; }
        public string? TenantId { get; set; }
    }

    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(
        [FromForm] UploadAdminImageRequest request,
        CancellationToken ct = default)
    {
        var normalizedScope = AdminImageUploadService.NormalizeScope(request.Scope);
        if (normalizedScope is null)
        {
            return BadRequest(new { message = "Upload scope is invalid." });
        }

        if (!AdminImageUploadService.IsAllowedImage(request.File))
        {
            return BadRequest(new { message = "Only .jpg, .jpeg, .png, .webp images up to 10MB are supported." });
        }

        var stored = await _uploadService.SaveImageAsync(
            request.File!,
            normalizedScope,
            request.TenantId,
            ct);

        var url = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{stored.RelativeUrl}";

        return Ok(new
        {
            url,
            stored.RelativeUrl,
            stored.FileName,
            stored.ContentType,
            stored.SizeBytes
        });
    }
}
