using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Modifications;

/// <summary>
/// The pure input the applier folds over: one active modification rule, already
/// projected from its <c>current_modification_rules</c> row. The caller is
/// responsible for passing rules in the correct (server-assigned) order.
/// </summary>
public sealed record ModificationRule(
    string Code,
    RuleType Type,
    string ParamsJson,
    int Priority);
