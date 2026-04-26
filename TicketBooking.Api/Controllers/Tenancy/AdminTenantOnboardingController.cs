using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
using TicketBooking.Domain.Auth;
using TicketBooking.Domain.Tenants;
using TicketBooking.Api.Services.Tenancy;
using TicketBooking.Infrastructure.Identity;
using TicketBooking.Infrastructure.Persistence;

namespace TicketBooking.Api.Controllers.Admin;

[ApiController]
[ApiVersion(1.0)]
[Route("api/v{version:apiVersion}/admin/tenant-onboarding")]
[Authorize(Roles = "Admin")]
public sealed class AdminTenantOnboardingController : ControllerBase
{
    private readonly PartnerOnboardingStore _store;
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;

    public AdminTenantOnboardingController(
        PartnerOnboardingStore store,
        AppDbContext db,
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager)
    {
        _store = store;
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public sealed class ReviewTenantOnboardingRequest
    {
        public string Status { get; set; } = "";
        public string? ReviewNote { get; set; }
        public string? ReviewerNote { get; set; }
        public string? RejectReason { get; set; }
        public string? NeedMoreInfoReason { get; set; }
    }

    public sealed class ProvisionTenantFromOnboardingRequest
    {
        public string TenantCode { get; set; } = "";
        public string TenantName { get; set; } = "";
        public string? ServiceType { get; set; }
        public int HoldMinutes { get; set; } = 5;
        public string OwnerEmail { get; set; } = "";
        public string OwnerFullName { get; set; } = "";
        public string? OwnerPhone { get; set; }
        public string InitialPassword { get; set; } = "";
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? q,
        CancellationToken ct = default)
    {
        var items = await _store.ListAsync(status, q, ct);
        var normalizedStatus = PartnerOnboardingStore.NormalizeStatus(status);

        return Ok(new
        {
            total = items.Count,
            pending = items.Count(x => string.Equals(x.Status, "PendingReview", StringComparison.OrdinalIgnoreCase)),
            items = items.Select(x => new
            {
                x.TrackingCode,
                x.ServiceType,
                x.BusinessName,
                x.TaxCode,
                x.Address,
                x.ContactEmail,
                x.ContactPhone,
                x.Status,
                x.SubmittedAt,
                x.ReviewedAt,
                x.ReviewedBy,
                ReviewNote = x.ReviewNote ?? x.ReviewerNote,
                x.ReviewerNote,
                x.RejectReason,
                x.NeedMoreInfoReason,
                x.TenantId,
                x.TenantCode,
                x.OwnerUserId,
                x.OwnerEmail,
                x.ProvisionedAt,
                x.ProvisionedBy,
                LegalDocument = new
                {
                    x.LegalDocument.OriginalFileName,
                    x.LegalDocument.ContentType,
                    x.LegalDocument.SizeBytes
                }
            }),
            filters = new
            {
                status = normalizedStatus,
                q
            }
        });
    }

    [HttpGet("{trackingCode}")]
    public async Task<IActionResult> GetById([FromRoute] string trackingCode, CancellationToken ct = default)
    {
        var item = await _store.GetAsync(trackingCode, ct);
        if (item is null)
        {
            return NotFound(new { message = "Onboarding request not found." });
        }

        return Ok(item);
    }

    [HttpPost("{trackingCode}/review")]
    public async Task<IActionResult> Review(
        [FromRoute] string trackingCode,
        [FromBody] ReviewTenantOnboardingRequest req,
        CancellationToken ct = default)
    {
        var status = PartnerOnboardingStore.NormalizeStatus(req.Status);
        if (status is null)
        {
            return BadRequest(new { message = "Status is invalid. Use PendingReview, Approved, Rejected or NeedsMoreInfo." });
        }

        var rejectReason = NormalizeOptional(req.RejectReason);
        var needMoreInfoReason = NormalizeOptional(req.NeedMoreInfoReason);
        if (string.Equals(status, "Rejected", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(rejectReason))
        {
            return BadRequest(new { message = "RejectReason is required when rejecting an onboarding request." });
        }

        if (string.Equals(status, "NeedsMoreInfo", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(needMoreInfoReason))
        {
            return BadRequest(new { message = "NeedMoreInfoReason is required when requesting more information." });
        }

        var reviewer = User?.Identity?.Name
            ?? User?.Claims.FirstOrDefault(x => x.Type == System.Security.Claims.ClaimTypes.Email)?.Value
            ?? User?.Claims.FirstOrDefault(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var reviewNote = NormalizeOptional(req.ReviewNote) ?? NormalizeOptional(req.ReviewerNote);
        var updated = await _store.ReviewAsync(trackingCode, status, reviewer, reviewNote, rejectReason, needMoreInfoReason, ct);
        if (updated is null)
        {
            return NotFound(new { message = "Onboarding request not found." });
        }

        return Ok(new
        {
            updated.TrackingCode,
            updated.Status,
            updated.ReviewedAt,
            updated.ReviewedBy,
            ReviewNote = updated.ReviewNote ?? updated.ReviewerNote,
            updated.ReviewerNote,
            updated.RejectReason,
            updated.NeedMoreInfoReason,
            updated.TenantId,
            updated.TenantCode,
            updated.OwnerUserId,
            updated.OwnerEmail,
            updated.ProvisionedAt,
            updated.ProvisionedBy
        });
    }

    [HttpPost("{trackingCode}/provision")]
    public async Task<IActionResult> ProvisionTenant(
        [FromRoute] string trackingCode,
        [FromBody] ProvisionTenantFromOnboardingRequest req,
        CancellationToken ct = default)
    {
        var onboarding = await _store.GetAsync(trackingCode, ct);
        if (onboarding is null)
        {
            return NotFound(new { message = "Onboarding request not found." });
        }

        if (!string.Equals(onboarding.Status, "Approved", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Please approve the onboarding request before provisioning a tenant." });
        }

        if (onboarding.TenantId.HasValue)
        {
            var existingTenant = await _db.Tenants.IgnoreQueryFilters()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == onboarding.TenantId.Value, ct);

            return Ok(new
            {
                alreadyProvisioned = true,
                tenant = existingTenant is null ? null : new
                {
                    existingTenant.Id,
                    existingTenant.Code,
                    existingTenant.Name,
                    Type = existingTenant.Type.ToString(),
                    Status = existingTenant.Status.ToString(),
                    existingTenant.HoldMinutes
                },
                onboarding.TenantId,
                onboarding.TenantCode,
                onboarding.OwnerUserId,
                onboarding.OwnerEmail,
                onboarding.ProvisionedAt
            });
        }

        var serviceType = PartnerOnboardingStore.NormalizeServiceType(req.ServiceType) ??
                          PartnerOnboardingStore.NormalizeServiceType(onboarding.ServiceType);
        if (serviceType is null)
        {
            return BadRequest(new { message = "ServiceType is invalid. Use bus, train, flight, hotel or tour." });
        }

        var tenantType = MapTenantType(serviceType);
        var tenantCode = NormalizeTenantCode(req.TenantCode);
        var tenantName = NormalizeRequired(req.TenantName, onboarding.BusinessName, "TenantName is required.");
        var ownerEmail = NormalizeEmail(req.OwnerEmail) ?? NormalizeEmail(onboarding.ContactEmail);
        var ownerFullName = NormalizeRequired(req.OwnerFullName, onboarding.BusinessName, "OwnerFullName is required.");
        var ownerPhone = NormalizeOptional(req.OwnerPhone) ?? NormalizeOptional(onboarding.ContactPhone);
        var password = req.InitialPassword?.Trim() ?? "";
        var holdMinutes = NormalizeHoldMinutes(req.HoldMinutes);

        if (tenantCode.Length == 0)
        {
            return BadRequest(new { message = "TenantCode is required." });
        }

        if (ownerEmail is null)
        {
            return BadRequest(new { message = "OwnerEmail is invalid or missing." });
        }

        if (password.Length > 0 && password.Length < 8)
        {
            return BadRequest(new { message = "InitialPassword must be at least 8 characters when creating a new owner user." });
        }

        var codeExists = await _db.Tenants.IgnoreQueryFilters()
            .AnyAsync(x => x.Code == tenantCode, ct);
        if (codeExists)
        {
            return Conflict(new { message = $"Tenant code '{tenantCode}' already exists." });
        }

        var actorUserId = GetCurrentUserId();
        var actor = GetReviewerIdentity();
        var now = DateTimeOffset.Now;
        var ownerRole = ResolveOwnerGlobalRole(tenantType);

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Code = tenantCode,
            Name = tenantName,
            Type = tenantType,
            Status = TenantStatus.Active,
            HoldMinutes = holdMinutes,
            IsDeleted = false,
            CreatedAt = now,
            CreatedByUserId = actorUserId
        };

        _db.Tenants.Add(tenant);

        var owner = await _userManager.FindByEmailAsync(ownerEmail);
        var ownerCreated = false;
        if (owner is null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return BadRequest(new { message = "InitialPassword is required when the owner user does not exist." });
            }

            owner = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = await BuildUniqueUserNameAsync(ownerEmail),
                Email = ownerEmail,
                NormalizedEmail = ownerEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                FullName = ownerFullName,
                PhoneNumber = ownerPhone,
                IsActive = true,
                CreatedAt = now
            };

            var createResult = await _userManager.CreateAsync(owner, password);
            if (!createResult.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Create owner user failed.",
                    errors = createResult.Errors.Select(x => x.Description).ToArray()
                });
            }

            ownerCreated = true;
        }
        else
        {
            owner.FullName = string.IsNullOrWhiteSpace(owner.FullName) ? ownerFullName : owner.FullName;
            owner.PhoneNumber = string.IsNullOrWhiteSpace(owner.PhoneNumber) ? ownerPhone : owner.PhoneNumber;
            owner.IsActive = true;
            owner.EmailConfirmed = true;
            owner.UpdatedAt = now;

            var updateResult = await _userManager.UpdateAsync(owner);
            if (!updateResult.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Update owner user failed.",
                    errors = updateResult.Errors.Select(x => x.Description).ToArray()
                });
            }
        }

        await EnsureIdentityRoleAsync(ownerRole);
        if (!await _userManager.IsInRoleAsync(owner, ownerRole))
        {
            var roleResult = await _userManager.AddToRoleAsync(owner, ownerRole);
            if (!roleResult.Succeeded)
            {
                return BadRequest(new
                {
                    message = "Assign owner global role failed.",
                    errors = roleResult.Errors.Select(x => x.Description).ToArray()
                });
            }
        }

        await EnsureTenantUserAsync(tenant.Id, owner.Id, ownerRole, isOwner: true, now, actorUserId, ct);
        var managerRole = await EnsureTenantSystemRolesAsync(tenant, now, actorUserId, ct);
        await EnsureTenantUserRoleAsync(tenant.Id, owner.Id, managerRole.Id, now, actorUserId, ct);

        await _db.SaveChangesAsync(ct);

        var updated = await _store.MarkProvisionedAsync(
            onboarding.TrackingCode,
            tenant.Id,
            tenant.Code,
            owner.Id,
            ownerEmail,
            actor,
            ct);

        return Ok(new
        {
            alreadyProvisioned = false,
            tenant = new
            {
                tenant.Id,
                tenant.Code,
                tenant.Name,
                Type = tenant.Type.ToString(),
                Status = tenant.Status.ToString(),
                tenant.HoldMinutes
            },
            owner = new
            {
                owner.Id,
                owner.UserName,
                owner.Email,
                owner.FullName,
                owner.PhoneNumber,
                Created = ownerCreated,
                GlobalRole = ownerRole
            },
            tenantRole = new
            {
                managerRole.Id,
                managerRole.Code,
                managerRole.Name
            },
            onboarding = updated
        });
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value
                  ?? User?.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    private string? GetReviewerIdentity()
        => User?.Identity?.Name
           ?? User?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value
           ?? User?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;

    private static TenantType MapTenantType(string serviceType)
        => serviceType.ToLowerInvariant() switch
        {
            "bus" => TenantType.Bus,
            "train" => TenantType.Train,
            "flight" => TenantType.Flight,
            "hotel" => TenantType.Hotel,
            "tour" => TenantType.Tour,
            _ => TenantType.Platform
        };

    private static string ResolveOwnerGlobalRole(TenantType type)
        => type switch
        {
            TenantType.Bus => RoleNames.QLNX,
            TenantType.Train => RoleNames.QLVT,
            TenantType.Flight => RoleNames.QLVMM,
            TenantType.Hotel => RoleNames.QLKS,
            TenantType.Tour => RoleNames.QLTour,
            _ => RoleNames.Customer
        };

    private static string NormalizeTenantCode(string? value)
    {
        var normalized = Regex.Replace((value ?? "").Trim().ToUpperInvariant(), "[^A-Z0-9_-]", "");
        return normalized.Length > 32 ? normalized[..32] : normalized;
    }

    private static string NormalizeRequired(string? primary, string? fallback, string errorMessage)
    {
        var value = NormalizeOptional(primary) ?? NormalizeOptional(fallback);
        return value ?? throw new InvalidOperationException(errorMessage);
    }

    private static string? NormalizeEmail(string? value)
    {
        var normalized = NormalizeOptional(value)?.ToLowerInvariant();
        if (normalized is null || !normalized.Contains('@') || normalized.Length > 256)
        {
            return null;
        }

        return normalized;
    }

    private static int NormalizeHoldMinutes(int value)
    {
        if (value < 1)
        {
            return 1;
        }

        return value > 60 ? 60 : value;
    }

    private async Task<string> BuildUniqueUserNameAsync(string email)
    {
        var localPart = email.Split('@', 2)[0].ToLowerInvariant();
        var baseName = Regex.Replace(localPart, "[^a-z0-9._-]", "");
        if (baseName.Length < 3)
        {
            baseName = $"partner{baseName}";
        }

        if (baseName.Length > 26)
        {
            baseName = baseName[..26];
        }

        var candidate = baseName;
        var index = 1;
        while (await _userManager.FindByNameAsync(candidate) is not null)
        {
            candidate = $"{baseName}{index}";
            if (candidate.Length > 32)
            {
                candidate = $"{baseName[..Math.Min(baseName.Length, 26)]}{index}";
            }

            index++;
        }

        return candidate;
    }

    private async Task EnsureIdentityRoleAsync(string roleName)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            return;
        }

        await _roleManager.CreateAsync(new AppRole(roleName));
    }

    private async Task EnsureTenantUserAsync(
        Guid tenantId,
        Guid userId,
        string roleName,
        bool isOwner,
        DateTimeOffset now,
        Guid? actorUserId,
        CancellationToken ct)
    {
        var link = await _db.TenantUsers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId, ct);

        if (link is null)
        {
            _db.TenantUsers.Add(new TenantUser
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                RoleName = roleName,
                IsOwner = isOwner,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            });
            return;
        }

        link.RoleName = roleName;
        link.IsOwner = isOwner;
        link.IsDeleted = false;
        link.UpdatedAt = now;
        link.UpdatedByUserId = actorUserId;
    }

    private async Task<TenantRole> EnsureTenantSystemRolesAsync(
        Tenant tenant,
        DateTimeOffset now,
        Guid? actorUserId,
        CancellationToken ct)
    {
        var modulePermission = ResolveModulePermissionCode(tenant.Type);
        var templates = new[]
        {
            new TenantRoleTemplate("MANAGER", "Quan ly", "Tenant owner and manager role.", BuildDistinctPermissions(
                modulePermission,
                "cms.posts.read",
                "cms.posts.write",
                "cms.posts.publish",
                "cms.media.manage",
                "cms.taxonomy.manage",
                "cms.redirects.manage",
                "cms.site-settings.manage",
                "cms.seo.audit",
                "tenant.dashboard.read",
                "tenant.bookings.read",
                "tenant.reviews.read",
                "tenant.staff.manage",
                "tenant.finance.read",
                "tenant.reports.read",
                "tenant.settings.read")),
            new TenantRoleTemplate("ACCOUNTANT", "Ke toan", "Tenant finance role.", BuildDistinctPermissions(
                "tenant.finance.read",
                "tenant.reports.read",
                "tenant.settings.read")),
            new TenantRoleTemplate("OPS", "Van hanh", "Tenant operations role.", BuildDistinctPermissions(
                modulePermission,
                "tenant.dashboard.read",
                "tenant.bookings.read")),
            new TenantRoleTemplate("SUPPORT", "CSKH", "Tenant support role.", BuildDistinctPermissions(
                "tenant.bookings.read",
                "tenant.reviews.read")),
            new TenantRoleTemplate("TICKET", "Dai ly ve", "Tenant ticket role.", BuildDistinctPermissions(
                "tenant.bookings.read",
                modulePermission,
                "ticket.scan"))
        };

        var manager = default(TenantRole);
        foreach (var template in templates)
        {
            var role = await EnsureTenantRoleAsync(tenant.Id, template, now, actorUserId, ct);
            manager ??= role;

            foreach (var permissionCode in template.PermissionCodes)
            {
                var permission = await EnsurePermissionAsync(permissionCode, now, actorUserId, ct);
                await EnsureTenantRolePermissionAsync(tenant.Id, role.Id, permission.Id, now, actorUserId, ct);
            }
        }

        return manager!;
    }

    private sealed record TenantRoleTemplate(
        string Code,
        string Name,
        string Description,
        IReadOnlyList<string> PermissionCodes);

    private static IReadOnlyList<string> BuildDistinctPermissions(params string?[] values)
        => values
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

    private async Task<TenantRole> EnsureTenantRoleAsync(
        Guid tenantId,
        TenantRoleTemplate template,
        DateTimeOffset now,
        Guid? actorUserId,
        CancellationToken ct)
    {
        var role = await _db.TenantRoles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == template.Code, ct);

        if (role is null)
        {
            role = new TenantRole
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Code = template.Code,
                Name = template.Name,
                Description = template.Description,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };

            _db.TenantRoles.Add(role);
            return role;
        }

        role.Name = template.Name;
        role.Description = template.Description;
        role.IsActive = true;
        role.IsDeleted = false;
        role.UpdatedAt = now;
        role.UpdatedByUserId = actorUserId;
        return role;
    }

    private async Task<Permission> EnsurePermissionAsync(
        string code,
        DateTimeOffset now,
        Guid? actorUserId,
        CancellationToken ct)
    {
        var permission = await _db.Permissions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Code == code, ct);

        if (permission is null)
        {
            permission = new Permission
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = code.Replace('.', ' '),
                Category = code.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "tenant",
                SortOrder = 0,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            };

            _db.Permissions.Add(permission);
            return permission;
        }

        permission.IsActive = true;
        permission.IsDeleted = false;
        permission.UpdatedAt = now;
        permission.UpdatedByUserId = actorUserId;
        return permission;
    }

    private async Task EnsureTenantRolePermissionAsync(
        Guid tenantId,
        Guid tenantRoleId,
        Guid permissionId,
        DateTimeOffset now,
        Guid? actorUserId,
        CancellationToken ct)
    {
        var link = await _db.TenantRolePermissions.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.TenantRoleId == tenantRoleId &&
                x.PermissionId == permissionId,
                ct);

        if (link is null)
        {
            _db.TenantRolePermissions.Add(new TenantRolePermission
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TenantRoleId = tenantRoleId,
                PermissionId = permissionId,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            });
            return;
        }

        link.IsDeleted = false;
        link.UpdatedAt = now;
        link.UpdatedByUserId = actorUserId;
    }

    private async Task EnsureTenantUserRoleAsync(
        Guid tenantId,
        Guid userId,
        Guid tenantRoleId,
        DateTimeOffset now,
        Guid? actorUserId,
        CancellationToken ct)
    {
        var link = await _db.TenantUserRoles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenantId &&
                x.TenantRoleId == tenantRoleId &&
                x.UserId == userId,
                ct);

        if (link is null)
        {
            _db.TenantUserRoles.Add(new TenantUserRole
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                TenantRoleId = tenantRoleId,
                UserId = userId,
                IsDeleted = false,
                CreatedAt = now,
                CreatedByUserId = actorUserId
            });
            return;
        }

        link.IsDeleted = false;
        link.UpdatedAt = now;
        link.UpdatedByUserId = actorUserId;
    }
}
