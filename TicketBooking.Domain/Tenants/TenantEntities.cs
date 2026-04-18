// FILE #015: TicketBooking.Domain/Tenants/TenantEntities.cs
using System;

namespace TicketBooking.Domain.Tenants
{
    public enum TenantType
    {
        Bus = 1,
        Train = 2,
        Flight = 3,
        Tour = 4,
        Hotel = 5,
        Platform = 99
    }

    public enum TenantStatus
    {
        Active = 1,
        Suspended = 2,
        Closed = 3
    }

    /// <summary>
    /// Tenant (multi-tenant owner).
    /// Soft delete applies (IsDeleted) — will be enforced by EF global filter later.
    /// </summary>
    public sealed class Tenant
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = "";        // NX001, KS001...
        public string Name { get; set; } = "";

        public TenantType Type { get; set; } = TenantType.Platform;
        public TenantStatus Status { get; set; } = TenantStatus.Active;

        // Config: hold duration per tenant (minutes), default 5
        public int HoldMinutes { get; set; } = 5;

        // Audit + soft delete (standard columns)
        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    /// <summary>
    /// Link user <-> tenant. One user can belong to many tenants.
    /// RoleName here is a "tenant scope role label" for quick filtering; fine-grained permissions come next phase.
    /// </summary>
    public sealed class TenantUser
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }

        public string RoleName { get; set; } = ""; // e.g. QLNX, QLKS, Staff, Accountant...
        public bool IsOwner { get; set; }

        public bool IsDeleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public Guid? UpdatedByUserId { get; set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}