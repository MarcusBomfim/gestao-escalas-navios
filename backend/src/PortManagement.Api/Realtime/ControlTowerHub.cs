using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PortManagement.Api.Realtime;

[Authorize]
public sealed class ControlTowerHub : Hub;

public static class RealtimeEvents
{
    public const string ControlTowerInvalidated = nameof(ControlTowerInvalidated);
}
