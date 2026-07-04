using Microsoft.EntityFrameworkCore;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Modifications;
using Ticketing.Infrastructure.Repositories;
using Ticketing.TestSupport;

namespace Ticketing.Infrastructure.Tests;

[Collection("sqlserver")]
public class CurrentFareRateRepositoryTests
{
    private readonly SqlServerFixture _sql;

    public CurrentFareRateRepositoryTests(SqlServerFixture sql) => _sql = sql;

    [Fact]
    public async Task Returns_the_active_rate_for_a_policy_code()
    {
        var db = await _sql.CreateDatabaseAsync();
        await Seed(db, CurrentFareRate.Create("POINT_TO_POINT", """{"ratePerKm":0.50,"minimumFare":2.00}"""));

        await using var ctx = db.NewContext();
        var rate = await new CurrentFareRateRepository(ctx).GetActiveByPolicyCodeAsync("POINT_TO_POINT");

        Assert.NotNull(rate);
        Assert.Contains("ratePerKm", rate!.ParamsJson);
    }

    [Fact]
    public async Task Returns_null_and_throws_for_a_missing_policy_code()
    {
        var db = await _sql.CreateDatabaseAsync();
        await using var ctx = db.NewContext();
        var repo = new CurrentFareRateRepository(ctx);

        Assert.Null(await repo.GetActiveByPolicyCodeAsync("NOPE"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.GetActiveOrThrowAsync("NOPE"));
    }

    [Fact]
    public async Task Ignores_inactive_rows()
    {
        var db = await _sql.CreateDatabaseAsync();
        await Seed(db, CurrentFareRate.Create("DAILY_PASS", """{"flatRate":8.00}"""));
        await Execute(db, "UPDATE current_fare_rates SET is_active = 0 WHERE policy_code = 'DAILY_PASS'");

        await using var ctx = db.NewContext();
        Assert.Null(await new CurrentFareRateRepository(ctx).GetActiveByPolicyCodeAsync("DAILY_PASS"));
    }

    private static async Task Seed(TestDatabase db, params CurrentFareRate[] rates)
    {
        await using var ctx = db.NewContext();
        ctx.CurrentFareRates.AddRange(rates);
        await ctx.SaveChangesAsync();
    }

    private static async Task Execute(TestDatabase db, string sql)
    {
        await using var ctx = db.NewContext();
        await ctx.Database.ExecuteSqlRawAsync(sql);
    }
}

[Collection("sqlserver")]
public class ModificationRuleRepositoryTests
{
    private readonly SqlServerFixture _sql;

    public ModificationRuleRepositoryTests(SqlServerFixture sql) => _sql = sql;

    private async Task<TestDatabase> SeededDb()
    {
        var db = await _sql.CreateDatabaseAsync();
        await using var ctx = db.NewContext();
        ctx.CurrentModificationRules.AddRange(
            CurrentModificationRule.Create("EXTRA_LUGGAGE", RuleType.PerUnit, """{"amountPerUnit":3.50}""", 100),
            CurrentModificationRule.Create("FIRST_CLASS", RuleType.Fixed, """{"amount":10.00}""", 200),
            CurrentModificationRule.Create("VIP_CLASS", RuleType.Fixed, """{"amount":25.00}""", 200),
            CurrentModificationRule.Create("LOYALTY_10", RuleType.Percentage, """{"percent":-0.10}""", 900));
        await ctx.SaveChangesAsync();
        return db;
    }

    [Fact]
    public async Task GetActiveByCodes_returns_only_the_requested_rules()
    {
        var db = await SeededDb();
        await using var ctx = db.NewContext();

        var result = await new ModificationRuleRepository(ctx)
            .GetActiveByCodesAsync(["FIRST_CLASS", "EXTRA_LUGGAGE", "UNKNOWN"]);

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("FIRST_CLASS"));
        Assert.True(result.ContainsKey("EXTRA_LUGGAGE"));
        Assert.False(result.ContainsKey("UNKNOWN"));
    }

    [Fact]
    public async Task GetActiveByCodes_with_no_codes_returns_empty()
    {
        var db = await SeededDb();
        await using var ctx = db.NewContext();
        Assert.Empty(await new ModificationRuleRepository(ctx).GetActiveByCodesAsync([]));
    }

    [Fact]
    public async Task GetAllActive_is_ordered_by_priority_then_code()
    {
        var db = await SeededDb();
        await using var ctx = db.NewContext();

        var codes = (await new ModificationRuleRepository(ctx).GetAllActiveAsync())
            .Select(r => r.ModificationCode).ToArray();

        Assert.Equal(new[] { "EXTRA_LUGGAGE", "FIRST_CLASS", "VIP_CLASS", "LOYALTY_10" }, codes);
    }

    [Fact]
    public async Task ValidateCodes_returns_the_codes_that_do_not_exist()
    {
        var db = await SeededDb();
        await using var ctx = db.NewContext();

        var invalid = await new ModificationRuleRepository(ctx).ValidateCodesAsync(["FIRST_CLASS", "GHOST"]);

        Assert.Equal(new[] { "GHOST" }, invalid);
    }

    [Fact]
    public async Task GetActiveRulesForApplier_projects_to_domain_rules()
    {
        var db = await SeededDb();
        await using var ctx = db.NewContext();

        var rules = await new ModificationRuleRepository(ctx).GetActiveRulesForApplierAsync();

        Assert.Equal(4, rules.Count);
        Assert.All(rules, r => Assert.False(string.IsNullOrEmpty(r.Code)));
    }
}

[Collection("sqlserver")]
public class TicketRepositoryTests
{
    private readonly SqlServerFixture _sql;

    public TicketRepositoryTests(SqlServerFixture sql) => _sql = sql;

    [Fact]
    public async Task Add_persists_the_whole_aggregate_in_one_save()
    {
        var db = await _sql.CreateDatabaseAsync();
        await using (var ctx = db.NewContext())
        {
            var ticket = Ticket.Issue("TKT-2026-AGG00001", ProductType.PointToPoint, 30m, 47m);
            ticket.RecordCalculation(
                FareCalculationSnapshot.Create("POINT_TO_POINT", """{"formula":"max(0.50 * 60, 2.00) = 30.00"}"""),
                [
                    AppliedTicketModification.From(new AppliedModification("EXTRA_LUGGAGE", RuleType.PerUnit, 2, """{"amountPerUnit":3.50}""", 7.00m, 1)),
                    AppliedTicketModification.From(new AppliedModification("FIRST_CLASS", RuleType.Fixed, 1, """{"amount":10.00}""", 10.00m, 2))
                ]);

            await new TicketRepository(ctx).AddAsync(ticket);
            await ctx.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        var saved = await read.Tickets.SingleAsync();
        Assert.True(saved.Id > 0);
        Assert.Equal(47m, saved.TotalFare);
        Assert.Equal(1, await read.FareCalculationSnapshots.CountAsync(s => s.TicketId == saved.Id));
        Assert.Equal(2, await read.AppliedTicketModifications.CountAsync(a => a.TicketId == saved.Id));
    }

    [Fact]
    public async Task ExistsByNumber_reflects_whether_a_number_is_taken()
    {
        var db = await _sql.CreateDatabaseAsync();
        await using (var ctx = db.NewContext())
        {
            ctx.Tickets.Add(Ticket.Issue("TKT-2026-EXISTS01", ProductType.DailyPass, 8m, 8m));
            await ctx.SaveChangesAsync();
        }

        await using var read = db.NewContext();
        var repo = new TicketRepository(read);
        Assert.True(await repo.ExistsByNumberAsync("TKT-2026-EXISTS01"));
        Assert.False(await repo.ExistsByNumberAsync("TKT-2026-NOPE0000"));
    }
}
