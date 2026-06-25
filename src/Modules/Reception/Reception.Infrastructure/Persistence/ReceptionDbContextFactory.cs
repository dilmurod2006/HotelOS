using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reception.Infrastructure.Persistence;

public sealed class ReceptionDbContextFactory : IDesignTimeDbContextFactory<ReceptionDbContext>
{
    public ReceptionDbContext CreateDbContext(string[] args)
    {
        var opts = new DbContextOptionsBuilder<ReceptionDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=hotelos;Username=postgres;Password=1234")
            .Options;
        return new ReceptionDbContext(opts);
    }
}
