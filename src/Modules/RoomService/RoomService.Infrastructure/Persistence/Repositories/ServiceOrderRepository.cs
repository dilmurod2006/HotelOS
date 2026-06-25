using Microsoft.EntityFrameworkCore;
using RoomService.Application.Interfaces;
using RoomService.Domain.Entities;
using RoomService.Domain.Enums;

namespace RoomService.Infrastructure.Persistence.Repositories;

internal sealed class ServiceOrderRepository : IServiceOrderRepository
{
    private readonly RoomServiceDbContext _context;

    public ServiceOrderRepository(RoomServiceDbContext context) => _context = context;

    public async Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ServiceOrders
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<ServiceOrder>> GetAllAsync(CancellationToken ct = default)
        => await _context.ServiceOrders
            .Include(o => o.LineItems)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceOrder>> GetByBookingIdAsync(
        Guid bookingId, CancellationToken ct = default)
        => await _context.ServiceOrders
            .Include(o => o.LineItems)
            .Where(o => o.BookingId == bookingId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceOrder>> GetByStatusAsync(
        OrderStatus status, CancellationToken ct = default)
        => await _context.ServiceOrders
            .Include(o => o.LineItems)
            .Where(o => o.Status == status)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(ServiceOrder order, CancellationToken ct = default)
        => await _context.ServiceOrders.AddAsync(order, ct);

    public Task UpdateAsync(ServiceOrder order, CancellationToken ct = default)
    {
        _context.ServiceOrders.Update(order);
        return Task.CompletedTask;
    }
}
