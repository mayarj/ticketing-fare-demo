using System;
using System.Collections.Generic;

namespace Ticketing.Application.Dtos;

/// <summary>
/// Request to issue a daily pass ticket.
/// </summary>
public sealed class DailyPassRequest
{
    /// <summary>
    /// Date the pass is valid for.
    /// </summary>
    public DateOnly TravelDate { get; set; }

    /// <summary>
    /// Zone or region the pass covers. Accepted for realism; the flat daily rate does
    /// not currently vary by zone (documented assumption).
    /// </summary>
    public string Zone { get; set; } = string.Empty;

    /// <summary>
    /// Optional modifications to apply to the ticket.
    /// Server applies modifications in priority order, not client order.
    /// </summary>
    public List<ModificationRequest>? Modifications { get; set; }

    /// <summary>
    /// Validates the request is complete and well-formed.
    /// </summary>
    public bool IsValid() =>
        TravelDate >= DateOnly.FromDateTime(DateTime.Today) &&
        !string.IsNullOrWhiteSpace(Zone);
}