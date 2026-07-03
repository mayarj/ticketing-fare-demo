using System.Collections.Generic;
using Ticketing.Domain.Enums;

namespace Ticketing.Application.Dtos;

/// <summary>
/// Request from client for a modification to apply. Server controls order via priority.
/// Client sends unordered list; server applies in priority order.
/// </summary>
public sealed class ModificationRequest
{
    /// <summary>
    /// Modification code (e.g., "FIRST_CLASS", "LUGGAGE", "INSURANCE").
    /// Must correspond to an active row in <see cref="CurrentModificationRule"/>.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Quantity of the modification to apply (e.g., number of bags).
    /// Defaults to 1 if not provided.
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Additional parameters for the modification (e.g., {"numberOfBags": 2}).
    /// Shape depends on the modification rule.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Validates that the request is well-formed.
    /// </summary>
    public bool IsValid() => 
        !string.IsNullOrWhiteSpace(Code) && 
        Quantity > 0;
}