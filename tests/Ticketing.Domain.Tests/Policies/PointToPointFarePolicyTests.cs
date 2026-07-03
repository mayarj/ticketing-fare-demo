using System.Text.Json;
using Ticketing.Domain.Contexts;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Policies;

namespace Ticketing.Domain.Tests.Policies;

public class PointToPointFarePolicyTests
{
    private const string RateParams = """{"ratePerKm": 0.50, "minimumFare": 2.00}""";

    private static PointToPointContext Context(decimal distanceKm) =>
        new() { Origin = "STATION_A", Destination = "STATION_C", DistanceKm = distanceKm };

    [Fact]
    public void Handles_returns_PointToPoint()
    {
        Assert.Equal(ProductType.PointToPoint, new PointToPointFarePolicy().Handles);
    }

    [Fact]
    public void Calculate_uses_rate_times_distance_when_above_minimum()
    {
        var result = new PointToPointFarePolicy().Calculate(Context(60m), RateParams);

        Assert.Equal(30.00m, result.Amount); // max(0.50 * 60, 2.00)
    }

    [Fact]
    public void Calculate_falls_back_to_minimum_fare_for_short_trips()
    {
        var result = new PointToPointFarePolicy().Calculate(Context(1m), RateParams); // 0.50 < 2.00

        Assert.Equal(2.00m, result.Amount);
    }

    [Fact]
    public void Calculate_writes_policy_code_and_human_readable_formula_into_inputs_json()
    {
        var result = new PointToPointFarePolicy().Calculate(Context(60m), RateParams);

        using var doc = JsonDocument.Parse(result.InputsJson);
        var root = doc.RootElement;
        Assert.Equal("POINT_TO_POINT", root.GetProperty("policy").GetString());
        Assert.Equal("max(0.50 * 60, 2.00) = 30.00", root.GetProperty("formula").GetString());
    }

    [Fact]
    public void Calculate_throws_a_loud_failure_when_given_the_wrong_context_type()
    {
        Assert.Throws<InvalidCastException>(
            () => new PointToPointFarePolicy().Calculate(new DailyPassContext(), RateParams));
    }

    [Fact]
    public void Calculate_throws_when_the_params_json_is_the_null_literal()
    {
        Assert.Throws<InvalidOperationException>(
            () => new PointToPointFarePolicy().Calculate(Context(60m), "null"));
    }
}
