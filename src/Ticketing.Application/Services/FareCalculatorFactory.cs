using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ticketing.Application.Abstractions;
using Ticketing.Application.Dtos;
using Ticketing.Domain.Contexts;
using Ticketing.Domain.Enums;
using Ticketing.Domain.Modifications;
using Ticketing.Domain.Policies;

namespace Ticketing.Application.Services;

/// <summary>
/// Composes the fare for one request: resolves the base-fare policy, loads its rate
/// row, loads and orders the requested modification rules, and folds them over the
/// base fare. Pure calculation — it never writes to the database (that is the
/// <see cref="TicketIssuer"/>'s job).
/// </summary>
public sealed class FareCalculatorFactory
{
    private readonly FarePolicyResolver _policies;
    private readonly ICurrentFareRateRepository _rates;
    private readonly IModificationRuleRepository _rules;
    private readonly ModificationApplier _applier;

    public FareCalculatorFactory(
        FarePolicyResolver policies,
        ICurrentFareRateRepository rates,
        IModificationRuleRepository rules,
        ModificationApplier applier)
    {
        _policies = policies;
        _rates = rates;
        _rules = rules;
        _applier = applier;
    }

    public async Task<CalculationOutcome> CalculateAsync(
        ProductType productType,
        FareCalculationContext context,
        IReadOnlyList<ModificationRequest> modifications,
        CancellationToken cancellationToken = default)
    {
        // 1. Base fare — algorithm comes from the policy, parameters from the data row.
        var policy = _policies.For(productType);
        var rate = await _rates.GetActiveOrThrowAsync(PolicyCodes.For(productType), cancellationToken);
        var baseResult = policy.Calculate(context, rate.ParamsJson);

        // 2. Modifications — treated as an unordered set; server orders by priority,
        //    tie-broken by code . Client order is ignored.
        var requested = modifications ?? Array.Empty<ModificationRequest>();
        var codes = requested
            .Select(m => m.Code)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var rulesByCode = await _rules.GetActiveByCodesAsync(codes, cancellationToken);

        var unknown = codes.Where(c => !rulesByCode.ContainsKey(c)).ToArray();
        if (unknown.Length > 0)
            throw new UnknownModificationException(unknown);

        var ordered = rulesByCode.Values
            .Select(rule => rule.ToRule())
            .OrderBy(rule => rule.Priority)
            .ThenBy(rule => rule.Code, StringComparer.Ordinal)
            .ToList();

        var quantities = requested
            .GroupBy(m => m.Code, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last().Quantity, StringComparer.Ordinal);

        var applied = _applier.Apply(baseResult.Amount, ordered, quantities);
        var total = baseResult.Amount + applied.Sum(a => a.Surcharge);

        return new CalculationOutcome(baseResult.Amount, baseResult.InputsJson, total, applied);
    }
}

/// <summary>
/// Maps a <see cref="ProductType"/> to the stable <c>policy_code</c> string used by the
/// database rows. Kept in one place so the factory and the issuer stay in agreement.
/// </summary>
internal static class PolicyCodes
{
    public static string For(ProductType type) => type switch
    {
        ProductType.PointToPoint => "POINT_TO_POINT",
        ProductType.DailyPass => "DAILY_PASS",
        _ => throw new NotSupportedException($"No policy code mapping for product type '{type}'.")
    };
}