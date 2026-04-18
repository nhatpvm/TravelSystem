using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Tours;

namespace TicketBooking.Infrastructure.Persistence.Tours;

public sealed class TourPackageConfiguration : IEntityTypeConfiguration<TourPackage>
{
    public void Configure(EntityTypeBuilder<TourPackage> b)
    {
        b.ToTable("TourPackages", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(300).IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.PricingRuleJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourId, x.IsDefault });
        b.HasIndex(x => new { x.TenantId, x.Status, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.Tour)
            .WithMany(x => x.Packages)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Components)
            .WithOne(x => x.TourPackage)
            .HasForeignKey(x => x.TourPackageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPackageComponentConfiguration : IEntityTypeConfiguration<TourPackageComponent>
{
    public void Configure(EntityTypeBuilder<TourPackageComponent> b)
    {
        b.ToTable("TourPackageComponents", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(300).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourPackageId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourPackageId, x.SortOrder });
        b.HasIndex(x => new { x.TenantId, x.TourPackageId, x.ComponentType });
        b.HasIndex(x => new { x.TenantId, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.TourPackage)
            .WithMany(x => x.Components)
            .HasForeignKey(x => x.TourPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Options)
            .WithOne(x => x.TourPackageComponent)
            .HasForeignKey(x => x.TourPackageComponentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPackageComponentOptionConfiguration : IEntityTypeConfiguration<TourPackageComponentOption>
{
    public void Configure(EntityTypeBuilder<TourPackageComponentOption> b)
    {
        b.ToTable("TourPackageComponentOptions", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(300).IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.SearchTemplateJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.RuleJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.PriceOverride).HasPrecision(18, 2);
        b.Property(x => x.CostOverride).HasPrecision(18, 2);
        b.Property(x => x.MarkupPercent).HasPrecision(9, 4);
        b.Property(x => x.MarkupAmount).HasPrecision(18, 2);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourPackageComponentId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourPackageComponentId, x.SortOrder });
        b.HasIndex(x => new { x.TenantId, x.SourceType, x.SourceEntityId });
        b.HasIndex(x => new { x.TenantId, x.IsActive, x.IsDeleted });

        b.HasOne(x => x.TourPackageComponent)
            .WithMany(x => x.Options)
            .HasForeignKey(x => x.TourPackageComponentId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.ScheduleOverrides)
            .WithOne(x => x.TourPackageComponentOption)
            .HasForeignKey(x => x.TourPackageComponentOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPackageScheduleOptionOverrideConfiguration : IEntityTypeConfiguration<TourPackageScheduleOptionOverride>
{
    public void Configure(EntityTypeBuilder<TourPackageScheduleOptionOverride> b)
    {
        b.ToTable("TourPackageScheduleOptionOverrides", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.CurrencyCode).HasMaxLength(10);
        b.Property(x => x.PriceOverride).HasPrecision(18, 2);
        b.Property(x => x.CostOverride).HasPrecision(18, 2);
        b.Property(x => x.BoundSnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.RuleOverrideJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourScheduleId, x.TourPackageComponentOptionId }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourScheduleId, x.Status, x.IsActive, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.BoundSourceEntityId });

        b.HasOne(x => x.TourSchedule)
            .WithMany(x => x.PackageOptionOverrides)
            .HasForeignKey(x => x.TourScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TourPackageComponentOption)
            .WithMany(x => x.ScheduleOverrides)
            .HasForeignKey(x => x.TourPackageComponentOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPackageReservationConfiguration : IEntityTypeConfiguration<TourPackageReservation>
{
    public void Configure(EntityTypeBuilder<TourPackageReservation> b)
    {
        b.ToTable("TourPackageReservations", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.HoldToken).HasMaxLength(100).IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.PackageSubtotalAmount).HasPrecision(18, 2);
        b.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.Notes).HasMaxLength(4000);
        b.Property(x => x.FailureReason).HasMaxLength(2000);

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.HoldToken }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourScheduleId, x.Status, x.HoldExpiresAt });
        b.HasIndex(x => new { x.TenantId, x.UserId, x.Status, x.IsDeleted });

        b.HasOne(x => x.Tour)
            .WithMany()
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TourSchedule)
            .WithMany()
            .HasForeignKey(x => x.TourScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TourPackage)
            .WithMany()
            .HasForeignKey(x => x.TourPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Items)
            .WithOne(x => x.TourPackageReservation)
            .HasForeignKey(x => x.TourPackageReservationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPackageReservationItemConfiguration : IEntityTypeConfiguration<TourPackageReservationItem>
{
    public void Configure(EntityTypeBuilder<TourPackageReservationItem> b)
    {
        b.ToTable("TourPackageReservationItems", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.LineAmount).HasPrecision(18, 2);
        b.Property(x => x.SourceHoldToken).HasMaxLength(100);
        b.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.Notes).HasMaxLength(4000);
        b.Property(x => x.ErrorMessage).HasMaxLength(2000);

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourPackageReservationId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.SourceType, x.SourceEntityId });
        b.HasIndex(x => new { x.TenantId, x.SourceHoldToken });

        b.HasOne(x => x.TourPackageReservation)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.TourPackageReservationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPackageBookingConfiguration : IEntityTypeConfiguration<TourPackageBooking>
{
    public void Configure(EntityTypeBuilder<TourPackageBooking> b)
    {
        b.ToTable("TourPackageBookings", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.PackageSubtotalAmount).HasPrecision(18, 2);
        b.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.Notes).HasMaxLength(4000);
        b.Property(x => x.FailureReason).HasMaxLength(2000);

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourPackageReservationId }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.TourScheduleId, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.UserId, x.Status, x.IsDeleted });

        b.HasOne(x => x.Tour)
            .WithMany()
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TourSchedule)
            .WithMany()
            .HasForeignKey(x => x.TourScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TourPackage)
            .WithMany()
            .HasForeignKey(x => x.TourPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TourPackageReservation)
            .WithMany()
            .HasForeignKey(x => x.TourPackageReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Items)
            .WithOne(x => x.TourPackageBooking)
            .HasForeignKey(x => x.TourPackageBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Cancellations)
            .WithOne(x => x.TourPackageBooking)
            .HasForeignKey(x => x.TourPackageBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Refunds)
            .WithOne(x => x.TourPackageBooking)
            .HasForeignKey(x => x.TourPackageBookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPackageBookingItemConfiguration : IEntityTypeConfiguration<TourPackageBookingItem>
{
    public void Configure(EntityTypeBuilder<TourPackageBookingItem> b)
    {
        b.ToTable("TourPackageBookingItems", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.LineAmount).HasPrecision(18, 2);
        b.Property(x => x.SourceHoldToken).HasMaxLength(100);
        b.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.Notes).HasMaxLength(4000);
        b.Property(x => x.ErrorMessage).HasMaxLength(2000);

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourPackageBookingId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.TourPackageReservationItemId }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.SourceType, x.SourceEntityId });
        b.HasIndex(x => new { x.TenantId, x.SourceHoldToken });

        b.HasOne(x => x.TourPackageBooking)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.TourPackageBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.CancellationItems)
            .WithOne(x => x.TourPackageBookingItem)
            .HasForeignKey(x => x.TourPackageBookingItemId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Refunds)
            .WithOne(x => x.TourPackageBookingItem)
            .HasForeignKey(x => x.TourPackageBookingItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPackageCancellationConfiguration : IEntityTypeConfiguration<TourPackageCancellation>
{
    public void Configure(EntityTypeBuilder<TourPackageCancellation> b)
    {
        b.ToTable("TourPackageCancellations", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.PenaltyAmount).HasPrecision(18, 2);
        b.Property(x => x.RefundAmount).HasPrecision(18, 2);
        b.Property(x => x.PolicyCode).HasMaxLength(50);
        b.Property(x => x.PolicyName).HasMaxLength(300);
        b.Property(x => x.ReasonCode).HasMaxLength(100);
        b.Property(x => x.ReasonText).HasMaxLength(2000);
        b.Property(x => x.OverrideNote).HasMaxLength(2000);
        b.Property(x => x.FailureReason).HasMaxLength(2000);
        b.Property(x => x.BookingSnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.DecisionSnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.Notes).HasMaxLength(4000);

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourPackageBookingId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.TourId, x.TourScheduleId, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.RequestedByUserId, x.Status, x.IsDeleted });

        b.HasOne(x => x.TourPackageBooking)
            .WithMany(x => x.Cancellations)
            .HasForeignKey(x => x.TourPackageBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Items)
            .WithOne(x => x.TourPackageCancellation)
            .HasForeignKey(x => x.TourPackageCancellationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Refunds)
            .WithOne(x => x.TourPackageCancellation)
            .HasForeignKey(x => x.TourPackageCancellationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPackageRescheduleConfiguration : IEntityTypeConfiguration<TourPackageReschedule>
{
    public void Configure(EntityTypeBuilder<TourPackageReschedule> b)
    {
        b.ToTable("TourPackageReschedules", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.ClientToken).HasMaxLength(100).IsRequired();
        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.SourcePackageSubtotalAmount).HasPrecision(18, 2);
        b.Property(x => x.TargetPackageSubtotalAmount).HasPrecision(18, 2);
        b.Property(x => x.PriceDifferenceAmount).HasPrecision(18, 2);
        b.Property(x => x.ReasonCode).HasMaxLength(100);
        b.Property(x => x.ReasonText).HasMaxLength(2000);
        b.Property(x => x.OverrideNote).HasMaxLength(2000);
        b.Property(x => x.FailureReason).HasMaxLength(2000);
        b.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.ResolutionSnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.Notes).HasMaxLength(4000);

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.SourceTourPackageBookingId, x.ClientToken }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.SourceTourPackageBookingId, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.TenantId, x.TargetTourPackageReservationId });
        b.HasIndex(x => new { x.TenantId, x.TargetTourPackageBookingId });
        b.HasIndex(x => new { x.TenantId, x.TargetTourScheduleId, x.Status, x.HoldExpiresAt });

        b.HasOne(x => x.Tour)
            .WithMany()
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SourceTourPackageBooking)
            .WithMany(x => x.SourceReschedules)
            .HasForeignKey(x => x.SourceTourPackageBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SourceTourPackageReservation)
            .WithMany(x => x.SourceReschedules)
            .HasForeignKey(x => x.SourceTourPackageReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SourceTourSchedule)
            .WithMany()
            .HasForeignKey(x => x.SourceTourScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SourceTourPackage)
            .WithMany()
            .HasForeignKey(x => x.SourceTourPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TargetTourSchedule)
            .WithMany()
            .HasForeignKey(x => x.TargetTourScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TargetTourPackage)
            .WithMany()
            .HasForeignKey(x => x.TargetTourPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TargetTourPackageReservation)
            .WithMany(x => x.TargetReschedules)
            .HasForeignKey(x => x.TargetTourPackageReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TargetTourPackageBooking)
            .WithMany(x => x.TargetReschedules)
            .HasForeignKey(x => x.TargetTourPackageBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.SourceTourPackageCancellation)
            .WithMany()
            .HasForeignKey(x => x.SourceTourPackageCancellationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPackageCancellationItemConfiguration : IEntityTypeConfiguration<TourPackageCancellationItem>
{
    public void Configure(EntityTypeBuilder<TourPackageCancellationItem> b)
    {
        b.ToTable("TourPackageCancellationItems", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.GrossLineAmount).HasPrecision(18, 2);
        b.Property(x => x.PenaltyAmount).HasPrecision(18, 2);
        b.Property(x => x.RefundAmount).HasPrecision(18, 2);
        b.Property(x => x.PolicyRuleJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.BookingItemSnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.SupplierSnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.SupplierNote).HasMaxLength(2000);
        b.Property(x => x.FailureReason).HasMaxLength(2000);
        b.Property(x => x.Notes).HasMaxLength(4000);

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourPackageCancellationId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.TourPackageBookingItemId, x.Status });

        b.HasOne(x => x.TourPackageCancellation)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.TourPackageCancellationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TourPackageBookingItem)
            .WithMany(x => x.CancellationItems)
            .HasForeignKey(x => x.TourPackageBookingItemId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Refund)
            .WithOne(x => x.TourPackageCancellationItem)
            .HasForeignKey<TourPackageRefund>(x => x.TourPackageCancellationItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPackageRefundConfiguration : IEntityTypeConfiguration<TourPackageRefund>
{
    public void Configure(EntityTypeBuilder<TourPackageRefund> b)
    {
        b.ToTable("TourPackageRefunds", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();
        b.Property(x => x.GrossLineAmount).HasPrecision(18, 2);
        b.Property(x => x.PenaltyAmount).HasPrecision(18, 2);
        b.Property(x => x.RefundAmount).HasPrecision(18, 2);
        b.Property(x => x.Provider).HasMaxLength(50);
        b.Property(x => x.ExternalReference).HasMaxLength(200);
        b.Property(x => x.ExternalPayloadJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.WebhookState).HasMaxLength(100);
        b.Property(x => x.LastProviderError).HasMaxLength(2000);
        b.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.Notes).HasMaxLength(4000);

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourPackageBookingId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.TourPackageBookingItemId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.TourPackageCancellationItemId }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.Provider, x.Status, x.IsDeleted });

        b.HasOne(x => x.TourPackageBooking)
            .WithMany(x => x.Refunds)
            .HasForeignKey(x => x.TourPackageBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TourPackageBookingItem)
            .WithMany(x => x.Refunds)
            .HasForeignKey(x => x.TourPackageBookingItemId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.TourPackageCancellation)
            .WithMany(x => x.Refunds)
            .HasForeignKey(x => x.TourPackageCancellationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Attempts)
            .WithOne(x => x.TourPackageRefund)
            .HasForeignKey(x => x.TourPackageRefundId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPackageRefundAttemptConfiguration : IEntityTypeConfiguration<TourPackageRefundAttempt>
{
    public void Configure(EntityTypeBuilder<TourPackageRefundAttempt> b)
    {
        b.ToTable("TourPackageRefundAttempts", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.Provider).HasMaxLength(50);
        b.Property(x => x.ExternalReference).HasMaxLength(200);
        b.Property(x => x.ExternalPayloadJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.ResponsePayloadJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.WebhookState).HasMaxLength(100);
        b.Property(x => x.LastProviderError).HasMaxLength(2000);
        b.Property(x => x.Notes).HasMaxLength(4000);

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourPackageRefundId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.Provider, x.Status, x.IsDeleted });

        b.HasOne(x => x.TourPackageRefund)
            .WithMany(x => x.Attempts)
            .HasForeignKey(x => x.TourPackageRefundId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TourPackageAuditEventConfiguration : IEntityTypeConfiguration<TourPackageAuditEvent>
{
    public void Configure(EntityTypeBuilder<TourPackageAuditEvent> b)
    {
        b.ToTable("TourPackageAuditEvents", "tours");
        b.HasKey(x => x.Id);

        b.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(300).IsRequired();
        b.Property(x => x.Status).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.CurrencyCode).HasMaxLength(10);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.MetadataJson).HasColumnType("nvarchar(max)");

        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        b.HasIndex(x => new { x.TenantId, x.TourId, x.CreatedAt });
        b.HasIndex(x => new { x.TenantId, x.TourPackageBookingId, x.CreatedAt });
        b.HasIndex(x => new { x.TenantId, x.TourPackageReservationId, x.CreatedAt });
        b.HasIndex(x => new { x.TenantId, x.TourPackageRescheduleId, x.CreatedAt });
        b.HasIndex(x => new { x.TenantId, x.EventType, x.CreatedAt });
        b.HasIndex(x => new { x.TenantId, x.Severity, x.CreatedAt });

        b.HasOne<Tour>()
            .WithMany()
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<TourSchedule>()
            .WithMany()
            .HasForeignKey(x => x.TourScheduleId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<TourPackage>()
            .WithMany()
            .HasForeignKey(x => x.TourPackageId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<TourPackageReservation>()
            .WithMany()
            .HasForeignKey(x => x.TourPackageReservationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<TourPackageBooking>()
            .WithMany()
            .HasForeignKey(x => x.TourPackageBookingId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<TourPackageBookingItem>()
            .WithMany()
            .HasForeignKey(x => x.TourPackageBookingItemId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<TourPackageCancellation>()
            .WithMany()
            .HasForeignKey(x => x.TourPackageCancellationId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<TourPackageRefund>()
            .WithMany()
            .HasForeignKey(x => x.TourPackageRefundId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<TourPackageReschedule>()
            .WithMany()
            .HasForeignKey(x => x.TourPackageRescheduleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
