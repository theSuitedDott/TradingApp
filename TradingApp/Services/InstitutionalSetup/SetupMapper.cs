using System.Globalization;
using TradingApp.DTOs.Setup;
using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Setup;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Maps engine setup results to API DTOs, including German display labels and message.
/// </summary>
public static class SetupMapper
{
    private static readonly IReadOnlyDictionary<string, string> GermanLabels = new Dictionary<string, string>
    {
        [ConditionNames.Trend] = "Klarer H4-Trend",
        [ConditionNames.Exhaustion] = "3-Push (Einstieg am 3. Push, Ziel vorheriger Peak)",
        [ConditionNames.LiquiditySweep] = "Liquiditäts-Sweep (Inducement)",
        [ConditionNames.Displacement] = "Displacement-Kerze",
        [ConditionNames.FairValueGap] = "Fair Value Gap (Einstieg)",
        [ConditionNames.MacroConfirmation] = "DXY/VIX-Bestätigung"
    };

    /// <summary>Maps a full setup result to the analysis DTO.</summary>
    /// <param name="result">Engine evaluation result.</param>
    public static SetupAnalysisDto ToAnalysis(InstitutionalSetupResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var conditions = result.Conditions
            .Select(c => new ConditionCheckDto(LabelFor(c.Name), c.Passed, c.Detail))
            .ToList();

        var opportunity = result.IsSetup ? ToOpportunity(result) : null;

        return new SetupAnalysisDto(
            result.Symbol,
            result.Exchange,
            result.Bias.ToString(),
            decimal.Round(result.Confidence, 2),
            result.IsSetup,
            conditions,
            result.DetectedAt,
            result.EntryPrice,
            result.StopLossPrice,
            result.TakeProfitPrice,
            opportunity);
    }

    /// <summary>Maps a valid setup result to a trade opportunity DTO with a fresh id.</summary>
    /// <param name="result">Engine evaluation result (must be a valid setup).</param>
    public static TradeOpportunityDto ToOpportunity(InstitutionalSetupResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.IsSetup)
        {
            throw new InvalidOperationException("Cannot map an incomplete setup to an opportunity.");
        }

        var entry = result.EntryPrice!.Value;
        var stop = result.StopLossPrice!.Value;
        var target = result.TakeProfitPrice!.Value;
        var risk = Math.Abs(entry - stop);
        var reward = Math.Abs(target - entry);
        var rewardToRisk = risk == 0 ? 0m : decimal.Round(reward / risk, 2);
        var direction = result.Bias == MarketBias.Bullish ? "Long" : "Short";
        var side = result.Bias == MarketBias.Bullish ? "Buy" : "Sell";

        var conditions = result.Conditions
            .Select(c => new ConditionCheckDto(LabelFor(c.Name), c.Passed, c.Detail))
            .ToList();

        return new TradeOpportunityDto(
            Guid.NewGuid(),
            result.Symbol,
            result.Exchange,
            direction,
            side,
            decimal.Round(entry, 2),
            decimal.Round(stop, 2),
            decimal.Round(target, 2),
            rewardToRisk,
            decimal.Round(result.Confidence, 2),
            BuildMessage(result, direction, entry, stop, target),
            result.DetectedAt,
            TradeOpportunityStore.ActiveStatus,
            conditions);
    }

    private static string LabelFor(string conditionName)
        => GermanLabels.TryGetValue(conditionName, out var label) ? label : conditionName;

    private static string BuildMessage(
        InstitutionalSetupResult result,
        string direction,
        decimal entry,
        decimal stop,
        decimal target)
    {
        var c = CultureInfo.GetCultureInfo("de-DE");
        return string.Create(c,
            $"Institutionelles Setup erkannt: {result.Symbol} ({result.Exchange}) – {direction}. " +
            $"Alle 6 Bedingungen erfüllt. Einstieg {entry:N2}, Stop-Loss {stop:N2}, Take-Profit {target:N2}.");
    }
}
