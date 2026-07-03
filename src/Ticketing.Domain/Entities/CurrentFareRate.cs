namespace Ticketing.Domain.Entities;

/// <summary>
/// Current, editable base fare rate for one <c>policy_code</c>. Read only at
/// calculation time; editing here does not affect any issued ticket. The
/// <see cref="ParamsJson"/> shape is owned and validated by the matching policy.
/// </summary>
public sealed class CurrentFareRate
{
    private CurrentFareRate() { } // EF

    public long Id { get; private set; }
    public string PolicyCode { get; private set; } = default!;
    public string ParamsJson { get; private set; } = default!;
    public DateTime EffectiveFrom { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Creates an active rate row. Used by the database seeder.</summary>
    public static CurrentFareRate Create(string policyCode, string paramsJson) =>
        new()
        {
            PolicyCode = policyCode,
            ParamsJson = paramsJson,
            EffectiveFrom = DateTime.UtcNow,
            IsActive = true,
            UpdatedAt = DateTime.UtcNow
        };
}
