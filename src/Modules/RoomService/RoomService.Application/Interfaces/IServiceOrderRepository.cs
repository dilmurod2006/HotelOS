using RoomService.Domain.Entities;
using RoomService.Domain.Enums;

namespace RoomService.Application.Interfaces;

public interface IServiceOrderRepository
{
    Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceOrder>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ServiceOrder>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceOrder>> GetByStatusAsync(OrderStatus status, CancellationToken ct = default);
    Task AddAsync(ServiceOrder order, CancellationToken ct = default);
    Task UpdateAsync(ServiceOrder order, CancellationToken ct = default);
}
