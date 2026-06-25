using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Housekeeping.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddHousekeepingApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        return services;
    }
}
