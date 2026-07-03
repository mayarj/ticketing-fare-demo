using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Modifications;

/// <summary>
/// The pure output of the applier for a single rule: the surcharge it produced and
/// the exact rule shape used, frozen. Persisted downstream as an
/// <c>AppliedTicketModification</c>.
/// </summary>
public sealed record AppliedModification(
    string Code,
    RuleType RuleType,
    int Quantity,
    string ParamsUsed,
    decimal Surcharge,
    int AppliedOrder);
