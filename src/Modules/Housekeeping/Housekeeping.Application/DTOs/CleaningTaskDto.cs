using Housekeeping.Domain.Enums;

namespace Housekeeping.Application.DTOs;

public sealed record CleaningTaskDto(
    Guid Id,
    Guid RoomId,
    string RoomNumber,
    int Floor,
    CleaningTaskStatus Status,
    CleaningPriority Priority,
    Guid? AssignedCleanerId,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    DateTime CreatedAt);
