using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Housekeeping.Infrastructure.Persistence;

public sealed class HousekeepingDbContextFactory : IDesignTimeDbContextFactory<HousekeepingDbContext>
{
    public HousekeepingDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<HousekeepingDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=hotelos;Username=postgres;Password=1234")
            .Options;
        return new HousekeepingDbContext(opts);
    }
}
