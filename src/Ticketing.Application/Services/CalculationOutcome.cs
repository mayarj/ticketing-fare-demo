using System.Collections.Generic;
using Ticketing.Domain.Modifications;

namespace Ticketing.Application.Services;

/// <summary>
/// The result of composing a base fare with its ordered modifications. Pure data with
/// no persistence — the <see cref="TicketIssuer"/> turns this into a ticket, a snapshot,
/// and the applied-modification rows.
/// </summary>
public sealed record CalculationOutcome(
    decimal BaseFare,
    string BaseFareInputs,
    decimal TotalFare,
    IReadOnlyList<AppliedModification> Applied);