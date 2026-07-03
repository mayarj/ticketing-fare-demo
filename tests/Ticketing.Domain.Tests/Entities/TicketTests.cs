using Ticketing.Domain.Entities;
using Ticketing.Domain.Enums;

namespace Ticketing.Domain.Tests.Entities;

public class TicketTests
{
    [Fact]
    public void Issue_sets_every_field_from_its_arguments()
    {
        var ticket = Ticket.Issue("T-000042", ProductType.PointToPoint, 30.00m, 47.00m);

        Assert.Equal("T-000042", ticket.Number);
        Assert.Equal(ProductType.PointToPoint, ticket.ProductType);
        Assert.Equal(30.00m, ticket.BaseFare);
        Assert.Equal(47.00m, ticket.TotalFare);
    }

    [Fact]
    public void Issue_stamps_issued_at_in_utc_at_creation_time()
    {
        var before = DateTime.UtcNow;
        var ticket = Ticket.Issue("T-1", ProductType.DailyPass, 8.00m, 8.00m);
        var after = DateTime.UtcNow;

        Assert.Equal(DateTimeKind.Utc, ticket.IssuedAt.Kind);
        Assert.InRange(ticket.IssuedAt, before, after);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Issue_rejects_a_missing_or_blank_ticket_number(string? ticketNumber)
    {
        Assert.Throws<ArgumentException>(
            () => Ticket.Issue(ticketNumber!, ProductType.PointToPoint, 30m, 47m));
    }
}
