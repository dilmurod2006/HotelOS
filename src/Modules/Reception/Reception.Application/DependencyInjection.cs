using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Reception.Application.Behaviours;
using Reception.Application.Services;

namespace Reception.Application;

/// <summary>
/// Extension method that registers all Reception Application layer services
/// into the ASP.NET Core DI container. Called from Program.cs.
///
/// This single entry point keeps the API project ignorant of the Application
/// layer's internal structure — it calls AddReceptionApplication() and everything
/// is wired up. This is the Facade pattern applied to dependency injection.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddReceptionApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // Register all MediatR handlers, event handlers, and pipeline behaviours
        // from this assembly using assembly scanning — no manual registration needed.
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline: ValidationBehaviour runs BEFORE every handler.
            // Order matters: validate first, then handle.
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
        });

        // Register all FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(assembly);

        // Register the Room Assignment Algorithm as a scoped service
        // (scoped = one instance per HTTP request, shares the EF Core DbContext scope)
        services.AddScoped<RoomAssignmentService>();

        return services;
    }
}
