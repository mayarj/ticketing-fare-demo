using Ticketing.Domain.Enums;
using Ticketing.Domain.Modifications;

namespace Ticketing.Domain.Tests.Modifications;

public class ModificationApplierTests
{
    private readonly ModificationApplier _applier = new();

    private static IReadOnlyDictionary<string, int> Quantities(params (string Code, int Qty)[] items) =>
        items.ToDictionary(x => x.Code, x => x.Qty);

    [Fact]
    public void Fixed_rule_adds_a_flat_amount_and_ignores_quantity()
    {
        var rules = new[] { new ModificationRule("FIRST_CLASS", RuleType.Fixed, """{"amount": 10.00}""", 200) };

        var applied = _applier.Apply(30.00m, rules, Quantities(("FIRST_CLASS", 5)));

        Assert.Single(applied);
        Assert.Equal(10.00m, applied[0].Surcharge); // 5 ignored for FIXED
        Assert.Equal(5, applied[0].Quantity);       // captured, but not multiplied
    }

    [Fact]
    public void PerUnit_rule_multiplies_the_amount_by_quantity()
    {
        var rules = new[] { new ModificationRule("EXTRA_LUGGAGE", RuleType.PerUnit, """{"amountPerUnit": 3.50}""", 100) };

        var applied = _applier.Apply(30.00m, rules, Quantities(("EXTRA_LUGGAGE", 2)));

        Assert.Equal(7.00m, applied[0].Surcharge); // 3.50 * 2
    }

    [Fact]
    public void PerUnit_rule_defaults_quantity_to_one_when_not_supplied()
    {
        var rules = new[] { new ModificationRule("EXTRA_LUGGAGE", RuleType.PerUnit, """{"amountPerUnit": 3.50}""", 100) };

        var applied = _applier.Apply(30.00m, rules, Quantities());

        Assert.Equal(1, applied[0].Quantity);
        Assert.Equal(3.50m, applied[0].Surcharge);
    }

    [Fact]
    public void Percentage_rule_applies_to_the_running_total_not_the_base_fare()
    {
        var rules = new[]
        {
            new ModificationRule("FIRST_CLASS", RuleType.Fixed,      """{"amount": 10.00}""", 200),
            new ModificationRule("SERVICE_FEE", RuleType.Percentage, """{"percent": 0.10}""",  900)
        };

        var applied = _applier.Apply(100.00m, rules, Quantities());

        Assert.Equal(10.00m, applied[0].Surcharge); // running -> 110.00
        Assert.Equal(11.00m, applied[1].Surcharge); // 110 * 0.10, NOT 100 * 0.10
    }

    [Fact]
    public void Percentage_rule_supports_discounts_via_a_negative_percent()
    {
        var rules = new[] { new ModificationRule("LOYALTY_10", RuleType.Percentage, """{"percent": -0.10}""", 900) };

        var applied = _applier.Apply(100.00m, rules, Quantities());

        Assert.Equal(-10.00m, applied[0].Surcharge);
    }

    [Fact]
    public void Apply_assigns_sequential_one_based_applied_order_following_input_order()
    {
        var rules = new[]
        {
            new ModificationRule("A", RuleType.Fixed, """{"amount": 1.00}""", 100),
            new ModificationRule("B", RuleType.Fixed, """{"amount": 1.00}""", 200),
            new ModificationRule("C", RuleType.Fixed, """{"amount": 1.00}""", 300)
        };

        var applied = _applier.Apply(0m, rules, Quantities());

        Assert.Equal(new[] { 1, 2, 3 }, applied.Select(a => a.AppliedOrder));
        Assert.Equal(new[] { "A", "B", "C" }, applied.Select(a => a.Code));
    }

    [Fact]
    public void Apply_freezes_the_rule_shape_into_each_result()
    {
        const string paramsJson = """{"amount": 10.00}""";
        var rules = new[] { new ModificationRule("FIRST_CLASS", RuleType.Fixed, paramsJson, 200) };

        var applied = _applier.Apply(30.00m, rules, Quantities());

        Assert.Equal("FIRST_CLASS", applied[0].Code);
        Assert.Equal(RuleType.Fixed, applied[0].RuleType);
        Assert.Equal(paramsJson, applied[0].ParamsUsed);
    }

    [Fact]
    public void Apply_reproduces_the_documented_end_to_end_example()
    {
        // DESIGN_DECISIONS §11: base 30.00 -> EXTRA_LUGGAGE x2 (+7.00) -> FIRST_CLASS (+10.00) = 47.00
        var rules = new[]
        {
            new ModificationRule("EXTRA_LUGGAGE", RuleType.PerUnit, """{"amountPerUnit": 3.50}""", 100),
            new ModificationRule("FIRST_CLASS",   RuleType.Fixed,   """{"amount": 10.00}""",       200)
        };

        var applied = _applier.Apply(30.00m, rules, Quantities(("EXTRA_LUGGAGE", 2), ("FIRST_CLASS", 1)));

        Assert.Equal(7.00m, applied[0].Surcharge);
        Assert.Equal(10.00m, applied[1].Surcharge);
        Assert.Equal(47.00m, 30.00m + applied.Sum(a => a.Surcharge));
    }

    [Fact]
    public void Apply_returns_empty_when_there_are_no_rules()
    {
        var applied = _applier.Apply(30.00m, Array.Empty<ModificationRule>(), Quantities());

        Assert.Empty(applied);
    }

    [Fact]
    public void Apply_throws_when_a_required_param_key_is_missing()
    {
        var rules = new[] { new ModificationRule("BROKEN", RuleType.Fixed, """{"wrongKey": 10.00}""", 100) };

        Assert.Throws<InvalidOperationException>(() => _applier.Apply(30.00m, rules, Quantities()));
    }
}
