// FILE #024: TicketBooking.Domain/Tenants/TenantRoleEntities.cs
using System;

namespace TicketBooking.Domain.Tenants
{
    /// <summary>
    /// Tenants role (role level nhỏ) - scoped within a tenant.
    /// Example: "NX_TicketAgent", "NX_Accountant", "KS_Receptionist"...
    /// </summary>
    public sealed class TenantRole
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }
        public string Code { get; set; } = "";      // unique per tenant
        public string Name { get; set; } = "";
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Assign tenant roles to a user (many-to-many).
    /// </summary>
    public sealed class TenantUserRole
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }
        public Guid TenantRoleId { get; set; }
        public Guid UserId { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Map tenant role -> permission (scoped within a tenant).
    /// </summary>
    public sealed class TenantRolePermission
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }
        public Guid TenantRoleId { get; set; }
        public Guid PermissionId { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}