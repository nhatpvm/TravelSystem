// FILE #311: TicketBooking.Api/Controllers/Cms/Manager/CmsManagerMediaAssetsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketBooking.Domain.Cms;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/manager/cms/media-assets")]
[Authorize(Roles = "Admin,QLNX,QLVT,QLVMM,QLKS,QLTour")]
public sealed class CmsManagerMediaAssetsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public CmsManagerMediaAssetsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] MediaAssetType? type = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? q = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 20 : pageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        var tenantId = _tenant.TenantId.Value;

        IQueryable<MediaAsset> query = _db.Set<MediaAsset>().AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (type.HasValue)
            query = query.Where(x => x.Type == type.Value);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.FileName.Contains(keyword) ||
                (x.Title != null && x.Title.Contains(keyword)) ||
                (x.AltText != null && x.AltText.Contains(keyword)) ||
                x.StorageKey.Contains(keyword) ||
                (x.PublicUrl != null && x.PublicUrl.Contains(keyword)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
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
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;

        IQueryable<MediaAsset> query = _db.Set<MediaAsset>().AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var entity = await query.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (entity is null)
            return NotFound(new { message = "Media asset not found." });

        var usedAsCoverCount = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.CoverMediaAssetId == id && !x.IsDeleted)
            .CountAsync(ct);

        return Ok(new
        {
            entity.Id,
            entity.TenantId,
            entity.Type,
            entity.FileName,
            entity.Title,
            entity.AltText,
            entity.StorageProvider,
            entity.StorageKey,
            entity.PublicUrl,
            entity.MimeType,
            entity.SizeBytes,
            entity.Width,
            entity.Height,
            entity.ChecksumSha256,
            entity.MetadataJson,
            entity.IsActive,
            entity.IsDeleted,
            entity.CreatedAt,
            entity.CreatedByUserId,
            entity.UpdatedAt,
            entity.UpdatedByUserId,
            UsedAsCoverCount = usedAsCoverCount
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CmsManagerUpsertMediaAssetRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var validation = ValidateRequest(req);
        if (validation is not null)
            return validation;

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

            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId
        };

        _db.Set<MediaAsset>().Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CmsManagerUpsertMediaAssetRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var validation = ValidateRequest(req);
        if (validation is not null)
            return validation;

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<MediaAsset>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Media asset not found." });

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

        entity.IsActive = req.IsActive;

        if (entity.IsDeleted)
            entity.IsDeleted = false;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/set-active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] CmsManagerSetMediaAssetActiveRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<MediaAsset>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Media asset not found." });

        entity.IsActive = req.IsActive;

        if (entity.IsDeleted && req.IsActive)
            entity.IsDeleted = false;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, [FromQuery] bool force = false, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<MediaAsset>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Media asset not found." });

        var usedAsCoverCount = await _db.Set<NewsPost>()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.CoverMediaAssetId == id && !x.IsDeleted)
            .CountAsync(ct);

        if (usedAsCoverCount > 0 && !force)
        {
            return Conflict(new
            {
                message = "Media asset is being used by posts.",
                usedAsCoverCount
            });
        }

        if (force && usedAsCoverCount > 0)
        {
            var posts = await _db.Set<NewsPost>()
                .IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.CoverMediaAssetId == id && !x.IsDeleted)
                .ToListAsync(ct);

            foreach (var post in posts)
            {
                post.CoverMediaAssetId = null;
                post.UpdatedAt = now;
                post.UpdatedByUserId = userId;
            }
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<MediaAsset>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Media asset not found." });

        entity.IsDeleted = false;
        entity.IsActive = true;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private IActionResult? ValidateRequest(CmsManagerUpsertMediaAssetRequest req)
    {
        if (req is null)
            return BadRequest(new { message = "Request body is required." });

        if (string.IsNullOrWhiteSpace(req.FileName))
            return BadRequest(new { message = "FileName is required." });

        if (string.IsNullOrWhiteSpace(req.StorageProvider))
            return BadRequest(new { message = "StorageProvider is required." });

        if (string.IsNullOrWhiteSpace(req.StorageKey))
            return BadRequest(new { message = "StorageKey is required." });

        if (string.IsNullOrWhiteSpace(req.MimeType))
            return BadRequest(new { message = "MimeType is required." });

        if (req.FileName.Trim().Length > 255)
            return BadRequest(new { message = "FileName max length is 255." });

        if (req.Title is not null && req.Title.Trim().Length > 200)
            return BadRequest(new { message = "Title max length is 200." });

        if (req.AltText is not null && req.AltText.Trim().Length > 300)
            return BadRequest(new { message = "AltText max length is 300." });

        if (req.StorageProvider.Trim().Length > 50)
            return BadRequest(new { message = "StorageProvider max length is 50." });

        if (req.StorageKey.Trim().Length > 500)
            return BadRequest(new { message = "StorageKey max length is 500." });

        if (req.PublicUrl is not null && req.PublicUrl.Trim().Length > 1000)
            return BadRequest(new { message = "PublicUrl max length is 1000." });

        if (req.MimeType.Trim().Length > 100)
            return BadRequest(new { message = "MimeType max length is 100." });

        if (req.ChecksumSha256 is not null && req.ChecksumSha256.Trim().Length > 128)
            return BadRequest(new { message = "ChecksumSha256 max length is 128." });

        if (req.SizeBytes < 0)
            return BadRequest(new { message = "SizeBytes must be >= 0." });

        if (req.Width.HasValue && req.Width.Value < 0)
            return BadRequest(new { message = "Width must be >= 0." });

        if (req.Height.HasValue && req.Height.Value < 0)
            return BadRequest(new { message = "Height must be >= 0." });

        return null;
    }

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string? NullIfWhite(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CmsManagerUpsertMediaAssetRequest
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

    public bool IsActive { get; set; } = true;
}

public sealed class CmsManagerSetMediaAssetActiveRequest
{
    public bool IsActive { get; set; }
}
