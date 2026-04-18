// FILE #120: TicketBooking.Api/Controllers/Admin/TenantRolePermissionsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Admin
{
    /// <summary>
    /// Admin management for tenants.TenantRolePermissions (TenantRole -> Permission).
    /// - Soft delete + restore
    /// - Unique: (TenantId, TenantRoleId, PermissionId)
    /// - Validates that TenantRole belongs to TenantId
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/admin/tenants/role-permissions")]
    [Authorize(Roles = "Admin")]
    public sealed class TenantRolePermissionsAdminController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TenantRolePermissionsAdminController(AppDbContext db)
        {
            _db = db;
        }

        public sealed class UpsertTenantRolePermissionRequest
        {
            public Guid TenantId { get; set; }
            public Guid TenantRoleId { get; set; }
            public Guid PermissionId { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] Guid? tenantId,
            [FromQuery] Guid? tenantRoleId,
            [FromQuery] Guid? permissionId,
            [FromQuery] string? q,
            [FromQuery] bool includeDeleted = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : (pageSize > 200 ? 200 : pageSize);

            IQueryable<TenantRolePermission> query = _db.TenantRolePermissions.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
                query = query.Where(x => x.TenantId == tenantId.Value);

            if (tenantRoleId.HasValue && tenantRoleId.Value != Guid.Empty)
                query = query.Where(x => x.TenantRoleId == tenantRoleId.Value);

            if (permissionId.HasValue && permissionId.Value != Guid.Empty)
                query = query.Where(x => x.PermissionId == permissionId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim().ToUpperInvariant();

                var tenantIds = _db.Tenants.AsNoTracking()
                    .Where(t => (!t.IsDeleted) && (t.Code.ToUpper().Contains(key) || t.Name.ToUpper().Contains(key)))
                    .Select(t => t.Id);

                var roleIds = _db.TenantRoles.AsNoTracking()
                    .Where(r => (!r.IsDeleted) && (r.Code.ToUpper().Contains(key) || r.Name.ToUpper().Contains(key)))
                    .Select(r => r.Id);

                var permIds = _db.Permissions.AsNoTracking()
                    .Where(p => (!p.IsDeleted) && (p.Code.ToUpper().Contains(key) || p.Name.ToUpper().Contains(key)))
                    .Select(p => p.Id);

                query = query.Where(x => tenantIds.Contains(x.TenantId) || roleIds.Contains(x.TenantRoleId) || permIds.Contains(x.PermissionId));
            }

            var total = await query.CountAsync(ct);

            var rows = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.TenantId,
                    x.TenantRoleId,
                    x.PermissionId,
                    x.IsDeleted,
                    x.CreatedAt,
                    x.UpdatedAt
                })
                .ToListAsync(ct);

            var tenantMap = await _db.Tenants.IgnoreQueryFilters().AsNoTracking()
                .Where(t => rows.Select(r => r.TenantId).Distinct().Contains(t.Id))
                .Select(t => new { t.Id, t.Code, t.Name, t.Type, t.Status })
                .ToDictionaryAsync(t => t.Id, ct);

            var roleMap = await _db.TenantRoles.IgnoreQueryFilters().AsNoTracking()
                .Where(r => rows.Select(r => r.TenantRoleId).Distinct().Contains(r.Id))
                .Select(r => new { r.Id, r.TenantId, r.Code, r.Name, r.IsActive })
                .ToDictionaryAsync(r => r.Id, ct);

            var permMap = await _db.Permissions.IgnoreQueryFilters().AsNoTracking()
                .Where(p => rows.Select(r => r.PermissionId).Distinct().Contains(p.Id))
                .Select(p => new { p.Id, p.Code, p.Name, p.IsActive })
                .ToDictionaryAsync(p => p.Id, ct);

            var items = rows.Select(r => new
            {
                r.Id,
                r.TenantId,
                Tenant = tenantMap.TryGetValue(r.TenantId, out var t) ? t : null,
                r.TenantRoleId,
                TenantRole = roleMap.TryGetValue(r.TenantRoleId, out var tr) ? tr : null,
                r.PermissionId,
                Permission = permMap.TryGetValue(r.PermissionId, out var p) ? p : null,
                r.IsDeleted,
                r.CreatedAt,
                r.UpdatedAt
            });

            return Ok(new { page, pageSize, total, items });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
        {
            IQueryable<TenantRolePermission> query = _db.TenantRolePermissions.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            var row = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (row is null)
                return NotFound(new { message = "TenantRolePermission not found." });

            var tenant = await _db.Tenants.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.TenantId, ct);
            var role = await _db.TenantRoles.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.TenantRoleId, ct);
            var perm = await _db.Permissions.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.PermissionId, ct);

            return Ok(new
            {
                row.Id,
                row.TenantId,
                Tenant = tenant is null ? null : new { tenant.Id, tenant.Code, tenant.Name, Type = tenant.Type.ToString(), Status = tenant.Status.ToString() },
                row.TenantRoleId,
                TenantRole = role is null ? null : new { role.Id, role.TenantId, role.Code, role.Name, role.IsActive },
                row.PermissionId,
                Permission = perm is null ? null : new { perm.Id, perm.Code, perm.Name, perm.IsActive },
                row.IsDeleted,
                row.CreatedAt,
                row.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UpsertTenantRolePermissionRequest req, CancellationToken ct = default)
        {
            if (req.TenantId == Guid.Empty) return BadRequest(new { message = "TenantId is required." });
            if (req.TenantRoleId == Guid.Empty) return BadRequest(new { message = "TenantRoleId is required." });
            if (req.PermissionId == Guid.Empty) return BadRequest(new { message = "PermissionId is required." });

            var tenantExists = await _db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == req.TenantId && !x.IsDeleted, ct);
            if (!tenantExists) return BadRequest(new { message = "TenantId not found." });

            var role = await _db.TenantRoles.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == req.TenantRoleId, ct);
            if (role is null || role.IsDeleted) return BadRequest(new { message = "TenantRoleId not found." });

            if (role.TenantId != req.TenantId)
                return BadRequest(new { message = "TenantRoleId does not belong to TenantId." });

            var permExists = await _db.Permissions.IgnoreQueryFilters().AnyAsync(x => x.Id == req.PermissionId && !x.IsDeleted, ct);
            if (!permExists) return BadRequest(new { message = "PermissionId not found." });

            var existing = await _db.TenantRolePermissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == req.TenantId && x.TenantRoleId == req.TenantRoleId && x.PermissionId == req.PermissionId, ct);

            if (existing is not null && !existing.IsDeleted)
                return Conflict(new { message = "TenantRolePermission already exists." });

            var now = DateTimeOffset.Now;

            if (existing is not null)
            {
                existing.IsDeleted = false;
                existing.UpdatedAt = now;
                await _db.SaveChangesAsync(ct);
                return Ok(new { ok = true, revived = true, id = existing.Id });
            }

            var link = new TenantRolePermission
            {
                Id = Guid.NewGuid(),
                TenantId = req.TenantId,
                TenantRoleId = req.TenantRoleId,
                PermissionId = req.PermissionId,
                IsDeleted = false,
                CreatedAt = now
            };

            _db.TenantRolePermissions.Add(link);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { version = "1.0", id = link.Id }, new { link.Id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpsertTenantRolePermissionRequest req, CancellationToken ct = default)
        {
            var link = await _db.TenantRolePermissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (link is null)
                return NotFound(new { message = "TenantRolePermission not found." });

            if (req.TenantId == Guid.Empty) return BadRequest(new { message = "TenantId is required." });
            if (req.TenantRoleId == Guid.Empty) return BadRequest(new { message = "TenantRoleId is required." });
            if (req.PermissionId == Guid.Empty) return BadRequest(new { message = "PermissionId is required." });

            var tenantExists = await _db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == req.TenantId && !x.IsDeleted, ct);
            if (!tenantExists) return BadRequest(new { message = "TenantId not found." });

            var role = await _db.TenantRoles.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == req.TenantRoleId, ct);
            if (role is null || role.IsDeleted) return BadRequest(new { message = "TenantRoleId not found." });

            if (role.TenantId != req.TenantId)
                return BadRequest(new { message = "TenantRoleId does not belong to TenantId." });

            var permExists = await _db.Permissions.IgnoreQueryFilters().AnyAsync(x => x.Id == req.PermissionId && !x.IsDeleted, ct);
            if (!permExists) return BadRequest(new { message = "PermissionId not found." });

            var dup = await _db.TenantRolePermissions.IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.TenantId == req.TenantId &&
                    x.TenantRoleId == req.TenantRoleId &&
                    x.PermissionId == req.PermissionId &&
                    x.Id != id &&
                    !x.IsDeleted, ct);

            if (dup)
                return Conflict(new { message = "Another TenantRolePermission already exists for this tenant + role + permission." });

            link.TenantId = req.TenantId;
            link.TenantRoleId = req.TenantRoleId;
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
            var link = await _db.TenantRolePermissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (link is null)
                return NotFound(new { message = "TenantRolePermission not found." });

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
            var link = await _db.TenantRolePermissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (link is null)
                return NotFound(new { message = "TenantRolePermission not found." });

            if (link.IsDeleted)
            {
                var dup = await _db.TenantRolePermissions.IgnoreQueryFilters()
                    .AnyAsync(x =>
                        x.TenantId == link.TenantId &&
                        x.TenantRoleId == link.TenantRoleId &&
                        x.PermissionId == link.PermissionId &&
                        x.Id != id &&
                        !x.IsDeleted, ct);

                if (dup)
                    return Conflict(new { message = "Cannot restore: another active TenantRolePermission already exists for this tenant+role+permission." });

                link.IsDeleted = false;
                link.UpdatedAt = DateTimeOffset.Now;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { ok = true });
        }
    }
}
