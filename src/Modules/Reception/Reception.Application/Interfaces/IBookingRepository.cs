using Reception.Domain.Entities;

namespace Reception.Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Booking>> GetActiveBookingsForRoomAsync(Guid roomId, CancellationToken ct = default);
    Task<Booking?> GetActiveBookingForRoomAsync(Guid roomId, CancellationToken ct = default);
    Task AddAsync(Booking booking, CancellationToken ct = default);
    Task UpdateAsync(Booking booking, CancellationToken ct = default);
}
