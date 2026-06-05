using TradingApp.TradingEngine.Models;
using TradingApp.TradingEngine.Setup;
using Xunit;

namespace TradingApp.TradingEngine.Tests.Setup;

public sealed class DxyVixConfirmationFilterTests
{
    private static DxyVixConfirmationFilter CreateSut()
        => new(new SwingTrendFilter(swingStrength: 1));

    [Fact]
    public void Confirm_WhenLongAndBothFalling_IsConfirmed()
    {
        var result = CreateSut().Confirm(
            MarketBias.Bullish,
            TestCandles.FallingStructure(),
            TestCandles.FallingStructure());

        Assert.True(result.IsConfirmed);
        Assert.Equal(MarketBias.Bearish, result.DxyBias);
        Assert.Equal(MarketBias.Bearish, result.VixBias);
    }

    [Fact]
    public void Confirm_WhenLongButDxyRising_IsNotConfirmed()
    {
        var result = CreateSut().Confirm(
            MarketBias.Bullish,
            TestCandles.RisingStructure(),
            TestCandles.FallingStructure());

        Assert.False(result.IsConfirmed);
    }
}
