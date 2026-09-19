namespace TradingApp.Configuration;

/// <summary>
/// Configuration for the institutional setup scanner.
/// </summary>
public sealed class InstitutionalSetupSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "InstitutionalSetup";

    /// <summary>Whether the background scanner is enabled.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Buy-setup scan interval in seconds.</summary>
    public int ScanIntervalSeconds { get; init; } = 60;

    /// <summary>Sell/exit check interval in seconds (open positions vs. SL/TP).</summary>
    public int ExitScanIntervalSeconds { get; init; } = 60;

    /// <summary>Symbol scanned by the demo mock provider.</summary>
    public string Symbol { get; init; } = "SAP";

    /// <summary>Exchange of the scanned symbol.</summary>
    public string Exchange { get; init; } = "XETRA";

    /// <summary>RSI period used for divergence analysis.</summary>
    public int RsiPeriod { get; init; } = 3;

    /// <summary>Maximum number of opportunities kept in memory.</summary>
    public int MaxOpportunities { get; init; } = 50;

    /// <summary>
    /// When true, the scanner fetches real multi-timeframe data from Yahoo Finance
    /// (H4 trend, H1 entry, DXY, VIX). When false, deterministic mock data is used.
    /// </summary>
    public bool UseLiveData { get; init; } = false;
}
