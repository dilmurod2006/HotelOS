using MediatR;
using Microsoft.Extensions.Logging;
using Maintenance.Domain.Events;
using Reception.Application.Interfaces;

namespace Reception.Application.EventHandlers;

/// <summary>
/// Cross-module Event Handler: Maintenance → Reception.
///
/// When the Maintenance module reports a CRITICAL issue (e.g., broken elevator,
/// flooding), Reception automatically marks the affected room as UnderMaintenance,
/// making it ineligible for any new bookings until the issue is resolved.
///
/// This is the BTEC TS-05 scenario: "broken shower, urgency Critical."
/// The dashboard shows the room status change in real time via SignalR
/// because room.PlaceUnderMaintenance() raises a RoomStatusChangedEvent.
/// </summary>
public sealed class CriticalIssueReportedEventHandler
    : INotificationHandler<CriticalIssueReportedEvent>
{
    private readonly IRoomRepository _roomRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CriticalIssueReportedEventHandler> _logger;

    public CriticalIssueReportedEventHandler(
        IRoomRepository roomRepository,
        IUnitOfWork unitOfWork,
        ILogger<CriticalIssueReportedEventHandler> logger)
    {
        _roomRepository = roomRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(CriticalIssueReportedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "CRITICAL maintenance issue {IssueId} reported for room {RoomNumber}. " +
            "Placing room under maintenance.",
            notification.IssueId, notification.RoomNumber);

        var room = await _roomRepository.GetByIdAsync(notification.RoomId, cancellationToken);
        if (room is null)
        {
            _logger.LogError("Room {RoomId} not found for critical issue handler.", notification.RoomId);
            return;
        }

        room.PlaceUnderMaintenance();

        await _roomRepository.UpdateAsync(room, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Room {RoomNumber} is now UnderMaintenance. " +
            "No new bookings will be accepted until issue {IssueId} is resolved.",
            notification.RoomNumber, notification.IssueId);
    }
}
