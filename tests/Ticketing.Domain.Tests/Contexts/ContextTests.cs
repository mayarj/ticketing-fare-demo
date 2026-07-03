using Ticketing.Domain.Contexts;
using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Tests.Contexts;

public class ContextTests
{
    [Fact]
    public void PointToPointContext_reports_its_product_type_and_carries_its_inputs()
    {
        var ctx = new PointToPointContext { Origin = "A", Destination = "B", DistanceKm = 60m };

        Assert.Equal(ProductType.PointToPoint, ctx.ProductType);
        Assert.Equal("A", ctx.Origin);
        Assert.Equal("B", ctx.Destination);
        Assert.Equal(60m, ctx.DistanceKm);
    }

    [Fact]
    public void DailyPassContext_reports_its_product_type_and_carries_its_inputs()
    {
        var ctx = new DailyPassContext { ValidOn = new DateOnly(2026, 7, 5) };

        Assert.Equal(ProductType.DailyPass, ctx.ProductType);
        Assert.Equal(new DateOnly(2026, 7, 5), ctx.ValidOn);
    }
}
