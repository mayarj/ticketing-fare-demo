using Ticketing.Application.Dtos;

namespace Ticketing.Application.Tests.Dtos;

public class PointToPointRequestValidationTests
{
    [Theory]
    [InlineData("STATION_A", "STATION_B", true)]
    [InlineData("STATION_A", "STATION_A", false)] // origin == destination
    [InlineData("", "STATION_B", false)]
    [InlineData("STATION_A", "", false)]
    [InlineData("   ", "STATION_B", false)]
    public void IsValid_requires_two_distinct_non_blank_stations(string origin, string destination, bool expected) =>
        Assert.Equal(expected, new PointToPointRequest { Origin = origin, Destination = destination }.IsValid());
}

public class DailyPassRequestValidationTests
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public void Today_with_a_zone_is_valid() =>
        Assert.True(new DailyPassRequest { TravelDate = Today, Zone = "ZONE_1" }.IsValid());

    [Fact]
    public void A_future_date_with_a_zone_is_valid() =>
        Assert.True(new DailyPassRequest { TravelDate = Today.AddDays(30), Zone = "ZONE_1" }.IsValid());

    [Fact]
    public void A_past_date_is_invalid() =>
        Assert.False(new DailyPassRequest { TravelDate = Today.AddDays(-1), Zone = "ZONE_1" }.IsValid());

    [Fact]
    public void A_blank_zone_is_invalid() =>
        Assert.False(new DailyPassRequest { TravelDate = Today, Zone = "   " }.IsValid());
}

public class ModificationRequestValidationTests
{
    [Theory]
    [InlineData("FIRST_CLASS", 1, true)]
    [InlineData("FIRST_CLASS", 3, true)]
    [InlineData("FIRST_CLASS", 0, false)]  // quantity must be positive
    [InlineData("FIRST_CLASS", -1, false)]
    [InlineData("", 1, false)]
    [InlineData("   ", 1, false)]
    public void IsValid_requires_a_code_and_a_positive_quantity(string code, int quantity, bool expected) =>
        Assert.Equal(expected, new ModificationRequest { Code = code, Quantity = quantity }.IsValid());
}
