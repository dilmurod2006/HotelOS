using Housekeeping.Application.Interfaces;
using Housekeeping.Domain.Entities;
using Housekeeping.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using Reception.Domain.Events;

namespace Housekeeping.Application.EventHandlers;

/// <summary>
/// Cross-module Event Handler: Reception → Housekeeping.
///
/// When a guest checks out, Reception raises RoomVacatedEvent.
/// MediatR routes it here — Housekeeping creates a CleaningTask automatically.
/// Reception has zero knowledge of this handler; modules communicate only via events.
///
/// Priority is Normal by default. Future enhancement: map floor/type to priority.
/// </summary>
public sealed class RoomVacatedEventHandler : INotificationHandler<RoomVacatedEvent>
{
    private readonly ICleaningTaskRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RoomVacatedEventHandler> _logger;

    public RoomVacatedEventHandler(
        ICleaningTaskRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<RoomVacatedEventHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(RoomVacatedEvent notification, CancellationToken cancellationToken)
    {
        var task = CleaningTask.Create(
            notification.RoomId,
            notification.RoomNumber,
            notification.Floor,
            CleaningPriority.Normal);

        await _repository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "CleaningTask {TaskId} created for room {RoomNumber} (floor {Floor}).",
            task.Id, task.RoomNumber, task.Floor);
    }
}
