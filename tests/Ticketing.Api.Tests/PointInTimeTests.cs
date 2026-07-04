using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Dtos;

namespace Ticketing.Api.Tests;

[Collection("api")]
public class PointInTimeTests
{
    private readonly ApiFactoryFixture _fixture;

    public PointInTimeTests(ApiFactoryFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Editing_a_fare_rate_does_not_change_already_issued_tickets()
    {
        // Own isolated database so mutating the rate can't affect other API tests.
        using var api = _fixture.CreateIsolatedApi();
        var client = api.CreateClient(); // migrates + seeds this database

        // Ticket A priced at the seeded rate (0.50/km => 60km base = 30.00).
        var ticketA = await IssuePointToPoint(client);
        Assert.Equal(30.00m, ticketA!.BaseFare);

        // An operator changes the current rate to 0.60/km (raw ADO.NET: EF's
        // ExecuteSqlRaw would misread the JSON braces as format placeholders).
        await using (var connection = new SqlConnection(api.ConnectionString))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText =
                "UPDATE current_fare_rates SET params = '{\"ratePerKm\":0.60,\"minimumFare\":2.00}' " +
                "WHERE policy_code = 'POINT_TO_POINT'";
            await command.ExecuteNonQueryAsync();
        }

        // Ticket B is priced at the new rate (60km base = 36.00).
        var ticketB = await IssuePointToPoint(client);
        Assert.Equal(36.00m, ticketB!.BaseFare);

        // Ticket A's persisted fare is frozen — the rate change did not rewrite history.
        await using (var ctx = api.NewContext())
        {
            var persistedA = await ctx.Tickets.SingleAsync(t => t.Number == ticketA.TicketNumber);
            Assert.Equal(30.00m, persistedA.BaseFare);
            Assert.Equal(ticketA.TotalFare, persistedA.TotalFare);
        }
    }

    private static async Task<TicketResponse?> IssuePointToPoint(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/tickets/point-to-point", new
        {
            origin = "STATION_A",
            destination = "STATION_C"
        });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TicketResponse>();
    }
}
