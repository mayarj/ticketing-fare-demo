using Ticketing.Infrastructure.Services;

namespace Ticketing.Infrastructure.Tests;

public class TicketNumberGeneratorTests
{
    private readonly TicketNumberGenerator _generator = new();

    [Fact]
    public void Generated_number_matches_the_documented_format()
    {
        var number = _generator.Generate();

        Assert.Matches(@"^TKT-\d{4}-[A-Z2-9]{8}$", number);
        Assert.True(_generator.IsValidFormat(number));
    }

    [Fact]
    public void Generated_number_carries_the_current_utc_year()
    {
        Assert.Equal(DateTime.UtcNow.Year, _generator.ExtractYear(_generator.Generate()));
    }

    [Theory]
    [InlineData("")]
    [InlineData("nope")]
    [InlineData("TKT-2026-ABC")]        // suffix too short
    [InlineData("TKT-2026-ABCDEFG1")]   // '1' is excluded from the alphabet
    [InlineData("tkt-2026-ABCDEFGH")]   // lowercase prefix
    public void IsValidFormat_rejects_malformed_numbers(string value)
    {
        Assert.False(_generator.IsValidFormat(value));
        Assert.Null(_generator.ExtractYear(value));
    }

    [Fact]
    public void Generates_distinct_numbers_across_many_calls()
    {
        var numbers = new HashSet<string>();
        for (var i = 0; i < 1_000; i++)
            numbers.Add(_generator.Generate());

        Assert.Equal(1_000, numbers.Count);
    }
}
