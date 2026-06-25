using Maintenance.Application;
using Maintenance.Application.Interfaces;
using Maintenance.Infrastructure.Persistence;
using Maintenance.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Maintenance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMaintenanceInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMaintenanceApplication();

        services.AddDbContext<MaintenanceDbContext>(opts =>
            opts.UseNpgsql(
                configuration.GetConnectionString("PostgreSQL")
                    ?? throw new InvalidOperationException(
                        "Connection string 'PostgreSQL' is not configured."),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "maintenance")));

        services.AddScoped<IMaintenanceIssueRepository, MaintenanceIssueRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
