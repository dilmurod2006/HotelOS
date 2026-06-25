using Housekeeping.Application.Commands.CompleteCleaning;
using Housekeeping.Application.Commands.StartCleaning;
using Housekeeping.Application.DTOs;
using Housekeeping.Application.Queries.GetCleaningTasks;
using Housekeeping.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelOS.API.Controllers;

[ApiController]
[Route("api/housekeeping")]
[Authorize(Roles = "Staff,Admin")]
public sealed class HousekeepingController : ControllerBase
{
    private readonly ISender _mediator;
    public HousekeepingController(ISender mediator) => _mediator = mediator;

    /// <summary>Returns all cleaning tasks, optionally filtered by status.</summary>
    [HttpGet("tasks")]
    [ProducesResponseType(typeof(IReadOnlyList<CleaningTaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTasks(
        [FromQuery] CleaningTaskStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetCleaningTasksQuery(status), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Cleaner starts a queued task.
    /// Room transitions: nothing yet — fires CleaningStartedEvent → Reception marks room Cleaning.
    /// </summary>
    [HttpPut("tasks/{taskId:guid}/start")]
    [ProducesResponseType(typeof(CleaningTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartCleaning(
        Guid taskId,
        [FromBody] StartCleaningRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new StartCleaningCommand(taskId, request.CleanerId), cancellationToken);

        if (!result.IsSuccess)
        {
            var e = result.Error!;
            return e.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { e.Code, e.Description })
                : BadRequest(new { e.Code, e.Description });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Cleaner completes a task.
    /// Fires CleaningCompletedEvent → Reception marks room Clean → room re-enters availability pool.
    /// </summary>
    [HttpPut("tasks/{taskId:guid}/complete")]
    [ProducesResponseType(typeof(CleaningTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompleteCleaning(
        Guid taskId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new CompleteCleaningCommand(taskId), cancellationToken);

        if (!result.IsSuccess)
        {
            var e = result.Error!;
            return e.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { e.Code, e.Description })
                : BadRequest(new { e.Code, e.Description });
        }
        return Ok(result.Value);
    }
}

public sealed record StartCleaningRequest(Guid CleanerId);
