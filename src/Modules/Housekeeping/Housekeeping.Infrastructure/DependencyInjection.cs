using Housekeeping.Application;
using Housekeeping.Application.Interfaces;
using Housekeeping.Infrastructure.Persistence;
using Housekeeping.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Housekeeping.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHousekeepingInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHousekeepingApplication();

        services.AddDbContext<HousekeepingDbContext>(opts =>
            opts.UseNpgsql(
                configuration.GetConnectionString("PostgreSQL")
                    ?? throw new InvalidOperationException(
                        "Connection string 'PostgreSQL' is not configured."),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "housekeeping")));

        services.AddScoped<ICleaningTaskRepository, CleaningTaskRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
