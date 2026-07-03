using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Ticketing.Application.Abstractions;

namespace Ticketing.Infrastructure.Services;

/// <summary>
/// Generates human-readable ticket numbers of the form <c>TKT-YYYY-XXXXXXXX</c>, where
/// the suffix is 8 cryptographically-random base-32 characters (ambiguous 0/O/1/I
/// excluded). Uniqueness is effectively guaranteed by the wide keyspace and enforced
/// as a hard invariant by the unique index on <c>tickets.ticket_number</c>.
/// </summary>
public sealed partial class TicketNumberGenerator : ITicketNumberGenerator
{
    private const string Prefix = "TKT";
    private const int SuffixLength = 8;
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // 32 chars, no 0/O/1/I

    public string Generate()
    {
        var year = DateTime.UtcNow.Year;
        Span<char> suffix = stackalloc char[SuffixLength];
        for (var i = 0; i < SuffixLength; i++)
            suffix[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        return $"{Prefix}-{year}-{new string(suffix)}";
    }

    public bool IsValidFormat(string ticketNumber) =>
        !string.IsNullOrWhiteSpace(ticketNumber) && FormatRegex().IsMatch(ticketNumber);

    public int? ExtractYear(string ticketNumber)
    {
        if (string.IsNullOrWhiteSpace(ticketNumber))
            return null;

        var match = FormatRegex().Match(ticketNumber);
        return match.Success ? int.Parse(match.Groups["year"].Value) : null;
    }

    [GeneratedRegex(@"^TKT-(?<year>\d{4})-[A-Z2-9]{8}$")]
    private static partial Regex FormatRegex();
}