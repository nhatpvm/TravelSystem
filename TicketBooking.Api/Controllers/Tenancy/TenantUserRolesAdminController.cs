// FILE #117: TicketBooking.Api/Controllers/Admin/TenantUserRolesAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Admin
{
    /// <summary>
    /// Admin management for tenants.TenantUserRoles
    /// - Maps User -> TenantRole in a tenant (fine-grained).
    /// - Soft delete + restore
    /// - Unique: (TenantId, TenantRoleId, UserId)
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/admin/tenants/user-roles")]
    [Authorize(Policy = "perm:tenants.manage")]
    public sealed class TenantUserRolesAdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public TenantUserRolesAdminController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public sealed class UpsertTenantUserRoleRequest
        {
            public Guid TenantId { get; set; }
            public Guid TenantRoleId { get; set; }
            public Guid UserId { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] Guid? tenantId,
            [FromQuery] Guid? tenantRoleId,
            [FromQuery] Guid? userId,
            [FromQuery] string? q,
            [FromQuery] bool includeDeleted = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : (pageSize > 200 ? 200 : pageSize);

            IQueryable<TenantUserRole> query = _db.TenantUserRoles.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
                query = query.Where(x => x.TenantId == tenantId.Value);

            if (tenantRoleId.HasValue && tenantRoleId.Value != Guid.Empty)
                query = query.Where(x => x.TenantRoleId == tenantRoleId.Value);

            if (userId.HasValue && userId.Value != Guid.Empty)
                query = query.Where(x => x.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim().ToUpperInvariant();

                var tenantIds = _db.Tenants.AsNoTracking()
                    .Where(t => (!t.IsDeleted) && (t.Code.ToUpper().Contains(key) || t.Name.ToUpper().Contains(key)))
                    .Select(t => t.Id);

                var roleIds = _db.TenantRoles.AsNoTracking()
                    .Where(r => (!r.IsDeleted) && (r.Code.ToUpper().Contains(key) || r.Name.ToUpper().Contains(key)))
                    .Select(r => r.Id);

                var userIds = _userManager.Users.AsNoTracking()
                    .Where(u =>
                        (u.UserName != null && u.UserName.ToUpper().Contains(key)) ||
                        (u.Email != null && u.Email.ToUpper().Contains(key)) ||
                        (u.FullName != null && u.FullName.ToUpper().Contains(key)))
                    .Select(u => u.Id);

                query = query.Where(x => tenantIds.Contains(x.TenantId) || roleIds.Contains(x.TenantRoleId) || userIds.Contains(x.UserId));
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
                    x.UserId,
                    x.IsDeleted,
                    x.CreatedAt,
                    x.UpdatedAt
                })
                .ToListAsync(ct);

            var tenantMap = await _db.Tenants.IgnoreQueryFilters()
                .Where(t => rows.Select(r => r.TenantId).Distinct().Contains(t.Id))
                .Select(t => new { t.Id, t.Code, t.Name, t.Type, t.Status })
                .ToDictionaryAsync(t => t.Id, ct);

            var roleMap = await _db.TenantRoles.IgnoreQueryFilters()
                .Where(r => rows.Select(r => r.TenantRoleId).Distinct().Contains(r.Id))
                .Select(r => new { r.Id, r.TenantId, r.Code, r.Name, r.IsActive })
                .ToDictionaryAsync(r => r.Id, ct);

            var userMap = await _userManager.Users.AsNoTracking()
                .Where(u => rows.Select(r => r.UserId).Distinct().Contains(u.Id))
                .Select(u => new { u.Id, u.UserName, u.Email, u.FullName, u.IsActive })
                .ToDictionaryAsync(u => u.Id, ct);

            var items = rows.Select(r => new
            {
                r.Id,
                r.TenantId,
                Tenant = tenantMap.TryGetValue(r.TenantId, out var t) ? t : null,
                r.TenantRoleId,
                TenantRole = roleMap.TryGetValue(r.TenantRoleId, out var tr) ? tr : null,
                r.UserId,
                User = userMap.TryGetValue(r.UserId, out var u) ? u : null,
                r.IsDeleted,
                r.CreatedAt,
                r.UpdatedAt
            });

            return Ok(new { page, pageSize, total, items });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
        {
            IQueryable<TenantUserRole> query = _db.TenantUserRoles.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            var row = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (row is null)
                return NotFound(new { message = "TenantUserRole not found." });

            var tenant = await _db.Tenants.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.TenantId, ct);
            var role = await _db.TenantRoles.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.TenantRoleId, ct);
            var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.UserId, ct);

            return Ok(new
            {
                row.Id,
                row.TenantId,
                Tenant = tenant is null ? null : new { tenant.Id, tenant.Code, tenant.Name, Type = tenant.Type.ToString(), Status = tenant.Status.ToString() },
                row.TenantRoleId,
                TenantRole = role is null ? null : new { role.Id, role.TenantId, role.Code, role.Name, role.IsActive },
                row.UserId,
                User = user is null ? null : new { user.Id, user.UserName, user.Email, user.FullName, user.IsActive },
                row.IsDeleted,
                row.CreatedAt,
                row.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UpsertTenantUserRoleRequest req, CancellationToken ct = default)
        {
            if (req.TenantId == Guid.Empty) return BadRequest(new { message = "TenantId is required." });
            if (req.TenantRoleId == Guid.Empty) return BadRequest(new { message = "TenantRoleId is required." });
            if (req.UserId == Guid.Empty) return BadRequest(new { message = "UserId is required." });

            var tenantExists = await _db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == req.TenantId && !x.IsDeleted, ct);
            if (!tenantExists) return BadRequest(new { message = "TenantId not found." });

            var role = await _db.TenantRoles.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == req.TenantRoleId, ct);
            if (role is null || role.IsDeleted) return BadRequest(new { message = "TenantRoleId not found." });

            if (role.TenantId != req.TenantId)
                return BadRequest(new { message = "TenantRoleId does not belong to TenantId." });

            var userExists = await _userManager.Users.AsNoTracking().AnyAsync(x => x.Id == req.UserId, ct);
            if (!userExists) return BadRequest(new { message = "UserId not found." });

            // Ensure the user is linked to tenant via TenantUsers (recommended)
            var tenantUserExists = await _db.TenantUsers.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == req.TenantId && x.UserId == req.UserId && !x.IsDeleted, ct);

            if (!tenantUserExists)
                return BadRequest(new { message = "User is not linked to this tenant (TenantUsers). Create tenant-user link first." });

            var existing = await _db.TenantUserRoles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == req.TenantId && x.TenantRoleId == req.TenantRoleId && x.UserId == req.UserId, ct);

            if (existing is not null && !existing.IsDeleted)
                return Conflict(new { message = "This user already has this tenant role." });

            var now = DateTimeOffset.Now;

            if (existing is not null)
            {
                existing.IsDeleted = false;
                existing.UpdatedAt = now;
                await _db.SaveChangesAsync(ct);
                return Ok(new { ok = true, revived = true, id = existing.Id });
            }

            var link = new TenantUserRole
            {
                Id = Guid.NewGuid(),
                TenantId = req.TenantId,
                TenantRoleId = req.TenantRoleId,
                UserId = req.UserId,
                IsDeleted = false,
                CreatedAt = now
            };

            _db.TenantUserRoles.Add(link);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { version = "1.0", id = link.Id }, new { link.Id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpsertTenantUserRoleRequest req, CancellationToken ct = default)
        {
            var link = await _db.TenantUserRoles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (link is null)
                return NotFound(new { message = "TenantUserRole not found." });

            if (req.TenantId == Guid.Empty) return BadRequest(new { message = "TenantId is required." });
            if (req.TenantRoleId == Guid.Empty) return BadRequest(new { message = "TenantRoleId is required." });
            if (req.UserId == Guid.Empty) return BadRequest(new { message = "UserId is required." });

            var tenantExists = await _db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == req.TenantId && !x.IsDeleted, ct);
            if (!tenantExists) return BadRequest(new { message = "TenantId not found." });

            var role = await _db.TenantRoles.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == req.TenantRoleId, ct);
            if (role is null || role.IsDeleted) return BadRequest(new { message = "TenantRoleId not found." });

            if (role.TenantId != req.TenantId)
                return BadRequest(new { message = "TenantRoleId does not belong to TenantId." });

            var userExists = await _userManager.Users.AsNoTracking().AnyAsync(x => x.Id == req.UserId, ct);
            if (!userExists) return BadRequest(new { message = "UserId not found." });

            var tenantUserExists = await _db.TenantUsers.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == req.TenantId && x.UserId == req.UserId && !x.IsDeleted, ct);

            if (!tenantUserExists)
                return BadRequest(new { message = "User is not linked to this tenant (TenantUsers). Create tenant-user link first." });

            // Unique (TenantId, TenantRoleId, UserId)
            var dup = await _db.TenantUserRoles.IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.TenantId == req.TenantId &&
                    x.TenantRoleId == req.TenantRoleId &&
                    x.UserId == req.UserId &&
                    x.Id != id &&
                    !x.IsDeleted, ct);

            if (dup)
                return Conflict(new { message = "Another TenantUserRole already exists for this tenant + role + user." });

            link.TenantId = req.TenantId;
            link.TenantRoleId = req.TenantRoleId;
            link.UserId = req.UserId;

            if (link.IsDeleted)
                link.IsDeleted = false;

            link.UpdatedAt = DateTimeOffset.Now;

            await _db.SaveChangesAsync(ct);

            return Ok(new { ok = true });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDelete([FromRoute] Guid id, CancellationToken ct = default)
        {
            var link = await _db.TenantUserRoles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (link is null)
                return NotFound(new { message = "TenantUserRole not found." });

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
            var link = await _db.TenantUserRoles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (link is null)
                return NotFound(new { message = "TenantUserRole not found." });

            if (link.IsDeleted)
            {
                var dup = await _db.TenantUserRoles.IgnoreQueryFilters()
                    .AnyAsync(x =>
                        x.TenantId == link.TenantId &&
                        x.TenantRoleId == link.TenantRoleId &&
                        x.UserId == link.UserId &&
                        x.Id != id &&
                        !x.IsDeleted, ct);

                if (dup)
                    return Conflict(new { message = "Cannot restore: another active TenantUserRole already exists for this tenant+role+user." });

                link.IsDeleted = false;
                link.UpdatedAt = DateTimeOffset.Now;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { ok = true });
        }
    }
}
