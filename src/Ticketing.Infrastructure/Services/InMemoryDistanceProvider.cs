using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ticketing.Application.Abstractions;
using Ticketing.Application.Services;

namespace Ticketing.Infrastructure.Services;

/// <summary>
/// Resolves point-to-point distances from a small in-memory, symmetric station map
/// Trivially swappable for a real table if the domain grows.
/// </summary>
public sealed class InMemoryDistanceProvider : IDistanceProvider
{
    private static readonly IReadOnlyDictionary<(string Origin, string Destination), double> Distances = Build();

    public Task<double?> GetDistanceKmAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken = default)
    {
        var key = (Normalise(origin), Normalise(destination));
        var result = Distances.TryGetValue(key, out var km) ? km : (double?)null;
        return Task.FromResult(result);
    }

    public async Task<double> GetDistanceKmOrThrowAsync(
        string origin,
        string destination,
        CancellationToken cancellationToken = default) =>
        await GetDistanceKmAsync(origin, destination, cancellationToken)
        ?? throw new RouteNotFoundException(origin, destination);

    private static string Normalise(string value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    private static IReadOnlyDictionary<(string, string), double> Build()
    {
        // One entry per pair; both directions are inserted below.
        var seed = new (string A, string B, double Km)[]
        {
            ("STATION_A", "STATION_B", 20),
            ("STATION_A", "STATION_C", 60),   
            ("STATION_B", "STATION_C", 45),
            ("STATION_A", "STATION_D", 120),
            ("STATION_C", "STATION_D", 75),
        };

        var map = new Dictionary<(string, string), double>();
        foreach (var (a, b, km) in seed)
        {
            map[(a, b)] = km;
            map[(b, a)] = km;
        }

        return map;
    }
}