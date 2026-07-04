using Ticketing.Application.Abstractions;
using Ticketing.Application.Dtos;
using Ticketing.Application.Services;
using Ticketing.Application.Tests.Fakes;
using Ticketing.Domain.Contexts;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Modifications;
using Ticketing.Domain.Policies;

namespace Ticketing.Application.Tests;

public class FareCalculatorFactoryEdgeCaseTests
{
    private static FareCalculatorFactory Build(
        ICurrentFareRateRepository rates,
        IModificationRuleRepository rules) =>
        new(
            new FarePolicyResolver([new PointToPointFarePolicy(), new DailyPassFarePolicy()]),
            rates,
            rules,
            new ModificationApplier());

    [Fact]
    public async Task Same_priority_rules_are_ordered_alphabetically_by_code()
    {
        var rates = new FakeCurrentFareRateRepository(
            CurrentFareRate.Create("DAILY_PASS", """{"flatRate":100.00}"""));
        var rules = new FakeModificationRuleRepository(
            CurrentModificationRule.Create("VIP_CLASS", RuleType.Fixed, """{"amount":25.00}""", 200),
            CurrentModificationRule.Create("FIRST_CLASS", RuleType.Fixed, """{"amount":10.00}""", 200));
        var factory = Build(rates, rules);

        var context = new DailyPassContext { ValidOn = new DateOnly(2026, 7, 5) };
        var requested = new List<ModificationRequest>
        {
            new() { Code = "VIP_CLASS" },   // sent first, but should apply second
            new() { Code = "FIRST_CLASS" }
        };

        var outcome = await factory.CalculateAsync(ProductType.DailyPass, context, requested);

        // Same priority (200) -> tie broken by ordinal code: FIRST_CLASS before VIP_CLASS.
        Assert.Equal(new[] { "FIRST_CLASS", "VIP_CLASS" }, outcome.Applied.Select(a => a.Code));
        Assert.Equal(new[] { 1, 2 }, outcome.Applied.Select(a => a.AppliedOrder));
    }

    [Fact]
    public async Task Duplicate_codes_are_applied_once_using_the_last_quantity()
    {
        var rates = new FakeCurrentFareRateRepository(
            CurrentFareRate.Create("DAILY_PASS", """{"flatRate":8.00}"""));
        var rules = new FakeModificationRuleRepository(
            CurrentModificationRule.Create("EXTRA_LUGGAGE", RuleType.PerUnit, """{"amountPerUnit":3.50}""", 100));
        var factory = Build(rates, rules);

        var context = new DailyPassContext { ValidOn = new DateOnly(2026, 7, 5) };
        var requested = new List<ModificationRequest>
        {
            new() { Code = "EXTRA_LUGGAGE", Quantity = 1 },
            new() { Code = "EXTRA_LUGGAGE", Quantity = 3 } // last one wins
        };

        var outcome = await factory.CalculateAsync(ProductType.DailyPass, context, requested);

        Assert.Single(outcome.Applied);
        Assert.Equal(3, outcome.Applied[0].Quantity);
        Assert.Equal(10.50m, outcome.Applied[0].Surcharge); // 3.50 * 3
        Assert.Equal(18.50m, outcome.TotalFare);
    }

    [Fact]
    public async Task No_modifications_returns_the_base_fare_only()
    {
        var rates = new FakeCurrentFareRateRepository(
            CurrentFareRate.Create("DAILY_PASS", """{"flatRate":8.00}"""));
        var factory = Build(rates, new FakeModificationRuleRepository());

        var context = new DailyPassContext { ValidOn = new DateOnly(2026, 7, 5) };

        var outcome = await factory.CalculateAsync(
            ProductType.DailyPass, context, Array.Empty<ModificationRequest>());

        Assert.Equal(8.00m, outcome.BaseFare);
        Assert.Equal(8.00m, outcome.TotalFare);
        Assert.Empty(outcome.Applied);
    }

    [Fact]
    public async Task Missing_rate_row_throws()
    {
        var factory = Build(new FakeCurrentFareRateRepository(), new FakeModificationRuleRepository());
        var context = new DailyPassContext { ValidOn = new DateOnly(2026, 7, 5) };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => factory.CalculateAsync(ProductType.DailyPass, context, Array.Empty<ModificationRequest>()));
    }
}
