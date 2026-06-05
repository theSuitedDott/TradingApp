using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TradingApp.Hubs;

/// <summary>
/// Realtime hub for paper trading portfolio and order updates.
/// </summary>
[Authorize]
public sealed class PaperTradingHub : Hub
{
    /// <summary>Builds the SignalR group name for a paper account.</summary>
    public static string AccountGroup(Guid accountId) => $"paper-account:{accountId}";

    /// <summary>Subscribes the connection to updates for a paper account.</summary>
    public async Task SubscribeToAccount(Guid accountId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, AccountGroup(accountId));
    }

    /// <summary>Unsubscribes the connection from a paper account group.</summary>
    public async Task UnsubscribeFromAccount(Guid accountId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, AccountGroup(accountId));
    }
}
