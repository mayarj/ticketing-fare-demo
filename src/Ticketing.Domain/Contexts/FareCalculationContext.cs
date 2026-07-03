using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Contexts;

/// <summary>
/// Abstract input carrier for a fare calculation. Each product type has its own
/// subclass carrying only the fields it needs — no god-object with nullable fields,
/// no <c>Dictionary&lt;string, object&gt;</c>. Strategies downcast to their expected type.
/// </summary>
public abstract class FareCalculationContext
{
    protected FareCalculationContext(ProductType productType) => ProductType = productType;

    public ProductType ProductType { get; }
}
