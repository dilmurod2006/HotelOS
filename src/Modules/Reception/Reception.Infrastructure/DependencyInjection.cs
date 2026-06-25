using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;
using Reception.Application.Interfaces;
using Reception.Infrastructure.BackgroundServices;
using Reception.Infrastructure.Persistence;
using Reception.Infrastructure.Persistence.Repositories;
using Reception.Infrastructure.Redis;

namespace Reception.Infrastructure;

/// <summary>
/// Extension method that registers all Reception Infrastructure services
/// into the ASP.NET Core DI container. Called from Program.cs.
///
/// Required appsettings.json entries:
/// {
///   "ConnectionStrings": {
///     "PostgreSQL": "Host=localhost;Port=5432;Database=hotelos;Username=postgres;Password=postgres",
///     "Redis":      "localhost:6379"
///   }
/// }
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddReceptionInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── PostgreSQL via EF Core (Npgsql) ───────────────────────────────────
        // ReceptionDbContext is Scoped — one instance per HTTP request.
        // Migrations assembly is set to this infrastructure assembly so that
        // `dotnet ef migrations add` targets the right project.
        services.AddDbContext<ReceptionDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("PostgreSQL")
                    ?? throw new InvalidOperationException(
                        "Connection string 'PostgreSQL' is not configured."),
                npgsql => npgsql.MigrationsAssembly(
                    typeof(DependencyInjection).Assembly.FullName)));

        // ── Repositories (Scoped — share the request's DbContext) ─────────────
        services.AddScoped<IRoomRepository,    RoomRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IGuestRepository,   GuestRepository>();

        // ── Unit of Work (Scoped — same lifetime as DbContext) ────────────────
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ── Redis Connection (Singleton — IConnectionMultiplexer is thread-safe) ─
        // StackExchange.Redis recommends a single long-lived multiplexer for the
        // application lifetime. It manages connection pooling internally.
        var rawRedis = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        // abortConnect=false: app starts even when Redis is temporarily unavailable
        var redisConnectionString = rawRedis.Contains("abortConnect")
            ? rawRedis
            : rawRedis + ",abortConnect=false";

        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConnectionString));

        // ── RedLock.net Factory (Singleton — wraps the multiplexer) ──────────
        // RedLockFactory is disposable; ASP.NET Core DI will call Dispose on shutdown.
        // Using the existing multiplexer avoids opening a second Redis TCP connection.
        services.AddSingleton(sp =>
        {
            var connection = sp.GetRequiredService<IConnectionMultiplexer>();
            var multiplexers = new List<RedLockMultiplexer>
            {
                new(connection)
            };
            return RedLockFactory.Create(multiplexers);
        });

        // ── Distributed Lock Service (Scoped — thin wrapper, no state) ────────
        services.AddScoped<IDistributedLockService, RedisDistributedLockService>();

        // ── Redis Booking Cache (Scoped) ──────────────────────────────────────
        services.AddScoped<IRedisBookingCache, RedisBookingCacheService>();

        // ── Payment Timeout BackgroundService (Singleton — long-lived listener) ─
        // Registered as a Hosted Service so ASP.NET Core starts it on application
        // startup and stops it gracefully on shutdown (sends CancellationToken).
        services.AddHostedService<PaymentTimeoutBackgroundService>();

        return services;
    }
}
