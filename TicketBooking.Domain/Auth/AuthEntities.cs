// FILE #023: TicketBooking.Domain/Auth/AuthEntities.cs
using System;

namespace TicketBooking.Domain.Auth
{
    /// <summary>
    /// Permission effect for overrides.
    /// Allow: grants permission; Deny: explicitly blocks even if roles grant it.
    /// </summary>
    public enum PermissionEffect
    {
        Allow = 1,
        Deny = 2
    }

    /// <summary>
    /// auth.Permissions
    /// Canonical permission codes (string) like: bus.trips.read, bus.trips.write, tenants.manage, cms.posts.publish...
    /// </summary>
    public sealed class Permission
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = "";          // unique
        public string Name { get; set; } = "";
        public string? Description { get; set; }

        public string? Category { get; set; }           // optional grouping: bus/train/flight/cms/tenants...
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        // Standard columns (Phase 5 will enforce conventions)
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// auth.RolePermissions (Identity Role -> Permission)
    /// Global mapping by Identity RoleId (GUID).
    /// </summary>
    public sealed class RolePermission
    {
        public Guid Id { get; set; }

        public Guid RoleId { get; set; }        // dbo.AspNetRoles.Id
        public Guid PermissionId { get; set; }  // auth.Permissions.Id

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// auth.UserPermissions (User -> Permission override)
    /// Can be tenant-scoped (TenantId != null) or global override (TenantId null).
    /// Deny overrides Allow and role grants.
    /// </summary>
    public sealed class UserPermission
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }         // dbo.AspNetUsers.Id
        public Guid PermissionId { get; set; }   // auth.Permissions.Id

        public Guid? TenantId { get; set; }      // null = global; set = apply only within that tenant context
        public PermissionEffect Effect { get; set; } = PermissionEffect.Allow;

        public string? Reason { get; set; }      // optional audit hint

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}