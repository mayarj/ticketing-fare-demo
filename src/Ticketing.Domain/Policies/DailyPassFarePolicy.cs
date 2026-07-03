using System.Text.Json;
using Ticketing.Domain.Contexts;
using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Policies;

/// <summary>
/// Base fare = a flat rate, independent of the requested date. The flat rate comes
/// from the policy's <c>current_fare_rates</c> row; this class owns the expected JSON
/// shape and validates it on read.
/// </summary>
public sealed class DailyPassFarePolicy : IFarePolicy
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record Params(decimal FlatRate);

    public ProductType Handles => ProductType.DailyPass;

    public FareResult Calculate(FareCalculationContext ctx, string paramsJson)
    {
        var p = JsonSerializer.Deserialize<Params>(paramsJson, Json)
                ?? throw new InvalidOperationException("DailyPass params missing.");

        // Safe: the factory builds a DailyPassContext for DAILY_PASS tickets.
        var c = (DailyPassContext)ctx;
        var fare = p.FlatRate;

        return new FareResult(
            Amount: fare,
            InputsJson: JsonSerializer.Serialize(new
            {
                policy   = "DAILY_PASS",
                flatRate = p.FlatRate,
                validOn  = c.ValidOn,
                formula  = $"flat {p.FlatRate}"
            }));
    }
}
