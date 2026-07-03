using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Policies;

/// <summary>
/// DI-driven lookup from <see cref="ProductType"/> to the policy that handles it.
/// Replaces any <c>switch</c> in the engine: every registered <see cref="IFarePolicy"/>
/// is injected and indexed by its <see cref="IFarePolicy.Handles"/> key. Adding a
/// product type is a new class plus one DI registration — no change here.
/// </summary>
public sealed class FarePolicyResolver
{
    private readonly IReadOnlyDictionary<ProductType, IFarePolicy> _byType;

    public FarePolicyResolver(IEnumerable<IFarePolicy> policies)
    {
        _byType = policies.ToDictionary(p => p.Handles);
    }

    public IFarePolicy For(ProductType type) =>
        _byType.TryGetValue(type, out var policy)
            ? policy
            : throw new InvalidOperationException($"No policy registered for {type}.");
}
