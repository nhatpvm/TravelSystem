// FILE #121: TicketBooking.Api/Controllers/Admin/UserPermissionsAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using TicketBooking.Domain.Auth;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Admin
{
    /// <summary>
    /// Admin management for auth.UserPermissions:
    /// - Assign permission directly to a user with Effect = Allow/Deny
    /// - Optional TenantId scope (null = global)
    /// - Soft delete + restore
    ///
    /// Notes:
    /// - Unique index in migration: (UserId, PermissionId, TenantId) with filter [TenantId] IS NOT NULL
    ///   => The database allows many rows with TenantId NULL (SQL unique filter excludes NULL).
    /// - Therefore, we enforce uniqueness in code for both cases:
    ///   - TenantId == null: unique (UserId, PermissionId)
    ///   - TenantId != null: unique (UserId, PermissionId, TenantId)
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/admin/auth/user-permissions")]
    [Authorize(Policy = "perm:tenants.manage")]
    public sealed class UserPermissionsAdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public UserPermissionsAdminController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public sealed class UpsertUserPermissionRequest
        {
            public Guid UserId { get; set; }
            public Guid PermissionId { get; set; }
            public Guid? TenantId { get; set; }   // null = global
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public PermissionEffect Effect { get; set; } = PermissionEffect.Allow;
            public string? Reason { get; set; }
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] Guid? userId,
            [FromQuery] Guid? permissionId,
            [FromQuery] Guid? tenantId,
            [FromQuery] PermissionEffect? effect,
            [FromQuery] string? q,
            [FromQuery] bool includeDeleted = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : (pageSize > 200 ? 200 : pageSize);

            IQueryable<UserPermission> query = _db.UserPermissions.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            if (userId.HasValue && userId.Value != Guid.Empty)
                query = query.Where(x => x.UserId == userId.Value);

            if (permissionId.HasValue && permissionId.Value != Guid.Empty)
                query = query.Where(x => x.PermissionId == permissionId.Value);

            if (tenantId.HasValue)
            {
                if (tenantId.Value == Guid.Empty)
                    query = query.Where(x => x.TenantId == null);
                else
                    query = query.Where(x => x.TenantId == tenantId.Value);
            }

            if (effect.HasValue)
                query = query.Where(x => x.Effect == effect.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim().ToUpperInvariant();

                var userIds = _userManager.Users.AsNoTracking()
                    .Where(u =>
                        (u.UserName != null && u.UserName.ToUpper().Contains(key)) ||
                        (u.Email != null && u.Email.ToUpper().Contains(key)) ||
                        (u.FullName != null && u.FullName.ToUpper().Contains(key)))
                    .Select(u => u.Id);

                var permIds = _db.Permissions.AsNoTracking()
                    .Where(p => (!p.IsDeleted) && (p.Code.ToUpper().Contains(key) || p.Name.ToUpper().Contains(key)))
                    .Select(p => p.Id);

                var tenantIds = _db.Tenants.AsNoTracking()
                    .Where(t => (!t.IsDeleted) && (t.Code.ToUpper().Contains(key) || t.Name.ToUpper().Contains(key)))
                    .Select(t => t.Id);

                query = query.Where(x =>
                    userIds.Contains(x.UserId) ||
                    permIds.Contains(x.PermissionId) ||
                    (x.TenantId != null && tenantIds.Contains(x.TenantId.Value)) ||
                    (x.Reason != null && x.Reason.ToUpper().Contains(key)));
            }

            var total = await query.CountAsync(ct);

            var rows = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.UserId,
                    x.PermissionId,
                    x.TenantId,
                    x.Effect,
                    x.Reason,
                    x.IsDeleted,
                    x.CreatedAt,
                    x.UpdatedAt
                })
                .ToListAsync(ct);

            var userMap = await _userManager.Users.AsNoTracking()
                .Where(u => rows.Select(r => r.UserId).Distinct().Contains(u.Id))
                .Select(u => new { u.Id, u.UserName, u.Email, u.FullName, u.IsActive })
                .ToDictionaryAsync(u => u.Id, ct);

            var permMap = await _db.Permissions.IgnoreQueryFilters().AsNoTracking()
                .Where(p => rows.Select(r => r.PermissionId).Distinct().Contains(p.Id))
                .Select(p => new { p.Id, p.Code, p.Name, p.IsActive })
                .ToDictionaryAsync(p => p.Id, ct);

            var tenantIdsDistinct = rows.Where(r => r.TenantId.HasValue).Select(r => r.TenantId!.Value).Distinct().ToList();
            var tenantMap = tenantIdsDistinct.Count == 0
                ? new Dictionary<Guid, object>()
                : await _db.Tenants.IgnoreQueryFilters().AsNoTracking()
                    .Where(t => tenantIdsDistinct.Contains(t.Id))
                    .Select(t => new { t.Id, t.Code, t.Name, t.Type, t.Status })
                    .ToDictionaryAsync(t => t.Id, t => (object)t, ct);

            var items = rows.Select(r => new
            {
                r.Id,
                r.UserId,
                User = userMap.TryGetValue(r.UserId, out var u) ? u : null,
                r.PermissionId,
                Permission = permMap.TryGetValue(r.PermissionId, out var p) ? p : null,
                r.TenantId,
                Tenant = r.TenantId.HasValue && tenantMap.TryGetValue(r.TenantId.Value, out var t) ? t : null,
                Effect = r.Effect.ToString(),
                r.Reason,
                r.IsDeleted,
                r.CreatedAt,
                r.UpdatedAt
            });

            return Ok(new { page, pageSize, total, items });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
        {
            IQueryable<UserPermission> query = _db.UserPermissions.AsNoTracking();
            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            var row = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (row is null)
                return NotFound(new { message = "UserPermission not found." });

            var user = await _userManager.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.UserId, ct);
            var perm = await _db.Permissions.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.PermissionId, ct);
            var tenant = row.TenantId.HasValue
                ? await _db.Tenants.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.TenantId.Value, ct)
                : null;

            return Ok(new
            {
                row.Id,
                row.UserId,
                User = user is null ? null : new { user.Id, user.UserName, user.Email, user.FullName, user.IsActive },
                row.PermissionId,
                Permission = perm is null ? null : new { perm.Id, perm.Code, perm.Name, perm.IsActive },
                row.TenantId,
                Tenant = tenant is null ? null : new { tenant.Id, tenant.Code, tenant.Name, Type = tenant.Type.ToString(), Status = tenant.Status.ToString() },
                Effect = row.Effect.ToString(),
                row.Reason,
                row.IsDeleted,
                row.CreatedAt,
                row.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UpsertUserPermissionRequest req, CancellationToken ct = default)
        {
            if (req.UserId == Guid.Empty) return BadRequest(new { message = "UserId is required." });
            if (req.PermissionId == Guid.Empty) return BadRequest(new { message = "PermissionId is required." });

            var userExists = await _userManager.Users.AsNoTracking().AnyAsync(x => x.Id == req.UserId, ct);
            if (!userExists) return BadRequest(new { message = "UserId not found." });

            var permExists = await _db.Permissions.IgnoreQueryFilters().AnyAsync(x => x.Id == req.PermissionId && !x.IsDeleted, ct);
            if (!permExists) return BadRequest(new { message = "PermissionId not found." });

            if (req.TenantId.HasValue && req.TenantId.Value != Guid.Empty)
            {
                var tenantExists = await _db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == req.TenantId.Value && !x.IsDeleted, ct);
                if (!tenantExists) return BadRequest(new { message = "TenantId not found." });
            }

            var tenantId = NormalizeTenantId(req.TenantId);

            // enforce uniqueness (also for TenantId null)
            var dup = await _db.UserPermissions.IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.UserId == req.UserId &&
                    x.PermissionId == req.PermissionId &&
                    ((tenantId == null && x.TenantId == null) || (tenantId != null && x.TenantId == tenantId)) &&
                    !x.IsDeleted, ct);

            if (dup)
                return Conflict(new { message = "UserPermission already exists for this user + permission (+ tenant scope)." });

            var now = DateTimeOffset.Now;

            var entity = new UserPermission
            {
                Id = Guid.NewGuid(),
                UserId = req.UserId,
                PermissionId = req.PermissionId,
                TenantId = tenantId,
                Effect = req.Effect,
                Reason = TrimOrNull(req.Reason, 500),
                IsDeleted = false,
                CreatedAt = now
            };

            _db.UserPermissions.Add(entity);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { version = "1.0", id = entity.Id }, new { entity.Id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpsertUserPermissionRequest req, CancellationToken ct = default)
        {
            var entity = await _db.UserPermissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity is null)
                return NotFound(new { message = "UserPermission not found." });

            if (req.UserId == Guid.Empty) return BadRequest(new { message = "UserId is required." });
            if (req.PermissionId == Guid.Empty) return BadRequest(new { message = "PermissionId is required." });

            var userExists = await _userManager.Users.AsNoTracking().AnyAsync(x => x.Id == req.UserId, ct);
            if (!userExists) return BadRequest(new { message = "UserId not found." });

            var permExists = await _db.Permissions.IgnoreQueryFilters().AnyAsync(x => x.Id == req.PermissionId && !x.IsDeleted, ct);
            if (!permExists) return BadRequest(new { message = "PermissionId not found." });

            if (req.TenantId.HasValue && req.TenantId.Value != Guid.Empty)
            {
                var tenantExists = await _db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == req.TenantId.Value && !x.IsDeleted, ct);
                if (!tenantExists) return BadRequest(new { message = "TenantId not found." });
            }

            var tenantId = NormalizeTenantId(req.TenantId);

            var dup = await _db.UserPermissions.IgnoreQueryFilters()
                .AnyAsync(x =>
                    x.UserId == req.UserId &&
                    x.PermissionId == req.PermissionId &&
                    ((tenantId == null && x.TenantId == null) || (tenantId != null && x.TenantId == tenantId)) &&
                    x.Id != id &&
                    !x.IsDeleted, ct);

            if (dup)
                return Conflict(new { message = "Another UserPermission already exists for this user + permission (+ tenant scope)." });

            entity.UserId = req.UserId;
            entity.PermissionId = req.PermissionId;
            entity.TenantId = tenantId;
            entity.Effect = req.Effect;
            entity.Reason = TrimOrNull(req.Reason, 500);

            if (entity.IsDeleted)
                entity.IsDeleted = false;

            entity.UpdatedAt = DateTimeOffset.Now;

            await _db.SaveChangesAsync(ct);

            return Ok(new { ok = true });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDelete([FromRoute] Guid id, CancellationToken ct = default)
        {
            var entity = await _db.UserPermissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity is null)
                return NotFound(new { message = "UserPermission not found." });

            if (!entity.IsDeleted)
            {
                entity.IsDeleted = true;
                entity.UpdatedAt = DateTimeOffset.Now;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { ok = true });
        }

        [HttpPost("{id:guid}/restore")]
        public async Task<IActionResult> Restore([FromRoute] Guid id, CancellationToken ct = default)
        {
            var entity = await _db.UserPermissions.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (entity is null)
                return NotFound(new { message = "UserPermission not found." });

            if (entity.IsDeleted)
            {
                var dup = await _db.UserPermissions.IgnoreQueryFilters()
                    .AnyAsync(x =>
                        x.UserId == entity.UserId &&
                        x.PermissionId == entity.PermissionId &&
                        ((entity.TenantId == null && x.TenantId == null) || (entity.TenantId != null && x.TenantId == entity.TenantId)) &&
                        x.Id != id &&
                        !x.IsDeleted, ct);

                if (dup)
                    return Conflict(new { message = "Cannot restore: another active UserPermission already exists with same scope." });

                entity.IsDeleted = false;
                entity.UpdatedAt = DateTimeOffset.Now;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { ok = true });
        }

        private static Guid? NormalizeTenantId(Guid? tenantId)
        {
            if (!tenantId.HasValue) return null;
            if (tenantId.Value == Guid.Empty) return null;
            return tenantId.Value;
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
