namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Error codes for institutional setup opportunity execution.
/// </summary>
public static class SetupErrorCodes
{
    /// <summary>The requested opportunity does not exist (or was evicted).</summary>
    public const string OpportunityNotFound = "opportunity_not_found";

    /// <summary>The opportunity is no longer active (already executed).</summary>
    public const string OpportunityNotActive = "opportunity_not_active";
}
