using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Contexts;

/// <summary>Inputs for a daily pass fare. <see cref="ValidOn"/> is captured for realism
/// but does not change the flat fare in this demo.</summary>
public sealed class DailyPassContext : FareCalculationContext
{
    public DailyPassContext() : base(ProductType.DailyPass) { }

    public DateOnly ValidOn { get; init; }
}
