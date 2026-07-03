using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ticketing.Application.Dtos;

namespace Ticketing.Application.Abstractions;

/// <summary>
/// Core service for issuing tickets. Uses the existing domain entities and policies.
/// </summary>
public interface ITicketIssuer
{
    /// <summary>
    /// Issues a point-to-point ticket.
    /// </summary>
    Task<TicketResponse> IssuePointToPointAsync(
        PointToPointRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a daily pass ticket.
    /// </summary>
    Task<TicketResponse> IssueDailyPassAsync(
        DailyPassRequest request,
        CancellationToken cancellationToken = default);
}