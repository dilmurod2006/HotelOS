using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace RoomService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRoomServiceApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
