using TradingApp.Services.HistoricalData;
using Xunit;

namespace TradingApp.Tests.HistoricalData;

public sealed class ForexSymbolNormalizerTests
{
    [Theory]
    [InlineData("EUR_USD", true)]
    [InlineData("GBP_USD", true)]
    [InlineData("EURUSD=X", true)]
    [InlineData("GBPUSD=X", true)]
    [InlineData("AAPL", false)]
    [InlineData("DX-Y.NYB", false)]
    public void IsOandaForex_RecognizesSupportedPairs(string symbol, bool expected)
    {
        Assert.Equal(expected, ForexSymbolNormalizer.IsOandaForex(symbol));
    }

    [Theory]
    [InlineData("EURUSD=X", "EUR_USD")]
    [InlineData("GBPUSD=X", "GBP_USD")]
    [InlineData("EUR/USD", "EUR_USD")]
    [InlineData("GBP_USD", "GBP_USD")]
    public void ToOandaInstrument_MapsLegacyTickers(string input, string expected)
    {
        Assert.Equal(expected, ForexSymbolNormalizer.ToOandaInstrument(input));
    }

    [Theory]
    [InlineData("EUR_USD", "EURUSD=X")]
    [InlineData("GBP_USD", "GBPUSD=X")]
    [InlineData("EURUSD=X", "EURUSD=X")]
    public void ToYahooSymbol_MapsForexPairs(string input, string expected)
    {
        Assert.Equal(expected, ForexSymbolNormalizer.ToYahooSymbol(input));
    }
}
