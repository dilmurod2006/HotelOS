using Reception.Domain.Entities;

namespace Reception.Application.Interfaces;

public interface IGuestRepository
{
    Task<Guest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guest?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(Guest guest, CancellationToken ct = default);
    Task UpdateAsync(Guest guest, CancellationToken ct = default);
}
