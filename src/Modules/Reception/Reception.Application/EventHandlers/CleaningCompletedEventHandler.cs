using MediatR;
using Microsoft.Extensions.Logging;
using Housekeeping.Domain.Events;
using Reception.Application.Interfaces;

namespace Reception.Application.EventHandlers;

/// <summary>
/// Cross-module Event Handler: Housekeeping → Reception.
///
/// BTEC Task 2.4 — Event-Driven Programming demonstration:
/// This handler is the subscriber in a publish/subscribe pattern.
/// Publisher: Housekeeping module (CleaningTask.CompleteTask())
/// Subscriber: Reception module (this class)
/// Broker:    MediatR in-process Publish/Subscribe
///
/// When a cleaner marks a room as clean, Housekeeping raises CleaningCompletedEvent.
/// MediatR routes it here WITHOUT Housekeeping knowing this handler exists.
/// Reception then marks the Room entity as Clean, updating LastCleanedAt —
/// the timestamp used by the Room Assignment Algorithm's "longest clean" sort.
/// Finally, a RoomStatusChangedEvent is raised which the SignalR hub converts
/// to a WebSocket push to all connected dashboard clients.
/// </summary>
public sealed class CleaningCompletedEventHandler
    : INotificationHandler<CleaningCompletedEvent>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CleaningCompletedEventHandler> _logger;

    public CleaningCompletedEventHandler(
        IRoomRepository roomRepository,
        IUnitOfWork unitOfWork,
        ILogger<CleaningCompletedEventHandler> logger)
    {
        _roomRepository = roomRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(CleaningCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "CleaningCompleted event received for room {RoomNumber} (RoomId: {RoomId})",
            notification.RoomNumber, notification.RoomId);

        var room = await _roomRepository.GetByIdAsync(notification.RoomId, cancellationToken);
        if (room is null)
        {
            // Non-fatal: log and continue — the room may have been decommissioned
            _logger.LogWarning(
                "Room {RoomId} from CleaningCompletedEvent not found in Reception module.",
                notification.RoomId);
            return;
        }

        // Transition: Cleaning → Clean (available for the Room Assignment Algorithm)
        // MarkClean() also records LastCleanedAt = UtcNow — critical for the
        // "longest clean" sorting criterion in the next booking assignment.
        room.MarkClean();

        await _roomRepository.UpdateAsync(room, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Room {RoomNumber} marked Clean at {CleanedAt}. " +
            "Now eligible for room assignment algorithm.",
            notification.RoomNumber, room.LastCleanedAt);
    }
}
