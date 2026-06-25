using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HotelOS.API.Hubs;

/// <summary>
/// Real-time dashboard hub for hotel staff.
///
/// Staff browsers connect to wss://.../hubs/dashboard and receive instant push
/// notifications whenever a room's status changes — no polling required.
///
/// JWT note: SignalR WebSocket connections cannot send Authorization headers in
/// the browser. The token must be passed as ?access_token=... in the query string;
/// Program.cs configures JwtBearer to read it from there for hub requests.
/// </summary>
[Authorize(Roles = "Staff,Admin")]
public sealed class DashboardHub : Hub
{
    // No server-callable client methods for now — push-only from the server side.
    // Clients subscribe to "RoomStatusChanged" events pushed by RoomStatusChangedSignalRHandler.
}
