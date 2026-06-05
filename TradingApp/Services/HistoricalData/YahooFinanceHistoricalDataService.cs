using System.Text.Json;
using TradingApp.TradingEngine.Models;

namespace TradingApp.Services.HistoricalData;

public interface IHistoricalDataService
{
    Task<IReadOnlyList<Candle>> GetHistoricalCandlesAsync(string symbol, string interval, string range, CancellationToken cancellationToken = default);
}

public class YahooFinanceHistoricalDataService(IHttpClientFactory httpClientFactory, ILogger<YahooFinanceHistoricalDataService> logger) : IHistoricalDataService
{
    public async Task<IReadOnlyList<Candle>> GetHistoricalCandlesAsync(string symbol, string interval, string range, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient("YahooFinance");
        // Yahoo Finance requires a user agent
        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
        {
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        }

        var url = $"https://query2.finance.yahoo.com/v8/finance/chart/{symbol}?interval={interval}&range={range}";
        logger.LogInformation("Fetching historical data from {Url}", url);

        var response = await client.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<YahooChartResponse>(content);

        if (data?.Chart?.Result == null || data.Chart.Result.Length == 0)
        {
            return Array.Empty<Candle>();
        }

        var result = data.Chart.Result[0];
        if (result.Timestamp == null || result.Indicators?.Quote == null || result.Indicators.Quote.Length == 0)
        {
            return Array.Empty<Candle>();
        }

        var quote = result.Indicators.Quote[0];
        var candles = new List<Candle>();

        for (int i = 0; i < result.Timestamp.Length; i++)
        {
            var open = quote.Open?[i];
            var high = quote.High?[i];
            var low = quote.Low?[i];
            var close = quote.Close?[i];

            if (open.HasValue && high.HasValue && low.HasValue && close.HasValue)
            {
                var time = DateTimeOffset.FromUnixTimeSeconds(result.Timestamp[i]);
                // Ensure high/low envelop open/close to avoid ArgumentException in Candle constructor
                var actualHigh = Math.Max(high.Value, Math.Max(open.Value, close.Value));
                var actualLow = Math.Min(low.Value, Math.Min(open.Value, close.Value));
                
                candles.Add(new Candle(time, open.Value, actualHigh, actualLow, close.Value));
            }
        }

        return candles;
    }
}
