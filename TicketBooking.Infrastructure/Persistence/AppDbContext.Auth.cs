// FILE #027: TicketBooking.Infrastructure/Persistence/AppDbContext.Auth.cs
using Microsoft.EntityFrameworkCore;
using TicketBooking.Domain.Auth;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Persistence.Configurations;

namespace TicketBooking.Infrastructure.Persistence
{
    /// <summary>
    /// Phase 4: Auth + TenantRoles module
    /// - Schema: auth (Permissions, RolePermissions, UserPermissions)
    /// - Schema: tenants (TenantRoles, TenantUserRoles, TenantRolePermissions)
    /// </summary>
    public sealed partial class AppDbContext
    {
        // auth schema
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

        // tenants schema (role-level)
        public DbSet<TenantRole> TenantRoles => Set<TenantRole>();
        public DbSet<TenantUserRole> TenantUserRoles => Set<TenantUserRole>();
        public DbSet<TenantRolePermission> TenantRolePermissions => Set<TenantRolePermission>();
    }

    public static class AppDbContextAuthModel
    {
        public static void ApplyAuthModel(ModelBuilder builder)
        {
            // auth.*
            builder.ApplyConfiguration(new PermissionConfiguration());
            builder.ApplyConfiguration(new RolePermissionConfiguration());
            builder.ApplyConfiguration(new UserPermissionConfiguration());

            // tenants.*
            builder.ApplyConfiguration(new TenantRoleConfiguration());
            builder.ApplyConfiguration(new TenantUserRoleConfiguration());
            builder.ApplyConfiguration(new TenantRolePermissionConfiguration());
        }
    }
}