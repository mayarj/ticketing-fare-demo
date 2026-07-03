namespace Ticketing.Domain.Entities;

/// <summary>
/// Point-in-time snapshot of the base fare inputs for one ticket. Immutable.
/// Holds the inputs/formula only — the resulting base and total live as typed
/// decimal columns on <see cref="Ticket"/>. Editing the <c>current_*</c> tables
/// never affects a snapshot once written.
/// </summary>
public sealed class FareCalculationSnapshot
{
    private FareCalculationSnapshot() { } // EF

    public long Id { get; private set; }
    public long TicketId { get; private set; }
    public string PolicyCode { get; private set; } = default!;
    public string BaseFareInputs { get; private set; } = default!;
    public DateTime CalculatedAt { get; private set; }

    /// <summary>
    /// Creates a snapshot. The <c>TicketId</c> FK is assigned by EF when the owning
    /// <see cref="Ticket"/> aggregate is saved, so it is not passed in here.
    /// </summary>
    public static FareCalculationSnapshot Create(
        string policyCode,
        string baseFareInputs) =>
        new()
        {
            PolicyCode = policyCode,
            BaseFareInputs = baseFareInputs,
            CalculatedAt = DateTime.UtcNow
        };
}
