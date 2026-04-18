// FILE #026: TicketBooking.Infrastructure/Persistence/Configurations/TenantRolesConfigurations.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Auth;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Identity;

namespace TicketBooking.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Phase 4: tenants role-level configurations (tenant-scoped roles + permissions).
    /// All tables live in schema: tenants.
    /// </summary>
    public sealed class TenantRoleConfiguration : IEntityTypeConfiguration<TenantRole>
    {
        public void Configure(EntityTypeBuilder<TenantRole> b)
        {
            b.ToTable("TenantRoles", "tenants");

            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(80).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);

            b.Property(x => x.IsActive).HasDefaultValue(true);

            // Unique per tenant (TenantId, Code)
            b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            b.HasIndex(x => x.TenantId);
            b.HasIndex(x => x.IsActive);

            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TenantUserRoleConfiguration : IEntityTypeConfiguration<TenantUserRole>
    {
        public void Configure(EntityTypeBuilder<TenantUserRole> b)
        {
            b.ToTable("TenantUserRoles", "tenants");

            b.HasKey(x => x.Id);

            // Unique per (TenantId, TenantRoleId, UserId)
            b.HasIndex(x => new { x.TenantId, x.TenantRoleId, x.UserId }).IsUnique();
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.TenantId);

            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TenantRole>()
                .WithMany()
                .HasForeignKey(x => x.TenantRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class TenantRolePermissionConfiguration : IEntityTypeConfiguration<TenantRolePermission>
    {
        public void Configure(EntityTypeBuilder<TenantRolePermission> b)
        {
            b.ToTable("TenantRolePermissions", "tenants");

            b.HasKey(x => x.Id);

            // Unique per (TenantId, TenantRoleId, PermissionId)
            b.HasIndex(x => new { x.TenantId, x.TenantRoleId, x.PermissionId }).IsUnique();
            b.HasIndex(x => x.TenantId);

            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<TenantRole>()
                .WithMany()
                .HasForeignKey(x => x.TenantRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}