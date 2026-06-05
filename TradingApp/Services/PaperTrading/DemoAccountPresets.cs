namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Built-in demo templates for paper accounts and broker links.
/// </summary>
internal static class DemoAccountPresets
{
    internal sealed record PaperPreset(
        string Id,
        string Name,
        string Description,
        decimal InitialBalance,
        string BaseCurrency);

    internal sealed record BrokerPreset(
        string Id,
        string BrokerName,
        string Description,
        string BaseCurrency,
        bool RequiresExternalAccountId);

    internal static readonly IReadOnlyList<PaperPreset> Paper =
    [
        new("starter-eur", "Starter Portfolio", "100.000 € virtuelles Startkapital für Aktien & Setups.", 100_000m, "EUR"),
        new("forex-usd", "Forex Demo", "50.000 $ für Währungspaare (EUR/USD, GBP/USD).", 50_000m, "USD"),
        new("compact-eur", "Kompakt Portfolio", "25.000 € für risikoarmes Üben.", 25_000m, "EUR")
    ];

    internal static readonly IReadOnlyList<BrokerPreset> Brokers =
    [
        new("simulated-feed", "Simulierter Markt", "Interner Demo-Feed (XETRA/Forex) — sofort nutzbar, keine Anmeldung.", "EUR", false),
        new("ib-paper", "Interactive Brokers Paper", "Verknüpfe deine IB Paper Trading Account-ID.", "USD", true),
        new("binance-testnet", "Binance Testnet", "Demo-Anbindung für Krypto-Testnet (nur Metadaten).", "USDT", true)
    ];

    internal static PaperPreset? FindPaper(string presetId) =>
        Paper.FirstOrDefault(p => string.Equals(p.Id, presetId, StringComparison.OrdinalIgnoreCase));

    internal static BrokerPreset? FindBroker(string presetId) =>
        Brokers.FirstOrDefault(p => string.Equals(p.Id, presetId, StringComparison.OrdinalIgnoreCase));
}
