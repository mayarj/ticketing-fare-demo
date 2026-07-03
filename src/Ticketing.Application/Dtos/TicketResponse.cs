using System;
using System.Collections.Generic;

namespace Ticketing.Application.Dtos;

/// <summary>
/// Response returned after successfully issuing a ticket.
/// Includes complete fare breakdown for auditability.
/// </summary>
public sealed class TicketResponse
{
    /// <summary>
    /// Unique ticket identifier.
    /// </summary>
    public long TicketId { get; set; }

    /// <summary>
    /// Human-readable ticket number (e.g., "TKT-2026-ABC123").
    /// </summary>
    public string TicketNumber { get; set; } = string.Empty;

    /// <summary>
    /// Product type (e.g., "PointToPoint", "DailyPass").
    /// </summary>
    public string ProductType { get; set; } = string.Empty;

    /// <summary>
    /// Date and time the ticket was issued (UTC).
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// Base fare before modifications were applied.
    /// </summary>
    public decimal BaseFare { get; set; }

    /// <summary>
    /// Breakdown of how the base fare was calculated.
    /// Example: {"policy": "POINT_TO_POINT", "ratePerKm": 0.50, "distance": 60}
    /// </summary>
    public string BaseFareBreakdown { get; set; } = string.Empty;

    /// <summary>
    /// Each modification applied, in order of application (by priority).
    /// </summary>
    public List<AppliedModificationResponse> Modifications { get; set; } = new();

    /// <summary>
    /// Total fare including all modifications.
    /// </summary>
    public decimal TotalFare { get; set; }

    /// <summary>
    /// Optional message for the customer.
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// One modification applied to a ticket, frozen at time of issue.
/// </summary>
public sealed class AppliedModificationResponse
{
    /// <summary>
    /// Modification code (e.g., "FIRST_CLASS").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description (e.g., "First Class Upgrade").
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity applied (e.g., 2 for two bags).
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Surcharge for this modification.
    /// </summary>
    public decimal Surcharge { get; set; }

    /// <summary>
    /// Order in which this modification was applied (1-based).
    /// Determined by server priority, not client order.
    /// </summary>
    public int AppliedOrder { get; set; }

    /// <summary>
    /// The fare after this modification was applied.
    /// Useful for understanding compounding effects.
    /// </summary>
    public decimal ResultingFare { get; set; }
}