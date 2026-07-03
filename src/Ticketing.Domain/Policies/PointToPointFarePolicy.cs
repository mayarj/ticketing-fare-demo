using System.Text.Json;
using Ticketing.Domain.Contexts;
using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Policies;

/// <summary>
/// Base fare = <c>max(ratePerKm * distanceKm, minimumFare)</c>. The rate parameters
/// come from the policy's <c>current_fare_rates</c> row; this class owns the expected
/// JSON shape and validates it on read.
/// </summary>
public sealed class PointToPointFarePolicy : IFarePolicy
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record Params(decimal RatePerKm, decimal MinimumFare);

    public ProductType Handles => ProductType.PointToPoint;

    public FareResult Calculate(FareCalculationContext ctx, string paramsJson)
    {
        var p = JsonSerializer.Deserialize<Params>(paramsJson, Json)
                ?? throw new InvalidOperationException("PointToPoint params missing.");

        // Safe: the factory builds a PointToPointContext for POINT_TO_POINT tickets.
        // A wrong context type throws InvalidCastException immediately — a loud failure.
        var c = (PointToPointContext)ctx;

        var raw = p.RatePerKm * c.DistanceKm;
        var fare = Math.Max(raw, p.MinimumFare);

        return new FareResult(
            Amount: fare,
            InputsJson: JsonSerializer.Serialize(new
            {
                policy      = "POINT_TO_POINT",
                ratePerKm   = p.RatePerKm,
                distanceKm  = c.DistanceKm,
                minimumFare = p.MinimumFare,
                formula     = $"max({p.RatePerKm} * {c.DistanceKm}, {p.MinimumFare}) = {fare}"
            }));
    }
}
