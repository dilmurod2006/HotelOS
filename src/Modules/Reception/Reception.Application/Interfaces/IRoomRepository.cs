using Reception.Domain.Entities;
using Reception.Domain.Enums;

namespace Reception.Application.Interfaces;

/// <summary>
/// Repository interface for Room persistence.
/// Defined in the Application layer so Domain stays free of infrastructure concerns.
/// Infrastructure implements this via EF Core — Application only depends on the abstraction.
/// </summary>
public interface IRoomRepository
{
    Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Room?> GetByRoomNumberAsync(string roomNumber, CancellationToken ct = default);

    /// <summary>
    /// Returns all Clean rooms matching the given type, ordered by LastCleanedAt ascending
    /// (oldest-clean first) for the assignment algorithm's rotation criterion.
    /// </summary>
    Task<IReadOnlyList<Room>> GetAvailableRoomsByTypeAsync(RoomType type, CancellationToken ct = default);

    Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(Room room, CancellationToken ct = default);
    Task UpdateAsync(Room room, CancellationToken ct = default);
}
