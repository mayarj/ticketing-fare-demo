using Ticketing.Application.Abstractions;
using Ticketing.Application.Dtos;
using Ticketing.Application.Services;
using Ticketing.Application.Tests.Fakes;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Modifications;
using Ticketing.Domain.Policies;

namespace Ticketing.Application.Tests;

public class TicketIssuerTests
{
    // Canonical seed used across the tests: the two rates and four modifications from the docs.
    private static FareCalculatorFactory BuildCalculator() =>
        new(
            new FarePolicyResolver([new PointToPointFarePolicy(), new DailyPassFarePolicy()]),
            new FakeCurrentFareRateRepository(
                CurrentFareRate.Create("POINT_TO_POINT", """{"ratePerKm":0.50,"minimumFare":2.00}"""),
                CurrentFareRate.Create("DAILY_PASS", """{"flatRate":8.00}""")),
            new FakeModificationRuleRepository(
                CurrentModificationRule.Create("EXTRA_LUGGAGE", RuleType.PerUnit, """{"amountPerUnit":3.50}""", 100),
                CurrentModificationRule.Create("FIRST_CLASS", RuleType.Fixed, """{"amount":10.00}""", 200),
                CurrentModificationRule.Create("VIP_CLASS", RuleType.Fixed, """{"amount":25.00}""", 200),
                CurrentModificationRule.Create("LOYALTY_10", RuleType.Percentage, """{"percent":-0.10}""", 900)),
            new ModificationApplier());

    private static TicketIssuer BuildIssuer(
        double? distanceKm = 60,
        FakeTicketRepository? tickets = null,
        FakeUnitOfWork? uow = null,
        FakeTicketNumberGenerator? numbers = null) =>
        new(
            BuildCalculator(),
            new FakeDistanceProvider(distanceKm),
            numbers ?? new FakeTicketNumberGenerator("TKT-2026-AAAA0001"),
            tickets ?? new FakeTicketRepository(),
            uow ?? new FakeUnitOfWork());

    private static PointToPointRequest ValidP2P() =>
        new() { Origin = "STATION_A", Destination = "STATION_C" };

    private static PointToPointRequest ValidP2PWithMods() =>
        new()
        {
            Origin = "STATION_A",
            Destination = "STATION_C",
            Modifications = [new() { Code = "FIRST_CLASS", Quantity = 1 }, new() { Code = "EXTRA_LUGGAGE", Quantity = 2 }]
        };

    [Fact]
    public async Task IssuePointToPoint_returns_the_documented_47_euro_breakdown()
    {
        var response = await BuildIssuer().IssuePointToPointAsync(ValidP2PWithMods());

        Assert.Equal(30.00m, response.BaseFare);
        Assert.Equal(47.00m, response.TotalFare);
        Assert.Equal("POINT_TO_POINT", response.ProductType);
        Assert.Equal("TKT-2026-AAAA0001", response.TicketNumber);
        Assert.Contains("POINT_TO_POINT", response.BaseFareBreakdown);
        // Server order (priority): EXTRA_LUGGAGE then FIRST_CLASS; running fare 37 -> 47.
        Assert.Equal(new[] { "EXTRA_LUGGAGE", "FIRST_CLASS" }, response.Modifications.Select(m => m.Code));
        Assert.Equal(new[] { 1, 2 }, response.Modifications.Select(m => m.AppliedOrder));
        Assert.Equal(new[] { 37.00m, 47.00m }, response.Modifications.Select(m => m.ResultingFare));
    }

    [Fact]
    public async Task IssueDailyPass_returns_flat_rate_plus_modifications()
    {
        var response = await BuildIssuer().IssueDailyPassAsync(new DailyPassRequest
        {
            TravelDate = DateOnly.FromDateTime(DateTime.Today),
            Zone = "ZONE_1",
            Modifications = [new() { Code = "VIP_CLASS", Quantity = 1 }]
        });

        Assert.Equal(8.00m, response.BaseFare);
        Assert.Equal(33.00m, response.TotalFare); // 8 + VIP 25
        Assert.Equal("DAILY_PASS", response.ProductType);
    }

    [Fact]
    public async Task IssuePointToPoint_with_no_modifications_returns_base_fare_only()
    {
        var response = await BuildIssuer().IssuePointToPointAsync(ValidP2P());

        Assert.Equal(30.00m, response.BaseFare);
        Assert.Equal(30.00m, response.TotalFare);
        Assert.Empty(response.Modifications);
    }

    [Fact]
    public async Task IssuePointToPoint_rejects_invalid_request_without_touching_persistence()
    {
        var tickets = new FakeTicketRepository();
        var uow = new FakeUnitOfWork();
        var issuer = BuildIssuer(tickets: tickets, uow: uow);

        await Assert.ThrowsAsync<ArgumentException>(
            () => issuer.IssuePointToPointAsync(new PointToPointRequest { Origin = "A", Destination = "A" }));

        Assert.Null(tickets.Added);
        Assert.Equal(0, uow.BeginCount);
    }

    [Fact]
    public async Task IssueDailyPass_rejects_a_past_travel_date()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => BuildIssuer().IssueDailyPassAsync(new DailyPassRequest
        {
            TravelDate = DateOnly.FromDateTime(DateTime.Today).AddDays(-1),
            Zone = "ZONE_1"
        }));
    }

    [Fact]
    public async Task Persistence_runs_begin_add_save_commit_in_order()
    {
        var journal = new List<string>();
        var tickets = new FakeTicketRepository(journal: journal);
        var uow = new FakeUnitOfWork(journal);

        await BuildIssuer(tickets: tickets, uow: uow).IssuePointToPointAsync(ValidP2PWithMods());

        Assert.Equal(new[] { "begin", "add", "save", "commit" }, journal);
        Assert.Equal(1, uow.CommitCount);
        Assert.Equal(0, uow.RollbackCount);
        Assert.NotNull(tickets.Added);
    }

    [Fact]
    public async Task Persistence_rolls_back_and_rethrows_when_save_fails()
    {
        var journal = new List<string>();
        var tickets = new FakeTicketRepository(journal: journal);
        var uow = new FakeUnitOfWork(journal) { ThrowOnSave = true };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => BuildIssuer(tickets: tickets, uow: uow).IssuePointToPointAsync(ValidP2PWithMods()));

        Assert.Equal(new[] { "begin", "add", "save", "rollback" }, journal);
        Assert.Equal(0, uow.CommitCount);
        Assert.Equal(1, uow.RollbackCount);
    }

    [Fact]
    public async Task Ticket_number_collision_retries_until_a_free_number_is_found()
    {
        var numbers = new FakeTicketNumberGenerator("N1", "N2", "N3", "N4", "N5");
        var tickets = new FakeTicketRepository(existingNumbers: ["N1", "N2", "N3", "N4"]);

        var response = await BuildIssuer(tickets: tickets, numbers: numbers).IssuePointToPointAsync(ValidP2P());

        Assert.Equal("N5", response.TicketNumber);
    }

    [Fact]
    public async Task Ticket_number_generation_gives_up_after_max_attempts()
    {
        var numbers = new FakeTicketNumberGenerator("DUP", "DUP", "DUP", "DUP", "DUP", "DUP");
        var tickets = new FakeTicketRepository(existingNumbers: ["DUP"]);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => BuildIssuer(tickets: tickets, numbers: numbers).IssuePointToPointAsync(ValidP2P()));
    }

    [Fact]
    public async Task Unknown_modification_bubbles_up_before_any_persistence()
    {
        var tickets = new FakeTicketRepository();
        var uow = new FakeUnitOfWork();
        var issuer = BuildIssuer(tickets: tickets, uow: uow);

        await Assert.ThrowsAsync<UnknownModificationException>(() => issuer.IssuePointToPointAsync(
            new PointToPointRequest { Origin = "STATION_A", Destination = "STATION_C", Modifications = [new() { Code = "NOPE" }] }));

        Assert.Null(tickets.Added);
        Assert.Equal(0, uow.BeginCount);
    }

    [Fact]
    public async Task Missing_route_bubbles_up_as_route_not_found()
    {
        await Assert.ThrowsAsync<RouteNotFoundException>(
            () => BuildIssuer(distanceKm: null).IssuePointToPointAsync(ValidP2P()));
    }

    [Fact]
    public async Task Persisted_ticket_carries_its_snapshot_and_applied_modifications()
    {
        var tickets = new FakeTicketRepository();

        await BuildIssuer(tickets: tickets).IssuePointToPointAsync(ValidP2PWithMods());

        var ticket = tickets.Added!;
        Assert.Equal(ProductType.PointToPoint, ticket.ProductType);
        Assert.NotNull(ticket.Snapshot);
        Assert.Equal(2, ticket.AppliedModifications.Count);
    }
}
