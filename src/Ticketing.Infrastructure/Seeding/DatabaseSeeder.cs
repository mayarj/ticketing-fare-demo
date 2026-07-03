using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Abstractions;
using Ticketing.Application.Dtos;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Seeding;

/// <summary>
/// Idempotently inserts the data needed to demonstrate the fare-calculation flow:
/// the base fare rates, the modification rules , and — through the real
/// pricing engine — a couple of sample sold products. Safe to run on every startup.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly TicketingDbContext _db;
    private readonly ITicketIssuer _issuer;

    public DatabaseSeeder(TicketingDbContext db, ITicketIssuer issuer)
    {
        _db = db;
        _issuer = issuer;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedFareRatesAsync(cancellationToken);
        await SeedModificationRulesAsync(cancellationToken);
        await SeedSampleTicketsAsync(cancellationToken);
    }

    private async Task SeedFareRatesAsync(CancellationToken cancellationToken)
    {
        if (await _db.CurrentFareRates.AnyAsync(cancellationToken))
            return;

        _db.CurrentFareRates.AddRange(
            CurrentFareRate.Create("POINT_TO_POINT", """{"ratePerKm":0.50,"minimumFare":2.00}"""),
            CurrentFareRate.Create("DAILY_PASS", """{"flatRate":8.00}"""));

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedModificationRulesAsync(CancellationToken cancellationToken)
    {
        if (await _db.CurrentModificationRules.AnyAsync(cancellationToken))
            return;

        _db.CurrentModificationRules.AddRange(
            CurrentModificationRule.Create("EXTRA_LUGGAGE", RuleType.PerUnit, """{"amountPerUnit":3.50}""", 100),
            CurrentModificationRule.Create("FIRST_CLASS", RuleType.Fixed, """{"amount":10.00}""", 200),
            CurrentModificationRule.Create("VIP_CLASS", RuleType.Fixed, """{"amount":25.00}""", 200),
            CurrentModificationRule.Create("LOYALTY_10", RuleType.Percentage, """{"percent":-0.10}""", 900));

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSampleTicketsAsync(CancellationToken cancellationToken)
    {
        if (await _db.Tickets.AnyAsync(cancellationToken))
            return;

        // Issue sample sold products through the real engine (the brief explicitly
        // allows sample products to be "added using your implemented pricing engine").
        await _issuer.IssuePointToPointAsync(new PointToPointRequest
        {
            Origin = "STATION_A",
            Destination = "STATION_C",
            Modifications = new List<ModificationRequest>
            {
                new() { Code = "FIRST_CLASS", Quantity = 1 },
                new() { Code = "EXTRA_LUGGAGE", Quantity = 2 }
            }
        }, cancellationToken);

        await _issuer.IssueDailyPassAsync(new DailyPassRequest
        {
            // Use the same local-date basis the request validator uses (DateTime.Today).
            TravelDate = DateOnly.FromDateTime(DateTime.Today),
            Zone = "ZONE_1",
            Modifications = new List<ModificationRequest>
            {
                new() { Code = "VIP_CLASS", Quantity = 1 }
            }
        }, cancellationToken);
    }
}