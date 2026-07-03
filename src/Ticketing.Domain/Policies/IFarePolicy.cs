using Ticketing.Domain.Contexts;
using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Policies;

/// <summary>
/// Strategy interface for base fare calculation. One concrete implementation per
/// product type; the algorithm lives in the class, the parameters live in a
/// <c>current_fare_rates</c> row passed in as <paramref name="paramsJson"/>.
/// </summary>
public interface IFarePolicy
{
    /// <summary>The product type this policy computes fares for.</summary>
    ProductType Handles { get; }

    FareResult Calculate(FareCalculationContext ctx, string paramsJson);
}
