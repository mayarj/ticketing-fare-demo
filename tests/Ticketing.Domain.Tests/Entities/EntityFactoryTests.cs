using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Modifications;

namespace Ticketing.Domain.Tests.Entities;

public class EntityFactoryTests
{
    [Fact]
    public void Snapshot_Create_maps_all_fields_and_stamps_utc()
    {
        // TicketId is assigned by EF via the owning aggregate, not by the factory.
        var snapshot = FareCalculationSnapshot.Create("POINT_TO_POINT", """{"formula":"x"}""");

        Assert.Equal("POINT_TO_POINT", snapshot.PolicyCode);
        Assert.Equal("""{"formula":"x"}""", snapshot.BaseFareInputs);
        Assert.Equal(DateTimeKind.Utc, snapshot.CalculatedAt.Kind);
    }

    [Fact]
    public void AppliedTicketModification_From_freezes_the_calculation_output_onto_a_ticket()
    {
        var calculated = new AppliedModification(
            Code: "EXTRA_LUGGAGE",
            RuleType: RuleType.PerUnit,
            Quantity: 2,
            ParamsUsed: """{"amountPerUnit": 3.50}""",
            Surcharge: 7.00m,
            AppliedOrder: 1);

        var entity = AppliedTicketModification.From(calculated);

        Assert.Equal("EXTRA_LUGGAGE", entity.ModificationCode);
        Assert.Equal(RuleType.PerUnit, entity.RuleType);
        Assert.Equal(2, entity.Quantity);
        Assert.Equal("""{"amountPerUnit": 3.50}""", entity.ParamsUsed);
        Assert.Equal(7.00m, entity.Surcharge);
        Assert.Equal(1, entity.AppliedOrder);
        Assert.Equal(DateTimeKind.Utc, entity.AppliedAt.Kind);
    }
}
