using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TicketBooking.Api.Services.Uploads;
using TicketBooking.Infrastructure.Identity;

namespace TicketBooking.Api.Controllers.Uploads;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/customer/uploads")]
[Authorize(Roles = $"{RoleNames.Customer},{RoleNames.Admin}")]
public sealed class CustomerUploadsController : ControllerBase
{
    private readonly PortalImageUploadService _uploadService;

    public CustomerUploadsController(PortalImageUploadService uploadService)
    {
        _uploadService = uploadService;
    }

    public sealed class UploadCustomerImageRequest
    {
        public IFormFile? File { get; set; }
        public string? Scope { get; set; }
    }

    [HttpPost("images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(
        [FromForm] UploadCustomerImageRequest request,
        CancellationToken ct = default)
    {
        var normalizedScope = PortalImageUploadService.NormalizeCustomerScope(request.Scope);
        if (normalizedScope is null)
        {
            return BadRequest(new { message = "Upload scope is invalid." });
        }

        if (!PortalImageUploadService.IsAllowedImage(request.File))
        {
            return BadRequest(new { message = "Only .jpg, .jpeg, .png, .webp images up to 10MB are supported." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(userId, out var parsedUserId) || parsedUserId == Guid.Empty)
        {
            return Unauthorized(new { message = "Invalid token subject." });
        }

        var stored = await _uploadService.SaveCustomerImageAsync(
            request.File!,
            normalizedScope,
            parsedUserId.ToString(),
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
