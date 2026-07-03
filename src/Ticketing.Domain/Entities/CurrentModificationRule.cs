using Ticketing.Domain.Enums;
using Ticketing.Domain.Modifications;

namespace Ticketing.Domain.Entities;

/// <summary>
/// Current, editable modification rule for one <c>modification_code</c>. Read only
/// at calculation time; editing here does not affect any issued ticket. Behaviour
/// is enumerated by <see cref="RuleType"/> — adding a modification is a seed row.
/// </summary>
public sealed class CurrentModificationRule
{
    private CurrentModificationRule() { } // EF

    public long Id { get; private set; }
    public string ModificationCode { get; private set; } = default!;
    public RuleType RuleType { get; private set; }
    public string ParamsJson { get; private set; } = default!;
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Projects the stored row into the pure input the applier folds over.</summary>
    public ModificationRule ToRule() =>
        new(ModificationCode, RuleType, ParamsJson, Priority);

    /// <summary>Creates an active modification rule row. Used by the database seeder.</summary>
    public static CurrentModificationRule Create(
        string modificationCode,
        RuleType ruleType,
        string paramsJson,
        int priority) =>
        new()
        {
            ModificationCode = modificationCode,
            RuleType = ruleType,
            ParamsJson = paramsJson,
            Priority = priority,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };
}
