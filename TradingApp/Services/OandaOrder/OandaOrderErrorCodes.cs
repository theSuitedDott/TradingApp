namespace TradingApp.Services.OandaOrder;

/// <summary>Error codes returned by <see cref="OandaOrderService"/>.</summary>
public static class OandaOrderErrorCodes
{
    /// <summary>OANDA credentials are not configured.</summary>
    public const string NotConfigured = "oanda_not_configured";

    /// <summary>The OANDA REST API returned a non-success status.</summary>
    public const string ApiError = "oanda_api_error";

    /// <summary>Order was accepted but did not open a trade (e.g. margin call).</summary>
    public const string NoTradeOpened = "oanda_no_trade_opened";
}
