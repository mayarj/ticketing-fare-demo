using Ticketing.Domain.Enums;
using Ticketing.Domain.Modifications;

namespace Ticketing.Domain.Entities;

/// <summary>
/// One modification actually applied to a ticket, frozen at application time.
/// Stored relationally so "which tickets used FIRST_CLASS?" is a plain SELECT.
/// Immutable.
/// </summary>
public sealed class AppliedTicketModification
{
    private AppliedTicketModification() { } // EF

    public long Id { get; private set; }
    public long TicketId { get; private set; }
    public string ModificationCode { get; private set; } = default!;
    public RuleType RuleType { get; private set; }
    public int Quantity { get; private set; }
    public string ParamsUsed { get; private set; } = default!;
    public decimal Surcharge { get; private set; }
    public int AppliedOrder { get; private set; }
    public DateTime AppliedAt { get; private set; }

    /// <summary>
    /// Freezes a calculated <see cref="AppliedModification"/> onto a ticket. The
    /// <c>TicketId</c> FK is assigned by EF via the owning <see cref="Ticket"/>
    /// aggregate on save, so it is not passed in here.
    /// </summary>
    public static AppliedTicketModification From(AppliedModification modification) =>
        new()
        {
            ModificationCode = modification.Code,
            RuleType = modification.RuleType,
            Quantity = modification.Quantity,
            ParamsUsed = modification.ParamsUsed,
            Surcharge = modification.Surcharge,
            AppliedOrder = modification.AppliedOrder,
            AppliedAt = DateTime.UtcNow
        };
}
