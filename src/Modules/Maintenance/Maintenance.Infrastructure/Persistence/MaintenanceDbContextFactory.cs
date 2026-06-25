using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maintenance.Infrastructure.Persistence;

public sealed class MaintenanceDbContextFactory : IDesignTimeDbContextFactory<MaintenanceDbContext>
{
    public MaintenanceDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<MaintenanceDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=hotelos;Username=postgres;Password=1234")
            .Options;
        return new MaintenanceDbContext(opts);
    }
}
