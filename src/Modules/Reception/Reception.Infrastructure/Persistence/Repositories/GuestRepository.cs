using Microsoft.EntityFrameworkCore;
using Reception.Application.Interfaces;
using Reception.Domain.Entities;

namespace Reception.Infrastructure.Persistence.Repositories;

internal sealed class GuestRepository : IGuestRepository
{
    private readonly ReceptionDbContext _context;

    public GuestRepository(ReceptionDbContext context) => _context = context;

    public async Task<Guest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Guests.FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<Guest?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        // Normalise to lowercase before querying — Guest.Create() stores emails lowercased
        var normalised = email.Trim().ToLowerInvariant();
        return await _context.Guests.FirstOrDefaultAsync(g => g.Email == normalised, ct);
    }

    public Task AddAsync(Guest guest, CancellationToken ct = default)
    {
        _context.Guests.Add(guest);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Guest guest, CancellationToken ct = default)
    {
        _context.Guests.Update(guest);
        return Task.CompletedTask;
    }
}
