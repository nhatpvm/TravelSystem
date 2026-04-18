// FILE #049: TicketBooking.Infrastructure/Persistence/Geo/GeoSyncLogConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TicketBooking.Domain.Geo;

namespace TicketBooking.Infrastructure.Persistence.Geo
{
    public sealed class GeoSyncLogConfiguration : IEntityTypeConfiguration<GeoSyncLog>
    {
        public void Configure(EntityTypeBuilder<GeoSyncLog> b)
        {
            b.ToTable("GeoSyncLogs", "geo");

            b.HasKey(x => x.Id);

            b.Property(x => x.Source).HasMaxLength(100).IsRequired();
            b.Property(x => x.Url).HasMaxLength(500).IsRequired();

            b.Property(x => x.Depth).IsRequired();

            b.Property(x => x.IsSuccess).IsRequired();
            b.Property(x => x.HttpStatus).IsRequired();

            b.Property(x => x.ErrorMessage).HasMaxLength(1000);
            b.Property(x => x.ErrorDetail).HasColumnType("nvarchar(max)");

            b.Property(x => x.CreatedAt).IsRequired();

            b.HasIndex(x => x.CreatedAt);
            b.HasIndex(x => new { x.Source, x.Depth });
        }
    }
}