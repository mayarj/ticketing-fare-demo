using Microsoft.AspNetCore.Mvc;
using Ticketing.Application.Abstractions;
using Ticketing.Application.Dtos;
using Ticketing.Application.Services;

namespace Ticketing.Api.Controllers;

/// <summary>
/// One endpoint per product type to issue a sold product and return its calculated
/// fare with a full breakdown . The API surface grows per product
/// type on purpose; the engine underneath is data-extensible.
/// </summary>
[ApiController]
[Route("api/tickets")]
[Produces("application/json")]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketIssuer _issuer;

    public TicketsController(ITicketIssuer issuer) => _issuer = issuer;

    /// <summary>Issues a point-to-point ticket (distance-based base fare).</summary>
    [HttpPost("point-to-point")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketResponse>> IssuePointToPoint(
        [FromBody] PointToPointRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.IsValid())
            return BadRequestProblem("Origin and destination are required and must differ.");

        try
        {
            var response = await _issuer.IssuePointToPointAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (RouteNotFoundException ex)
        {
            return BadRequestProblem(ex.Message);
        }
        catch (UnknownModificationException ex)
        {
            return BadRequestProblem(ex.Message);
        }
    }

    /// <summary>Issues a daily-pass ticket (flat daily base fare).</summary>
    [HttpPost("daily-pass")]
    [ProducesResponseType(typeof(TicketResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TicketResponse>> IssueDailyPass(
        [FromBody] DailyPassRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.IsValid())
            return BadRequestProblem("A valid future travel date and zone are required.");

        try
        {
            var response = await _issuer.IssueDailyPassAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (UnknownModificationException ex)
        {
            return BadRequestProblem(ex.Message);
        }
    }

    private ObjectResult BadRequestProblem(string detail) =>
        Problem(detail: detail, statusCode: StatusCodes.Status400BadRequest, title: "Invalid request");
}