using Reception.Domain.Enums;

namespace Reception.Application.DTOs;

/// <summary>
/// Data Transfer Object for Room data sent to the API layer and eventually
/// to the SignalR dashboard. Deliberately omits internal fields like RowVersion —
/// ISO/IEC 27001: expose only what consumers need, nothing more.
/// </summary>
public sealed record RoomDto(
    Guid Id,
    string RoomNumber,
    int Floor,
    RoomType Type,
    RoomStatus Status,
    decimal NightlyRate,
    string Currency,
    bool IsNearElevator,
    bool IsNearStaircase,
    DateTime? LastCleanedAt);
