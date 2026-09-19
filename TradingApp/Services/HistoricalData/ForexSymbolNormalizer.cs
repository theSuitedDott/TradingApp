namespace TradingApp.Services.HistoricalData;

/// <summary>
/// Normalizes forex symbols between Yahoo, OANDA and display formats.
/// Only EUR/USD and GBP/USD are supported for OANDA integration.
/// </summary>
public static class ForexSymbolNormalizer
{
    private static readonly HashSet<string> SupportedOandaInstruments = new(StringComparer.OrdinalIgnoreCase)
    {
        "EUR_USD",
        "GBP_USD"
    };

    /// <summary>Returns true when the symbol maps to a supported OANDA instrument.</summary>
    public static bool IsOandaForex(string symbol) =>
        SupportedOandaInstruments.Contains(ToOandaInstrument(symbol));

    /// <summary>Maps legacy Yahoo tickers and shorthand to OANDA instrument ids.</summary>
    public static string ToOandaInstrument(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        var normalized = symbol.Trim().ToUpperInvariant();
        return normalized switch
        {
            "EURUSD=X" or "EURUSD" or "EUR/USD" => "EUR_USD",
            "GBPUSD=X" or "GBPUSD" or "GBP/USD" => "GBP_USD",
            _ => normalized
        };
    }

    /// <summary>Maps OANDA/legacy symbols to Yahoo Finance tickers for historical candles.</summary>
    public static string ToYahooSymbol(string symbol)
    {
        return ToOandaInstrument(symbol) switch
        {
            "EUR_USD" => "EURUSD=X",
            "GBP_USD" => "GBPUSD=X",
            _ => symbol.Trim().ToUpperInvariant()
        };
    }

    /// <summary>Human-readable pair label (e.g. EUR_USD → EUR/USD).</summary>
    public static string ToDisplayLabel(string oandaInstrument) =>
        oandaInstrument.Replace('_', '/');
}
