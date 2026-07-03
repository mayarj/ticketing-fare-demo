using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Entities;

/// <summary>
/// An issued, sold product. A single flat entity regardless of product type —
/// polymorphism lives on fare calculation (<c>IFarePolicy</c>), not here, so there
/// is no EF Core inheritance mapping. Never updated after creation.
/// </summary>
public sealed class Ticket
{
    private readonly List<AppliedTicketModification> _appliedModifications = [];

    private Ticket() { } // EF

    public long Id { get; private set; }
    public string Number { get; private set; } = default!;
    public ProductType ProductType { get; private set; }
    public DateTime IssuedAt { get; private set; }
    public decimal BaseFare { get; private set; }
    public decimal TotalFare { get; private set; }

    /// <summary>
    /// The base-fare inputs snapshot for this ticket. Owned by the aggregate; EF sets
    /// the FK from this navigation, so the whole graph persists in one <c>SaveChanges</c>.
    /// </summary>
    public FareCalculationSnapshot? Snapshot { get; private set; }

    /// <summary>The modifications applied to this ticket, frozen at issue time.</summary>
    public IReadOnlyCollection<AppliedTicketModification> AppliedModifications => _appliedModifications;

    /// <summary>
    /// Named-constructor factory. No Builder, no fluent chain — <c>Ticket</c>
    /// construction has neither many optional inputs nor multiple partial states.
    /// </summary>
    public static Ticket Issue(
        string ticketNumber,
        ProductType type,
        decimal baseFare,
        decimal totalFare)
    {
        if (string.IsNullOrWhiteSpace(ticketNumber))
            throw new ArgumentException("Ticket number is required.", nameof(ticketNumber));

        return new Ticket
        {
            Number = ticketNumber,
            ProductType = type,
            BaseFare = baseFare,
            TotalFare = totalFare,
            IssuedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Attaches the base-fare snapshot and applied modifications to this ticket as one
    /// aggregate. FKs are assigned by EF when the graph is saved — callers do not set
    /// <c>TicketId</c> on the children.
    /// </summary>
    public void RecordCalculation(
        FareCalculationSnapshot snapshot,
        IEnumerable<AppliedTicketModification> modifications)
    {
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _appliedModifications.Clear();
        _appliedModifications.AddRange(modifications);
    }
}
