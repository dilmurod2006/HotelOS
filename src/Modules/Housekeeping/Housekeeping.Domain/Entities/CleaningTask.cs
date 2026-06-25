using HotelOS.SharedKernel.Abstractions;
using Housekeeping.Domain.Enums;
using Housekeeping.Domain.Events;

namespace Housekeeping.Domain.Entities;

/// <summary>
/// Aggregate Root: CleaningTask.
///
/// Created automatically when a RoomVacatedEvent is received from the Reception module.
/// Demonstrates event-driven programming: the task does not exist until an event triggers it.
/// The state transitions (Queued → InProgress → Completed) illustrate procedural programming:
/// a strict sequential set of steps enforced by the domain entity.
/// </summary>
public sealed class CleaningTask : AggregateRoot<Guid>
{
    /// <summary>
    /// RoomId references Reception.Room without a foreign key dependency.
    /// Modules store IDs of entities from other modules — they never share DbContexts.
    /// This is the Modular Monolith's isolation guarantee.
    /// </summary>
    public Guid RoomId { get; private set; }
    public string RoomNumber { get; private set; } = null!;
    public int Floor { get; private set; }
    public CleaningTaskStatus Status { get; private set; }
    public CleaningPriority Priority { get; private set; }
    public Guid? AssignedCleanerId { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CleaningTask() { }

    public static CleaningTask Create(
        Guid roomId,
        string roomNumber,
        int floor,
        CleaningPriority priority = CleaningPriority.Normal)
    {
        return new CleaningTask
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            RoomNumber = roomNumber,
            Floor = floor,
            Status = CleaningTaskStatus.Queued,
            Priority = priority,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Transition: Queued → InProgress. Assigns a cleaner and starts the clock.</summary>
    public void StartCleaning(Guid cleanerId)
    {
        if (Status != CleaningTaskStatus.Queued)
            throw new InvalidOperationException($"Task {Id} is not queued. Current status: {Status}.");

        AssignedCleanerId = cleanerId;
        Status = CleaningTaskStatus.InProgress;
        StartedAt = DateTime.UtcNow;

        RaiseDomainEvent(new CleaningStartedEvent(Id, RoomId, RoomNumber));
    }

    /// <summary>
    /// Transition: InProgress → Completed.
    /// This event triggers the Reception module (via MediatR) to mark the room as Clean.
    /// </summary>
    public void CompleteTask()
    {
        if (Status != CleaningTaskStatus.InProgress)
            throw new InvalidOperationException($"Task {Id} is not in progress. Current status: {Status}.");

        Status = CleaningTaskStatus.Completed;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new CleaningCompletedEvent(Id, RoomId, RoomNumber));
    }
}
