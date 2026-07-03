using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistence.Configurations;

internal sealed class CurrentFareRateConfiguration : IEntityTypeConfiguration<CurrentFareRate>
{
    public void Configure(EntityTypeBuilder<CurrentFareRate> builder)
    {
        builder.ToTable("current_fare_rates", t =>
            t.HasCheckConstraint("CHK_current_fare_rates_json", "ISJSON([params]) = 1"));

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.PolicyCode)
            .HasColumnName("policy_code")
            .HasColumnType("varchar(50)")
            .IsRequired();
        builder.HasIndex(r => r.PolicyCode)
            .IsUnique()
            .HasDatabaseName("UQ_current_fare_rates_policy");

        builder.Property(r => r.ParamsJson)
            .HasColumnName("params")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(r => r.EffectiveFrom)
            .HasColumnName("effective_from")
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");
    }
}
