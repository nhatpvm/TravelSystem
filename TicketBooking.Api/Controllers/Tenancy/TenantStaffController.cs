using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TicketBooking.Domain.Auth;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Auth;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Api.Controllers;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/tenant/staff")]
[Authorize(Policy = "perm:tenant.staff.manage")]
public sealed class TenantStaffController : ControllerBase
{
    private sealed class StaffRoleTemplate
    {
        public string Code { get; init; } = "";
        public string Label { get; init; } = "";
        public string StorageRoleName { get; init; } = "";
        public string[] Permissions { get; init; } = Array.Empty<string>();
    }

    private sealed class TenantStaffItem
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public string UserName { get; init; } = "";
        public string Name { get; init; } = "";
        public string Email { get; init; } = "";
        public string? Phone { get; init; }
        public string RoleCode { get; init; } = "";
        public string RoleLabel { get; init; } = "";
        public string RawRoleName { get; init; } = "";
        public bool IsOwner { get; init; }
        public bool Active { get; init; }
        public DateTimeOffset JoinedAt { get; init; }
        public DateTimeOffset? LastActivityAt { get; init; }
        public string[] Permissions { get; init; } = Array.Empty<string>();
    }

    private sealed class StaffRow
    {
        public TenantUser Link { get; init; } = new();
        public AppUser User { get; init; } = new();
    }

    private static readonly StaffRoleTemplate[] StaffRoleTemplates =
    {
        new()
        {
            Code = "manager",
            Label = "Quản lý",
            StorageRoleName = "Manager",
            Permissions =
            [
                "Xem tất cả báo cáo",
                "Quản lý nhân viên",
                "Chỉnh sửa cấu hình",
                "Phê duyệt thanh toán",
                "Quản lý kho"
            ]
        },
        new()
        {
            Code = "accountant",
            Label = "Kế toán",
            StorageRoleName = "Accountant",
            Permissions =
            [
                "Xem báo cáo tài chính",
                "Xuất hoá đơn",
                "Xem lịch sử giao dịch"
            ]
        },
        new()
        {
            Code = "ops",
            Label = "Vận hành",
            StorageRoleName = "Ops",
            Permissions =
            [
                "Quản lý chuyến/phòng",
                "Cập nhật giá và tồn kho",
                "Xem đơn hàng"
            ]
        },
        new()
        {
            Code = "support",
            Label = "CSKH",
            StorageRoleName = "Support",
            Permissions =
            [
                "Xem đơn hàng",
                "Liên hệ khách",
                "Ghi chú đơn hàng",
                "Xem lịch sử khách"
            ]
        },
        new()
        {
            Code = "ticket",
            Label = "Đại lý vé",
            StorageRoleName = "Ticket",
            Permissions =
            [
                "Xem đơn hàng",
                "Quét vé và phát vé",
                "Xem lịch trình"
            ]
        }
    };

    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly ITenantContext _tenantContext;
    private readonly IPermissionService _permissionService;

    public TenantStaffController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        ITenantContext tenantContext,
        IPermissionService permissionService)
    {
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
        _tenantContext = tenantContext;
        _permissionService = permissionService;
    }

    public sealed class CreateTenantStaffRequest
    {
        public string? UserName { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string RoleCode { get; set; } = "manager";
        public bool IsOwner { get; set; }
    }

    public sealed class UpdateTenantStaffRequest
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string RoleCode { get; set; } = "manager";
        public bool IsOwner { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? q,
        [FromQuery] string? roleCode,
        [FromQuery] bool includeInactive = true,
        CancellationToken ct = default)
    {
        var tenant = await GetCurrentTenantAsync(ct);
        if (tenant is null)
            return BadRequest(new { message = "Tenant context is required for tenant staff management." });

        var guard = await EnsureCanManageStaffAsync(tenant, ct);
        if (guard is not null)
            return guard;

        var rows = await (
            from link in _db.TenantUsers.IgnoreQueryFilters().AsNoTracking()
            join user in _userManager.Users.AsNoTracking() on link.UserId equals user.Id
            where link.TenantId == tenant.Id
            orderby link.IsOwner descending, link.CreatedAt, user.FullName
            select new StaffRow
            {
                Link = link,
                User = user
            }
        ).ToListAsync(ct);

        var visibleRows = await FilterVisibleRowsAsync(rows, tenant.Type, ct);

        var items = visibleRows
            .Select(x => MapItem(x.Link, x.User))
            .Where(x => includeInactive || x.Active)
            .Where(x => string.IsNullOrWhiteSpace(roleCode) || x.RoleCode.Equals(roleCode.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(x =>
                string.IsNullOrWhiteSpace(q) ||
                x.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                x.Email.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (x.Phone?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .OrderByDescending(x => x.IsOwner)
            .ThenByDescending(x => x.Active)
            .ThenBy(x => x.Name)
            .ToList();

        return Ok(new
        {
            Tenant = new
            {
                tenant.Id,
                tenant.Code,
                tenant.Name,
                Type = tenant.Type.ToString(),
                Status = tenant.Status.ToString()
            },
            Stats = new
            {
                Total = visibleRows.Count,
                Active = visibleRows.Count(x => !x.Link.IsDeleted && x.User.IsActive),
                Inactive = visibleRows.Count(x => x.Link.IsDeleted || !x.User.IsActive),
                Roles = StaffRoleTemplates.Length
            },
            RoleOptions = StaffRoleTemplates.Select(x => new
            {
                x.Code,
                x.Label,
                x.Permissions
            }),
            Total = items.Count,
            Items = items
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct = default)
    {
        var tenant = await GetCurrentTenantAsync(ct);
        if (tenant is null)
            return BadRequest(new { message = "Tenant context is required for tenant staff management." });

        var guard = await EnsureCanManageStaffAsync(tenant, ct);
        if (guard is not null)
            return guard;

        var row = await (
            from link in _db.TenantUsers.IgnoreQueryFilters().AsNoTracking()
            join user in _userManager.Users.AsNoTracking() on link.UserId equals user.Id
            where link.TenantId == tenant.Id && link.Id == id
            select new StaffRow
            {
                Link = link,
                User = user
            }
        ).FirstOrDefaultAsync(ct);

        if (row is null)
            return NotFound(new { message = "Tenant staff member not found." });

        var canView = row.Link.IsOwner || await UserHasAccessRoleAsync(row.User.Id, ResolveTenantAccessRole(tenant.Type), ct);
        if (!canView)
            return NotFound(new { message = "Tenant staff member not found." });

        return Ok(MapItem(row.Link, row.User));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantStaffRequest req, CancellationToken ct = default)
    {
        var tenant = await GetCurrentTenantAsync(ct);
        if (tenant is null)
            return BadRequest(new { message = "Tenant context is required for tenant staff management." });

        var guard = await EnsureCanManageStaffAsync(tenant, ct);
        if (guard is not null)
            return guard;

        var roleTemplate = ResolveRoleTemplate(req.RoleCode);
        if (roleTemplate is null)
            return BadRequest(new { message = "RoleCode is invalid." });

        var fullName = ToTitleCase(req.FullName);
        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { message = "FullName is required." });

        var email = NormalizeEmail(req.Email);
        if (email is null)
            return BadRequest(new { message = "Email is invalid." });

        if (string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Password is required." });

        var phoneNumber = NormalizeOptional(req.PhoneNumber, 30);
        var accessRole = ResolveTenantAccessRole(tenant.Type);
        var now = DateTimeOffset.Now;

        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            var userName = await ResolveAvailableUserNameAsync(req.UserName, email, ct);
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                EmailConfirmed = true,
                IsActive = true,
                FullName = fullName,
                PhoneNumber = phoneNumber,
                CreatedAt = now
            };

            var create = await _userManager.CreateAsync(user, req.Password);
            if (!create.Succeeded)
                return BadRequest(new { message = "Create user failed.", errors = create.Errors.Select(x => x.Description).ToArray() });
        }
        else
        {
            var currentLink = await _db.TenantUsers.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.UserId == user.Id, ct);

            if (currentLink is not null && !currentLink.IsDeleted)
                return Conflict(new { message = "This user already belongs to the current tenant." });

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.UpdatedAt = now;

            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
                return BadRequest(new { message = "Update existing user failed.", errors = update.Errors.Select(x => x.Description).ToArray() });
        }

        await EnsureAccessRoleAsync(user, accessRole);

        var link = await _db.TenantUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenant.Id && x.UserId == user.Id, ct);

        if (link is null)
        {
            link = new TenantUser
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = user.Id,
                RoleName = roleTemplate.StorageRoleName,
                IsOwner = req.IsOwner,
                IsDeleted = false,
                CreatedAt = now
            };
            _db.TenantUsers.Add(link);
        }
        else
        {
            link.RoleName = roleTemplate.StorageRoleName;
            link.IsOwner = req.IsOwner;
            link.IsDeleted = false;
            link.UpdatedAt = now;
        }

        await EnsureTenantRoleAssignmentAsync(tenant, link, roleTemplate, ct);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { version = "1.0", id = link.Id }, new { link.Id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTenantStaffRequest req, CancellationToken ct = default)
    {
        var tenant = await GetCurrentTenantAsync(ct);
        if (tenant is null)
            return BadRequest(new { message = "Tenant context is required for tenant staff management." });

        var guard = await EnsureCanManageStaffAsync(tenant, ct);
        if (guard is not null)
            return guard;

        var link = await _db.TenantUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.Id, ct);

        if (link is null)
            return NotFound(new { message = "Tenant staff member not found." });

        var user = await _userManager.FindByIdAsync(link.UserId.ToString());
        if (user is null)
            return NotFound(new { message = "Linked user not found." });

        var roleTemplate = ResolveRoleTemplate(req.RoleCode);
        if (roleTemplate is null)
            return BadRequest(new { message = "RoleCode is invalid." });

        var fullName = ToTitleCase(req.FullName);
        if (string.IsNullOrWhiteSpace(fullName))
            return BadRequest(new { message = "FullName is required." });

        var email = NormalizeEmail(req.Email);
        if (email is null)
            return BadRequest(new { message = "Email is invalid." });

        var phoneNumber = NormalizeOptional(req.PhoneNumber, 30);

        var emailOwner = await _userManager.FindByEmailAsync(email);
        if (emailOwner is not null && emailOwner.Id != user.Id)
            return Conflict(new { message = "Email already exists." });

        user.Email = email;
        user.NormalizedEmail = email.ToUpperInvariant();
        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        user.UpdatedAt = DateTimeOffset.Now;

        var updateUser = await _userManager.UpdateAsync(user);
        if (!updateUser.Succeeded)
            return BadRequest(new { message = "Update user failed.", errors = updateUser.Errors.Select(x => x.Description).ToArray() });

        link.RoleName = roleTemplate.StorageRoleName;
        link.IsOwner = req.IsOwner;
        link.IsDeleted = false;
        link.UpdatedAt = DateTimeOffset.Now;

        await EnsureAccessRoleAsync(user, ResolveTenantAccessRole(tenant.Type));
        await EnsureTenantRoleAssignmentAsync(tenant, link, roleTemplate, ct);
        await _db.SaveChangesAsync(ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id, CancellationToken ct = default)
    {
        var tenant = await GetCurrentTenantAsync(ct);
        if (tenant is null)
            return BadRequest(new { message = "Tenant context is required for tenant staff management." });

        var guard = await EnsureCanManageStaffAsync(tenant, ct);
        if (guard is not null)
            return guard;

        var link = await _db.TenantUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.Id, ct);

        if (link is null)
            return NotFound(new { message = "Tenant staff member not found." });

        if (_tenantContext.UserId.HasValue && link.UserId == _tenantContext.UserId.Value)
            return BadRequest(new { message = "You cannot deactivate your own tenant account." });

        if (!link.IsDeleted)
        {
            link.IsDeleted = true;
            link.UpdatedAt = DateTimeOffset.Now;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true, active = false });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore([FromRoute] Guid id, CancellationToken ct = default)
    {
        var tenant = await GetCurrentTenantAsync(ct);
        if (tenant is null)
            return BadRequest(new { message = "Tenant context is required for tenant staff management." });

        var guard = await EnsureCanManageStaffAsync(tenant, ct);
        if (guard is not null)
            return guard;

        var link = await _db.TenantUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.Id, ct);

        if (link is null)
            return NotFound(new { message = "Tenant staff member not found." });

        var user = await _userManager.FindByIdAsync(link.UserId.ToString());
        if (user is null)
            return NotFound(new { message = "Linked user not found." });

        if (link.IsDeleted)
        {
            link.IsDeleted = false;
            link.UpdatedAt = DateTimeOffset.Now;
            await EnsureAccessRoleAsync(user, ResolveTenantAccessRole(tenant.Type));
            var roleTemplate = ResolveRoleTemplate(link.RoleName, link.IsOwner) ?? StaffRoleTemplates[0];
            await EnsureTenantRoleAssignmentAsync(tenant, link, roleTemplate, ct);
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { ok = true, active = true });
    }

    private async Task<Tenant?> GetCurrentTenantAsync(CancellationToken ct)
    {
        if (!_tenantContext.TenantId.HasValue || _tenantContext.TenantId.Value == Guid.Empty)
            return null;

        return await _db.Tenants.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == _tenantContext.TenantId.Value && !x.IsDeleted, ct);
    }

    private async Task<List<StaffRow>> FilterVisibleRowsAsync(List<StaffRow> rows, TenantType tenantType, CancellationToken ct)
    {
        if (rows.Count == 0)
            return rows;

        var accessRole = ResolveTenantAccessRole(tenantType);
        var userIds = rows.Select(x => (Guid)x.User.Id).Distinct().ToArray();

        var accessibleUserIds = await (
            from userRole in _db.Set<IdentityUserRole<Guid>>().AsNoTracking()
            join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
                  && role.Name != null
                  && role.Name == accessRole
            select userRole.UserId
        ).Distinct().ToListAsync(ct);

        return rows
            .Where(x => x.Link.IsOwner || accessibleUserIds.Contains((Guid)x.User.Id))
            .ToList();
    }

    private async Task<bool> UserHasAccessRoleAsync(Guid userId, string accessRole, CancellationToken ct)
    {
        return await (
            from userRole in _db.Set<IdentityUserRole<Guid>>().AsNoTracking()
            join role in _db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userRole.UserId == userId
                  && role.Name != null
                  && role.Name == accessRole
            select userRole.UserId
        ).AnyAsync(ct);
    }

    private async Task<IActionResult?> EnsureCanManageStaffAsync(Tenant tenant, CancellationToken ct)
    {
        if (_tenantContext.IsAdmin)
            return null;

        if (!_tenantContext.UserId.HasValue || _tenantContext.UserId.Value == Guid.Empty)
            return Unauthorized(new { message = "Invalid user identity." });

        await EnsureCurrentUserSystemRoleAsync(tenant, ct);

        var canManage = await _permissionService.HasPermissionAsync(
            _tenantContext.UserId.Value,
            "tenant.staff.manage",
            tenant.Id,
            ct);

        return canManage ? null : Forbid();
    }

    private async Task EnsureCurrentUserSystemRoleAsync(Tenant tenant, CancellationToken ct)
    {
        if (!_tenantContext.UserId.HasValue || _tenantContext.UserId.Value == Guid.Empty)
            return;

        var link = await _db.TenantUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenant.Id &&
                     x.UserId == _tenantContext.UserId.Value &&
                     !x.IsDeleted,
                ct);

        if (link is null)
            return;

        var roleTemplate = ResolveRoleTemplate(link.RoleName, link.IsOwner) ?? StaffRoleTemplates[0];
        await EnsureTenantRoleAssignmentAsync(tenant, link, roleTemplate, ct);
        await _db.SaveChangesAsync(ct);
    }

    private static TenantStaffItem MapItem(TenantUser link, AppUser user)
    {
        var template = ResolveRoleTemplate(link.RoleName, link.IsOwner) ?? StaffRoleTemplates[0];
        return new TenantStaffItem
        {
            Id = link.Id,
            UserId = user.Id,
            UserName = user.UserName ?? "",
            Name = string.IsNullOrWhiteSpace(user.FullName) ? (user.UserName ?? user.Email ?? "Nhân viên") : user.FullName,
            Email = user.Email ?? "",
            Phone = user.PhoneNumber,
            RoleCode = template.Code,
            RoleLabel = template.Label,
            RawRoleName = link.RoleName,
            IsOwner = link.IsOwner,
            Active = !link.IsDeleted && user.IsActive,
            JoinedAt = link.CreatedAt != default ? link.CreatedAt : user.CreatedAt,
            LastActivityAt = user.UpdatedAt ?? link.UpdatedAt ?? (user.CreatedAt != default ? user.CreatedAt : link.CreatedAt),
            Permissions = template.Permissions
        };
    }

    private async Task EnsureAccessRoleAsync(AppUser user, string accessRole)
    {
        if (!await _roleManager.RoleExistsAsync(accessRole))
            throw new InvalidOperationException($"Role '{accessRole}' does not exist.");

        if (!await _userManager.IsInRoleAsync(user, accessRole))
        {
            var add = await _userManager.AddToRoleAsync(user, accessRole);
            if (!add.Succeeded)
                throw new InvalidOperationException($"Failed to assign role '{accessRole}' to '{user.UserName}'.");
        }
    }

    private async Task EnsureTenantRoleAssignmentAsync(
        Tenant tenant,
        TenantUser link,
        StaffRoleTemplate roleTemplate,
        CancellationToken ct)
    {
        var now = DateTimeOffset.Now;
        var permissionCodes = ResolvePermissionCodes(tenant.Type, roleTemplate);
        var permissionMap = new Dictionary<string, Permission>(StringComparer.OrdinalIgnoreCase);

        foreach (var permissionCode in permissionCodes)
        {
            permissionMap[permissionCode] = await EnsurePermissionAsync(permissionCode, now, ct);
        }

        var tenantRole = await EnsureTenantRoleAsync(tenant, roleTemplate, now, ct);
        await EnsureTenantRolePermissionsAsync(tenant.Id, tenantRole, permissionMap, permissionCodes, now, ct);
        await EnsureTenantUserRoleAsync(tenant.Id, link.UserId, tenantRole, now, ct);

        if (!string.Equals(link.RoleName, roleTemplate.StorageRoleName, StringComparison.OrdinalIgnoreCase))
        {
            link.RoleName = roleTemplate.StorageRoleName;
            link.UpdatedAt = now;
        }
    }

    private async Task<Permission> EnsurePermissionAsync(string code, DateTimeOffset now, CancellationToken ct)
    {
        var existing = await _db.Permissions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Code == code, ct);

        if (existing is not null)
        {
            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                existing.UpdatedAt = now;
            }

            if (!existing.IsActive)
            {
                existing.IsActive = true;
                existing.UpdatedAt = now;
            }

            return existing;
        }

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code.Replace('.', ' '),
            Category = code.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "tenant",
            SortOrder = 0,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now
        };

        _db.Permissions.Add(permission);
        return permission;
    }

    private async Task<TenantRole> EnsureTenantRoleAsync(
        Tenant tenant,
        StaffRoleTemplate roleTemplate,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var roleCode = roleTemplate.Code.ToUpperInvariant();
        var existing = await _db.TenantRoles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x => x.TenantId == tenant.Id &&
                     x.Code.ToUpper() == roleCode,
                ct);

        if (existing is null)
        {
            existing = new TenantRole
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Code = roleCode,
                Name = roleTemplate.Label,
                Description = $"System-managed {roleTemplate.Label} role for tenant portal.",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now
            };

            _db.TenantRoles.Add(existing);
            return existing;
        }

        if (existing.IsDeleted)
        {
            existing.IsDeleted = false;
            existing.UpdatedAt = now;
        }

        if (!existing.IsActive)
        {
            existing.IsActive = true;
            existing.UpdatedAt = now;
        }

        if (!string.Equals(existing.Name, roleTemplate.Label, StringComparison.Ordinal))
        {
            existing.Name = roleTemplate.Label;
            existing.UpdatedAt = now;
        }

        return existing;
    }

    private async Task EnsureTenantRolePermissionsAsync(
        Guid tenantId,
        TenantRole tenantRole,
        IReadOnlyDictionary<string, Permission> permissionMap,
        IReadOnlyCollection<string> permissionCodes,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var existingLinks = await _db.TenantRolePermissions.IgnoreQueryFilters()
            .Where(x => x.TenantId == tenantId && x.TenantRoleId == tenantRole.Id)
            .ToListAsync(ct);

        var targetPermissionIds = permissionCodes
            .Where(permissionMap.ContainsKey)
            .Select(x => permissionMap[x].Id)
            .ToHashSet();

        foreach (var permissionId in targetPermissionIds)
        {
            var current = existingLinks.FirstOrDefault(x => x.PermissionId == permissionId);
            if (current is null)
            {
                _db.TenantRolePermissions.Add(new TenantRolePermission
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    TenantRoleId = tenantRole.Id,
                    PermissionId = permissionId,
                    IsDeleted = false,
                    CreatedAt = now
                });
                continue;
            }

            if (current.IsDeleted)
            {
                current.IsDeleted = false;
                current.UpdatedAt = now;
            }
        }

        foreach (var stale in existingLinks.Where(x => !x.IsDeleted && !targetPermissionIds.Contains(x.PermissionId)))
        {
            stale.IsDeleted = true;
            stale.UpdatedAt = now;
        }
    }

    private async Task EnsureTenantUserRoleAsync(
        Guid tenantId,
        Guid userId,
        TenantRole targetRole,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var systemRoleCodes = StaffRoleTemplates
            .Select(x => x.Code.ToUpperInvariant())
            .ToArray();

        var existingLinks = await (
            from tur in _db.TenantUserRoles.IgnoreQueryFilters()
            join tr in _db.TenantRoles.IgnoreQueryFilters() on tur.TenantRoleId equals tr.Id
            where tur.TenantId == tenantId &&
                  tur.UserId == userId &&
                  systemRoleCodes.Contains(tr.Code.ToUpper())
            select tur
        ).ToListAsync(ct);

        foreach (var row in existingLinks)
        {
            if (row.TenantRoleId == targetRole.Id)
            {
                if (row.IsDeleted)
                {
                    row.IsDeleted = false;
                    row.UpdatedAt = now;
                }

                return;
            }

            if (!row.IsDeleted)
            {
                row.IsDeleted = true;
                row.UpdatedAt = now;
            }
        }

        _db.TenantUserRoles.Add(new TenantUserRole
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TenantRoleId = targetRole.Id,
            UserId = userId,
            IsDeleted = false,
            CreatedAt = now
        });
    }

    private async Task<string> ResolveAvailableUserNameAsync(string? requestedUserName, string email, CancellationToken ct)
    {
        var candidate = NormalizeUserName(requestedUserName);
        if (!string.IsNullOrWhiteSpace(candidate) && await _userManager.FindByNameAsync(candidate) is null)
            return candidate;

        var baseUserName = NormalizeUserName(email.Split('@')[0]);
        if (string.IsNullOrWhiteSpace(baseUserName))
            baseUserName = $"staff{DateTimeOffset.Now:MMdd}";

        var current = baseUserName;
        var index = 1;
        while (await _userManager.Users.AsNoTracking().AnyAsync(x => x.NormalizedUserName == current.ToUpperInvariant(), ct))
        {
            current = $"{baseUserName}{index++}";
        }

        return current;
    }

    private static StaffRoleTemplate? ResolveRoleTemplate(string? rawRoleName, bool isOwner = false)
    {
        var normalized = (rawRoleName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return isOwner ? StaffRoleTemplates[0] : null;

        var match = StaffRoleTemplates.FirstOrDefault(x =>
            x.Code.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
            x.Label.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
            x.StorageRoleName.Equals(normalized, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
            return match;

        if (isOwner || normalized.StartsWith("QL", StringComparison.OrdinalIgnoreCase))
            return StaffRoleTemplates[0];

        if (normalized.Contains("ACCOUNT", StringComparison.OrdinalIgnoreCase))
            return StaffRoleTemplates.First(x => x.Code == "accountant");

        if (normalized.Contains("SUPPORT", StringComparison.OrdinalIgnoreCase))
            return StaffRoleTemplates.First(x => x.Code == "support");

        if (normalized.Contains("TICKET", StringComparison.OrdinalIgnoreCase))
            return StaffRoleTemplates.First(x => x.Code == "ticket");

        if (normalized.Contains("OPS", StringComparison.OrdinalIgnoreCase) || normalized.Contains("VAN HANH", StringComparison.OrdinalIgnoreCase))
            return StaffRoleTemplates.First(x => x.Code == "ops");

        return StaffRoleTemplates[0];
    }

    private static string[] ResolvePermissionCodes(TenantType tenantType, StaffRoleTemplate roleTemplate)
    {
        var modulePermission = ResolveModulePermissionCode(tenantType);

        return roleTemplate.Code switch
        {
            "manager" => DistinctPermissions(
                modulePermission,
                "tenant.dashboard.read",
                "tenant.bookings.read",
                "tenant.reviews.read",
                "tenant.staff.manage",
                "tenant.finance.read",
                "tenant.reports.read",
                "tenant.settings.read"),
            "accountant" => DistinctPermissions(
                "tenant.finance.read",
                "tenant.reports.read",
                "tenant.settings.read"),
            "ops" => DistinctPermissions(
                modulePermission,
                "tenant.dashboard.read",
                "tenant.bookings.read"),
            "support" => DistinctPermissions(
                "tenant.bookings.read",
                "tenant.reviews.read"),
            "ticket" => DistinctPermissions(
                modulePermission,
                "tenant.bookings.read",
                "ticket.scan"),
            _ => DistinctPermissions(
                modulePermission,
                "tenant.dashboard.read")
        };
    }

    private static string[] DistinctPermissions(params string?[] permissionCodes)
        => permissionCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string? ResolveModulePermissionCode(TenantType tenantType)
        => tenantType switch
        {
            TenantType.Bus => "bus.trips.read",
            TenantType.Train => "train.trips.read",
            TenantType.Flight => "flight.inventory.read",
            TenantType.Hotel => "hotel.inventory.read",
            TenantType.Tour => "tour.inventory.read",
            _ => null
        };

    private static string ResolveTenantAccessRole(TenantType tenantType)
        => tenantType switch
        {
            TenantType.Bus => RoleNames.QLNX,
            TenantType.Train => RoleNames.QLVT,
            TenantType.Flight => RoleNames.QLVMM,
            TenantType.Hotel => RoleNames.QLKS,
            TenantType.Tour => RoleNames.QLTour,
            _ => RoleNames.Customer
        };

    private static string? NormalizeEmail(string? email)
    {
        email = (email ?? "").Trim();
        if (email.Length < 5 || !email.Contains('@'))
            return null;

        return email.ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            normalized = normalized[..maxLength];
        return normalized;
    }

    private static string ToTitleCase(string? input)
    {
        input = (input ?? "").Trim();
        if (input.Length == 0) return "";
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            var p = parts[i];
            if (p.Length == 0) continue;
            var first = char.ToUpperInvariant(p[0]);
            var rest = p.Length > 1 ? p[1..].ToLowerInvariant() : "";
            parts[i] = first + rest;
        }
        return string.Join(' ', parts);
    }

    private static string NormalizeUserName(string? value)
    {
        var normalized = (value ?? "").Trim().ToLowerInvariant();
        if (normalized.Length == 0)
            return "";

        normalized = Regex.Replace(normalized, "[^a-z0-9._-]", "");
        if (normalized.Length < 3)
            normalized = normalized.PadRight(3, 'x');
        if (normalized.Length > 32)
            normalized = normalized[..32];

        return normalized;
    }
}
