// FILE: TicketBooking.Api/Controllers/MediaAssetsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketBooking.Domain.Cms;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

/// <summary>
/// Phase 7 (CMS/SEO) - Admin CRUD for cms.MediaAssets
/// - Admin only
/// - Multi-tenant write requires X-TenantId
/// - This controller manages metadata only (upload/storage integration will be in Phase Files module later)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/cms/media-assets")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class MediaAssetsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public MediaAssetsAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private IActionResult RequireTenant()
        => BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? NullIfWhite(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] MediaAssetType? type = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.Set<MediaAsset>().AsNoTracking();

        if (includeDeleted)
            q = q.IgnoreQueryFilters();

        if (_tenant.HasTenant && _tenant.TenantId.HasValue)
            q = q.Where(x => x.TenantId == _tenant.TenantId.Value);

        if (type.HasValue)
            q = q.Where(x => x.Type == type.Value);

        if (isActive.HasValue)
            q = q.Where(x => x.IsActive == isActive.Value);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.Type,
                x.FileName,
                x.Title,
                x.AltText,
                x.StorageProvider,
                x.StorageKey,
                x.PublicUrl,
                x.MimeType,
                x.SizeBytes,
                x.Width,
                x.Height,
                x.ChecksumSha256,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = _db.Set<MediaAsset>().AsNoTracking();
        if (includeDeleted) q = q.IgnoreQueryFilters();

        var entity = await q.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound(new { message = "Media asset not found." });

        if (_tenant.HasTenant && _tenant.TenantId.HasValue && entity.TenantId != _tenant.TenantId.Value)
            return NotFound(new { message = "Media asset not found." });

        return Ok(entity);
    }

    public sealed class CreateRequest
    {
        public MediaAssetType Type { get; set; } = MediaAssetType.Image;

        public string FileName { get; set; } = "";
        public string StorageProvider { get; set; } = "local";
        public string StorageKey { get; set; } = "";

        public string? Title { get; set; }
        public string? AltText { get; set; }
        public string? PublicUrl { get; set; }

        public string MimeType { get; set; } = "application/octet-stream";
        public long SizeBytes { get; set; }

        public int? Width { get; set; }
        public int? Height { get; set; }

        public string? ChecksumSha256 { get; set; }
        public string? MetadataJson { get; set; }

        public bool? IsActive { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        if (string.IsNullOrWhiteSpace(req.FileName))
            return BadRequest(new { message = "FileName is required." });

        if (string.IsNullOrWhiteSpace(req.StorageProvider))
            return BadRequest(new { message = "StorageProvider is required." });

        if (string.IsNullOrWhiteSpace(req.StorageKey))
            return BadRequest(new { message = "StorageKey is required." });

        if (string.IsNullOrWhiteSpace(req.MimeType))
            return BadRequest(new { message = "MimeType is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = new MediaAsset
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = req.Type,

            FileName = req.FileName.Trim(),
            Title = NullIfWhite(req.Title),
            AltText = NullIfWhite(req.AltText),

            StorageProvider = req.StorageProvider.Trim(),
            StorageKey = req.StorageKey.Trim(),
            PublicUrl = NullIfWhite(req.PublicUrl),

            MimeType = req.MimeType.Trim(),
            SizeBytes = req.SizeBytes,
            Width = req.Width,
            Height = req.Height,
            ChecksumSha256 = NullIfWhite(req.ChecksumSha256),
            MetadataJson = string.IsNullOrWhiteSpace(req.MetadataJson) ? null : req.MetadataJson,

            IsActive = req.IsActive ?? true,

            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, id = entity.Id });
    }

    public sealed class UpdateRequest
    {
        public MediaAssetType Type { get; set; } = MediaAssetType.Image;

        public string FileName { get; set; } = "";
        public string StorageProvider { get; set; } = "local";
        public string StorageKey { get; set; } = "";

        public string? Title { get; set; }
        public string? AltText { get; set; }
        public string? PublicUrl { get; set; }

        public string MimeType { get; set; } = "application/octet-stream";
        public long SizeBytes { get; set; }

        public int? Width { get; set; }
        public int? Height { get; set; }

        public string? ChecksumSha256 { get; set; }
        public string? MetadataJson { get; set; }

        public bool? IsActive { get; set; }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        if (string.IsNullOrWhiteSpace(req.FileName))
            return BadRequest(new { message = "FileName is required." });

        if (string.IsNullOrWhiteSpace(req.StorageProvider))
            return BadRequest(new { message = "StorageProvider is required." });

        if (string.IsNullOrWhiteSpace(req.StorageKey))
            return BadRequest(new { message = "StorageKey is required." });

        if (string.IsNullOrWhiteSpace(req.MimeType))
            return BadRequest(new { message = "MimeType is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<MediaAsset>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Media asset not found." });

        entity.Type = req.Type;

        entity.FileName = req.FileName.Trim();
        entity.Title = NullIfWhite(req.Title);
        entity.AltText = NullIfWhite(req.AltText);

        entity.StorageProvider = req.StorageProvider.Trim();
        entity.StorageKey = req.StorageKey.Trim();
        entity.PublicUrl = NullIfWhite(req.PublicUrl);

        entity.MimeType = req.MimeType.Trim();
        entity.SizeBytes = req.SizeBytes;
        entity.Width = req.Width;
        entity.Height = req.Height;

        entity.ChecksumSha256 = NullIfWhite(req.ChecksumSha256);
        entity.MetadataJson = string.IsNullOrWhiteSpace(req.MetadataJson) ? null : req.MetadataJson;

        if (req.IsActive.HasValue) entity.IsActive = req.IsActive.Value;

        if (entity.IsDeleted) entity.IsDeleted = false;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<MediaAsset>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Media asset not found." });

        if (!entity.IsDeleted)
        {
            entity.IsDeleted = true;
            entity.IsActive = false;
            entity.UpdatedAt = now;
            entity.UpdatedByUserId = userId;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<MediaAsset>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Media asset not found." });

        if (entity.IsDeleted)
        {
            entity.IsDeleted = false;
            entity.IsActive = true;
            entity.UpdatedAt = now;
            entity.UpdatedByUserId = userId;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true });
    }
}

