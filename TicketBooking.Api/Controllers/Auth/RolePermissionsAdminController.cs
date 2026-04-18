// FILE #119: TicketBooking.Api/Controllers/Admin/RolePermissionsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Auth;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Admin
{
    /// <summary>
    /// Admin management for auth.RolePermissions (Identity Role -> Permission).
    /// - Global table (NOT tenant-owned)
    /// - Soft delete + restore
    /// - Unique: (RoleId, PermissionId)
    ///
    /// Use this when you want global RBAC roles (Admin/QLNX/...) to have default permissions.
    /// Tenant-specific fine-grained is handled by tenants.TenantRolePermissions.
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/admin/auth/role-permissions")]
    [Authorize(Roles = "Admin")]
    public sealed class RolePermissionsAdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly RoleManager<AppRole> _roleManager;

        public RolePermissionsAdminController(AppDbContext db, RoleManager<AppRole> roleManager)
        {
            _db = db;
            _roleManager = roleManager;
        }

        public sealed class UpsertRolePermissionRequest
        {
            public Guid RoleId { get; set; }
            public Guid PermissionId { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] Guid? roleId,
            [FromQuery] Guid? permissionId,
            [FromQuery] string? q,
            [FromQuery] bool includeDeleted = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : (pageSize > 200 ? 200 : pageSize);

            IQueryable<RolePermission> query = _db.RolePermissions.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            if (roleId.HasValue && roleId.Value != Guid.Empty)
                query = query.Where(x => x.RoleId == roleId.Value);

            if (permissionId.HasValue && permissionId.Value != Guid.Empty)
                query = query.Where(x => x.PermissionId == permissionId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim().ToUpperInvariant();

                var roleIds = _roleManager.Roles.AsNoTracking()
                    .Where(r => r.Name != null && r.Name.ToUpper().Contains(key))
                    .Select(r => r.Id);

                var permIds = _db.Permissions.AsNoTracking()
                    .Where(p => (!p.IsDeleted) && (p.Code.ToUpper().Contains(key) || p.Name.ToUpper().Contains(key)))
                    .Select(p => p.Id);

                query = query.Where(x => roleIds.Contains(x.RoleId) || permIds.Contains(x.PermissionId));
            }

            var total = await query.CountAsync(ct);

            var rows = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.RoleId,
                    x.PermissionId,
                    x.IsDeleted,
                    x.CreatedAt,
                    x.UpdatedAt
                })
                .ToListAsync(ct);

            var roleMap = await _roleManager.Roles.AsNoTracking()
                .Where(r => rows.Select(x => x.RoleId).Distinct().Contains(r.Id))
                .Select(r => new { r.Id, r.Name })
                .ToDictionaryAsync(r => r.Id, ct);

            var permMap = await _db.Permissions.IgnoreQueryFilters().AsNoTracking()
                .Where(p => rows.Select(x => x.PermissionId).Distinct().Contains(p.Id))
                .Select(p => new { p.Id, p.Code, p.Name, p.IsActive })
                .ToDictionaryAsync(p => p.Id, ct);

            var items = rows.Select(x => new
            {
                x.Id,
                x.RoleId,
                Role = roleMap.TryGetValue(x.RoleId, out var r) ? r : null,
                x.PermissionId,
                Permission = permMap.TryGetValue(x.PermissionId, out var p) ? p : null,
                x.IsDeleted,
                x.CreatedAt,
                x.UpdatedAt
            });

            return Ok(new { page, pageSize, total, items });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
        {
            IQueryable<RolePermission> query = _db.RolePermissions.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            var row = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (row is null)
                return NotFound(new { message = "RolePermission not found." });

            var role = await _roleManager.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.RoleId, ct);
            var perm = await _db.Permissions.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.PermissionId, ct);

            return Ok(new
            {
                row.Id,
                row.RoleId,
                Role = role is null ? null : new { role.Id, role.Name },
                row.PermissionId,
                Permission = perm is null ? null : new { perm.Id, perm.Code, perm.Name, perm.IsActive },
                row.IsDeleted,
                row.CreatedAt,
                row.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UpsertRolePermissionRequest req, CancellationToken ct = default)
        {
            if (req.RoleId == Guid.Empty) return BadRequest(new { message = "RoleId is required." });
            if (req.PermissionId == Guid.Empty) return BadRequest(new { message = "PermissionId is required." });

            var roleExists = await _roleManager.Roles.AsNoTracking().AnyAsync(x => x.Id == req.RoleId, ct);
            if (!roleExists) return BadRequest(new { message = "RoleId not found." });

            var permExists = await _db.Permissions.IgnoreQueryFilters().AnyAsync(x => x.Id == req.PermissionId && !x.IsDeleted, ct);
            if (!permExists) return BadRequest(new { message = "PermissionId not found." });

            var existing = await _db.RolePermissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.RoleId == req.RoleId && x.PermissionId == req.PermissionId, ct);

            if (existing is not null && !existing.IsDeleted)
                return Conflict(new { message = "RolePermission already exists." });

            var now = DateTimeOffset.Now;

            if (existing is not null)
            {
                existing.IsDeleted = false;
                existing.UpdatedAt = now;
                await _db.SaveChangesAsync(ct);
                return Ok(new { ok = true, revived = true, id = existing.Id });
            }

            var link = new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = req.RoleId,
                PermissionId = req.PermissionId,
                IsDeleted = false,
                CreatedAt = now
            };

            _db.RolePermissions.Add(link);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { version = "1.0", id = link.Id }, new { link.Id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpsertRolePermissionRequest req, CancellationToken ct = default)
        {
            var link = await _db.RolePermissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (link is null)
                return NotFound(new { message = "RolePermission not found." });

            if (req.RoleId == Guid.Empty) return BadRequest(new { message = "RoleId is required." });
            if (req.PermissionId == Guid.Empty) return BadRequest(new { message = "PermissionId is required." });

            var roleExists = await _roleManager.Roles.AsNoTracking().AnyAsync(x => x.Id == req.RoleId, ct);
            if (!roleExists) return BadRequest(new { message = "RoleId not found." });

            var permExists = await _db.Permissions.IgnoreQueryFilters().AnyAsync(x => x.Id == req.PermissionId && !x.IsDeleted, ct);
            if (!permExists) return BadRequest(new { message = "PermissionId not found." });

            // Unique (RoleId, PermissionId)
            var dup = await _db.RolePermissions.IgnoreQueryFilters()
                .AnyAsync(x => x.RoleId == req.RoleId && x.PermissionId == req.PermissionId && x.Id != id && !x.IsDeleted, ct);

            if (dup)
                return Conflict(new { message = "Another RolePermission already exists for this RoleId + PermissionId." });

            link.RoleId = req.RoleId;
            link.PermissionId = req.PermissionId;

            if (link.IsDeleted)
                link.IsDeleted = false;

            link.UpdatedAt = DateTimeOffset.Now;

            await _db.SaveChangesAsync(ct);

            return Ok(new { ok = true });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDelete([FromRoute] Guid id, CancellationToken ct = default)
        {
            var link = await _db.RolePermissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (link is null)
                return NotFound(new { message = "RolePermission not found." });

            if (!link.IsDeleted)
            {
                link.IsDeleted = true;
                link.UpdatedAt = DateTimeOffset.Now;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { ok = true });
        }

        [HttpPost("{id:guid}/restore")]
        public async Task<IActionResult> Restore([FromRoute] Guid id, CancellationToken ct = default)
        {
            var link = await _db.RolePermissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (link is null)
                return NotFound(new { message = "RolePermission not found." });

            if (link.IsDeleted)
            {
                var dup = await _db.RolePermissions.IgnoreQueryFilters()
                    .AnyAsync(x => x.RoleId == link.RoleId && x.PermissionId == link.PermissionId && x.Id != id && !x.IsDeleted, ct);

                if (dup)
                    return Conflict(new { message = "Cannot restore: another active RolePermission already exists for this role + permission." });

                link.IsDeleted = false;
                link.UpdatedAt = DateTimeOffset.Now;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { ok = true });
        }
    }
}
