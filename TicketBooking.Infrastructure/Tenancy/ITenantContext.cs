// FILE #019: TicketBooking.Infrastructure/Tenancy/ITenantContext.cs
using System;
using System.Collections.Generic;

namespace TicketBooking.Infrastructure.Tenancy
{
    /// <summary>
    /// Scoped tenant context for the current request.
    /// Set by middleware (Phase 3) and later used by:
    /// - EF Core query filters (TenantId + SoftDelete)
    /// - Enforcement rules (Admin write must switch tenant)
    /// </summary>
    public interface ITenantContext
    {
        Guid? TenantId { get; }
        bool HasTenant { get; }

        Guid? UserId { get; }
        bool IsAuthenticated { get; }

        bool IsAdmin { get; }

        /// <summary>
        /// TenantIds the current user belongs to (from tenants.TenantUsers).
        /// Used to validate X-TenantId for non-admin when user belongs to multiple tenants.
        /// </summary>
        IReadOnlyCollection<Guid> UserTenantIds { get; }

        /// <summary>
        /// True when request is a write (POST/PUT/PATCH/DELETE) and admin must provide X-TenantId.
        /// Middleware sets this for diagnostics/logging.
        /// </summary>
        bool RequiresTenantForWrite { get; }

        void SetUser(Guid userId, bool isAdmin, IReadOnlyCollection<Guid> userTenantIds);
        void SetTenant(Guid? tenantId);
        void SetRequiresTenantForWrite(bool requires);

        void Clear();
    }

    /// <summary>
    /// Default implementation (Scoped).
    /// </summary>
    public sealed class TenantContext : ITenantContext
    {
        private readonly HashSet<Guid> _userTenantIds = new();

        public Guid? TenantId { get; private set; }
        public bool HasTenant => TenantId.HasValue && TenantId.Value != Guid.Empty;

        public Guid? UserId { get; private set; }
        public bool IsAuthenticated => UserId.HasValue && UserId.Value != Guid.Empty;

        public bool IsAdmin { get; private set; }

        public IReadOnlyCollection<Guid> UserTenantIds => _userTenantIds;

        public bool RequiresTenantForWrite { get; private set; }

        public void SetUser(Guid userId, bool isAdmin, IReadOnlyCollection<Guid> userTenantIds)
        {
            UserId = userId;
            IsAdmin = isAdmin;

            _userTenantIds.Clear();
            if (userTenantIds is not null)
            {
                foreach (var id in userTenantIds)
                {
                    if (id != Guid.Empty)
                        _userTenantIds.Add(id);
                }
            }
        }

        public void SetTenant(Guid? tenantId)
        {
            TenantId = (tenantId.HasValue && tenantId.Value != Guid.Empty) ? tenantId : null;
        }

        public void SetRequiresTenantForWrite(bool requires)
        {
            RequiresTenantForWrite = requires;
        }

        public void Clear()
        {
            TenantId = null;
            UserId = null;
            IsAdmin = false;
            RequiresTenantForWrite = false;
            _userTenantIds.Clear();
        }
    }

    public static class HttpMethodHelper
    {
        public static bool IsWriteMethod(string? method)
        {
            if (string.IsNullOrWhiteSpace(method)) return false;
            return method.Equals("POST", StringComparison.OrdinalIgnoreCase)
                || method.Equals("PUT", StringComparison.OrdinalIgnoreCase)
                || method.Equals("PATCH", StringComparison.OrdinalIgnoreCase)
                || method.Equals("DELETE", StringComparison.OrdinalIgnoreCase);
        }
    }
}