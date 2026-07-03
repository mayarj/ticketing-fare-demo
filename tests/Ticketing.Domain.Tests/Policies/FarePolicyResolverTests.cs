using Ticketing.Domain.Enums;
using Ticketing.Domain.Policies;

namespace Ticketing.Domain.Tests.Policies;

public class FarePolicyResolverTests
{
    private static FarePolicyResolver BuildResolver() =>
        new([new PointToPointFarePolicy(), new DailyPassFarePolicy()]);

    [Fact]
    public void For_returns_the_policy_that_handles_the_requested_type()
    {
        var resolver = BuildResolver();

        Assert.IsType<PointToPointFarePolicy>(resolver.For(ProductType.PointToPoint));
        Assert.IsType<DailyPassFarePolicy>(resolver.For(ProductType.DailyPass));
    }


    [Fact]
    public void Constructor_fails_loudly_when_two_policies_claim_the_same_type()
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new FarePolicyResolver([new PointToPointFarePolicy(), new PointToPointFarePolicy()]));
    }
}
