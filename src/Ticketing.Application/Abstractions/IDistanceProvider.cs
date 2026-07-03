using System.Threading;
using System.Threading.Tasks;

namespace Ticketing.Application.Abstractions;

/// <summary>
/// Provides distance calculations between locations.
/// </summary>
public interface IDistanceProvider
{
    /// <summary>
    /// Calculates the distance in kilometers between two locations.
    /// Returns null if route cannot be calculated.
    /// </summary>
    Task<double?> GetDistanceKmAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the distance in kilometers or throws if route not found.
    /// </summary>
    Task<double> GetDistanceKmOrThrowAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken = default);
}