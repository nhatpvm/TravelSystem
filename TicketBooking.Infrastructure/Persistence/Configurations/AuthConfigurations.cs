// FILE #025 (UPDATE): TicketBooking.Infrastructure/Persistence/Configurations/AuthConfigurations.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Auth;
using TicketBooking.Infrastructure.Identity;

namespace TicketBooking.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Phase 4: auth schema configurations.
    /// </summary>
    public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> b)
        {
            b.ToTable("Permissions", "auth");

            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(200).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(1000);
            b.Property(x => x.Category).HasMaxLength(100);

            b.Property(x => x.SortOrder).HasDefaultValue(0);
            b.Property(x => x.IsActive).HasDefaultValue(true);

            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.Category);
            b.HasIndex(x => x.IsActive);

            b.Property(x => x.RowVersion).IsRowVersion();

            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
        }
    }

    public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> b)
        {
            b.ToTable("RolePermissions", "auth");

            b.HasKey(x => x.Id);

            b.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();

            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.HasOne<AppRole>()
                .WithMany()
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
    {
        public void Configure(EntityTypeBuilder<UserPermission> b)
        {
            b.ToTable("UserPermissions", "auth");

            b.HasKey(x => x.Id);

            b.Property(x => x.Effect).HasConversion<int>().IsRequired();
            b.Property(x => x.Reason).HasMaxLength(500);

            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.TenantId);

            // ✅ IMPORTANT:
            // Ensure uniqueness for BOTH cases:
            // 1) TenantId IS NULL (global override) -> unique by (UserId, PermissionId)
            // 2) TenantId IS NOT NULL (tenant scoped) -> unique by (UserId, PermissionId, TenantId)
            b.HasIndex(x => new { x.UserId, x.PermissionId })
                .IsUnique()
                .HasFilter("[TenantId] IS NULL");

            b.HasIndex(x => new { x.UserId, x.PermissionId, x.TenantId })
                .IsUnique()
                .HasFilter("[TenantId] IS NOT NULL");

            b.Property(x => x.RowVersion).IsRowVersion();
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne<Permission>()
                .WithMany()
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}