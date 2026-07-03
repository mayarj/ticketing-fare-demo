using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Ticketing.Application.Abstractions;

namespace Ticketing.Infrastructure.Persistence;

/// <summary>
/// Coordinates a single transaction over the shared <see cref="TicketingDbContext"/>.
/// The context is owned by DI (scoped) and disposed by the container; this type only
/// owns the transaction it opens.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly TicketingDbContext _db;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(TicketingDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        _transaction ??= await _db.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeTransactionAsync();
        }
    }

    public void Dispose() => _transaction?.Dispose();

    private async Task DisposeTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}