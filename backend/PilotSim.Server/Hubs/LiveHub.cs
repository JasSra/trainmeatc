using Microsoft.AspNetCore.SignalR;

namespace PilotSim.Server.Hubs;

public class LiveHub : Hub
{
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session-{sessionId}");
    }

    // Client methods that can be called:
    // - partialTranscript
    // - instructorVerdict
    // - atcTransmission
    // - ttsReady
    // - scoreTick
}