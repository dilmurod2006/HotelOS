namespace Housekeeping.Domain.Enums;

/// <summary>
/// State machine for a cleaning task.
/// Each transition is valid only in the specified order — enforced by CleaningTask entity methods.
/// Queued → InProgress → Completed
/// </summary>
public enum CleaningTaskStatus
{
    Queued = 1,       // Task created, waiting for an available cleaner
    InProgress = 2,   // Assigned cleaner has started cleaning
    Completed = 3     // Room is now Clean and available for assignment
}
