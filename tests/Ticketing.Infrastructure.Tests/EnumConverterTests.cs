using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.TestSupport;

namespace Ticketing.Infrastructure.Tests;

[Collection("sqlserver")]
public class EnumConverterTests
{
    private readonly SqlServerFixture _sql;

    public EnumConverterTests(SqlServerFixture sql) => _sql = sql;

    [Fact]
    public async Task RuleType_is_persisted_as_its_screaming_snake_code()
    {
        var db = await _sql.CreateDatabaseAsync();
        await using (var ctx = db.NewContext())
        {
            ctx.CurrentModificationRules.Add(
                CurrentModificationRule.Create("EXTRA_LUGGAGE", RuleType.PerUnit, """{"amountPerUnit":3.50}""", 100));
            await ctx.SaveChangesAsync();
        }

        Assert.Equal("PER_UNIT", await ScalarAsync(db, "SELECT rule_type FROM current_modification_rules"));
    }

    [Fact]
    public async Task ProductType_persists_as_a_code_and_round_trips_back_to_the_enum()
    {
        var db = await _sql.CreateDatabaseAsync();
        await using (var ctx = db.NewContext())
        {
            ctx.Tickets.Add(Ticket.Issue("TKT-2026-AAAA0001", ProductType.PointToPoint, 30m, 47m));
            await ctx.SaveChangesAsync();
        }

        Assert.Equal("POINT_TO_POINT", await ScalarAsync(db, "SELECT product_type FROM tickets"));

        await using var read = db.NewContext();
        var ticket = await read.Tickets.SingleAsync();
        Assert.Equal(ProductType.PointToPoint, ticket.ProductType);
    }

    private static async Task<string?> ScalarAsync(TestDatabase db, string sql)
    {
        await using var connection = new SqlConnection(db.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (string?)await command.ExecuteScalarAsync();
    }
}
