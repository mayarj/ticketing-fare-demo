using System.Threading;
using System.Threading.Tasks;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.Abstractions;

/// <summary>
/// Repository for reading current fare rates. These are the editable rates
/// used for NEW ticket calculations. Read-only at calculation time.
/// </summary>
public interface ICurrentFareRateRepository
{
    /// <summary>
    /// Gets the active rate for a specific policy code.
    /// </summary>
    Task<CurrentFareRate?> GetActiveByPolicyCodeAsync(
        string policyCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active rates.
    /// </summary>
    Task<IReadOnlyList<CurrentFareRate>> GetAllActiveAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active rate or throws if not found.
    /// </summary>
    Task<CurrentFareRate> GetActiveOrThrowAsync(
        string policyCode,
        CancellationToken cancellationToken = default);
}