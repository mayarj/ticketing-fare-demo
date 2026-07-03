using Ticketing.Application.Abstractions;
using Ticketing.Application.Services;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Modifications;

namespace Ticketing.Application.Tests.Fakes;

/// <summary>In-memory fare-rate store keyed by policy code.</summary>
public sealed class FakeCurrentFareRateRepository : ICurrentFareRateRepository
{
    private readonly Dictionary<string, CurrentFareRate> _byCode;

    public FakeCurrentFareRateRepository(params CurrentFareRate[] rates) =>
        _byCode = rates.ToDictionary(r => r.PolicyCode, StringComparer.Ordinal);

    public Task<CurrentFareRate?> GetActiveByPolicyCodeAsync(string policyCode, CancellationToken ct = default) =>
        Task.FromResult(_byCode.GetValueOrDefault(policyCode));

    public Task<IReadOnlyList<CurrentFareRate>> GetAllActiveAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CurrentFareRate>>(_byCode.Values.ToList());

    public Task<CurrentFareRate> GetActiveOrThrowAsync(string policyCode, CancellationToken ct = default) =>
        _byCode.TryGetValue(policyCode, out var rate)
            ? Task.FromResult(rate)
            : throw new InvalidOperationException($"No active fare rate for '{policyCode}'.");
}

/// <summary>In-memory modification-rule store.</summary>
public sealed class FakeModificationRuleRepository : IModificationRuleRepository
{
    private readonly List<CurrentModificationRule> _rules;

    public FakeModificationRuleRepository(params CurrentModificationRule[] rules) => _rules = rules.ToList();

    public Task<CurrentModificationRule?> GetActiveByCodeAsync(string code, CancellationToken ct = default) =>
        Task.FromResult(_rules.FirstOrDefault(r => r.ModificationCode == code));

    public Task<Dictionary<string, CurrentModificationRule>> GetActiveByCodesAsync(
        IEnumerable<string> codes, CancellationToken ct = default)
    {
        var wanted = codes.ToHashSet(StringComparer.Ordinal);
        return Task.FromResult(_rules
            .Where(r => wanted.Contains(r.ModificationCode))
            .ToDictionary(r => r.ModificationCode, StringComparer.Ordinal));
    }

    public Task<IReadOnlyList<CurrentModificationRule>> GetAllActiveAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<CurrentModificationRule>>(_rules.ToList());

    public Task<IReadOnlyList<ModificationRule>> GetActiveRulesForApplierAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ModificationRule>>(_rules.Select(r => r.ToRule()).ToList());

    public Task<IReadOnlyList<string>> ValidateCodesAsync(IEnumerable<string> codes, CancellationToken ct = default)
    {
        var have = _rules.Select(r => r.ModificationCode).ToHashSet(StringComparer.Ordinal);
        return Task.FromResult<IReadOnlyList<string>>(codes.Where(c => !have.Contains(c)).ToList());
    }

    public Task<CurrentModificationRule> GetActiveOrThrowAsync(string code, CancellationToken ct = default) =>
        _rules.FirstOrDefault(r => r.ModificationCode == code) is { } rule
            ? Task.FromResult(rule)
            : throw new InvalidOperationException($"No active modification rule for '{code}'.");
}

/// <summary>Captures the added ticket and answers existence from a preset set.</summary>
public sealed class FakeTicketRepository : ITicketRepository
{
    private readonly HashSet<string> _existing;

    public FakeTicketRepository(IEnumerable<string>? existingNumbers = null) =>
        _existing = new HashSet<string>(existingNumbers ?? Array.Empty<string>(), StringComparer.Ordinal);

    public Ticket? Added { get; private set; }

    public Task AddAsync(Ticket ticket, CancellationToken ct = default)
    {
        Added = ticket;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByNumberAsync(string number, CancellationToken ct = default) =>
        Task.FromResult(_existing.Contains(number));
}

/// <summary>Records how the transaction methods were invoked.</summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int BeginCount { get; private set; }
    public int CommitCount { get; private set; }
    public int RollbackCount { get; private set; }
    public int SaveCount { get; private set; }

    public Task BeginTransactionAsync(CancellationToken ct = default) { BeginCount++; return Task.CompletedTask; }
    public Task CommitTransactionAsync(CancellationToken ct = default) { CommitCount++; return Task.CompletedTask; }
    public Task RollbackTransactionAsync(CancellationToken ct = default) { RollbackCount++; return Task.CompletedTask; }
    public Task<int> SaveChangesAsync(CancellationToken ct = default) { SaveCount++; return Task.FromResult(1); }
    public void Dispose() { }
}

/// <summary>Returns a fixed distance, or throws when configured with null.</summary>
public sealed class FakeDistanceProvider : IDistanceProvider
{
    private readonly double? _distance;

    public FakeDistanceProvider(double? distance) => _distance = distance;

    public Task<double?> GetDistanceKmAsync(string origin, string destination, CancellationToken ct = default) =>
        Task.FromResult(_distance);

    public Task<double> GetDistanceKmOrThrowAsync(string origin, string destination, CancellationToken ct = default) =>
        _distance is { } km ? Task.FromResult(km) : throw new RouteNotFoundException(origin, destination);
}

/// <summary>Hands out preset ticket numbers in order (to drive collision-retry tests).</summary>
public sealed class FakeTicketNumberGenerator : ITicketNumberGenerator
{
    private readonly Queue<string> _numbers;

    public FakeTicketNumberGenerator(params string[] numbers) => _numbers = new Queue<string>(numbers);

    public string Generate() => _numbers.Count > 0 ? _numbers.Dequeue() : "TKT-2026-FALLBACK";
    public bool IsValidFormat(string ticketNumber) => true;
    public int? ExtractYear(string ticketNumber) => 2026;
}