using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Contexts;

/// <summary>Inputs for a point-to-point fare: the two stations and the distance between them.</summary>
public sealed class PointToPointContext : FareCalculationContext
{
    public PointToPointContext() : base(ProductType.PointToPoint) { }

    public string Origin { get; init; } = default!;
    public string Destination { get; init; } = default!;
    public decimal DistanceKm { get; init; }
}
