using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Abstractions;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Repositories;

/// <summary>
/// Persists ticket aggregates. <see cref="AddAsync"/> stages the ticket together with
/// its owned snapshot and applied-modification rows (via the aggregate navigations);
/// the write is committed by the unit of work.
/// </summary>
public sealed class TicketRepository : ITicketRepository
{
    private readonly TicketingDbContext _db;

    public TicketRepository(TicketingDbContext db) => _db = db;

    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default) =>
        await _db.Tickets.AddAsync(ticket, cancellationToken);

    public Task<bool> ExistsByNumberAsync(string number, CancellationToken cancellationToken = default) =>
        _db.Tickets
            .AsNoTracking()
            .AnyAsync(t => t.Number == number, cancellationToken);
}