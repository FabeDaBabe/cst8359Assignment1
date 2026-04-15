using Microsoft.AspNetCore.SignalR;

namespace Assignment_1.Hubs
{
    public class EventHub : Hub
    {
        public async Task JoinEventGroup(string eventId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"event-{eventId}");
        }
    }
}