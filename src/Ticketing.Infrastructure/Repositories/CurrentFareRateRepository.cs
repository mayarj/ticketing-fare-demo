using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Abstractions;
using Ticketing.Domain.Entities;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Repositories;

/// <summary>
/// Reads active fare rates. Read-only at calculation time, so all queries use
/// </summary>
public sealed class CurrentFareRateRepository : ICurrentFareRateRepository
{
    private readonly TicketingDbContext _db;

    public CurrentFareRateRepository(TicketingDbContext db) => _db = db;

    public Task<CurrentFareRate?> GetActiveByPolicyCodeAsync(
        string policyCode,
        CancellationToken cancellationToken = default) =>
        _db.CurrentFareRates
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IsActive && r.PolicyCode == policyCode, cancellationToken);

    public async Task<IReadOnlyList<CurrentFareRate>> GetAllActiveAsync(
        CancellationToken cancellationToken = default) =>
        await _db.CurrentFareRates
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.PolicyCode)
            .ToListAsync(cancellationToken);

    public async Task<CurrentFareRate> GetActiveOrThrowAsync(
        string policyCode,
        CancellationToken cancellationToken = default) =>
        await GetActiveByPolicyCodeAsync(policyCode, cancellationToken)
        ?? throw new InvalidOperationException($"No active fare rate for policy code '{policyCode}'.");
}