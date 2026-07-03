using System.Text.Json;
using Ticketing.Domain.Contexts;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Policies;

namespace Ticketing.Domain.Tests.Policies;

public class DailyPassFarePolicyTests
{
    private const string RateParams = """{"flatRate": 8.00}""";

    private static DailyPassContext Context() => new() { ValidOn = new DateOnly(2026, 7, 5) };

    [Fact]
    public void Handles_returns_DailyPass()
    {
        Assert.Equal(ProductType.DailyPass, new DailyPassFarePolicy().Handles);
    }

    [Fact]
    public void Calculate_returns_the_flat_rate_regardless_of_date()
    {
        var result = new DailyPassFarePolicy().Calculate(Context(), RateParams);

        Assert.Equal(8.00m, result.Amount);
    }

    [Fact]
    public void Calculate_writes_policy_code_into_inputs_json()
    {
        var result = new DailyPassFarePolicy().Calculate(Context(), RateParams);

        using var doc = JsonDocument.Parse(result.InputsJson);
        Assert.Equal("DAILY_PASS", doc.RootElement.GetProperty("policy").GetString());
    }

    [Fact]
    public void Calculate_throws_a_loud_failure_when_given_the_wrong_context_type()
    {
        var wrong = new PointToPointContext { Origin = "A", Destination = "B", DistanceKm = 5m };

        Assert.Throws<InvalidCastException>(
            () => new DailyPassFarePolicy().Calculate(wrong, RateParams));
    }
}
