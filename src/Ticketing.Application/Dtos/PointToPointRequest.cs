using System.Collections.Generic;

namespace Ticketing.Application.Dtos;

/// <summary>
/// Request to issue a point-to-point ticket.
/// </summary>
public sealed class PointToPointRequest
{
    /// <summary>
    /// Origin station/location.
    /// </summary>
    public string Origin { get; set; } = string.Empty;

    /// <summary>
    /// Destination station/location.
    /// </summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// Optional modifications to apply to the ticket.
    /// Server applies modifications in priority order, not client order.
    /// </summary>
    public List<ModificationRequest>? Modifications { get; set; }

    /// <summary>
    /// Validates the request is complete and well-formed.
    /// </summary>
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(Origin) &&
        !string.IsNullOrWhiteSpace(Destination) &&
        Origin != Destination;
}