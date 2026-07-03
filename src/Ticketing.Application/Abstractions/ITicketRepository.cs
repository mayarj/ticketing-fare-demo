using System.Threading;
using System.Threading.Tasks;
using Ticketing.Domain.Entities;

namespace Ticketing.Application.Abstractions;

/// <summary>
/// Repository for ticket persistence. A ticket is the root of a small aggregate that
/// also owns its <see cref="FareCalculationSnapshot"/> and its
/// <see cref="AppliedTicketModification"/> rows, so adding the ticket persists the
/// whole graph. Tickets are immutable once created.
/// </summary>
public interface ITicketRepository
{
    /// <summary>
    /// Stages a new ticket (and its attached snapshot + applied modifications) for
    /// insertion. The actual write happens when the unit of work is saved/committed.
    /// </summary>
    Task AddAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a ticket number is already taken — used to keep generated
    /// ticket numbers unique (backed by a unique index as the final guard).
    /// </summary>
    Task<bool> ExistsByNumberAsync(
        string number,
        CancellationToken cancellationToken = default);
}