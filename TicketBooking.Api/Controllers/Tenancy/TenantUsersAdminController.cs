// FILE #115: TicketBooking.Api/Controllers/Admin/TenantUsersAdminController.cs
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
    /// Admin management for tenants.TenantUsers (user belongs to many tenants).
    /// - Global admin endpoint (no X-TenantId required)
    /// - Soft delete + restore
    /// - Unique: (TenantId, UserId)
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/admin/tenants/users")]
    [Authorize(Roles = "Admin")]
    public sealed class TenantUsersAdminController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public TenantUsersAdminController(AppDbContext db, UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public sealed class UpsertTenantUserRequest
        {
            public Guid TenantId { get; set; }
            public Guid UserId { get; set; }

            /// <summary>
            /// Legacy role name stored on TenantUsers (you already have RoleName in schema).
            /// Keep it for quick filtering / backward compatibility.
            /// Actual fine-grained roles are in tenants.TenantUserRoles.
            /// </summary>
            public string RoleName { get; set; } = "Member";

            public bool IsOwner { get; set; } = false;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] Guid? tenantId,
            [FromQuery] Guid? userId,
            [FromQuery] string? q,
            [FromQuery] bool includeDeleted = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : (pageSize > 200 ? 200 : pageSize);

            IQueryable<TenantUser> query = _db.TenantUsers.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
                query = query.Where(x => x.TenantId == tenantId.Value);

            if (userId.HasValue && userId.Value != Guid.Empty)
                query = query.Where(x => x.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim().ToUpperInvariant();

                // search by tenant code/name + username/email/fullname (via joins)
                var tenantIds = _db.Tenants.AsNoTracking()
                    .Where(t => (!t.IsDeleted) && (t.Code.ToUpper().Contains(key) || t.Name.ToUpper().Contains(key)))
                    .Select(t => t.Id);

                var userIds = _userManager.Users.AsNoTracking()
                    .Where(u =>
                        (u.UserName != null && u.UserName.ToUpper().Contains(key)) ||
                        (u.Email != null && u.Email.ToUpper().Contains(key)) ||
                        (u.FullName != null && u.FullName.ToUpper().Contains(key)))
                    .Select(u => u.Id);

                query = query.Where(x => tenantIds.Contains(x.TenantId) || userIds.Contains(x.UserId));
            }

            var total = await query.CountAsync(ct);

            // Load small maps for output
            var rows = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.TenantId,
                    x.UserId,
                    x.RoleName,
                    x.IsOwner,
                    x.IsDeleted,
                    x.CreatedAt,
                    x.UpdatedAt
                })
                .ToListAsync(ct);

            var tenantMap = await _db.Tenants.IgnoreQueryFilters()
                .Where(t => rows.Select(r => r.TenantId).Distinct().Contains(t.Id))
                .Select(t => new { t.Id, t.Code, t.Name, t.Type, t.Status, t.IsDeleted })
                .ToDictionaryAsync(t => t.Id, ct);

            var userMap = await _userManager.Users.AsNoTracking()
                .Where(u => rows.Select(r => r.UserId).Distinct().Contains(u.Id))
                .Select(u => new { u.Id, u.UserName, u.Email, u.FullName, u.IsActive })
                .ToDictionaryAsync(u => u.Id, ct);

            var items = rows.Select(r => new
            {
                r.Id,
                r.TenantId,
                Tenant = tenantMap.TryGetValue(r.TenantId, out var t) ? t : null,
                r.UserId,
                User = userMap.TryGetValue(r.UserId, out var u) ? u : null,
                r.RoleName,
                r.IsOwner,
                r.IsDeleted,
                r.CreatedAt,
                r.UpdatedAt
            });

            return Ok(new { page, pageSize, total, items });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
        {
            IQueryable<TenantUser> query = _db.TenantUsers.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            var row = await query.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (row is null)
                return NotFound(new { message = "TenantUser not found." });

            var tenant = await _db.Tenants.IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == row.TenantId, ct);

            var user = await _userManager.Users.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == row.UserId, ct);

            return Ok(new
            {
                row.Id,
                row.TenantId,
                Tenant = tenant is null ? null : new { tenant.Id, tenant.Code, tenant.Name, Type = tenant.Type.ToString(), Status = tenant.Status.ToString(), tenant.IsDeleted },
                row.UserId,
                User = user is null ? null : new { user.Id, user.UserName, user.Email, user.FullName, user.IsActive },
                row.RoleName,
                row.IsOwner,
                row.IsDeleted,
                row.CreatedAt,
                row.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UpsertTenantUserRequest req, CancellationToken ct = default)
        {
            if (req.TenantId == Guid.Empty)
                return BadRequest(new { message = "TenantId is required." });

            if (req.UserId == Guid.Empty)
                return BadRequest(new { message = "UserId is required." });

            var tenantExists = await _db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == req.TenantId && !x.IsDeleted, ct);
            if (!tenantExists)
                return BadRequest(new { message = "TenantId not found." });

            var userExists = await _userManager.Users.AsNoTracking().AnyAsync(x => x.Id == req.UserId, ct);
            if (!userExists)
                return BadRequest(new { message = "UserId not found." });

            var roleName = NormalizeRoleName(req.RoleName);

            var existing = await _db.TenantUsers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == req.TenantId && x.UserId == req.UserId, ct);

            if (existing is not null && !existing.IsDeleted)
                return Conflict(new { message = "This user is already linked to this tenant." });

            var now = DateTimeOffset.Now;

            if (existing is not null)
            {
                existing.IsDeleted = false;
                existing.RoleName = roleName;
                existing.IsOwner = req.IsOwner;
                existing.UpdatedAt = now;

                await _db.SaveChangesAsync(ct);
                return Ok(new { ok = true, revived = true, id = existing.Id });
            }

            var link = new TenantUser
            {
                Id = Guid.NewGuid(),
                TenantId = req.TenantId,
                UserId = req.UserId,
                RoleName = roleName,
                IsOwner = req.IsOwner,
                IsDeleted = false,
                CreatedAt = now
            };

            _db.TenantUsers.Add(link);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { version = "1.0", id = link.Id }, new { link.Id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpsertTenantUserRequest req, CancellationToken ct = default)
        {
            var link = await _db.TenantUsers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (link is null)
                return NotFound(new { message = "TenantUser not found." });

            if (req.TenantId == Guid.Empty)
                return BadRequest(new { message = "TenantId is required." });

            if (req.UserId == Guid.Empty)
                return BadRequest(new { message = "UserId is required." });

            var tenantExists = await _db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == req.TenantId && !x.IsDeleted, ct);
            if (!tenantExists)
                return BadRequest(new { message = "TenantId not found." });

            var userExists = await _userManager.Users.AsNoTracking().AnyAsync(x => x.Id == req.UserId, ct);
            if (!userExists)
                return BadRequest(new { message = "UserId not found." });

            // Unique (TenantId, UserId)
            var dup = await _db.TenantUsers.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == req.TenantId && x.UserId == req.UserId && x.Id != id && !x.IsDeleted, ct);

            if (dup)
                return Conflict(new { message = "Another TenantUser already links this user to this tenant." });

            link.TenantId = req.TenantId;
            link.UserId = req.UserId;
            link.RoleName = NormalizeRoleName(req.RoleName);
            link.IsOwner = req.IsOwner;

            if (link.IsDeleted)
                link.IsDeleted = false;

            link.UpdatedAt = DateTimeOffset.Now;

            await _db.SaveChangesAsync(ct);

            return Ok(new { ok = true });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDelete([FromRoute] Guid id, CancellationToken ct = default)
        {
            var link = await _db.TenantUsers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (link is null)
                return NotFound(new { message = "TenantUser not found." });

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
            var link = await _db.TenantUsers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (link is null)
                return NotFound(new { message = "TenantUser not found." });

            if (link.IsDeleted)
            {
                // Ensure uniqueness on restore
                var dup = await _db.TenantUsers.IgnoreQueryFilters()
                    .AnyAsync(x => x.TenantId == link.TenantId && x.UserId == link.UserId && x.Id != id && !x.IsDeleted, ct);

                if (dup)
                    return Conflict(new { message = "Cannot restore: another active link already exists for this tenant+user." });

                link.IsDeleted = false;
                link.UpdatedAt = DateTimeOffset.Now;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { ok = true });
        }

        private static string NormalizeRoleName(string? roleName)
        {
            roleName = (roleName ?? "").Trim();
            if (roleName.Length == 0) roleName = "Member";
            if (roleName.Length > 50) roleName = roleName[..50];
            // Viết hoa chữ cái đầu theo style
            return string.Join(' ', roleName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p[1..].ToLowerInvariant() : "")));
        }
    }
}
