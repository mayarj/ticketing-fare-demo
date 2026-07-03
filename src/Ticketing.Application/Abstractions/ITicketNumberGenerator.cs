namespace Ticketing.Application.Abstractions;

/// <summary>
/// Generates unique, human-readable ticket numbers.
/// Format: TKT-YYYY-[8 character alphanumeric sequence]
/// </summary>
public interface ITicketNumberGenerator
{
    /// <summary>
    /// Generates a new unique ticket number.
    /// </summary>
    string Generate();

    /// <summary>
    /// Validates whether a ticket number is in the correct format.
    /// </summary>
    bool IsValidFormat(string ticketNumber);

    /// <summary>
    /// Extracts the year from a ticket number.
    /// </summary>
    int? ExtractYear(string ticketNumber);
}