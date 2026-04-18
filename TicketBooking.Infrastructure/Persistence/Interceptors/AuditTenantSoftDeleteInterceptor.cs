// FILE: TicketBooking.Infrastructure/Persistence/Interceptors/AuditTenantSoftDeleteInterceptor.cs
// FILE #035 (FIX)

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TicketBooking.Infrastructure.Tenancy;

namespace TicketBooking.Infrastructure.Persistence.Interceptors
{
    /// <summary>
    /// Phase 5: SaveChanges interceptor to enforce:
    /// - Standard audit columns (CreatedAt/UpdatedAt, CreatedByUserId/UpdatedByUserId) in Vietnam time (+07:00)
    /// - Soft delete for ALL business entities with IsDeleted
    /// - Tenant enforcement for tenant-owned entities (those having TenantId property)
    /// - Block TenantId changes after insert
    /// - Admin write must switch tenant (TenantContext.TenantId must be set for write requests)
    ///
    /// IMPORTANT FIX:
    /// - ASP.NET Core Identity framework tables (AspNet*) are allowed to hard delete.
    ///   Examples:
    ///   - AspNetUserRoles
    ///   - AspNetUserClaims
    ///   - AspNetUserLogins
    ///   - AspNetUserTokens
    ///   - AspNetRoleClaims
    ///
    /// This is required because Identity APIs such as RemoveFromRolesAsync()
    /// perform real deletes on those framework join tables.
    /// </summary>
    public sealed class AuditTenantSoftDeleteInterceptor : SaveChangesInterceptor
    {
        private static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);

        private readonly ITenantContext _tenantContext;

        public AuditTenantSoftDeleteInterceptor(ITenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            ApplyRules(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            ApplyRules(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void ApplyRules(DbContext? db)
        {
            if (db is null) return;

            // Admin write requires tenant switch (TenantId header) – enforce early
            if (_tenantContext.IsAuthenticated &&
                _tenantContext.IsAdmin &&
                _tenantContext.RequiresTenantForWrite &&
                !_tenantContext.HasTenant)
            {
                throw new InvalidOperationException("Admin write requires X-TenantId (tenant switch) but TenantContext has no TenantId.");
            }

            var now = GetVietnamNow();
            var userId = _tenantContext.UserId;

            foreach (var entry in db.ChangeTracker.Entries())
            {
                if (entry.State is EntityState.Detached or EntityState.Unchanged)
                    continue;

                // Soft delete for business tables, hard delete allowed for ASP.NET Identity framework tables
                if (entry.State == EntityState.Deleted)
                {
                    if (IsIdentityFrameworkTable(entry))
                    {
                        // Let Identity framework tables delete normally
                        continue;
                    }

                    if (HasProperty(entry, "IsDeleted"))
                    {
                        entry.State = EntityState.Modified;
                        entry.Property("IsDeleted").CurrentValue = true;

                        // update audit on delete
                        SetIfExists(entry, "UpdatedAt", now);
                        if (userId.HasValue)
                            SetIfExists(entry, "UpdatedByUserId", userId.Value);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Hard delete is not allowed for entity '{entry.Metadata.ClrType.Name}'. Add IsDeleted to enable soft delete.");
                    }

                    // deleted is now modified, so audit / tenant checks below can still apply
                }

                // Audit columns
                if (entry.State == EntityState.Added)
                {
                    SetIfExists(entry, "CreatedAt", now);
                    if (userId.HasValue)
                        SetIfExists(entry, "CreatedByUserId", userId.Value);

                    SetIfExists(entry, "IsDeleted", false);

                    // Tenant enforcement for tenant-owned tables
                    EnforceTenantOnAdded(entry);
                }
                else if (entry.State == EntityState.Modified)
                {
                    SetIfExists(entry, "UpdatedAt", now);
                    if (userId.HasValue)
                        SetIfExists(entry, "UpdatedByUserId", userId.Value);

                    // Prevent changing TenantId
                    EnforceTenantNotChanged(entry);
                }
            }
        }

        private void EnforceTenantOnAdded(EntityEntry entry)
        {
            if (!HasProperty(entry, "TenantId"))
                return;

            var tenantProp = entry.Property("TenantId");
            var tenantClrType = tenantProp.Metadata.ClrType;

            // CASE 1:
            // Nullable TenantId (Guid?) => global-or-tenant-scoped entity
            // Example: auth.UserPermissions with TenantId = null (global) or specific tenant
            if (tenantClrType == typeof(Guid?))
            {
                var current = tenantProp.CurrentValue;

                // null => allowed (global row)
                if (current is null)
                    return;

                // explicit Guid => allowed
                if (current is Guid g && g != Guid.Empty)
                {
                    // If context already has tenant, payload must match context
                    if (_tenantContext.HasTenant && g != _tenantContext.TenantId!.Value)
                    {
                        throw new InvalidOperationException(
                            $"TenantId mismatch for entity '{entry.Metadata.ClrType.Name}'. " +
                            $"Payload TenantId = {g}, Context TenantId = {_tenantContext.TenantId.Value}.");
                    }

                    return;
                }

                // Guid.Empty => normalize to null for nullable TenantId
                tenantProp.CurrentValue = null;
                return;
            }

            // CASE 2:
            // Non-nullable Guid TenantId
            if (tenantClrType == typeof(Guid))
            {
                var current = tenantProp.CurrentValue;

                // If payload already contains a valid TenantId:
                // - if context has tenant => must match
                // - if context has no tenant => allow (global admin tenancy endpoints)
                if (current is Guid currentGuid && currentGuid != Guid.Empty)
                {
                    if (_tenantContext.HasTenant && currentGuid != _tenantContext.TenantId!.Value)
                    {
                        throw new InvalidOperationException(
                            $"TenantId mismatch for entity '{entry.Metadata.ClrType.Name}'. " +
                            $"Payload TenantId = {currentGuid}, Context TenantId = {_tenantContext.TenantId.Value}.");
                    }

                    return;
                }

                // If payload has no TenantId, try to fill from context
                if (_tenantContext.IsAuthenticated && !_tenantContext.IsAdmin && !_tenantContext.HasTenant)
                    throw new InvalidOperationException("Tenant context is required for non-admin operations.");

                if (_tenantContext.HasTenant)
                {
                    tenantProp.CurrentValue = _tenantContext.TenantId!.Value;
                    return;
                }

                throw new InvalidOperationException(
                    $"Cannot insert tenant-owned entity '{entry.Metadata.ClrType.Name}' without TenantId in context.");
            }
        }




        private void EnforceTenantNotChanged(EntityEntry entry)
        {
            if (!HasProperty(entry, "TenantId"))
                return;

            var tenantProp = entry.Property("TenantId");
            if (!tenantProp.IsModified)
                return;

            // For global admin tenancy endpoints:
            // middleware bypasses tenant context, but payload may legitimately change TenantId.
            // Example: Admin updates TenantUser / TenantRole / TenantUserRole / TenantRolePermission.
            if (!_tenantContext.HasTenant)
            {
                var current = tenantProp.CurrentValue;

                if (current is Guid g && g != Guid.Empty)
                    return;

                if (current == null)
                {
                    // nullable TenantId entities can be null or guid
                    return;
                }

                throw new InvalidOperationException(
                    $"TenantId cannot be empty for entity '{entry.Metadata.ClrType.Name}'.");
            }

            // Normal tenant-scoped requests: do not allow changing TenantId
            var original = tenantProp.OriginalValue;
            var currentValue = tenantProp.CurrentValue;

            if (original is Guid og && currentValue is Guid cg && og == cg)
                return;

            throw new InvalidOperationException($"TenantId cannot be changed for entity '{entry.Metadata.ClrType.Name}'.");
        }


        private static bool HasProperty(EntityEntry entry, string propName)
            => entry.Metadata.FindProperty(propName) is not null;

        private static void SetIfExists<T>(EntityEntry entry, string propName, T value)
        {
            if (entry.Metadata.FindProperty(propName) is null)
                return;

            entry.Property(propName).CurrentValue = value!;
        }

        private static bool IsIdentityFrameworkTable(EntityEntry entry)
        {
            var tableName = entry.Metadata.GetTableName();
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            return tableName.StartsWith("AspNet", StringComparison.OrdinalIgnoreCase);
        }

        private static DateTimeOffset GetVietnamNow()
        {
            var now = DateTimeOffset.UtcNow;
            return now.ToOffset(VnOffset);
        }
    }
}
