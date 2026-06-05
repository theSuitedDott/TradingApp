namespace TradingApp.DTOs.Paper;

/// <summary>
/// Onboarding status for demo paper and broker connections.
/// </summary>
public sealed record DemoTradingSetupStatusDto(
    bool HasPaperAccount,
    int PaperAccountCount,
    int LinkedBrokerCount,
    PaperAccountResponse? DefaultPaperAccount,
    IReadOnlyList<BrokerAccountResponse> LinkedBrokers,
    string RecommendedAction);
