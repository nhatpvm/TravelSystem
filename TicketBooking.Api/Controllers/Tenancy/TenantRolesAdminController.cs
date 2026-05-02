// FILE #116: TicketBooking.Api/Controllers/Admin/TenantRolesAdminController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Admin
{
    /// <summary>
    /// Admin CRUD for tenants.TenantRoles (fine-grained roles per tenant).
    /// - Global admin endpoint (no X-TenantId required)
    /// - Soft delete + restore
    /// - Unique: (TenantId, Code)
    /// </summary>
    [ApiController]
    [ApiVersion(1.0)]
    [Route("api/v{version:apiVersion}/admin/tenants/roles")]
    [Authorize(Policy = "perm:tenants.manage")]
    public sealed class TenantRolesAdminController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TenantRolesAdminController(AppDbContext db)
        {
            _db = db;
        }

        public sealed class TenantRoleUpsertRequest
        {
            public Guid TenantId { get; set; }
            public string Code { get; set; } = "";        // e.g. "qlnx.ticket.create"
            public string Name { get; set; } = "";        // display
            public string? Description { get; set; }
            public bool IsActive { get; set; } = true;
        }

        [HttpGet]
        public async Task<IActionResult> List(
            [FromQuery] Guid? tenantId,
            [FromQuery] string? q,
            [FromQuery] bool includeDeleted = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : (pageSize > 200 ? 200 : pageSize);

            IQueryable<TenantRole> query = _db.TenantRoles.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            if (tenantId.HasValue && tenantId.Value != Guid.Empty)
                query = query.Where(x => x.TenantId == tenantId.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = q.Trim().ToUpperInvariant();
                query = query.Where(x =>
                    x.Code.ToUpper().Contains(key) ||
                    x.Name.ToUpper().Contains(key) ||
                    (x.Description != null && x.Description.ToUpper().Contains(key)));
            }

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderBy(x => x.TenantId)
                .ThenBy(x => x.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    x.Id,
                    x.TenantId,
                    x.Code,
                    x.Name,
                    x.Description,
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
            IQueryable<TenantRole> query = _db.TenantRoles.AsNoTracking();

            if (includeDeleted)
                query = query.IgnoreQueryFilters();

            var item = await query
                .Where(x => x.Id == id)
                .Select(x => new
                {
                    x.Id,
                    x.TenantId,
                    x.Code,
                    x.Name,
                    x.Description,
                    x.IsActive,
                    x.IsDeleted,
                    x.CreatedAt,
                    x.UpdatedAt
                })
                .FirstOrDefaultAsync(ct);

            if (item is null)
                return NotFound(new { message = "TenantRole not found." });

            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TenantRoleUpsertRequest req, CancellationToken ct = default)
        {
            if (req.TenantId == Guid.Empty)
                return BadRequest(new { message = "TenantId is required." });

            var tenantExists = await _db.Tenants.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == req.TenantId && !x.IsDeleted, ct);

            if (!tenantExists)
                return BadRequest(new { message = "TenantId not found." });

            var code = NormalizeCode(req.Code);
            var name = NormalizeName(req.Name);

            if (code.Length == 0)
                return BadRequest(new { message = "Code is required." });

            if (name.Length == 0)
                return BadRequest(new { message = "Name is required." });

            // Unique (TenantId, Code)
            var exists = await _db.TenantRoles.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == req.TenantId && x.Code == code, ct);

            if (exists)
                return Conflict(new { message = $"TenantRole code '{code}' already exists for this tenant." });

            var now = DateTimeOffset.Now;

            var role = new TenantRole
            {
                Id = Guid.NewGuid(),
                TenantId = req.TenantId,
                Code = code,
                Name = name,
                Description = TrimOrNull(req.Description, 1000),
                IsActive = req.IsActive,
                IsDeleted = false,
                CreatedAt = now
            };

            _db.TenantRoles.Add(role);
            await _db.SaveChangesAsync(ct);

            return CreatedAtAction(nameof(GetById), new { version = "1.0", id = role.Id }, new { role.Id });
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] TenantRoleUpsertRequest req, CancellationToken ct = default)
        {
            var role = await _db.TenantRoles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (role is null)
                return NotFound(new { message = "TenantRole not found." });

            if (req.TenantId == Guid.Empty)
                return BadRequest(new { message = "TenantId is required." });

            var tenantExists = await _db.Tenants.IgnoreQueryFilters()
                .AnyAsync(x => x.Id == req.TenantId && !x.IsDeleted, ct);

            if (!tenantExists)
                return BadRequest(new { message = "TenantId not found." });

            var code = NormalizeCode(req.Code);
            var name = NormalizeName(req.Name);

            if (code.Length == 0)
                return BadRequest(new { message = "Code is required." });

            if (name.Length == 0)
                return BadRequest(new { message = "Name is required." });

            var dup = await _db.TenantRoles.IgnoreQueryFilters()
                .AnyAsync(x => x.TenantId == req.TenantId && x.Code == code && x.Id != id && !x.IsDeleted, ct);

            if (dup)
                return Conflict(new { message = $"Another TenantRole already uses code '{code}' for this tenant." });

            role.TenantId = req.TenantId;
            role.Code = code;
            role.Name = name;
            role.Description = TrimOrNull(req.Description, 1000);
            role.IsActive = req.IsActive;

            if (role.IsDeleted)
                role.IsDeleted = false;

            role.UpdatedAt = DateTimeOffset.Now;

            await _db.SaveChangesAsync(ct);

            return Ok(new { ok = true });
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> SoftDelete([FromRoute] Guid id, CancellationToken ct = default)
        {
            var role = await _db.TenantRoles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (role is null)
                return NotFound(new { message = "TenantRole not found." });

            if (!role.IsDeleted)
            {
                role.IsDeleted = true;
                role.UpdatedAt = DateTimeOffset.Now;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { ok = true });
        }

        [HttpPost("{id:guid}/restore")]
        public async Task<IActionResult> Restore([FromRoute] Guid id, CancellationToken ct = default)
        {
            var role = await _db.TenantRoles.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (role is null)
                return NotFound(new { message = "TenantRole not found." });

            if (role.IsDeleted)
            {
                var dup = await _db.TenantRoles.IgnoreQueryFilters()
                    .AnyAsync(x => x.TenantId == role.TenantId && x.Code == role.Code && x.Id != id && !x.IsDeleted, ct);

                if (dup)
                    return Conflict(new { message = "Cannot restore: another active TenantRole already uses the same code in this tenant." });

                role.IsDeleted = false;
                role.UpdatedAt = DateTimeOffset.Now;
                await _db.SaveChangesAsync(ct);
            }

            return Ok(new { ok = true });
        }

        private static string NormalizeCode(string? code)
        {
            code = (code ?? "").Trim();
            if (code.Length == 0) return "";
            if (code.Length > 80) code = code[..80];
            return code;
        }

        private static string NormalizeName(string? name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) return "";
            if (name.Length > 200) name = name[..200];

            // Viết hoa chữ cái đầu mỗi từ
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
