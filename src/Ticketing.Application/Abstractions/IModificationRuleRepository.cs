using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Modifications;

namespace Ticketing.Application.Abstractions;

/// <summary>
/// Repository for reading current modification rules. These are the editable
/// rules used for NEW ticket calculations. Read-only at calculation time.
/// </summary>
public interface IModificationRuleRepository
{
    /// <summary>
    /// Gets an active modification rule by its code.
    /// </summary>
    Task<CurrentModificationRule?> GetActiveByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple active modification rules by their codes.
    /// </summary>
    Task<Dictionary<string, CurrentModificationRule>> GetActiveByCodesAsync(
        IEnumerable<string> codes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active modification rules, ordered by priority.
    /// </summary>
    Task<IReadOnlyList<CurrentModificationRule>> GetAllActiveAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Projects active modification rules into the pure input the applier uses.
    /// </summary>
    Task<IReadOnlyList<ModificationRule>> GetActiveRulesForApplierAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that all modification codes exist and are active.
    /// Returns list of invalid codes.
    /// </summary>
    Task<IReadOnlyList<string>> ValidateCodesAsync(
        IEnumerable<string> codes,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active rule or throws if not found.
    /// </summary>
    Task<CurrentModificationRule> GetActiveOrThrowAsync(
        string code,
        CancellationToken cancellationToken = default);
}