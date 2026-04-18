// FILE #133 (NEW): TicketBooking.Api/Controllers/NewsCategoriesAdminController.cs
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

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/cms/categories")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class NewsCategoriesAdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenant;

    public NewsCategoriesAdminController(AppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    private Guid? CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private IActionResult RequireTenant()
        => BadRequest(new { message = "X-TenantId is required for admin write requests." });

    private static string NormalizeSlug(string slug)
    {
        slug = (slug ?? "").Trim().ToLowerInvariant();
        if (slug.Length == 0) return "";
        var parts = slug.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join('-', parts);
    }

    public sealed class CreateRequest
    {
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Description { get; set; }
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    public sealed class UpdateRequest
    {
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string? Description { get; set; }
        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var query = _db.Set<NewsCategory>().AsQueryable();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenant.HasTenant && _tenant.TenantId.HasValue)
            query = query.Where(x => x.TenantId == _tenant.TenantId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(x =>
                x.Name.Contains(q) ||
                x.Slug.Contains(q) ||
                (x.Description != null && x.Description.Contains(q)));
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
        var query = _db.Set<NewsCategory>().AsQueryable();

        if (includeDeleted)
            query = query.IgnoreQueryFilters();

        if (_tenant.HasTenant && _tenant.TenantId.HasValue)
            query = query.Where(x => x.TenantId == _tenant.TenantId.Value);

        var entity = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound(new { message = "Category not found." });

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
            entity.UpdatedByUserId
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { message = "Name is required." });

        if (string.IsNullOrWhiteSpace(req.Slug))
            return BadRequest(new { message = "Slug is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var slug = NormalizeSlug(req.Slug);

        var exists = await _db.Set<NewsCategory>().IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Slug == slug, ct);

        if (exists)
            return Conflict(new { message = "Slug already exists in this tenant." });

        var entity = new NewsCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = req.Name.Trim(),
            Slug = slug,
            Description = req.Description,
            SortOrder = req.SortOrder,
            IsActive = req.IsActive,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = userId
        };

        _db.Set<NewsCategory>().Add(entity);
        await _db.SaveChangesAsync(ct);

        return Ok(new { id = entity.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRequest req, CancellationToken ct = default)
    {
        if (!_tenant.HasTenant || !_tenant.TenantId.HasValue)
            return RequireTenant();

        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest(new { message = "Name is required." });

        if (string.IsNullOrWhiteSpace(req.Slug))
            return BadRequest(new { message = "Slug is required." });

        var tenantId = _tenant.TenantId.Value;
        var now = DateTimeOffset.Now;
        var userId = CurrentUserId();

        var entity = await _db.Set<NewsCategory>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Category not found." });

        var slug = NormalizeSlug(req.Slug);

        var exists = await _db.Set<NewsCategory>().IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenantId && x.Slug == slug && x.Id != id, ct);

        if (exists)
            return Conflict(new { message = "Slug already exists in this tenant." });

        entity.Name = req.Name.Trim();
        entity.Slug = slug;
        entity.Description = req.Description;
        entity.SortOrder = req.SortOrder;
        entity.IsActive = req.IsActive;

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

        var entity = await _db.Set<NewsCategory>()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Category not found." });

        entity.IsDeleted = true;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
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

        var entity = await _db.Set<NewsCategory>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);

        if (entity is null) return NotFound(new { message = "Category not found." });

        entity.IsDeleted = false;
        entity.UpdatedAt = now;
        entity.UpdatedByUserId = userId;

        await _db.SaveChangesAsync(ct);
        return Ok(new { ok = true });
    }
}

