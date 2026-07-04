using Ticketing.Application.Abstractions;
using Ticketing.Application.Services;
using Ticketing.Infrastructure.Services;

namespace Ticketing.Infrastructure.Tests;

public class InMemoryDistanceProviderTests
{
    private readonly InMemoryDistanceProvider _provider = new();

    [Fact]
    public async Task Known_route_returns_the_seeded_distance()
    {
        Assert.Equal(60d, await _provider.GetDistanceKmOrThrowAsync("STATION_A", "STATION_C"));
    }

    [Fact]
    public async Task Route_is_symmetric()
    {
        var forward = await _provider.GetDistanceKmOrThrowAsync("STATION_A", "STATION_B");
        var reverse = await _provider.GetDistanceKmOrThrowAsync("STATION_B", "STATION_A");
        Assert.Equal(forward, reverse);
    }

    [Fact]
    public async Task Lookup_is_case_and_whitespace_insensitive()
    {
        Assert.Equal(60d, await _provider.GetDistanceKmOrThrowAsync("  station_a ", "station_c"));
    }

    [Fact]
    public async Task Unknown_route_returns_null_from_the_try_method()
    {
        Assert.Null(await _provider.GetDistanceKmAsync("NOWHERE", "STATION_A"));
    }

    [Fact]
    public async Task Unknown_route_throws_from_the_or_throw_method()
    {
        await Assert.ThrowsAsync<RouteNotFoundException>(
            () => _provider.GetDistanceKmOrThrowAsync("NOWHERE", "STATION_A"));
    }
}
