using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Setup;
using Xunit;

namespace TradingApp.TradingEngine.Tests.Setup;

public sealed class SwingTrendFilterTests
{
    [Fact]
    public void DetermineBias_WithRisingStructure_ReturnsBullish()
    {
        var sut = new SwingTrendFilter(swingStrength: 1);

        var bias = sut.DetermineBias(TestCandles.RisingStructure());

        Assert.Equal(MarketBias.Bullish, bias);
    }

    [Fact]
    public void DetermineBias_WithFallingStructure_ReturnsBearish()
    {
        var sut = new SwingTrendFilter(swingStrength: 1);

        var bias = sut.DetermineBias(TestCandles.FallingStructure());

        Assert.Equal(MarketBias.Bearish, bias);
    }

    [Fact]
    public void DetermineBias_WithInsufficientSwings_ReturnsNeutral()
    {
        var sut = new SwingTrendFilter(swingStrength: 1);
        IReadOnlyList<Candle> flat =
        [
            TestCandles.Of(0, 99m, 101m),
            TestCandles.Of(1, 99m, 101m),
            TestCandles.Of(2, 99m, 101m)
        ];

        var bias = sut.DetermineBias(flat);

        Assert.Equal(MarketBias.Neutral, bias);
    }
}
