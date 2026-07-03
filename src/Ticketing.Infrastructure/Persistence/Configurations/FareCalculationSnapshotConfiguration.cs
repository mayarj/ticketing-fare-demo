using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistence.Configurations;

internal sealed class FareCalculationSnapshotConfiguration : IEntityTypeConfiguration<FareCalculationSnapshot>
{
    public void Configure(EntityTypeBuilder<FareCalculationSnapshot> builder)
    {
        builder.ToTable("fare_calculation_snapshots", t =>
            t.HasCheckConstraint("CHK_snapshot_inputs_json", "ISJSON([base_fare_inputs]) = 1"));

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.TicketId)
            .HasColumnName("ticket_id")
            .IsRequired();

        builder.Property(s => s.PolicyCode)
            .HasColumnName("policy_code")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(s => s.BaseFareInputs)
            .HasColumnName("base_fare_inputs")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(s => s.CalculatedAt)
            .HasColumnName("calculated_at")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

    }
}
