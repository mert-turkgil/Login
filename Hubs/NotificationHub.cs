using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Login.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            await base.OnConnectedAsync();
        }
        public async Task SendNotificationToUser(string userId, string message)
        {
            // Optionally, add any validation logic here.
            await Clients.User(userId).SendAsync("ReceiveNotification", message);
        }
        public async Task SendNotificationToGroup(string groupName, string message)
        {
            await Clients.Group(groupName).SendAsync("ReceiveNotification", message);
        }
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            // Optionally, send a confirmation to the caller.
            await Clients.Caller.SendAsync("ReceiveNotification", $"Joined group: {groupName}");
        }
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            // Optionally, send a confirmation to the caller.
            await Clients.Caller.SendAsync("ReceiveNotification", $"Left group: {groupName}");
        }
        public async Task OnCallerAsync()
        {
            // Example: Log the connection or add the connection to a default group.
            await Clients.Caller.SendAsync("ReceiveNotification", "Connected to NotificationHub");
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(System.Exception? exception)
        {
            // Optionally handle disconnection, such as removing the connection from groups.
            await base.OnDisconnectedAsync(exception);
        }

                
    }
}