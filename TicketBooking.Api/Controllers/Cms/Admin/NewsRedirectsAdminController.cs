// FILE: TicketBooking.Api/Controllers/NewsRedirectsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Cms;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

/// <summary>
/// Phase 7 (CMS/SEO) - Admin CRUD for cms.NewsRedirects
/// - Admin only
/// - Multi-tenant write: requires X-TenantId (handled by TenantContextMiddleware)
/// - Unique: (TenantId, FromPath)
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/cms/news-redirects")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class NewsRedirectsAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ILogger<NewsRedirectsAdminController> _logger;

    public NewsRedirectsAdminController(AppDbContext db, ITenantContext tenant, ILogger<NewsRedirectsAdminController> logger)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q = null,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var query = _db.Set<NewsRedirect>().AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        // If X-TenantId provided => tenant-scoped list
        if (_tenant.HasTenant && _tenant.TenantId.HasValue)
        {
            var tenantId = _tenant.TenantId.Value;
            query = query.Where(x => x.TenantId == tenantId);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim();
            query = query.Where(x =>
                x.FromPath.Contains(k) ||
                x.ToPath.Contains(k) ||
                (x.Reason != null && x.Reason.Contains(k)));
        }

        var items = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.FromPath,
                x.ToPath,
                x.StatusCode,
                x.IsRegex,
                x.Reason,
                x.IsActive,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new { items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = _db.Set<NewsRedirect>().AsNoTracking();
        if (includeDeleted) q = q.IgnoreQueryFilters();

        var entity = await q.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound(new { message = "Redirect not found." });

        // If tenant context exists, enforce it (Admin overview can omit tenant header)
        if (_tenant.HasTenant && _tenant.TenantId.HasValue && entity.TenantId != _tenant.TenantId.Value)
            return NotFound(new { message = "Redirect not found." });

        return Ok(ToDto(entity));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        ValidateOrThrow(req.FromPath, req.ToPath);

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var from = NormalizePath(req.FromPath);
        var to = NormalizePath(req.ToPath);

        var exists = await _db.Set<NewsRedirect>().IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.FromPath == from, ct);

        if (exists)
            return Conflict(new { message = "FromPath already exists in this tenant." });

        var entity = new NewsRedirect
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FromPath = from,
            ToPath = to,
            StatusCode = req.StatusCode ?? RedirectStatusCode.MovedPermanently301,
            IsRegex = req.IsRegex ?? false,
            Reason = NullIfWhite(req.Reason),
            IsActive = req.IsActive ?? true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId
        };

        _db.Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        ValidateOrThrow(req.FromPath, req.ToPath);

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsRedirect>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Redirect not found." });

        var from = NormalizePath(req.FromPath);
        var to = NormalizePath(req.ToPath);

        var exists = await _db.Set<NewsRedirect>().IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.FromPath == from && x.Id != id, ct);

        if (exists)
            return Conflict(new { message = "FromPath already exists in this tenant." });

        entity.FromPath = from;
        entity.ToPath = to;
        entity.StatusCode = req.StatusCode ?? entity.StatusCode;
        entity.IsRegex = req.IsRegex ?? entity.IsRegex;
        entity.Reason = NullIfWhite(req.Reason);
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

        var entity = await _db.Set<NewsRedirect>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Redirect not found." });

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

        var entity = await _db.Set<NewsRedirect>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Redirect not found." });

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

    // -------------------------
    // Helpers
    // -------------------------

    private IActionResult RequireTenant()
        => BadRequest(new { message = "Missing tenant context. Please provide X-TenantId." });

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirst("sub")?.Value ?? User.FindFirst("uid")?.Value;
        if (Guid.TryParse(raw, out var id)) return id;
        return null;
    }

    private static void ValidateOrThrow(string? fromPath, string? toPath)
    {
        if (string.IsNullOrWhiteSpace(fromPath))
            throw new ArgumentException("FromPath is required.");

        if (string.IsNullOrWhiteSpace(toPath))
            throw new ArgumentException("ToPath is required.");
    }

    private static string NormalizePath(string path)
    {
        path = path.Trim();
        if (!path.StartsWith('/')) path = "/" + path;
        // keep case as-is; websites may be case-sensitive depending on routing; but most VN sites use lowercase slugs.
        // You can uncomment to force lowercase:
        // path = path.ToLowerInvariant();
        return path;
    }

    private static string? NullIfWhite(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static object ToDto(NewsRedirect x) => new
    {
        x.Id,
        x.TenantId,
        x.FromPath,
        x.ToPath,
        x.StatusCode,
        x.IsRegex,
        x.Reason,
        x.IsActive,
        x.IsDeleted,
        x.CreatedAt,
        x.CreatedByUserId,
        x.UpdatedAt,
        x.UpdatedByUserId
    };

    // -------------------------
    // DTOs
    // -------------------------

    public sealed class CreateRequest
    {
        public string FromPath { get; set; } = "";
        public string ToPath { get; set; } = "";

        public RedirectStatusCode? StatusCode { get; set; }
        public bool? IsRegex { get; set; }
        public string? Reason { get; set; }
        public bool? IsActive { get; set; }
    }

    public sealed class UpdateRequest
    {
        public string FromPath { get; set; } = "";
        public string ToPath { get; set; } = "";

        public RedirectStatusCode? StatusCode { get; set; }
        public bool? IsRegex { get; set; }
        public string? Reason { get; set; }
        public bool? IsActive { get; set; }
    }
}

