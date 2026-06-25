using System.Reflection;
using System.Text;
using HotelOS.API.Hubs;
using Housekeeping.Application;
using Housekeeping.Infrastructure;
using Maintenance.Application;
using Maintenance.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Reception.Application;
using Reception.Infrastructure;
using RoomService.Application;
using RoomService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ── Module DI Registrations ───────────────────────────────────────────────────
// Application layer: MediatR handlers, validators, domain services
builder.Services.AddReceptionApplication();
builder.Services.AddHousekeepingApplication();
builder.Services.AddRoomServiceApplication();
builder.Services.AddMaintenanceApplication();

// Infrastructure layer: DbContext, repositories, Redis, background services
builder.Services.AddReceptionInfrastructure(builder.Configuration);
builder.Services.AddHousekeepingInfrastructure(builder.Configuration);
builder.Services.AddRoomServiceInfrastructure(builder.Configuration);
builder.Services.AddMaintenanceInfrastructure(builder.Configuration);

// ── API-Project MediatR (SignalR handler lives here) ─────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// ── Redis IDistributedCache ───────────────────────────────────────────────────
// Powers the GetAvailableRoomsQuery cache shield (30-second TTL).
// Under 1,000 concurrent users only one PostgreSQL query fires per 30-second window.
builder.Services.AddStackExchangeRedisCache(opts =>
    opts.Configuration = builder.Configuration.GetConnectionString("Redis"));

// ── SignalR ───────────────────────────────────────────────────────────────────
// ASP.NET Core 8 includes SignalR in the framework; no extra NuGet package needed.
builder.Services.AddSignalR();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]
    ?? throw new InvalidOperationException("JWT Key is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSection["Issuer"],
            ValidAudience            = jwtSection["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // SignalR WebSocket connections cannot send Authorization headers in the browser.
        // The token is passed as ?access_token=... in the query string for hub connections.
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                var path  = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) &&
                    path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── Controllers & Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "HotelOS API",
        Version = "v1",
        Description =
            "GrandStay Hotel management system — Modular Monolith, .NET 8, CQRS + DDD.\n" +
            "Roles: Guest (client endpoints), Staff/Admin (admin endpoints)."
    });

    // Allow Swagger UI to send JWT tokens in the Authorization header
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter: Bearer {your JWT token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                    { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ─────────────────────────────────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Order matters: Authentication must precede Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map the SignalR DashboardHub — staff browsers connect here via WebSocket
app.MapHub<DashboardHub>("/hubs/dashboard");

app.Run();
