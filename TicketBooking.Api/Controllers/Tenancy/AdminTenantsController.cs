// FILE #114: TicketBooking.Api/Controllers/Admin/AdminTenantsController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Admin
{
    /// <summary>
    /// Phase 3-5: Admin CRUD for tenants.Tenants
    /// - Global table (NOT tenant-owned)
    /// - Soft delete supported (IsDeleted + Restore)
    /// - Code unique (case-insensitive recommended by normalized storage)
    ///
    /// Notes:
    /// - Admin can manage tenants without X-TenantId.
    /// - Tenant switch (X-TenantId) is for writing tenant-owned business tables, not for managing the Tenants table itself.
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/admin/tenants")]
    [Authorize(Policy = "perm:tenants.manage")]
    public sealed class AdminTenantsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminTenantsController(AppDbContext db)
        {
            _db = db;
        }

        public sealed class TenantUpsertRequest
        {
            public string Code { get; set; } = "";                 // NX001, KS001...
            public string Name { get; set; } = "";
            public TenantType Type { get; set; } = TenantType.Bus;
            public TenantStatus Status { get; set; } = TenantStatus.Active;

            public int HoldMinutes { get; set; } = 5;              // tenant config
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] string? q,
            [FromQuery] TenantType? type,
            [FromQuery] TenantStatus? status,
            [FromQuery] bool includeDeleted = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : (pageSize > 200 ? 200 : pageSize);

            IQueryable<Tenant> query = _db.Tenants.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            if (type.HasValue)
                query = query.Where(x => x.Type == type.Value);

            if (status.HasValue)
                query = query.Where(x => x.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim().ToUpperInvariant();
                query = query.Where(x =>
                    x.Code.ToUpper().Contains(key) ||
                    x.Name.ToUpper().Contains(key));
            }

            var total = await query.CountAsync(ct);

            var rows = await query
                .OrderBy(x => x.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.Name,
                    Type = x.Type.ToString(),
                    Status = x.Status.ToString(),
                    x.HoldMinutes,
                    x.IsDeleted,
                    x.CreatedAt,
                    x.UpdatedAt
                })
                .ToListAsync(ct);

            var tenantIds = rows.Select(x => x.Id).ToArray();
            var userStats = tenantIds.Length == 0
                ? new Dictionary<Guid, (int UsersCount, int OwnersCount)>()
                : await _db.TenantUsers.IgnoreQueryFilters().AsNoTracking()
                    .Where(x => tenantIds.Contains(x.TenantId) && !x.IsDeleted)
                    .GroupBy(x => x.TenantId)
                    .Select(x => new
                    {
                        TenantId = x.Key,
                        UsersCount = x.Count(),
                        OwnersCount = x.Count(y => y.IsOwner)
                    })
                    .ToDictionaryAsync(x => x.TenantId, x => (x.UsersCount, x.OwnersCount), ct);

            Dictionary<Guid, (string? OwnerName, string? OwnerEmail)> ownerContacts;
            if (tenantIds.Length == 0)
            {
                ownerContacts = new Dictionary<Guid, (string? OwnerName, string? OwnerEmail)>();
            }
            else
            {
                var ownerRows = await (
                    from tu in _db.TenantUsers.IgnoreQueryFilters().AsNoTracking()
                    join u in _db.Users.AsNoTracking() on tu.UserId equals u.Id
                    where tenantIds.Contains(tu.TenantId) && tu.IsOwner && !tu.IsDeleted
                    orderby tu.CreatedAt
                    select new
                    {
                        tu.TenantId,
                        OwnerName = u.FullName,
                        OwnerEmail = u.Email
                    }
                ).ToListAsync(ct);

                ownerContacts = ownerRows
                    .GroupBy(x => x.TenantId)
                    .ToDictionary(
                        x => x.Key,
                        x =>
                        {
                            var first = x.First();
                            return ((string?)first.OwnerName, (string?)first.OwnerEmail);
                        });
            }

            var items = rows.Select(x =>
            {
                userStats.TryGetValue(x.Id, out var stats);
                ownerContacts.TryGetValue(x.Id, out var owner);
                return new
                {
                    x.Id,
                    x.Code,
                    x.Name,
                    x.Type,
                    x.Status,
                    x.HoldMinutes,
                    UsersCount = stats.UsersCount,
                    OwnersCount = stats.OwnersCount,
                    OwnerName = owner.OwnerName,
                    OwnerEmail = owner.OwnerEmail,
                    x.IsDeleted,
                    x.CreatedAt,
                    x.UpdatedAt
                };
            });

            return Ok(new { page, pageSize, total, items });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById([FromRoute] Guid id, [FromQuery] bool includeDeleted = false, CancellationToken ct = default)
        {
            IQueryable<Tenant> query = _db.Tenants.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            var item = await query
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.Code,
                    x.Name,
                    Type = x.Type.ToString(),
                    Status = x.Status.ToString(),
                    x.HoldMinutes,
                    x.IsDeleted,
                    x.CreatedAt,
                    x.UpdatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (item is null)
                return NotFound(new { message = "Tenant not found." });

            var usersCount = await _db.TenantUsers.IgnoreQueryFilters().AsNoTracking()
                .CountAsync(x => x.TenantId == id && !x.IsDeleted, ct);

            var ownersCount = await _db.TenantUsers.IgnoreQueryFilters().AsNoTracking()
                .CountAsync(x => x.TenantId == id && x.IsOwner && !x.IsDeleted, ct);

            var tenantRolesCount = await _db.TenantRoles.IgnoreQueryFilters().AsNoTracking()
                .CountAsync(x => x.TenantId == id && !x.IsDeleted, ct);

            var owner = await (
                from tu in _db.TenantUsers.IgnoreQueryFilters().AsNoTracking()
                join u in _db.Users.AsNoTracking() on tu.UserId equals u.Id
                where tu.TenantId == id && tu.IsOwner && !tu.IsDeleted
                orderby tu.CreatedAt
                select new
                {
                    OwnerName = u.FullName,
                    OwnerEmail = u.Email
                }
            ).FirstOrDefaultAsync(ct);

            return Ok(new
            {
                item.Id,
                item.Code,
                item.Name,
                item.Type,
                item.Status,
                item.HoldMinutes,
                UsersCount = usersCount,
                OwnersCount = ownersCount,
                TenantRolesCount = tenantRolesCount,
                OwnerName = owner?.OwnerName,
                OwnerEmail = owner?.OwnerEmail,
                item.IsDeleted,
                item.CreatedAt,
                item.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TenantUpsertRequest req, CancellationToken ct = default)
        {
            var code = NormalizeCode(req.Code);
            var name = (req.Name ?? "").Trim();

            if (code.Length == 0)
                return BadRequest(new { message = "Code is required." });

            if (name.Length == 0)
                return BadRequest(new { message = "Name is required." });

            var holdMinutes = NormalizeHoldMinutes(req.HoldMinutes);

            var exists = await _db.Tenants.IgnoreQueryFilters()
                .AnyAsync(x => x.Code == code, ct);

            if (exists)
                return Conflict(new { message = $"Tenant code '{code}' already exists." });

            var now = DateTimeOffset.Now;

            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                Type = req.Type,
                Status = req.Status,
                HoldMinutes = holdMinutes,
                IsDeleted = false,
                CreatedAt = now
            };

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { version = "1.0", id = tenant.Id }, new { tenant.Id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] TenantUpsertRequest req, CancellationToken ct = default)
        {
            var tenant = await _db.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (tenant is null)
                return NotFound(new { message = "Tenant not found." });

            var code = NormalizeCode(req.Code);
            var name = (req.Name ?? "").Trim();

            if (code.Length == 0)
                return BadRequest(new { message = "Code is required." });

            if (name.Length == 0)
                return BadRequest(new { message = "Name is required." });

            var holdMinutes = NormalizeHoldMinutes(req.HoldMinutes);

            var exists = await _db.Tenants.IgnoreQueryFilters()
                .AnyAsync(x => x.Code == code && x.Id != id, ct);

            if (exists)
                return Conflict(new { message = $"Tenant code '{code}' already exists." });

            tenant.Code = code;
            tenant.Name = name;
            tenant.Type = req.Type;
            tenant.Status = req.Status;
            tenant.HoldMinutes = holdMinutes;

            if (tenant.IsDeleted)
                tenant.IsDeleted = false;

            tenant.UpdatedAt = DateTimeOffset.Now;

            await _db.SaveChangesAsync(ct);

            return Ok(new { ok = true });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDelete([FromRoute] Guid id, CancellationToken ct = default)
        {
            var tenant = await _db.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (tenant is null)
                return NotFound(new { message = "Tenant not found." });

            if (!tenant.IsDeleted)
            {
                tenant.IsDeleted = true;
                tenant.UpdatedAt = DateTimeOffset.Now;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { ok = true });
        }

        [HttpPost("{id:guid}/restore")]
        public async Task<IActionResult> Restore([FromRoute] Guid id, CancellationToken ct = default)
        {
            var tenant = await _db.Tenants.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (tenant is null)
                return NotFound(new { message = "Tenant not found." });

            if (tenant.IsDeleted)
            {
                // Ensure code uniqueness on restore
                var exists = await _db.Tenants.IgnoreQueryFilters()
                    .AnyAsync(x => x.Code == tenant.Code && x.Id != id && !x.IsDeleted, ct);

                if (exists)
                    return Conflict(new { message = $"Cannot restore: another active tenant already uses code '{tenant.Code}'." });

                tenant.IsDeleted = false;
                tenant.UpdatedAt = DateTimeOffset.Now;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { ok = true });
        }

        private static string NormalizeCode(string? code)
        {
            code = (code ?? "").Trim().ToUpperInvariant();
            // limit to 32 (matches configuration)
            if (code.Length > 32) code = code[..32];
            return code;
        }

        private static int NormalizeHoldMinutes(int holdMinutes)
        {
            // Keep sane bounds: 1..60 for booking/seat holds
            if (holdMinutes < 1) holdMinutes = 1;
            if (holdMinutes > 60) holdMinutes = 60;
            return holdMinutes;
        }
    }
}
