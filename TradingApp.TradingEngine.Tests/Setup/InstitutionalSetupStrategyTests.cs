using TradingApp.TradingEngine.Indicators;
using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Setup;
using Xunit;

namespace TradingApp.TradingEngine.Tests.Setup;

public sealed class InstitutionalSetupStrategyTests
{
    private static readonly DateTimeOffset Origin = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate_WhenAllConditionsPass_EmitsTradeOpportunity()
    {
        var sut = BuildStrategy(
            bias: MarketBias.Bullish,
            exhaustion: SampleExhaustion(),
            sweep: new SweepResult(96m, 95m, "swept"),
            displacement: SampleDisplacement(),
            fvg: new PriceZone(100m, 102m),
            macroConfirmed: true);

        var result = sut.Evaluate(BuildInput());

        Assert.True(result.IsSetup);
        Assert.Equal(MarketBias.Bullish, result.Bias);
        Assert.Equal(6, result.Conditions.Count);
        Assert.All(result.Conditions, c => Assert.True(c.Passed));
        Assert.Equal(1m, result.Confidence);
        Assert.Equal(101m, result.EntryPrice); // FVG midpoint
        Assert.NotNull(result.StopLossPrice);
        Assert.NotNull(result.TakeProfitPrice);
        Assert.True(result.StopLossPrice < result.EntryPrice);
        Assert.True(result.TakeProfitPrice > result.EntryPrice);
    }

    [Fact]
    public void Evaluate_WhenTrendNeutral_ReturnsIncompleteWithSixConditions()
    {
        var sut = BuildStrategy(
            bias: MarketBias.Neutral,
            exhaustion: SampleExhaustion(),
            sweep: new SweepResult(96m, 95m, "swept"),
            displacement: SampleDisplacement(),
            fvg: new PriceZone(100m, 102m),
            macroConfirmed: true);

        var result = sut.Evaluate(BuildInput());

        Assert.False(result.IsSetup);
        Assert.Equal(6, result.Conditions.Count);
        Assert.False(result.Conditions[0].Passed);
        Assert.Equal(0m, result.Confidence);
        Assert.Null(result.EntryPrice);
    }

    [Fact]
    public void Evaluate_WhenExhaustionMissing_ShortCircuitsAfterTrend()
    {
        var sut = BuildStrategy(
            bias: MarketBias.Bullish,
            exhaustion: null,
            sweep: new SweepResult(96m, 95m, "swept"),
            displacement: SampleDisplacement(),
            fvg: new PriceZone(100m, 102m),
            macroConfirmed: true);

        var result = sut.Evaluate(BuildInput());

        Assert.False(result.IsSetup);
        Assert.True(result.Conditions[0].Passed);
        Assert.False(result.Conditions[1].Passed);
        Assert.Contains(result.Conditions, c => c.Detail.Contains("Not evaluated", StringComparison.Ordinal));
    }

    private static InstitutionalSetupStrategy BuildStrategy(
        MarketBias bias,
        ExhaustionResult? exhaustion,
        SweepResult? sweep,
        DisplacementResult? displacement,
        PriceZone? fvg,
        bool macroConfirmed) =>
        new(
            new StubTrendFilter(bias),
            new StubExhaustionDetector(exhaustion),
            new StubSweepDetector(sweep),
            new StubDisplacementDetector(displacement),
            new StubFvgDetector(fvg),
            new StubMacroFilter(macroConfirmed),
            new StubRsiCalculator());

    private static InstitutionalSetupInput BuildInput() =>
        new("SAP", "XETRA",
            TestCandles.RisingStructure(),
            TestCandles.RisingStructure(),
            TestCandles.FallingStructure(),
            TestCandles.FallingStructure());

    private static ExhaustionResult SampleExhaustion()
        => new([new SwingPoint(0, TestCandles.Of(0, 40m, 42m), isHigh: false)], "exhaustion");

    private static DisplacementResult SampleDisplacement()
        => new(5, TestCandles.Of(5, 100m, 106m, 100m, 106m), 3m, "displacement");

    private sealed class StubTrendFilter(MarketBias bias) : ITrendFilter
    {
        public MarketBias DetermineBias(IReadOnlyList<Candle> candles) => bias;
    }

    private sealed class StubExhaustionDetector(ExhaustionResult? result) : IExhaustionDetector
    {
        public ExhaustionResult? Detect(IReadOnlyList<Candle> candles, MarketBias bias, IReadOnlyList<decimal?> rsi)
            => result;
    }

    private sealed class StubSweepDetector(SweepResult? result) : ILiquiditySweepDetector
    {
        public SweepResult? Detect(IReadOnlyList<Candle> candles, MarketBias bias) => result;
    }

    private sealed class StubDisplacementDetector(DisplacementResult? result) : IDisplacementDetector
    {
        public DisplacementResult? Detect(IReadOnlyList<Candle> candles, MarketBias bias) => result;
    }

    private sealed class StubFvgDetector(PriceZone? zone) : IFairValueGapDetector
    {
        public PriceZone? Detect(IReadOnlyList<Candle> candles, MarketBias bias, int displacementIndex) => zone;
    }

    private sealed class StubMacroFilter(bool confirmed) : IMacroConfirmationFilter
    {
        public MacroConfirmationResult Confirm(
            MarketBias bias,
            IReadOnlyList<Candle> dxyCandles,
            IReadOnlyList<Candle> vixCandles)
            => new(confirmed, MarketBias.Bearish, MarketBias.Bearish, confirmed ? "confirmed" : "rejected");
    }

    private sealed class StubRsiCalculator : IRsiCalculator
    {
        public IReadOnlyList<decimal?> Calculate(IReadOnlyList<Candle> candles, int period = 14)
            => new decimal?[candles.Count];
    }
}
