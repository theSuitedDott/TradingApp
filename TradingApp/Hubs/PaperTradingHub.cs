using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TradingApp.Hubs;

/// <summary>
/// Realtime hub for paper trading portfolio and order updates.
/// </summary>
[Authorize]
public sealed class PaperTradingHub : Hub
{
    /// <summary>SignalR group that receives institutional setup opportunities.</summary>
    public const string SetupGroup = "institutional-setups";

    /// <summary>Builds the SignalR group name for a paper account.</summary>
    public static string AccountGroup(Guid accountId) => $"paper-account:{accountId}";

    /// <summary>Builds the SignalR group name for live quote updates on an instrument.</summary>
    public static string QuoteGroup(string symbol, string exchange) =>
        $"market-quote:{symbol.Trim().ToUpperInvariant()}:{exchange.Trim().ToUpperInvariant()}";

    /// <summary>Subscribes the connection to institutional setup notifications.</summary>
    public async Task SubscribeToSetups()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SetupGroup);
    }

    /// <summary>Unsubscribes the connection from institutional setup notifications.</summary>
    public async Task UnsubscribeFromSetups()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SetupGroup);
    }

    /// <summary>Subscribes the connection to live quote ticks for chart updates.</summary>
    public async Task SubscribeToQuote(string symbol, string exchange)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, QuoteGroup(symbol, exchange));
    }

    /// <summary>Unsubscribes the connection from live quote ticks.</summary>
    public async Task UnsubscribeFromQuote(string symbol, string exchange)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, QuoteGroup(symbol, exchange));
    }

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
