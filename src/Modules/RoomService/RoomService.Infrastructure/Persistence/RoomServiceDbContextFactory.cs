using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RoomService.Infrastructure.Persistence;

public sealed class RoomServiceDbContextFactory : IDesignTimeDbContextFactory<RoomServiceDbContext>
{
    public RoomServiceDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<RoomServiceDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=hotelos;Username=postgres;Password=1234")
            .Options;
        return new RoomServiceDbContext(opts);
    }
}
