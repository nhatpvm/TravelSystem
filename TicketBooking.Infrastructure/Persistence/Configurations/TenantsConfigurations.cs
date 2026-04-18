// FILE #016: TicketBooking.Infrastructure/Persistence/Configurations/TenantsConfigurations.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Tenants;
using TicketBooking.Infrastructure.Identity;

namespace TicketBooking.Infrastructure.Persistence.Configurations
{
    /// <summary>
    /// Phase 3: tenants schema configurations.
    /// Notes:
    /// - PK defaults (NEWSEQUENTIALID) + audit defaults will be enforced later in Phase 5 via conventions/interceptor.
    /// - Here we set schema, lengths, indexes, and relationships.
    /// </summary>
    public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> b)
        {
            b.ToTable("Tenants", "tenants");

            b.HasKey(x => x.Id);

            b.Property(x => x.Code).HasMaxLength(32).IsRequired();
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();

            b.Property(x => x.Type).HasConversion<int>().IsRequired();
            b.Property(x => x.Status).HasConversion<int>().IsRequired();

            b.Property(x => x.HoldMinutes).HasDefaultValue(5);

            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.Type);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();
            // Audit columns (types are set by EF defaults; Phase 5 will enforce conventions)
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
        }
    }

    public sealed class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
    {
        public void Configure(EntityTypeBuilder<TenantUser> b)
        {
            b.ToTable("TenantUsers", "tenants");

            b.HasKey(x => x.Id);

            b.Property(x => x.RoleName).HasMaxLength(50).IsRequired();

            // Unique: a user can be linked to the same tenant only once
            b.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();

            // Helpful indexes
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => x.TenantId);

            b.Property(x => x.RowVersion)
                .HasColumnType("varbinary(max)")
                .IsConcurrencyToken();

            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.UpdatedAt);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);

            // FK: TenantUsers.TenantId -> tenants.Tenants.Id
            // We don't configure navigation properties yet (keep entities minimal).
            b.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            // FK: TenantUsers.UserId -> dbo.AspNetUsers.Id (AppUser)
            b.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}