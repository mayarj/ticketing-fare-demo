using System;
using System.Collections.Generic;
using System.Linq;

namespace Ticketing.Application.Services;

/// <summary>
/// Thrown when a request references one or more modification codes that do not exist
/// or are not active. Maps to an HTTP 400 at the API boundary.
/// </summary>
public sealed class UnknownModificationException : Exception
{
    public UnknownModificationException(IEnumerable<string> codes)
        : base($"Unknown or inactive modification code(s): {string.Join(", ", codes)}.")
    {
        Codes = codes.ToArray();
    }

    public IReadOnlyList<string> Codes { get; }
}

/// <summary>
/// Thrown when no distance is known for the requested origin/destination pair.
/// Maps to an HTTP 400 at the API boundary.
/// </summary>
public sealed class RouteNotFoundException : Exception
{
    public RouteNotFoundException(string origin, string destination)
        : base($"No known route between '{origin}' and '{destination}'.")
    {
        Origin = origin;
        Destination = destination;
    }

    public string Origin { get; }
    public string Destination { get; }
}