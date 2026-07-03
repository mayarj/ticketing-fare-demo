using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;

namespace Ticketing.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Fare &amp; Ticket bounded context. Maps the five tables
/// two editable <c>current_*</c> tables read at calculation time,
/// and three immutable tables written when a ticket is issued.
/// All entity mappings live in <c>Persistence/Configurations</c> and are applied by
/// convention from this assembly.
/// </summary>
public class TicketingDbContext : DbContext
{
    public TicketingDbContext(DbContextOptions<TicketingDbContext> options) : base(options)
    {
    }

    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<FareCalculationSnapshot> FareCalculationSnapshots => Set<FareCalculationSnapshot>();
    public DbSet<AppliedTicketModification> AppliedTicketModifications => Set<AppliedTicketModification>();
    public DbSet<CurrentFareRate> CurrentFareRates => Set<CurrentFareRate>();
    public DbSet<CurrentModificationRule> CurrentModificationRules => Set<CurrentModificationRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TicketingDbContext).Assembly);
    }
}

/// <summary>
/// Value converters that persist the domain enums as the stable
/// seed data and <c>policy_code</c> / <c>modification_code</c> columns use — rather
/// than EF's default .NET member names.
/// </summary>
internal static class EnumConverters
{
    public static readonly ValueConverter<ProductType, string> ProductType =
        new(v => ToCode(v), v => ToProductType(v));

    public static readonly ValueConverter<RuleType, string> RuleType =
        new(v => ToCode(v), v => ToRuleType(v));

    private static string ToCode(ProductType value) => value switch
    {
        Domain.Enums.ProductType.PointToPoint => "POINT_TO_POINT",

        Domain.Enums.ProductType.DailyPass    => "DAILY_PASS",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unmapped product type.")
    };

    private static ProductType ToProductType(string code) => code switch
    {
        "POINT_TO_POINT" => Domain.Enums.ProductType.PointToPoint,
        "DAILY_PASS"     => Domain.Enums.ProductType.DailyPass,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown product type code.")
    };

    private static string ToCode(RuleType value) => value switch
    {
        Domain.Enums.RuleType.Fixed      => "FIXED",
        Domain.Enums.RuleType.PerUnit    => "PER_UNIT",
        Domain.Enums.RuleType.Percentage => "PERCENTAGE",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unmapped rule type.")
    };

    private static RuleType ToRuleType(string code) => code switch
    {
        "FIXED"      => Domain.Enums.RuleType.Fixed,
        "PER_UNIT"   => Domain.Enums.RuleType.PerUnit,
        "PERCENTAGE" => Domain.Enums.RuleType.Percentage,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown rule type code.")
    };
}
