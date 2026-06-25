using MediatR;
using Microsoft.Extensions.Logging;
using Housekeeping.Domain.Events;
using Reception.Application.Interfaces;

namespace Reception.Application.EventHandlers;

/// <summary>
/// Cross-module Event Handler: Housekeeping → Reception.
/// When a cleaner starts cleaning a room, Reception updates it to Cleaning status
/// so the dashboard shows the intermediate state (not just Dirty → Clean).
/// </summary>
public sealed class CleaningStartedEventHandler
    : INotificationHandler<CleaningStartedEvent>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CleaningStartedEventHandler> _logger;

    public CleaningStartedEventHandler(
        IRoomRepository roomRepository,
        IUnitOfWork unitOfWork,
        ILogger<CleaningStartedEventHandler> logger)
    {
        _roomRepository = roomRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(CleaningStartedEvent notification, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetByIdAsync(notification.RoomId, cancellationToken);
        if (room is null)
        {
            _logger.LogWarning("Room {RoomId} not found for CleaningStartedEvent.", notification.RoomId);
            return;
        }

        // Transition: Dirty → Cleaning
        room.StartCleaning();

        await _roomRepository.UpdateAsync(room, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Room {RoomNumber} is now being cleaned.", notification.RoomNumber);
    }
}
