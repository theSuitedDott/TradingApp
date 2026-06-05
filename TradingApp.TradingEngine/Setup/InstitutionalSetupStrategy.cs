using TradingApp.TradingEngine.Indicators;
using TradingApp.TradingEngine.Models;

namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Composes the six condition detectors into the full institutional setup ("institutional cutting").
/// A trade opportunity is only emitted when every condition is satisfied; the entry is anchored in
/// the Fair Value Gap, the stop-loss beyond the swept liquidity, and the take-profit at a fixed
/// reward-to-risk multiple.
/// </summary>
public sealed class InstitutionalSetupStrategy : IInstitutionalSetupStrategy
{
    private readonly ITrendFilter _trendFilter;
    private readonly IExhaustionDetector _exhaustionDetector;
    private readonly ILiquiditySweepDetector _sweepDetector;
    private readonly IDisplacementDetector _displacementDetector;
    private readonly IFairValueGapDetector _fvgDetector;
    private readonly IMacroConfirmationFilter _macroFilter;
    private readonly IRsiCalculator _rsiCalculator;
    private readonly decimal _rewardToRisk;
    private readonly decimal _stopBufferFraction;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates the composite institutional setup strategy.
    /// </summary>
    /// <param name="trendFilter">Higher-timeframe trend filter.</param>
    /// <param name="exhaustionDetector">Three-push exhaustion detector.</param>
    /// <param name="sweepDetector">Liquidity sweep detector.</param>
    /// <param name="displacementDetector">Displacement detector.</param>
    /// <param name="fvgDetector">Fair Value Gap detector.</param>
    /// <param name="macroFilter">DXY/VIX confirmation filter.</param>
    /// <param name="rsiCalculator">RSI calculator for divergence analysis.</param>
    /// <param name="rewardToRisk">Reward-to-risk multiple for the take-profit (default 2.0).</param>
    /// <param name="stopBufferFraction">Fractional buffer beyond the swept level for the stop (default 0.1%).</param>
    /// <param name="timeProvider">Time source (defaults to the system clock).</param>
    public InstitutionalSetupStrategy(
        ITrendFilter trendFilter,
        IExhaustionDetector exhaustionDetector,
        ILiquiditySweepDetector sweepDetector,
        IDisplacementDetector displacementDetector,
        IFairValueGapDetector fvgDetector,
        IMacroConfirmationFilter macroFilter,
        IRsiCalculator rsiCalculator,
        decimal rewardToRisk = 2m,
        decimal stopBufferFraction = 0.001m,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(trendFilter);
        ArgumentNullException.ThrowIfNull(exhaustionDetector);
        ArgumentNullException.ThrowIfNull(sweepDetector);
        ArgumentNullException.ThrowIfNull(displacementDetector);
        ArgumentNullException.ThrowIfNull(fvgDetector);
        ArgumentNullException.ThrowIfNull(macroFilter);
        ArgumentNullException.ThrowIfNull(rsiCalculator);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rewardToRisk, 0m);
        ArgumentOutOfRangeException.ThrowIfNegative(stopBufferFraction);

        _trendFilter = trendFilter;
        _exhaustionDetector = exhaustionDetector;
        _sweepDetector = sweepDetector;
        _displacementDetector = displacementDetector;
        _fvgDetector = fvgDetector;
        _macroFilter = macroFilter;
        _rsiCalculator = rsiCalculator;
        _rewardToRisk = rewardToRisk;
        _stopBufferFraction = stopBufferFraction;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public InstitutionalSetupResult Evaluate(InstitutionalSetupInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var now = _timeProvider.GetUtcNow();
        var conditions = new List<ConditionCheck>(ConditionNames.Ordered.Count);

        // 1. Clear H4 trend.
        var bias = _trendFilter.DetermineBias(input.HigherTimeframeCandles);
        var trendPassed = bias != MarketBias.Neutral;
        conditions.Add(new ConditionCheck(
            ConditionNames.Trend,
            trendPassed,
            trendPassed ? $"Clear H4 {bias} trend." : "No clear H4 trend (structure not aligned)."));
        if (!trendPassed)
        {
            return Incomplete(input, bias, conditions, now);
        }

        // 2. Three-push exhaustion with RSI divergence.
        var rsi = _rsiCalculator.Calculate(input.EntryTimeframeCandles, input.RsiPeriod);
        var exhaustion = _exhaustionDetector.Detect(input.EntryTimeframeCandles, bias, rsi);
        conditions.Add(new ConditionCheck(
            ConditionNames.Exhaustion,
            exhaustion is not null,
            exhaustion?.Detail ?? "Correction has not exhausted in three diverging pushes."));
        if (exhaustion is null)
        {
            return Incomplete(input, bias, conditions, now);
        }

        // 3. Liquidity sweep (inducement).
        var sweep = _sweepDetector.Detect(input.EntryTimeframeCandles, bias);
        conditions.Add(new ConditionCheck(
            ConditionNames.LiquiditySweep,
            sweep is not null,
            sweep?.Detail ?? "Final push did not sweep a liquidity level."));
        if (sweep is null)
        {
            return Incomplete(input, bias, conditions, now);
        }

        // 4. Displacement candle back in trend direction.
        var displacement = _displacementDetector.Detect(input.EntryTimeframeCandles, bias);
        conditions.Add(new ConditionCheck(
            ConditionNames.Displacement,
            displacement is not null,
            displacement?.Detail ?? "No displacement candle in trend direction."));
        if (displacement is null)
        {
            return Incomplete(input, bias, conditions, now);
        }

        // 5. Fair Value Gap entry zone.
        var fvg = _fvgDetector.Detect(input.EntryTimeframeCandles, bias, displacement.Index);
        conditions.Add(new ConditionCheck(
            ConditionNames.FairValueGap,
            fvg is not null,
            fvg is not null
                ? $"Fair Value Gap entry zone {fvg.Lower:F2} – {fvg.Upper:F2}."
                : "Displacement left no Fair Value Gap to enter."));
        if (fvg is null)
        {
            return Incomplete(input, bias, conditions, now);
        }

        // 6. DXY/VIX macro confirmation.
        var macro = _macroFilter.Confirm(bias, input.DxyCandles, input.VixCandles);
        conditions.Add(new ConditionCheck(
            ConditionNames.MacroConfirmation,
            macro.IsConfirmed,
            macro.Detail));
        if (!macro.IsConfirmed)
        {
            return Incomplete(input, bias, conditions, now);
        }

        var (entry, stopLoss, takeProfit) = BuildLevels(bias, fvg, sweep);
        return new InstitutionalSetupResult(
            input.Symbol,
            input.Exchange,
            bias,
            conditions,
            now,
            fvg,
            entry,
            stopLoss,
            takeProfit);
    }

    private (decimal Entry, decimal StopLoss, decimal TakeProfit) BuildLevels(
        MarketBias bias,
        PriceZone fvg,
        SweepResult sweep)
    {
        var entry = fvg.Midpoint;
        if (bias == MarketBias.Bullish)
        {
            var stopLoss = sweep.ExtremePrice * (1m - _stopBufferFraction);
            var risk = entry - stopLoss;
            var takeProfit = entry + (_rewardToRisk * risk);
            return (entry, stopLoss, takeProfit);
        }
        else
        {
            var stopLoss = sweep.ExtremePrice * (1m + _stopBufferFraction);
            var risk = stopLoss - entry;
            var takeProfit = entry - (_rewardToRisk * risk);
            return (entry, stopLoss, takeProfit);
        }
    }

    private static InstitutionalSetupResult Incomplete(
        InstitutionalSetupInput input,
        MarketBias bias,
        List<ConditionCheck> conditions,
        DateTimeOffset now)
    {
        for (var i = conditions.Count; i < ConditionNames.Ordered.Count; i++)
        {
            conditions.Add(new ConditionCheck(
                ConditionNames.Ordered[i],
                passed: false,
                detail: "Not evaluated (an earlier condition failed)."));
        }

        return new InstitutionalSetupResult(input.Symbol, input.Exchange, bias, conditions, now);
    }
}
