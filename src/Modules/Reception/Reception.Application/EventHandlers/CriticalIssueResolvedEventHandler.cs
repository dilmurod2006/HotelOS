using MediatR;
using Microsoft.Extensions.Logging;
using Maintenance.Domain.Events;
using Reception.Application.Interfaces;

namespace Reception.Application.EventHandlers;

/// <summary>
/// Cross-module Event Handler: Maintenance → Reception.
/// When a critical issue is resolved, the room returns to Available service.
/// </summary>
public sealed class CriticalIssueResolvedEventHandler
    : INotificationHandler<CriticalIssueResolvedEvent>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CriticalIssueResolvedEventHandler> _logger;

    public CriticalIssueResolvedEventHandler(
        IRoomRepository roomRepository,
        IUnitOfWork unitOfWork,
        ILogger<CriticalIssueResolvedEventHandler> logger)
    {
        _roomRepository = roomRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(CriticalIssueResolvedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Critical issue {IssueId} resolved. Returning room {RoomNumber} to service.",
            notification.IssueId, notification.RoomNumber);

        var room = await _roomRepository.GetByIdAsync(notification.RoomId, cancellationToken);
        if (room is null)
        {
            _logger.LogError("Room {RoomId} not found for issue resolution handler.", notification.RoomId);
            return;
        }

        room.ReturnToService();

        await _roomRepository.UpdateAsync(room, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Room {RoomNumber} is now Available for new bookings.", notification.RoomNumber);
    }
}
