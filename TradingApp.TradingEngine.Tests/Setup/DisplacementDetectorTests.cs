using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Setup;
using Xunit;

namespace TradingApp.TradingEngine.Tests.Setup;

public sealed class DisplacementDetectorTests
{
    private static Candle SmallBody(int i) => TestCandles.Ohlc(i, 100m, 100.6m, 99.6m, 100.2m);

    [Fact]
    public void Detect_WithLargeBullishCandle_ReturnsResult()
    {
        var candles = new List<Candle>();
        for (var i = 0; i < 8; i++)
        {
            candles.Add(SmallBody(i));
        }

        candles.Add(TestCandles.Ohlc(8, 100m, 106.5m, 99.8m, 106m)); // strong bullish body
        var sut = new DisplacementDetector(bodyMultiplier: 1.5m, lookback: 5);

        var result = sut.Detect(candles, MarketBias.Bullish);

        Assert.NotNull(result);
        Assert.Equal(8, result!.Index);
        Assert.True(result.BodyRatio >= 1.5m);
    }

    [Fact]
    public void Detect_WithoutStrongCandle_ReturnsNull()
    {
        var candles = Enumerable.Range(0, 9).Select(SmallBody).ToList();
        var sut = new DisplacementDetector(bodyMultiplier: 1.5m, lookback: 5);

        var result = sut.Detect(candles, MarketBias.Bullish);

        Assert.Null(result);
    }
}
