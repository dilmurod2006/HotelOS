using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RoomService.Application;
using RoomService.Application.Interfaces;
using RoomService.Infrastructure.Persistence;
using RoomService.Infrastructure.Persistence.Repositories;

namespace RoomService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddRoomServiceInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRoomServiceApplication();

        services.AddDbContext<RoomServiceDbContext>(opts =>
            opts.UseNpgsql(
                configuration.GetConnectionString("PostgreSQL")
                    ?? throw new InvalidOperationException(
                        "Connection string 'PostgreSQL' is not configured."),
                b => b.MigrationsHistoryTable("__EFMigrationsHistory", "roomservice")));

        services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
