// FILE #312: TicketBooking.Api/Controllers/Cms/Manager/CmsManagerRedirectsController.cs
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
[Route("api/v{version:apiVersion}/manager/cms/redirects")]
[Authorize(Roles = "Admin,QLNX,QLVT,QLVMM,QLKS,QLTour")]
public sealed class CmsManagerRedirectsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public CmsManagerRedirectsController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 50 : pageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        var tenantId = _tenant.TenantId.Value;

        IQueryable<NewsRedirect> query = _db.Set<NewsRedirect>().AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.FromPath.Contains(keyword) ||
                x.ToPath.Contains(keyword) ||
                (x.Reason != null && x.Reason.Contains(keyword)));
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

        return Ok(new { page, pageSize, total, items });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;

        IQueryable<NewsRedirect> query = _db.Set<NewsRedirect>().AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var entity = await query.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (entity is null)
            return NotFound(new { message = "Redirect not found." });

        return Ok(new
        {
            entity.Id,
            entity.TenantId,
            entity.FromPath,
            entity.ToPath,
            entity.StatusCode,
            entity.IsRegex,
            entity.Reason,
            entity.IsActive,
            entity.IsDeleted,
            entity.CreatedAt,
            entity.CreatedByUserId,
            entity.UpdatedAt,
            entity.UpdatedByUserId
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CmsManagerUpsertRedirectRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var validation = ValidateRequest(req);
        if (validation is not null)
            return validation;

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var fromPath = NormalizePath(req.FromPath);
        var toPath = NormalizePath(req.ToPath);

        var exists = await _db.Set<NewsRedirect>()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.FromPath == fromPath, ct);

        if (exists)
            return Conflict(new { message = "FromPath already exists in this tenant." });

        var entity = new NewsRedirect
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FromPath = fromPath,
            ToPath = toPath,
            StatusCode = req.StatusCode,
            IsRegex = req.IsRegex,
            Reason = NullIfWhite(req.Reason),
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId
        };

        _db.Set<NewsRedirect>().Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CmsManagerUpsertRedirectRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var validation = ValidateRequest(req);
        if (validation is not null)
            return validation;

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsRedirect>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Redirect not found." });

        var fromPath = NormalizePath(req.FromPath);
        var toPath = NormalizePath(req.ToPath);

        var exists = await _db.Set<NewsRedirect>()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.FromPath == fromPath && x.Id != id, ct);

        if (exists)
            return Conflict(new { message = "FromPath already exists in this tenant." });

        entity.FromPath = fromPath;
        entity.ToPath = toPath;
        entity.StatusCode = req.StatusCode;
        entity.IsRegex = req.IsRegex;
        entity.Reason = NullIfWhite(req.Reason);
        entity.IsActive = req.IsActive;

        if (entity.IsDeleted)
            entity.IsDeleted = false;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/set-active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] CmsManagerSetRedirectActiveRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsRedirect>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Redirect not found." });

        entity.IsActive = req.IsActive;

        if (entity.IsDeleted && req.IsActive)
            entity.IsDeleted = false;

        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> SoftDelete(Guid id, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsRedirect>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Redirect not found." });

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

        var entity = await _db.Set<NewsRedirect>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Redirect not found." });

        var dup = await _db.Set<NewsRedirect>()
            .IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.FromPath == entity.FromPath &&
                x.Id != id &&
                !x.IsDeleted, ct);

        if (dup)
            return Conflict(new { message = "Cannot restore because another active redirect already uses the same FromPath." });

        entity.IsDeleted = false;
        entity.IsActive = true;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private IActionResult? ValidateRequest(CmsManagerUpsertRedirectRequest req)
    {
        if (req is null)
            return BadRequest(new { message = "Request body is required." });

        if (string.IsNullOrWhiteSpace(req.FromPath))
            return BadRequest(new { message = "FromPath is required." });

        if (string.IsNullOrWhiteSpace(req.ToPath))
            return BadRequest(new { message = "ToPath is required." });

        var fromPath = NormalizePath(req.FromPath);
        var toPath = NormalizePath(req.ToPath);

        if (fromPath.Length > 500)
            return BadRequest(new { message = "FromPath max length is 500." });

        if (toPath.Length > 1000)
            return BadRequest(new { message = "ToPath max length is 1000." });

        if (req.Reason is not null && req.Reason.Trim().Length > 500)
            return BadRequest(new { message = "Reason max length is 500." });

        if (!req.IsRegex && string.Equals(fromPath, toPath, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "FromPath and ToPath cannot be the same." });

        return null;
    }

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string NormalizePath(string path)
    {
        path = (path ?? "").Trim();
        if (path.Length == 0) return "/";
        if (!path.StartsWith('/')) path = "/" + path;
        return path;
    }

    private static string? NullIfWhite(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CmsManagerUpsertRedirectRequest
{
    public string FromPath { get; set; } = "";
    public string ToPath { get; set; } = "";
    public RedirectStatusCode StatusCode { get; set; } = RedirectStatusCode.MovedPermanently301;
    public bool IsRegex { get; set; } = false;
    public string? Reason { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class CmsManagerSetRedirectActiveRequest
{
    public bool IsActive { get; set; }
}
