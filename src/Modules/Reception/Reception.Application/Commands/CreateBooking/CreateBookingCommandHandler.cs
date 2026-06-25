using MediatR;
using Microsoft.Extensions.Logging;
using HotelOS.SharedKernel.Common;
using HotelOS.SharedKernel.Exceptions;
using Reception.Application.DTOs;
using Reception.Application.Interfaces;
using Reception.Application.Services;
using Reception.Domain.Entities;
using Reception.Domain.ValueObjects;

namespace Reception.Application.Commands.CreateBooking;

/// <summary>
/// MediatR Handler: CreateBookingCommandHandler.
///
/// ══════════════════════════════════════════════════════════════════════════════
/// TWO-PHASE DOUBLE-BOOKING SHIELD (BTEC Step 2 core requirement)
/// ══════════════════════════════════════════════════════════════════════════════
///
/// PHASE 1 — Redis Distributed Lock (RedLock.net):
///   Before any database write, we acquire an exclusive lock keyed to the target
///   room's ID. If two requests race for the same room, only one acquires the lock;
///   the other gets an immediate failure response without touching the database.
///   This eliminates contention before it reaches the DB layer.
///
///   Lock key format:  "hotelos:room-lock:{roomId}"
///   Lock expiry:       30 seconds (far longer than any DB write needs, ensures
///                      the lock is always eventually released even if the server
///                      crashes mid-handler).
///
/// PHASE 2 — EF Core Optimistic Concurrency (RowVersion fallback):
///   Even with a Redis lock, network partitions or Redis node failures can cause
///   two writes to reach PostgreSQL simultaneously. EF Core's RowVersion column
///   on Room means the second UPDATE fails with DbUpdateConcurrencyException if
///   another process already changed the row. We catch this and return a clean
///   failure result — never a 500 error.
///
/// ══════════════════════════════════════════════════════════════════════════════
/// PAYMENT WINDOW (15-minute TTL):
/// ══════════════════════════════════════════════════════════════════════════════
///   After the room is reserved (status → PendingPayment), a Redis key is set
///   with a 15-minute TTL. The BackgroundService (Step 3) monitors Redis keyspace
///   expiry events. When the TTL fires, it publishes PaymentTimedOutEvent via
///   MediatR, which reverts the room to Available and pushes a SignalR update
///   to the dashboard — no manual cleanup required.
/// </summary>
public sealed class CreateBookingCommandHandler
    : IRequestHandler<CreateBookingCommand, Result<BookingDto>>
{
    // ── Dependencies injected by DI container ────────────────────────────────
    private readonly IRoomRepository _roomRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IGuestRepository _guestRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDistributedLockService _lockService;
    private readonly IRedisBookingCache _redisCache;
    private readonly RoomAssignmentService _assignmentService;
    private readonly ILogger<CreateBookingCommandHandler> _logger;

    // Lock configuration constants — extracted to named constants (coding standard: no magic numbers)
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PaymentWindowTtl = TimeSpan.FromMinutes(15);

    public CreateBookingCommandHandler(
        IRoomRepository roomRepository,
        IBookingRepository bookingRepository,
        IGuestRepository guestRepository,
        IUnitOfWork unitOfWork,
        IDistributedLockService lockService,
        IRedisBookingCache redisCache,
        RoomAssignmentService assignmentService,
        ILogger<CreateBookingCommandHandler> logger)
    {
        _roomRepository = roomRepository;
        _bookingRepository = bookingRepository;
        _guestRepository = guestRepository;
        _unitOfWork = unitOfWork;
        _lockService = lockService;
        _redisCache = redisCache;
        _assignmentService = assignmentService;
        _logger = logger;
    }

    public async Task<Result<BookingDto>> Handle(
        CreateBookingCommand command,
        CancellationToken cancellationToken)
    {
        // ── PRE-FLIGHT CHECK 1: Guest must exist ─────────────────────────────
        var guest = await _guestRepository.GetByIdAsync(command.GuestId, cancellationToken);
        if (guest is null)
        {
            _logger.LogWarning("Booking attempt for non-existent guest {GuestId}", command.GuestId);
            return Result<BookingDto>.Failure(
                new Error("Booking.GuestNotFound", $"Guest {command.GuestId} was not found."));
        }

        // ── ROOM ASSIGNMENT ALGORITHM ─────────────────────────────────────────
        // Run the five-filter algorithm (type → status → longest-clean → floor → proximity)
        // to find the optimal candidate room BEFORE acquiring any lock.
        // We do this outside the lock to keep the lock window as short as possible.
        var stayPeriod = DateRange.Create(command.CheckIn, command.CheckOut);
        var candidateRoom = await _assignmentService.FindBestRoomAsync(
            command.RequestedRoomType,
            command.PreferredFloor,
            command.ProximityPreference,
            cancellationToken);

        if (candidateRoom is null)
        {
            _logger.LogInformation(
                "No available {RoomType} rooms for {CheckIn}–{CheckOut}",
                command.RequestedRoomType, command.CheckIn, command.CheckOut);

            // TS-07: Return clear "no rooms available" result — never crash
            return Result<BookingDto>.Failure(
                new Error("Booking.NoRoomsAvailable",
                    $"No {command.RequestedRoomType} rooms are currently available. " +
                    "Please try a different room type or check back later."));
        }

        // ── PHASE 1: ACQUIRE REDIS DISTRIBUTED LOCK ──────────────────────────
        // Lock key is scoped to this specific room so parallel requests for
        // DIFFERENT rooms do not block each other — only same-room contention is serialised.
        var lockKey = $"hotelos:room-lock:{candidateRoom.Id}";

        await using var distributedLock = await _lockService.TryAcquireAsync(
            lockKey,
            LockExpiry,
            cancellationToken);

        if (distributedLock is null || !distributedLock.IsAcquired)
        {
            // Another request is currently booking this exact room.
            // Return a retryable failure — the client should immediately retry
            // which will trigger the algorithm again and may find a different room.
            _logger.LogWarning(
                "Could not acquire lock for room {RoomId} — concurrent booking in progress",
                candidateRoom.Id);

            return Result<BookingDto>.Failure(
                new Error("Booking.RoomTemporarilyUnavailable",
                    "This room is currently being processed by another request. Please try again."));
        }

        // ── INSIDE THE LOCK ───────────────────────────────────────────────────
        // The lock is held. Now re-read the room from the database to get the
        // LATEST state (the candidate may have been booked between our algorithm
        // call above and now — the lock prevents it, but we re-read for the
        // fresh RowVersion token needed by EF Core's optimistic concurrency check).
        var room = await _roomRepository.GetByIdAsync(candidateRoom.Id, cancellationToken);

        if (room is null)
        {
            return Result<BookingDto>.Failure(
                new Error("Booking.RoomNotFound", "The selected room no longer exists."));
        }

        // Double-check status inside the lock — in case the algorithm found it
        // available but it was booked in the microseconds before we locked
        if (room.Status is not (Reception.Domain.Enums.RoomStatus.Available
                             or Reception.Domain.Enums.RoomStatus.Clean))
        {
            return Result<BookingDto>.Failure(
                new Error("Booking.RoomNoLongerAvailable",
                    "The selected room was just reserved by another guest. Please try again."));
        }

        try
        {
            // ── DOMAIN STATE CHANGE ───────────────────────────────────────────
            // room.HoldForPayment() enforces the state transition and raises the
            // RoomStatusChangedEvent internally. Events are collected by AggregateRoot
            // and dispatched after SaveChangesAsync succeeds.
            room.HoldForPayment();

            // ── CREATE BOOKING ENTITY ─────────────────────────────────────────
            var booking = Booking.Create(
                guestId: guest.Id,
                roomId: room.Id,
                stayPeriod: stayPeriod,
                nightlyRate: room.NightlyRate,
                requestedRoomType: command.RequestedRoomType,
                preferredFloor: command.PreferredFloor,
                proximityPreference: command.ProximityPreference);

            await _bookingRepository.AddAsync(booking, cancellationToken);
            await _roomRepository.UpdateAsync(room, cancellationToken);

            // ── PHASE 2: SAVE WITH OPTIMISTIC CONCURRENCY ────────────────────
            // SaveChangesAsync sends:
            //   UPDATE "Rooms" SET "Status" = 'PendingPayment', "RowVersion" = @newVersion
            //   WHERE "Id" = @roomId AND "RowVersion" = @expectedVersion
            //
            // If another process already changed the row (e.g., Redis failure let
            // two requests through), PostgreSQL returns 0 rows affected, and EF Core
            // throws DbUpdateConcurrencyException — caught in the block below.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ── SET PAYMENT WINDOW TTL IN REDIS ──────────────────────────────
            // Do this AFTER the database commit so we only start the countdown
            // if the booking was actually persisted successfully.
            // The BackgroundService (Step 3) will auto-release the room if
            // this key expires before ConfirmPayment is called.
            await _redisCache.SetPaymentWindowAsync(
                bookingId: booking.Id,
                roomId: room.Id,
                ttl: PaymentWindowTtl,
                ct: cancellationToken);

            _logger.LogInformation(
                "Booking {BookingId} created for guest {GuestId} in room {RoomNumber}. " +
                "Payment window: {Minutes} minutes.",
                booking.Id, guest.Id, room.RoomNumber, PaymentWindowTtl.TotalMinutes);

            return Result<BookingDto>.Success(MapToDto(booking, guest, room));
        }
        catch (ConcurrencyConflictException ex)
        {
            // ── PHASE 2 CAUGHT: OPTIMISTIC CONCURRENCY FAILURE ───────────────
            // The Infrastructure UoW re-throws DbUpdateConcurrencyException as
            // ConcurrencyConflictException so this Application layer stays clean
            // of any EF Core assembly reference. The RowVersion mismatch means
            // another process (possibly via a Redis node failure edge case) already
            // modified this room row. We log and return a clean failure — the client
            // can retry immediately with a fresh room selection.
            _logger.LogWarning(ex,
                "Optimistic concurrency conflict booking room {RoomId} — " +
                "row was modified by a concurrent request.",
                room.Id);

            return Result<BookingDto>.Failure(Error.ConcurrencyConflict);
        }
        // The lock is released here automatically via `await using` (IAsyncDisposable)
    }

    // ── Private mapper ────────────────────────────────────────────────────────
    private static BookingDto MapToDto(Booking booking, Domain.Entities.Guest guest, Domain.Entities.Room room)
        => new(
            Id: booking.Id,
            GuestId: guest.Id,
            GuestFullName: guest.Name.FullName,
            RoomId: room.Id,
            RoomNumber: room.RoomNumber.Value,
            Floor: room.Floor,
            RoomType: room.Type,
            Status: booking.Status,
            CheckIn: booking.StayPeriod.CheckIn,
            CheckOut: booking.StayPeriod.CheckOut,
            Nights: booking.StayPeriod.NightCount,
            RoomCharge: booking.RoomCharge.Amount,
            RoomServiceCharges: booking.RoomServiceCharges.Amount,
            ExtraFees: booking.ExtraFees.Amount,
            TotalBill: booking.TotalBill?.Amount,
            Currency: booking.RoomCharge.Currency,
            CreatedAt: booking.CreatedAt);
}
