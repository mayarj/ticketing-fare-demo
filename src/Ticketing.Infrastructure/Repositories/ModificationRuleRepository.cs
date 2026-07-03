using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Ticketing.Application.Abstractions;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Modifications;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Repositories;

/// <summary>
/// Reads active modification rules. Read-only at calculation time (<c>AsNoTracking()</c>).
/// </summary>
public sealed class ModificationRuleRepository : IModificationRuleRepository
{
    private readonly TicketingDbContext _db;

    public ModificationRuleRepository(TicketingDbContext db) => _db = db;

    public Task<CurrentModificationRule?> GetActiveByCodeAsync(
        string code,
        CancellationToken cancellationToken = default) =>
        _db.CurrentModificationRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.IsActive && r.ModificationCode == code, cancellationToken);

    public async Task<Dictionary<string, CurrentModificationRule>> GetActiveByCodesAsync(
        IEnumerable<string> codes,
        CancellationToken cancellationToken = default)
    {
        var wanted = codes.Distinct(StringComparer.Ordinal).ToArray();
        if (wanted.Length == 0)
            return new Dictionary<string, CurrentModificationRule>(StringComparer.Ordinal);

        var rows = await _db.CurrentModificationRules
            .AsNoTracking()
            .Where(r => r.IsActive && wanted.Contains(r.ModificationCode))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.ModificationCode, StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<CurrentModificationRule>> GetAllActiveAsync(
        CancellationToken cancellationToken = default) =>
        await _db.CurrentModificationRules
            .AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.ModificationCode)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ModificationRule>> GetActiveRulesForApplierAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await GetAllActiveAsync(cancellationToken);
        return rows.Select(r => r.ToRule()).ToList();
    }

    public async Task<IReadOnlyList<string>> ValidateCodesAsync(
        IEnumerable<string> codes,
        CancellationToken cancellationToken = default)
    {
        var wanted = codes.Distinct(StringComparer.Ordinal).ToArray();
        if (wanted.Length == 0)
            return Array.Empty<string>();

        var found = await _db.CurrentModificationRules
            .AsNoTracking()
            .Where(r => r.IsActive && wanted.Contains(r.ModificationCode))
            .Select(r => r.ModificationCode)
            .ToListAsync(cancellationToken);

        return wanted.Except(found, StringComparer.Ordinal).ToArray();
    }

    public async Task<CurrentModificationRule> GetActiveOrThrowAsync(
        string code,
        CancellationToken cancellationToken = default) =>
        await GetActiveByCodeAsync(code, cancellationToken)
        ?? throw new InvalidOperationException($"No active modification rule for code '{code}'.");
}
