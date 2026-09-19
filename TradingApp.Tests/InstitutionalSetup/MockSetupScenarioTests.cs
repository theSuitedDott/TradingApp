using TradingApp.Services.InstitutionalSetup;
using TradingApp.TradingEngine.Indicators;
using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Setup;
using Xunit;

namespace TradingApp.Tests.InstitutionalSetup;

/// <summary>
/// Verifies that the demo mock data produces a complete bullish setup with the
/// exact strategy configuration registered in DI (swing strength 1).
/// </summary>
public sealed class MockSetupScenarioTests
{
    private static InstitutionalSetupStrategy BuildStrategy() =>
        new(
            new SwingTrendFilter(swingStrength: 1),
            new ThreePushExhaustionDetector(swingStrength: 1),
            new LiquiditySweepDetector(swingStrength: 1),
            new DisplacementDetector(bodyMultiplier: 1.5m, lookback: 10),
            new FairValueGapDetector(),
            new DxyVixConfirmationFilter(new SwingTrendFilter(swingStrength: 1)),
            new RsiCalculator());

    [Fact]
    public void MockData_ProducesCompleteBullishSetup()
    {
        var input = new MockSetupCandleProvider().BuildInput("SAP", "XETRA", rsiPeriod: 3);

        var result = BuildStrategy().Evaluate(input);

        Assert.True(result.IsSetup, DescribeFailures(result));
        Assert.Equal(MarketBias.Bullish, result.Bias);
        Assert.Equal(6, result.Conditions.Count);
        Assert.All(result.Conditions, c => Assert.True(c.Passed, c.Detail));
        Assert.Equal(102m, result.EntryPrice); // 3rd push low
        Assert.Equal(117m, result.TakeProfitPrice); // prior swing high before correction
        Assert.True(result.StopLossPrice < result.EntryPrice);
        Assert.True(result.TakeProfitPrice > result.EntryPrice);
    }

    private static string DescribeFailures(InstitutionalSetupResult result)
        => string.Join("; ", result.Conditions.Select(c => $"{c.Name}={c.Passed} ({c.Detail})"));
}
