using Microsoft.EntityFrameworkCore;
using Reception.Application.Interfaces;
using Reception.Domain.Entities;
using Reception.Domain.Enums;

namespace Reception.Infrastructure.Persistence.Repositories;

internal sealed class BookingRepository : IBookingRepository
{
    private readonly ReceptionDbContext _context;

    public BookingRepository(ReceptionDbContext context) => _context = context;

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<Booking>> GetActiveBookingsForRoomAsync(
        Guid roomId, CancellationToken ct = default)
        => await _context.Bookings
            .Where(b => b.RoomId == roomId
                     && b.Status != BookingStatus.CheckedOut
                     && b.Status != BookingStatus.Cancelled)
            .ToListAsync(ct);

    public async Task<Booking?> GetActiveBookingForRoomAsync(
        Guid roomId, CancellationToken ct = default)
        => await _context.Bookings
            .FirstOrDefaultAsync(b => b.RoomId == roomId
                && (b.Status == BookingStatus.Pending
                 || b.Status == BookingStatus.Confirmed
                 || b.Status == BookingStatus.CheckedIn), ct);

    public Task AddAsync(Booking booking, CancellationToken ct = default)
    {
        _context.Bookings.Add(booking);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        _context.Bookings.Update(booking);
        return Task.CompletedTask;
    }
}
