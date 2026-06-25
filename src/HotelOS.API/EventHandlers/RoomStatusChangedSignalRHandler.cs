using HotelOS.API.Hubs;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Reception.Domain.Events;

namespace HotelOS.API.EventHandlers;

/// <summary>
/// Bridges the MediatR domain event bus to SignalR WebSocket connections.
///
/// Flow:
///   Room state changes → UnitOfWork dispatches RoomStatusChangedEvent via MediatR
///   → this handler receives it → pushes "RoomStatusChanged" to ALL connected staff clients.
///
/// This handler lives in the API project (not Application) because IHubContext is an
/// ASP.NET Core concern. The Application layer stays framework-agnostic.
/// </summary>
public sealed class RoomStatusChangedSignalRHandler
    : INotificationHandler<RoomStatusChangedEvent>
{
    private readonly IHubContext<DashboardHub> _hub;

    public RoomStatusChangedSignalRHandler(IHubContext<DashboardHub> hub)
        => _hub = hub;

    public Task Handle(RoomStatusChangedEvent notification, CancellationToken cancellationToken)
        => _hub.Clients.All.SendAsync(
            "RoomStatusChanged",
            new
            {
                notification.RoomId,
                notification.RoomNumber,
                NewStatus = notification.NewStatus.ToString(),
                ChangedAtUtc = DateTime.UtcNow
            },
            cancellationToken);
}
