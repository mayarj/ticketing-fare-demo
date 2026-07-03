using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ticketing.Domain.Entities;

namespace Ticketing.Infrastructure.Persistence.Configurations;

internal sealed class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.Number)
            .HasColumnName("ticket_number")
            .HasColumnType("varchar(50)")
            .IsRequired();
        builder.HasIndex(t => t.Number)
            .IsUnique()
            .HasDatabaseName("UQ_tickets_ticket_number");

        builder.Property(t => t.ProductType)
            .HasColumnName("product_type")
            .HasColumnType("varchar(50)")
            .HasConversion(EnumConverters.ProductType)
            .IsRequired();

        builder.Property(t => t.IssuedAt)
            .HasColumnName("issued_at")
            .HasColumnType("datetime2")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(t => t.BaseFare)
            .HasColumnName("base_fare")
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(t => t.TotalFare)
            .HasColumnName("total_fare")
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        // Aggregate: the ticket owns its snapshot (1:1) and its applied modifications
        // (1:many). Configuring the relationships from the principal end lets EF assign
        // the ticket_id FKs when the whole graph is saved in one SaveChanges.
        builder.HasOne(t => t.Snapshot)
            .WithOne()
            .HasForeignKey<FareCalculationSnapshot>(s => s.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.AppliedModifications)
            .WithOne()
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.AppliedModifications)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
