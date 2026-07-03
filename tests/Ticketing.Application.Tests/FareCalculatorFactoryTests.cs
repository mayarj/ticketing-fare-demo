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

public class FareCalculatorFactoryTests
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
    public async Task Reproduces_the_documented_point_to_point_walkthrough()
    {
        var rates = new FakeCurrentFareRateRepository(
            CurrentFareRate.Create("POINT_TO_POINT", """{"ratePerKm":0.50,"minimumFare":2.00}"""));
        var rules = new FakeModificationRuleRepository(
            CurrentModificationRule.Create("EXTRA_LUGGAGE", RuleType.PerUnit, """{"amountPerUnit":3.50}""", 100),
            CurrentModificationRule.Create("FIRST_CLASS", RuleType.Fixed, """{"amount":10.00}""", 200));
        var factory = Build(rates, rules);

        var context = new PointToPointContext { Origin = "STATION_A", Destination = "STATION_C", DistanceKm = 60m };
        var requested = new List<ModificationRequest>
        {
            new() { Code = "FIRST_CLASS", Quantity = 1 },   // client order intentionally reversed
            new() { Code = "EXTRA_LUGGAGE", Quantity = 2 }
        };

        var outcome = await factory.CalculateAsync(ProductType.PointToPoint, context, requested);

        Assert.Equal(30.00m, outcome.BaseFare);
        Assert.Equal(47.00m, outcome.TotalFare);
        // Server orders by priority: EXTRA_LUGGAGE (100) before FIRST_CLASS (200).
        Assert.Equal(new[] { "EXTRA_LUGGAGE", "FIRST_CLASS" }, outcome.Applied.Select(a => a.Code));
        Assert.Equal(new[] { 1, 2 }, outcome.Applied.Select(a => a.AppliedOrder));
        Assert.Equal(7.00m, outcome.Applied[0].Surcharge);
        Assert.Equal(10.00m, outcome.Applied[1].Surcharge);
    }

    [Fact]
    public async Task Percentage_modifications_compound_over_the_running_total()
    {
        var rates = new FakeCurrentFareRateRepository(
            CurrentFareRate.Create("DAILY_PASS", """{"flatRate":100.00}"""));
        var rules = new FakeModificationRuleRepository(
            CurrentModificationRule.Create("VIP_CLASS", RuleType.Fixed, """{"amount":10.00}""", 200),
            CurrentModificationRule.Create("LOYALTY_10", RuleType.Percentage, """{"percent":-0.10}""", 900));
        var factory = Build(rates, rules);

        var context = new DailyPassContext { ValidOn = new DateOnly(2026, 7, 5) };
        var requested = new List<ModificationRequest>
        {
            new() { Code = "LOYALTY_10" },
            new() { Code = "VIP_CLASS" }
        };

        var outcome = await factory.CalculateAsync(ProductType.DailyPass, context, requested);

        // 100 + VIP 10 = 110, then -10% of 110 = -11 => 99.
        Assert.Equal(10.00m, outcome.Applied[0].Surcharge);
        Assert.Equal(-11.00m, outcome.Applied[1].Surcharge);
        Assert.Equal(99.00m, outcome.TotalFare);
    }

    [Fact]
    public async Task Unknown_modification_code_throws()
    {
        var rates = new FakeCurrentFareRateRepository(
            CurrentFareRate.Create("DAILY_PASS", """{"flatRate":8.00}"""));
        var rules = new FakeModificationRuleRepository(); // nothing active
        var factory = Build(rates, rules);

        var context = new DailyPassContext { ValidOn = new DateOnly(2026, 7, 5) };
        var requested = new List<ModificationRequest> { new() { Code = "DOES_NOT_EXIST" } };

        await Assert.ThrowsAsync<UnknownModificationException>(
            () => factory.CalculateAsync(ProductType.DailyPass, context, requested));
    }
}