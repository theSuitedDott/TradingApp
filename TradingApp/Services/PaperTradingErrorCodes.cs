namespace TradingApp.Services;

/// <summary>
/// Error codes for paper trading operations.
/// </summary>
public static class PaperTradingErrorCodes
{
    public const string AccountNotFound = "paper_account_not_found";
    public const string OrderNotFound = "order_not_found";
    public const string AccessDenied = "access_denied";
    public const string InsufficientFunds = "insufficient_funds";
    public const string InsufficientPosition = "insufficient_position";
    public const string InvalidOrderState = "invalid_order_state";
    public const string NoMarketPrice = "no_market_price";
    public const string AccountInactive = "account_inactive";
    public const string DuplicateClientOrderId = "duplicate_client_order_id";
    public const string AccountNameExists = "account_name_exists";
}
