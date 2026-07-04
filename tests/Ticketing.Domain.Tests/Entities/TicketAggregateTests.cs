using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Modifications;

namespace Ticketing.Domain.Tests.Entities;

public class TicketAggregateTests
{
    private static Ticket NewTicket() =>
        Ticket.Issue("TKT-2026-AAAA0001", ProductType.PointToPoint, 30m, 47m);

    private static AppliedTicketModification Mod(string code) =>
        AppliedTicketModification.From(
            new AppliedModification(code, RuleType.Fixed, 1, """{"amount":10.00}""", 10m, 1));

    [Fact]
    public void RecordCalculation_attaches_the_snapshot_and_modifications()
    {
        var ticket = NewTicket();
        var snapshot = FareCalculationSnapshot.Create("POINT_TO_POINT", """{"formula":"x"}""");

        ticket.RecordCalculation(snapshot, [Mod("FIRST_CLASS"), Mod("VIP_CLASS")]);

        Assert.Same(snapshot, ticket.Snapshot);
        Assert.Equal(
            new[] { "FIRST_CLASS", "VIP_CLASS" },
            ticket.AppliedModifications.Select(m => m.ModificationCode));
    }

    [Fact]
    public void RecordCalculation_replaces_any_previously_recorded_modifications()
    {
        var ticket = NewTicket();
        ticket.RecordCalculation(FareCalculationSnapshot.Create("POINT_TO_POINT", "{}"), [Mod("FIRST_CLASS")]);
        ticket.RecordCalculation(FareCalculationSnapshot.Create("POINT_TO_POINT", "{}"), [Mod("VIP_CLASS")]);

        Assert.Single(ticket.AppliedModifications);
        Assert.Equal("VIP_CLASS", ticket.AppliedModifications.Single().ModificationCode);
    }

    [Fact]
    public void RecordCalculation_rejects_a_null_snapshot()
    {
        Assert.Throws<ArgumentNullException>(() => NewTicket().RecordCalculation(null!, []));
    }
}
