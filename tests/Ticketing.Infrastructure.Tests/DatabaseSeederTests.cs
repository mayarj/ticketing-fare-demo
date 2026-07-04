using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Services;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Modifications;
using Ticketing.Domain.Policies;
using Ticketing.Infrastructure.Persistence;
using Ticketing.Infrastructure.Repositories;
using Ticketing.Infrastructure.Seeding;
using Ticketing.Infrastructure.Services;
using Ticketing.TestSupport;

namespace Ticketing.Infrastructure.Tests;

[Collection("sqlserver")]
public class DatabaseSeederTests
{
    private readonly SqlServerFixture _sql;

    public DatabaseSeederTests(SqlServerFixture sql) => _sql = sql;

    // Wires the real engine end-to-end so the sample tickets are priced by the actual
    // pipeline and persisted through real SQL Server.
    private static DatabaseSeeder BuildSeeder(TicketingDbContext db)
    {
        var factory = new FareCalculatorFactory(
            new FarePolicyResolver([new PointToPointFarePolicy(), new DailyPassFarePolicy()]),
            new CurrentFareRateRepository(db),
            new ModificationRuleRepository(db),
            new ModificationApplier());

        var issuer = new TicketIssuer(
            factory,
            new InMemoryDistanceProvider(),
            new TicketNumberGenerator(),
            new TicketRepository(db),
            new UnitOfWork(db));

        return new DatabaseSeeder(db, issuer);
    }

    [Fact]
    public async Task Seed_populates_rates_rules_and_sample_tickets_through_the_real_engine()
    {
        var db = await _sql.CreateDatabaseAsync();
        await using (var ctx = db.NewContext())
            await BuildSeeder(ctx).SeedAsync();

        await using var read = db.NewContext();
        Assert.Equal(2, await read.CurrentFareRates.CountAsync());
        Assert.Equal(4, await read.CurrentModificationRules.CountAsync());

        // The P2P sample must have priced to €47 through the real pipeline + persistence.
        var p2p = await read.Tickets.SingleAsync(t => t.ProductType == ProductType.PointToPoint);
        Assert.Equal(47.00m, p2p.TotalFare);
        Assert.Equal(2, await read.AppliedTicketModifications.CountAsync(a => a.TicketId == p2p.Id));
        Assert.True(await read.FareCalculationSnapshots.AnyAsync(s => s.TicketId == p2p.Id));
    }

    [Fact]
    public async Task Seed_is_idempotent()
    {
        var db = await _sql.CreateDatabaseAsync();
        await using (var ctx = db.NewContext()) await BuildSeeder(ctx).SeedAsync();
        await using (var ctx = db.NewContext()) await BuildSeeder(ctx).SeedAsync();

        await using var read = db.NewContext();
        Assert.Equal(2, await read.CurrentFareRates.CountAsync());
        Assert.Equal(4, await read.CurrentModificationRules.CountAsync());
        Assert.Equal(2, await read.Tickets.CountAsync()); // still only the two samples
    }
}
