namespace Ticketing.Domain.Policies;

/// <summary>
/// The return value of a base fare strategy: the computed amount plus a JSON blob of
/// the inputs and a human-readable formula, frozen later into the ticket's snapshot.
/// </summary>
public sealed record FareResult(decimal Amount, string InputsJson);
