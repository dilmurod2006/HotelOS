using HotelOS.SharedKernel.Common;
using Maintenance.Application.Commands.ReportIssue;
using Maintenance.Application.Commands.ResolveIssue;
using Maintenance.Application.DTOs;
using Maintenance.Application.Queries.GetPriorityQueue;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reception.Application.Commands.CheckOut;
using Reception.Application.DTOs;
using RoomService.Application.Commands.AdvanceOrderStatus;
using RoomService.Application.DTOs;
using RoomService.Application.Queries.GetOrders;
using RoomService.Domain.Enums;

namespace HotelOS.API.Controllers;

/// <summary>
/// Staff/Admin-facing API. All endpoints require JWT with "Staff" or "Admin" role.
/// ISO/IEC 27001: separation of duties — guests cannot reach these operations.
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Staff,Admin")]
public sealed class AdminController : ControllerBase
{
    private readonly ISender _mediator;

    public AdminController(ISender mediator) => _mediator = mediator;

    // ── Maintenance Priority Queue ────────────────────────────────────────────

    /// <summary>
    /// Returns the maintenance priority queue.
    /// Algorithm: Critical → High → Normal → Low (urgency ASC), then CreatedAt ASC (FIFO).
    /// PriorityRank 1 = next technician assignment.
    /// </summary>
    [HttpGet("maintenance/queue")]
    [ProducesResponseType(typeof(IReadOnlyList<MaintenanceIssueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaintenanceQueue(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPriorityQueueQuery(), cancellationToken);
        if (!result.IsSuccess)
        {
            var e = result.Error!;
            return StatusCode(500, new { e.Code, e.Description });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Reports a new maintenance issue for a room.
    /// Critical issues automatically trigger RoomStatusChanged → room placed under maintenance.
    /// </summary>
    [HttpPost("maintenance/issues")]
    [ProducesResponseType(typeof(MaintenanceIssueDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReportIssue(
        [FromBody] ReportIssueCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            var e = result.Error!;
            return BadRequest(new { e.Code, e.Description });
        }
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>
    /// Resolves a maintenance issue. For Critical issues this triggers
    /// CriticalIssueResolvedEvent → room returned to Available.
    /// </summary>
    [HttpPut("maintenance/issues/{issueId:guid}/resolve")]
    [ProducesResponseType(typeof(MaintenanceIssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResolveIssue(
        Guid issueId,
        [FromBody] ResolveIssueRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ResolveIssueCommand(issueId, request.ResolutionNotes), cancellationToken);

        if (!result.IsSuccess)
        {
            var e = result.Error!;
            return e.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { e.Code, e.Description })
                : BadRequest(new { e.Code, e.Description });
        }
        return Ok(result.Value);
    }

    // ── Room Service Orders ───────────────────────────────────────────────────

    /// <summary>
    /// Returns all room-service orders, optionally filtered by status.
    /// Status values: 1=Received, 2=Preparing, 3=Delivering, 4=Delivered
    /// </summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] OrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetOrdersQuery(status), cancellationToken);
        if (!result.IsSuccess)
        {
            var e = result.Error!;
            return StatusCode(500, new { e.Code, e.Description });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Advances a room-service order through the State Machine:
    ///   Received → Preparing → Delivering → Delivered
    /// Each call advances exactly one step. Invalid transitions return 400.
    /// </summary>
    [HttpPut("orders/{orderId:guid}/advance")]
    [ProducesResponseType(typeof(ServiceOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdvanceOrderStatus(
        Guid orderId, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new AdvanceOrderStatusCommand(orderId), cancellationToken);
        if (!result.IsSuccess)
        {
            var e = result.Error!;
            return e.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase)
                ? NotFound(new { e.Code, e.Description })
                : BadRequest(new { e.Code, e.Description });
        }
        return Ok(result.Value);
    }

    // ── Check-Out ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Processes guest check-out: computes the final bill, releases the room,
    /// and notifies Housekeeping to begin cleaning.
    /// </summary>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(BillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckOut(
        [FromBody] CheckOutRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new CheckOutCommand(request.BookingId, request.ApplyLateCheckoutFee),
            cancellationToken);

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

// ── Request DTOs ───────────────────────────────────────────────────────────────

public sealed record ResolveIssueRequest(string ResolutionNotes);
public sealed record CheckOutRequest(Guid BookingId, bool ApplyLateCheckoutFee = false);
