using Microsoft.EntityFrameworkCore;
using Reception.Application.Interfaces;
using Reception.Domain.Entities;
using Reception.Domain.Enums;

namespace Reception.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of IRoomRepository.
///
/// The repository does NOT sort by LastCleanedAt — the Application-layer
/// RoomAssignmentService does the final in-memory sort (using the null-handling
/// logic: null → DateTime.MaxValue → sorts last). Sorting in the repository would
/// duplicate logic and make it harder to test in isolation.
///
/// GetByRoomNumberAsync parses the room number string into floor+unit components
/// because RoomNumber.Value is computed (not stored) in the DB — only Floor and Unit
/// columns exist (see RoomConfiguration).
/// </summary>
internal sealed class RoomRepository : IRoomRepository
{
    private readonly ReceptionDbContext _context;

    public RoomRepository(ReceptionDbContext context) => _context = context;

    public async Task<Room?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Room?> GetByRoomNumberAsync(string roomNumber, CancellationToken ct = default)
    {
        // Room numbers follow the format "{floor}{unit:D2}" (always 3 digits):
        // "105" → floor = 1, unit = 05 = 5
        // "320" → floor = 3, unit = 20
        if (roomNumber.Length != 3) return null;
        if (!int.TryParse(roomNumber[..1], out var floor)) return null;
        if (!int.TryParse(roomNumber[1..], out var unit)) return null;

        return await _context.Rooms
            .FirstOrDefaultAsync(
                r => r.RoomNumber.Floor == floor && r.RoomNumber.Unit == unit, ct);
    }

    public async Task<IReadOnlyList<Room>> GetAvailableRoomsByTypeAsync(
        RoomType type, CancellationToken ct = default)
    {
        // Returns all bookable rooms of the requested type.
        // The Application-layer RoomAssignmentService performs the multi-step filtering
        // and sorting (type-match → status → longest-clean → floor → proximity).
        return await _context.Rooms
            .Where(r => r.Type == type
                     && (r.Status == RoomStatus.Available || r.Status == RoomStatus.Clean))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Room>> GetAllAsync(CancellationToken ct = default)
        => await _context.Rooms
            .OrderBy(r => r.Floor)
            .ThenBy(r => r.RoomNumber.Unit)
            .ToListAsync(ct);

    public Task AddAsync(Room room, CancellationToken ct = default)
    {
        _context.Rooms.Add(room);
        return Task.CompletedTask; // Change is tracked; commit happens in UnitOfWork
    }

    public Task UpdateAsync(Room room, CancellationToken ct = default)
    {
        _context.Rooms.Update(room);
        return Task.CompletedTask;
    }
}
