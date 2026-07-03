using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistence.Configurations;

internal sealed class CurrentModificationRuleConfiguration : IEntityTypeConfiguration<CurrentModificationRule>
{
    public void Configure(EntityTypeBuilder<CurrentModificationRule> builder)
    {
        builder.ToTable("current_modification_rules", t =>
            t.HasCheckConstraint("CHK_current_mod_rules_json", "ISJSON([params]) = 1"));

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.ModificationCode)
            .HasColumnName("modification_code")
            .HasColumnType("varchar(50)")
            .IsRequired();
        builder.HasIndex(r => r.ModificationCode)
            .IsUnique()
            .HasDatabaseName("UQ_current_mod_rules_code");

        builder.Property(r => r.RuleType)
            .HasColumnName("rule_type")
            .HasColumnType("varchar(50)")
            .HasConversion(EnumConverters.RuleType)
            .IsRequired();

        builder.Property(r => r.ParamsJson)
            .HasColumnName("params")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(r => r.Priority)
            .HasColumnName("priority")
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
