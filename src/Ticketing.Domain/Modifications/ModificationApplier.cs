using System.Text.Json;
using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Modifications;

/// <summary>
/// A single, data-driven applier that folds an ordered list of modification rules
/// over a base fare. Pure (no DB, no logging, no side effects), deterministic, and
/// order-sensitive — the caller supplies rules already in the correct order.
/// The behaviour space is enumerated by <see cref="RuleType"/>.
/// </summary>
public sealed class ModificationApplier
{
    public IReadOnlyList<AppliedModification> Apply(
        decimal baseFare,
        IReadOnlyList<ModificationRule> orderedRules,
        IReadOnlyDictionary<string, int> quantitiesByCode)
    {
        var running = baseFare;
        var applied = new List<AppliedModification>(orderedRules.Count);

        for (var i = 0; i < orderedRules.Count; i++)
        {
            var rule = orderedRules[i];
            var qty = quantitiesByCode.GetValueOrDefault(rule.Code, 1);

            var surcharge = rule.Type switch
            {
                RuleType.Fixed      => ReadDecimal(rule.ParamsJson, "amount"),
                RuleType.PerUnit    => ReadDecimal(rule.ParamsJson, "amountPerUnit") * qty,
                RuleType.Percentage => running * ReadDecimal(rule.ParamsJson, "percent"),
                _ => throw new InvalidOperationException($"Unknown rule type: {rule.Type}")
            };

            running += surcharge;

            applied.Add(new AppliedModification(
                Code:         rule.Code,
                RuleType:     rule.Type,
                Quantity:     qty,
                ParamsUsed:   rule.ParamsJson,   // frozen snapshot
                Surcharge:    surcharge,
                AppliedOrder: i + 1));
        }

        return applied;
    }

    private static decimal ReadDecimal(string json, string property)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty(property, out var value))
            throw new InvalidOperationException(
                $"Modification params missing '{property}': {json}");

        return value.GetDecimal();
    }
}
