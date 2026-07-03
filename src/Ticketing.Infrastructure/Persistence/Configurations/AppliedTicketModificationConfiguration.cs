using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistence.Configurations;

internal sealed class AppliedTicketModificationConfiguration : IEntityTypeConfiguration<AppliedTicketModification>
{
    public void Configure(EntityTypeBuilder<AppliedTicketModification> builder)
    {
        builder.ToTable("applied_ticket_modifications", t =>
            t.HasCheckConstraint("CHK_applied_params_json", "ISJSON([params_used]) = 1"));

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.TicketId)
            .HasColumnName("ticket_id")
            .IsRequired();

        builder.Property(a => a.ModificationCode)
            .HasColumnName("modification_code")
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.Property(a => a.RuleType)
            .HasColumnName("rule_type")
            .HasColumnType("varchar(50)")
            .HasConversion(EnumConverters.RuleType)
            .IsRequired();

        builder.Property(a => a.Quantity)
            .HasColumnName("quantity")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(a => a.ParamsUsed)
            .HasColumnName("params_used")
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(a => a.Surcharge)
            .HasColumnName("surcharge")
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(a => a.AppliedOrder)
            .HasColumnName("applied_order")
            .IsRequired();

        builder.Property(a => a.AppliedAt)
            .HasColumnName("applied_at")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

    }
}
