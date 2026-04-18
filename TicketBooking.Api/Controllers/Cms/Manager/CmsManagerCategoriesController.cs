// FILE #309: TicketBooking.Api/Controllers/Cms/Manager/CmsManagerCategoriesController.cs
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
[Route("api/v{version:apiVersion}/manager/cms/categories")]
[Authorize(Roles = "Admin,QLNX,QLVT,QLVMM,QLKS,QLTour")]
public sealed class CmsManagerCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public CmsManagerCategoriesController(AppDbContext db, ITenantContext tenant)
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

        IQueryable<NewsCategory> query = _db.Set<NewsCategory>().AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        query = query.Where(x => x.TenantId == tenantId);

        if (isActive.HasValue)
            query = query.Where(x => x.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(x =>
                x.Name.Contains(keyword) ||
                x.Slug.Contains(keyword) ||
                (x.Description != null && x.Description.Contains(keyword)));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TenantId,
                x.Name,
                x.Slug,
                x.Description,
                x.SortOrder,
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

        IQueryable<NewsCategory> query = _db.Set<NewsCategory>().AsNoTracking();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        var entity = await query.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (entity is null)
            return NotFound(new { message = "Category not found." });

        var postCount = await _db.Set<NewsPostCategory>()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.CategoryId == id && !x.IsDeleted)
            .Select(x => x.PostId)
            .Distinct()
            .CountAsync(ct);

        return Ok(new
        {
            entity.Id,
            entity.TenantId,
            entity.Name,
            entity.Slug,
            entity.Description,
            entity.SortOrder,
            entity.IsActive,
            entity.IsDeleted,
            entity.CreatedAt,
            entity.CreatedByUserId,
            entity.UpdatedAt,
            entity.UpdatedByUserId,
            PostCount = postCount
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CmsManagerUpsertCategoryRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var validation = ValidateRequest(req);
        if (validation is not null)
            return validation;

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();
        var slug = NormalizeSlug(req.Slug);

        var exists = await _db.Set<NewsCategory>()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Slug == slug, ct);

        if (exists)
            return Conflict(new { message = "Slug already exists in this tenant." });

        var entity = new NewsCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = req.Name.Trim(),
            Slug = slug,
            Description = NullIfWhite(req.Description),
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId
        };

        _db.Set<NewsCategory>().Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true, id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CmsManagerUpsertCategoryRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return BadRequest(new { message = "X-TenantId is required." });

        var validation = ValidateRequest(req);
        if (validation is not null)
            return validation;

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();
        var slug = NormalizeSlug(req.Slug);

        var entity = await _db.Set<NewsCategory>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Category not found." });

        var exists = await _db.Set<NewsCategory>()
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Slug == slug && x.Id != id, ct);

        if (exists)
            return Conflict(new { message = "Slug already exists in this tenant." });

        entity.Name = req.Name.Trim();
        entity.Slug = slug;
        entity.Description = NullIfWhite(req.Description);
        entity.SortOrder = req.SortOrder;
        entity.IsActive = req.IsActive;

        if (entity.IsDeleted)
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

        var entity = await _db.Set<NewsCategory>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Category not found." });

        var linkedPostCount = await _db.Set<NewsPostCategory>()
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.CategoryId == id && !x.IsDeleted)
            .CountAsync(ct);

        if (linkedPostCount > 0 && !force)
        {
            return Conflict(new
            {
                message = "Category is still linked to posts.",
                linkedPostCount
            });
        }

        if (force && linkedPostCount > 0)
        {
            var mappings = await _db.Set<NewsPostCategory>()
                .IgnoreQueryFilters()
                .Where(x => x.TenantId == tenantId && x.CategoryId == id && !x.IsDeleted)
                .ToListAsync(ct);

            foreach (var mapping in mappings)
            {
                mapping.IsDeleted = true;
                mapping.UpdatedAt = now;
                mapping.UpdatedByUserId = userId;
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

        var entity = await _db.Set<NewsCategory>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null)
            return NotFound(new { message = "Category not found." });

        var dup = await _db.Set<NewsCategory>()
            .IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.Slug == entity.Slug &&
                x.Id != id &&
                !x.IsDeleted, ct);

        if (dup)
            return Conflict(new { message = "Cannot restore because another active category already uses the same slug." });

        entity.IsDeleted = false;
        entity.IsActive = true;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }

    private IActionResult? ValidateRequest(CmsManagerUpsertCategoryRequest req)
    {
        if (req is null)
            return BadRequest(new { message = "Request body is required." });

        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { message = "Name is required." });

        if (string.IsNullOrWhiteSpace(req.Slug))
            return BadRequest(new { message = "Slug is required." });

        if (req.Name.Trim().Length > 200)
            return BadRequest(new { message = "Name max length is 200." });

        if (NormalizeSlug(req.Slug).Length > 200)
            return BadRequest(new { message = "Slug max length is 200." });

        if (req.Description is not null && req.Description.Trim().Length > 2000)
            return BadRequest(new { message = "Description max length is 2000." });

        return null;
    }

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static string NormalizeSlug(string slug)
    {
        slug = (slug ?? "").Trim().ToLowerInvariant();
        if (slug.Length == 0) return "";

        var parts = slug.Split(new[] { ' ', '\t', '\r', '\n', '/' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join('-', parts);
    }

    private static string? NullIfWhite(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class CmsManagerUpsertCategoryRequest
{
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
