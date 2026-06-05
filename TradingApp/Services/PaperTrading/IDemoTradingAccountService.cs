using TradingApp.DTOs.Paper;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Demo onboarding: provision virtual paper accounts and link demo broker connections.
/// </summary>
public interface IDemoTradingAccountService
{
    /// <summary>Returns available paper account presets.</summary>
    IReadOnlyList<DemoAccountPresetDto> GetPaperPresets();

    /// <summary>Returns available demo broker link presets.</summary>
    IReadOnlyList<DemoAccountPresetDto> GetBrokerPresets();

    /// <summary>Creates a paper account from a preset for the current user.</summary>
    Task<ServiceResult<PaperAccountResponse>> ProvisionPaperAccountAsync(
        Guid userId,
        ProvisionDemoAccountRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a default demo paper account when the user has none yet.</summary>
    Task<ServiceResult<PaperAccountResponse>> EnsureDefaultPaperAccountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Links a demo broker connection (metadata only, live trading disabled).</summary>
    Task<ServiceResult<BrokerAccountResponse>> LinkDemoBrokerAsync(
        Guid userId,
        LinkDemoBrokerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Lists demo broker connections for the user.</summary>
    Task<ServiceResult<IReadOnlyList<BrokerAccountResponse>>> GetLinkedBrokersAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>Returns onboarding status and recommendations.</summary>
    Task<ServiceResult<DemoTradingSetupStatusDto>> GetSetupStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
