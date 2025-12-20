using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace NodPT.API.Hubs;

[Authorize]
public class NodptHub : Hub
{
    private const string MasterGroup = "Master";
    private readonly ILogger<NodptHub> _logger;

    public NodptHub(ILogger<NodptHub> logger)
    {
        _logger = logger;
        _logger.LogInformation($"signalR started");
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var clientType = Context.User?.FindFirst("client_type")?.Value;

        _logger.LogInformation($"Client connected: {Context.ConnectionId}, UserId: {userId}, ClientType: {clientType}");

        // Automatically add user to their user-specific group for routing
        if (!string.IsNullOrEmpty(userId))
        {
            var userGroup = $"user:{userId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, userGroup);
            _logger.LogInformation($"Client {Context.ConnectionId} automatically joined user group: {userGroup}");
        }

        // Send welcome message to the connected client
        await Clients.Caller.SendAsync("Hello", "Welcome! SignalR connection established successfully.");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}, UserId: {userId}");

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinMasterGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, MasterGroup);
        _logger.LogInformation($"Client {Context.ConnectionId} joined Master group");
        await Clients.Caller.SendAsync("JoinedMasterGroup", "Successfully joined master monitoring group");
    }

    public async Task JoinGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            await Clients.Caller.SendAsync("Error", "Group name cannot be empty");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation($"Client {Context.ConnectionId} joined group: {groupName}");
        await Clients.Caller.SendAsync("JoinedGroup", groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
        {
            await Clients.Caller.SendAsync("Error", "Group name cannot be empty");
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation($"Client {Context.ConnectionId} left group: {groupName}");
        await Clients.Caller.SendAsync("LeftGroup", groupName);
    }

    public async Task SendMessage(string user, string message, string? targetGroup = null)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";
        var timestamp = DateTime.UtcNow;

        var messageData = new
        {
            User = user,
            Message = message,
            SenderId = userId,
            Timestamp = timestamp,
            Group = targetGroup ?? "All"
        };

        if (string.IsNullOrWhiteSpace(targetGroup))
        {
            // Send to all clients
            await Clients.All.SendAsync("ReceiveMessage", messageData);
            _logger.LogInformation($"Message sent to all clients by {user} ({userId})");
        }
        else
        {
            // Send to specific group
            await Clients.Group(targetGroup).SendAsync("ReceiveMessage", messageData);
            _logger.LogInformation($"Message sent to group '{targetGroup}' by {user} ({userId})");
        }

        // Inform master clients about the message
        await InformMaster(messageData);
    }

    private async Task InformMaster(object messageData)
    {
        await Clients.Group(MasterGroup).SendAsync("MonitorMessage", messageData);
        _logger.LogInformation("Message forwarded to Master monitoring group");
    }
}
