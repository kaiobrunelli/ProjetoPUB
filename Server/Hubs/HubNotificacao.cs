using Microsoft.AspNetCore.SignalR;

namespace SipubDesembolsos.Server.Hubs;

public class HubNotificacao : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            Console.WriteLine($"[Hub] {userId} conectado → grupo '{userId}' | ConnectionId: {Context.ConnectionId}");
        }
        else
        {
            Console.WriteLine($"[Hub] Conexão anônima | ConnectionId: {Context.ConnectionId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            Console.WriteLine($"[Hub] {userId} desconectado.");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
