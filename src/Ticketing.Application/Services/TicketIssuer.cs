using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ticketing.Application.Abstractions;
using Ticketing.Application.Dtos;
using Ticketing.Domain.Contexts;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;

namespace Ticketing.Application.Services;

/// <summary>
/// Orchestrates ticket issuance: build the calculation context, run the fare
/// calculation, then persist the ticket aggregate (ticket + snapshot + applied
/// modifications) inside a single transaction and shape the response.
/// The factory <em>calculates</em>; the issuer <em>persists</em>.
/// </summary>
public sealed class TicketIssuer : ITicketIssuer
{
    private const int MaxTicketNumberAttempts = 5;

    private readonly FareCalculatorFactory _calculator;
    private readonly IDistanceProvider _distances;
    private readonly ITicketNumberGenerator _numbers;
    private readonly ITicketRepository _tickets;
    private readonly IUnitOfWork _unitOfWork;

    public TicketIssuer(
        FareCalculatorFactory calculator,
        IDistanceProvider distances,
        ITicketNumberGenerator numbers,
        ITicketRepository tickets,
        IUnitOfWork unitOfWork)
    {
        _calculator = calculator;
        _distances = distances;
        _numbers = numbers;
        _tickets = tickets;
        _unitOfWork = unitOfWork;
    }

    public async Task<TicketResponse> IssuePointToPointAsync(
        PointToPointRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.IsValid())
            throw new ArgumentException("Origin and destination are required and must differ.");

        var distanceKm = await _distances.GetDistanceKmOrThrowAsync(
            request.Origin, request.Destination, cancellationToken);

        var context = new PointToPointContext
        {
            Origin = request.Origin,
            Destination = request.Destination,
            DistanceKm = (decimal)distanceKm
        };

        return await IssueAsync(ProductType.PointToPoint, context, request.Modifications, cancellationToken);
    }

    public async Task<TicketResponse> IssueDailyPassAsync(
        DailyPassRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.IsValid())
            throw new ArgumentException("A valid future travel date and zone are required.");

        // Zone is accepted for realism but does not change the flat rate (documented).
        var context = new DailyPassContext { ValidOn = request.TravelDate };

        return await IssueAsync(ProductType.DailyPass, context, request.Modifications, cancellationToken);
    }

    private async Task<TicketResponse> IssueAsync(
        ProductType productType,
        FareCalculationContext context,
        List<ModificationRequest>? modifications,
        CancellationToken cancellationToken)
    {
        var mods = (IReadOnlyList<ModificationRequest>?)modifications ?? Array.Empty<ModificationRequest>();

        var outcome = await _calculator.CalculateAsync(productType, context, mods, cancellationToken);

        var number = await NextTicketNumberAsync(cancellationToken);
        var ticket = Ticket.Issue(number, productType, outcome.BaseFare, outcome.TotalFare);
        var snapshot = FareCalculationSnapshot.Create(PolicyCodes.For(productType), outcome.BaseFareInputs);
        var applied = outcome.Applied.Select(AppliedTicketModification.From).ToList();
        ticket.RecordCalculation(snapshot, applied);

        await PersistAsync(ticket, cancellationToken);

        return MapToResponse(ticket, productType, outcome);
    }

    private async Task<string> NextTicketNumberAsync(CancellationToken cancellationToken)
    {
        // Random numbers collide only vanishingly rarely; the unique index is the final
        // guard. Pre-checking keeps the happy path free of insert failures.
        for (var attempt = 0; attempt < MaxTicketNumberAttempts; attempt++)
        {
            var candidate = _numbers.Generate();
            if (!await _tickets.ExistsByNumberAsync(candidate, cancellationToken))
                return candidate;
        }

        throw new InvalidOperationException("Unable to generate a unique ticket number.");
    }

    private async Task PersistAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _tickets.AddAsync(ticket, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static TicketResponse MapToResponse(
        Ticket ticket,
        ProductType productType,
        CalculationOutcome outcome)
    {
        // outcome.Applied is already in server-applied order; accumulate the running
        // fare so each row shows its compounding effect.
        var running = outcome.BaseFare;
        var modifications = new List<AppliedModificationResponse>(outcome.Applied.Count);

        foreach (var applied in outcome.Applied)
        {
            running += applied.Surcharge;
            modifications.Add(new AppliedModificationResponse
            {
                Code = applied.Code,
                Description = applied.Code, // no description column in the demo schema
                Quantity = applied.Quantity,
                Surcharge = applied.Surcharge,
                AppliedOrder = applied.AppliedOrder,
                ResultingFare = running
            });
        }

        return new TicketResponse
        {
            TicketId = ticket.Id,
            TicketNumber = ticket.Number,
            ProductType = PolicyCodes.For(productType),
            IssuedAt = ticket.IssuedAt,
            BaseFare = ticket.BaseFare,
            BaseFareBreakdown = outcome.BaseFareInputs,
            Modifications = modifications,
            TotalFare = ticket.TotalFare
        };
    }
}