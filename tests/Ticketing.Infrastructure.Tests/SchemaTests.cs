using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Modifications;
using Ticketing.TestSupport;

namespace Ticketing.Infrastructure.Tests;

[Collection("sqlserver")]
public class SchemaTests
{
    private readonly SqlServerFixture _sql;

    public SchemaTests(SqlServerFixture sql) => _sql = sql;

    [Fact]
    public async Task Isjson_check_constraint_rejects_invalid_json()
    {
        var db = await _sql.CreateDatabaseAsync();
        await using var connection = new SqlConnection(db.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO current_fare_rates (policy_code, params, effective_from) VALUES ('X', 'not-json', GETUTCDATE())";

        await Assert.ThrowsAsync<SqlException>(() => command.ExecuteNonQueryAsync());
    }

    [Fact]
    public async Task Valid_json_insert_succeeds_and_column_defaults_apply()
    {
        var db = await _sql.CreateDatabaseAsync();
        await using var connection = new SqlConnection(db.ConnectionString);
        await connection.OpenAsync();

        await using (var insert = connection.CreateCommand())
        {
            insert.CommandText =
                "INSERT INTO current_fare_rates (policy_code, params, effective_from) VALUES ('X', '{\"a\":1}', GETUTCDATE())";
            Assert.Equal(1, await insert.ExecuteNonQueryAsync());
        }

        await using var read = connection.CreateCommand();
        read.CommandText = "SELECT is_active FROM current_fare_rates WHERE policy_code = 'X'";
        Assert.True((bool)(await read.ExecuteScalarAsync())!); // is_active DEFAULT 1
    }

    [Fact]
    public async Task Policy_code_unique_index_is_enforced()
    {
        var db = await _sql.CreateDatabaseAsync();
        await using var ctx = db.NewContext();

        ctx.CurrentFareRates.Add(CurrentFareRate.Create("DUPLICATE", """{"a":1}"""));
        await ctx.SaveChangesAsync();

        ctx.CurrentFareRates.Add(CurrentFareRate.Create("DUPLICATE", """{"a":2}"""));
        await Assert.ThrowsAnyAsync<Exception>(() => ctx.SaveChangesAsync());
    }

    [Fact]
    public async Task Deleting_a_ticket_cascades_to_its_snapshot_and_applied_modifications()
    {
        var db = await _sql.CreateDatabaseAsync();

        await using (var ctx = db.NewContext())
        {
            var ticket = Ticket.Issue("TKT-2026-CASCADE1", ProductType.PointToPoint, 30m, 47m);
            ticket.RecordCalculation(
                FareCalculationSnapshot.Create("POINT_TO_POINT", """{"formula":"x"}"""),
                [AppliedTicketModification.From(
                    new AppliedModification("FIRST_CLASS", RuleType.Fixed, 1, """{"amount":10.00}""", 10.00m, 1))]);
            ctx.Tickets.Add(ticket);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = db.NewContext())
        {
            ctx.Tickets.Remove(await ctx.Tickets.SingleAsync());
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = db.NewContext())
        {
            Assert.False(await ctx.FareCalculationSnapshots.AnyAsync());
            Assert.False(await ctx.AppliedTicketModifications.AnyAsync());
        }
    }
}
