using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.Application.Abstractions;
using Ticketing.Application.Dtos;

namespace Ticketing.Api.Tests;

[Collection("api")]
public class TicketsEndpointTests
{
    private readonly ApiFactoryFixture _fixture;
    private readonly HttpClient _client;

    public TicketsEndpointTests(ApiFactoryFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    [Fact]
    public async Task Point_to_point_endpoint_returns_the_47_euro_breakdown()
    {
        var response = await _client.PostAsJsonAsync("/api/tickets/point-to-point", new
        {
            origin = "STATION_A",
            destination = "STATION_C",
            modifications = new[]
            {
                new { code = "FIRST_CLASS", quantity = 1 },
                new { code = "EXTRA_LUGGAGE", quantity = 2 }
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.NotNull(body);
        Assert.Equal(30.00m, body!.BaseFare);
        Assert.Equal(47.00m, body.TotalFare);
        Assert.Equal("POINT_TO_POINT", body.ProductType);
        // Server-assigned order regardless of the request order above.
        Assert.Equal(new[] { "EXTRA_LUGGAGE", "FIRST_CLASS" }, body.Modifications.Select(m => m.Code));
        Assert.Equal(new[] { 37.00m, 47.00m }, body.Modifications.Select(m => m.ResultingFare));
        Assert.Matches(@"^TKT-\d{4}-[A-Z2-9]{8}$", body.TicketNumber);
    }

    [Fact]
    public async Task Daily_pass_endpoint_applies_the_flat_rate_plus_vip()
    {
        var response = await _client.PostAsJsonAsync("/api/tickets/daily-pass", new
        {
            travelDate = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
            zone = "ZONE_1",
            modifications = new[] { new { code = "VIP_CLASS", quantity = 1 } }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(8.00m, body!.BaseFare);
        Assert.Equal(33.00m, body.TotalFare);
    }

    [Fact]
    public async Task Point_to_point_without_modifications_returns_base_only()
    {
        var response = await _client.PostAsJsonAsync("/api/tickets/point-to-point", new
        {
            origin = "STATION_A",
            destination = "STATION_C"
        });

        var body = await response.Content.ReadFromJsonAsync<TicketResponse>();
        Assert.Equal(30.00m, body!.TotalFare);
        Assert.Empty(body.Modifications);
    }

    [Theory]
    [InlineData("STATION_A", "STATION_A")] // origin == destination
    [InlineData("", "STATION_C")]          // blank origin
    public async Task Invalid_point_to_point_request_returns_400(string origin, string destination)
    {
        var response = await _client.PostAsJsonAsync("/api/tickets/point-to-point", new { origin, destination });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_station_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/tickets/point-to-point", new
        {
            origin = "NOWHERE",
            destination = "STATION_C"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_modification_code_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/tickets/point-to-point", new
        {
            origin = "STATION_A",
            destination = "STATION_C",
            modifications = new[] { new { code = "NOT_A_REAL_MOD", quantity = 1 } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Daily_pass_with_a_past_date_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/tickets/daily-pass", new
        {
            travelDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1).ToString("yyyy-MM-dd"),
            zone = "ZONE_1"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_document_lists_both_endpoints()
    {
        var json = await _client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/tickets/point-to-point", json);
        Assert.Contains("/api/tickets/daily-pass", json);
    }

    [Fact]
    public void The_container_resolves_the_ticket_issuer_graph()
    {
        using var scope = _fixture.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ITicketIssuer>());
    }
}
