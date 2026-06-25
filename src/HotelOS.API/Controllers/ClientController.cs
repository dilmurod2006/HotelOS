using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reception.Application.Commands.ConfirmPayment;
using Reception.Application.Commands.CreateBooking;
using Reception.Application.Commands.RegisterGuest;
using Reception.Application.DTOs;
using Reception.Application.Queries.GetAvailableRooms;
using Reception.Domain.Enums;
using RoomService.Application.Commands.PlaceOrder;
using RoomService.Application.DTOs;

namespace HotelOS.API.Controllers;

/// <summary>
/// Guest-facing API. All endpoints require an authenticated JWT with the "Guest" role.
/// ISO/IEC 27001: role-based access control ensures guests cannot reach admin operations.
/// </summary>
[ApiController]
[Route("api/client")]
[Authorize(Roles = "Guest")]
public sealed class ClientController : ControllerBase
{
    private readonly ISender _mediator;

    public ClientController(ISender mediator) => _mediator = mediator;

        // ── Guest Registration ────────────────────────────────────────────────────

    /// <summary>Registers a new guest and returns the guest profile with ID.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GuestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterGuest(
        [FromBody] RegisterGuestCommand command,
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

    // ── Available Rooms ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns all currently available rooms.
    /// HIGH-LOAD: This endpoint is backed by a 30-second Redis cache.
    /// 1,000 concurrent requests only generate ONE PostgreSQL query per 30-second window.
    /// </summary>
    [HttpGet("rooms")]
    [ProducesResponseType(typeof(IReadOnlyList<RoomDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableRooms(
        [FromQuery] RoomType? type = null,
        [FromQuery] int? floor = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAvailableRoomsQuery(type, floor), cancellationToken);
        if (!result.IsSuccess)
        {
            var e = result.Error!;
            return BadRequest(new { e.Code, e.Description });
        }
        return Ok(result.Value);
    }

    // ── Bookings ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new room booking. Protected by a Redis distributed lock (RedLock.net)
    /// and EF Core RowVersion optimistic concurrency inside the command handler.
    /// </summary>
    [HttpPost("bookings")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBooking(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new CreateBookingCommand(
            request.GuestId,
            request.RoomType,
            request.CheckIn,
            request.CheckOut,
            request.PreferredFloor,
            request.ProximityPreference);

        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            var error = result.Error!;
            return error.Code.Contains("Conflict", StringComparison.OrdinalIgnoreCase)
                   || error.Code == "General.ConcurrencyConflict"
                ? Conflict(new { error.Code, error.Description })
                : BadRequest(new { error.Code, error.Description });
        }

        return CreatedAtAction(nameof(GetAvailableRooms), result.Value);
    }

    // ── Confirm Payment ───────────────────────────────────────────────────────

    /// <summary>Confirms payment for a booking, moving room PendingPayment → Occupied.</summary>
    [HttpPost("bookings/{bookingId:guid}/confirm-payment")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmPayment(
        Guid bookingId,
        [FromBody] ConfirmPaymentRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ConfirmPaymentCommand(bookingId, request?.CardLast4Digits ?? "****"), cancellationToken);
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
    /// Places a food/beverage order for the guest's room.
    /// The order enters the State Machine at status Received.
    /// </summary>
    [HttpPost("orders")]
    [ProducesResponseType(typeof(ServiceOrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] PlaceOrderCommand command,
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
}

// ── Request DTOs ───────────────────────────────────────────────────────────────

public sealed record ConfirmPaymentRequest(string CardLast4Digits);

public sealed record CreateBookingRequest(
    Guid GuestId,
    RoomType RoomType,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int? PreferredFloor,
    ProximityPreference ProximityPreference);
