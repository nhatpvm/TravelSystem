// FILE #118: TicketBooking.Api/Controllers/Admin/PermissionsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Auth;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Admin
{
    /// <summary>
    /// Admin CRUD for auth.Permissions
    /// - Global table (NOT tenant-owned)
    /// - Soft delete + restore
    /// - Unique: Code
    ///
    /// Permission code format (you chose):
    ///   bus.trips.read, bus.trips.write, tenants.manage, cms.posts.publish, ...
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/admin/auth/permissions")]
    [Authorize(Policy = "perm:tenants.manage")]
    public sealed class PermissionsAdminController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PermissionsAdminController(AppDbContext db)
        {
            _db = db;
        }

        public sealed class PermissionUpsertRequest
        {
            public string Code { get; set; } = "";
            public string Name { get; set; } = "";
            public string? Description { get; set; }
            public string? Category { get; set; }
            public int SortOrder { get; set; } = 0;
            public bool IsActive { get; set; } = true;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? q,
            [FromQuery] string? category,
            [FromQuery] bool? isActive,
            [FromQuery] bool includeDeleted = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : (pageSize > 200 ? 200 : pageSize);

            IQueryable<Permission> query = _db.Permissions.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            if (!string.IsNullOrWhiteSpace(category))
            {
                var cat = category.Trim();
                query = query.Where(x => x.Category != null && x.Category == cat);
            }

            if (isActive.HasValue)
                query = query.Where(x => x.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim().ToUpperInvariant();
                query = query.Where(x =>
                    x.Code.ToUpper().Contains(key) ||
                    x.Name.ToUpper().Contains(key) ||
                    (x.Description != null && x.Description.ToUpper().Contains(key)) ||
                    (x.Category != null && x.Category.ToUpper().Contains(key)));
            }

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderBy(x => x.Category)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.Name,
                    x.Description,
                    x.Category,
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
        public async Task<IActionResult> GetById([FromRoute] Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
        {
            IQueryable<Permission> query = _db.Permissions.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            var item = await query
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.Name,
                    x.Description,
                    x.Category,
                    x.SortOrder,
                    x.IsActive,
                    x.IsDeleted,
                    x.CreatedAt,
                    x.UpdatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (item is null)
                return NotFound(new { message = "Permission not found." });

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PermissionUpsertRequest req, CancellationToken ct = default)
        {
            var code = NormalizeCode(req.Code);
            var name = NormalizeName(req.Name);

            if (code.Length == 0)
                return BadRequest(new { message = "Code is required." });

            if (name.Length == 0)
                return BadRequest(new { message = "Name is required." });

            var exists = await _db.Permissions.IgnoreQueryFilters()
                .AnyAsync(x => x.Code == code, ct);

            if (exists)
                return Conflict(new { message = $"Permission code '{code}' already exists." });

            var now = DateTimeOffset.Now;

            var p = new Permission
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                Description = TrimOrNull(req.Description, 1000),
                Category = TrimOrNull(req.Category, 100),
                SortOrder = req.SortOrder,
                IsActive = req.IsActive,
                IsDeleted = false,
                CreatedAt = now
            };

            _db.Permissions.Add(p);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { version = "1.0", id = p.Id }, new { p.Id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] PermissionUpsertRequest req, CancellationToken ct = default)
        {
            var p = await _db.Permissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (p is null)
                return NotFound(new { message = "Permission not found." });

            var code = NormalizeCode(req.Code);
            var name = NormalizeName(req.Name);

            if (code.Length == 0)
                return BadRequest(new { message = "Code is required." });

            if (name.Length == 0)
                return BadRequest(new { message = "Name is required." });

            var dup = await _db.Permissions.IgnoreQueryFilters()
                .AnyAsync(x => x.Code == code && x.Id != id && !x.IsDeleted, ct);

            if (dup)
                return Conflict(new { message = $"Another Permission already uses code '{code}'." });

            p.Code = code;
            p.Name = name;
            p.Description = TrimOrNull(req.Description, 1000);
            p.Category = TrimOrNull(req.Category, 100);
            p.SortOrder = req.SortOrder;
            p.IsActive = req.IsActive;

            if (p.IsDeleted)
                p.IsDeleted = false;

            p.UpdatedAt = DateTimeOffset.Now;

            await _db.SaveChangesAsync(ct);

            return Ok(new { ok = true });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDelete([FromRoute] Guid id, CancellationToken ct = default)
        {
            var p = await _db.Permissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (p is null)
                return NotFound(new { message = "Permission not found." });

            if (!p.IsDeleted)
            {
                p.IsDeleted = true;
                p.UpdatedAt = DateTimeOffset.Now;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { ok = true });
        }

        [HttpPost("{id:guid}/restore")]
        public async Task<IActionResult> Restore([FromRoute] Guid id, CancellationToken ct = default)
        {
            var p = await _db.Permissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (p is null)
                return NotFound(new { message = "Permission not found." });

            if (p.IsDeleted)
            {
                var dup = await _db.Permissions.IgnoreQueryFilters()
                    .AnyAsync(x => x.Code == p.Code && x.Id != id && !x.IsDeleted, ct);

                if (dup)
                    return Conflict(new { message = $"Cannot restore: another active Permission already uses code '{p.Code}'." });

                p.IsDeleted = false;
                p.UpdatedAt = DateTimeOffset.Now;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { ok = true });
        }

        private static string NormalizeCode(string? code)
        {
            code = (code ?? "").Trim();
            if (code.Length == 0) return "";
            if (code.Length > 200) code = code[..200];
            return code;
        }

        private static string NormalizeName(string? name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) return "";
            if (name.Length > 200) name = name[..200];

            // Viết hoa chữ cái đầu
            return string.Join(' ', name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p[1..].ToLowerInvariant() : "")));
        }

        private static string? TrimOrNull(string? input, int maxLen)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var s = input.Trim();
            if (s.Length > maxLen) s = s[..maxLen];
            return s;
        }
    }
}
