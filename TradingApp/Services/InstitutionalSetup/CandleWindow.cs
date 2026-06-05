using TradingApp.TradingEngine.Models;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Zero-copy view over the first <paramref name="count"/> candles of a source list.
/// </summary>
internal sealed class CandleWindow(IReadOnlyList<Candle> source, int count) : IReadOnlyList<Candle>
{
    public int Count => count;

    public Candle this[int index]
    {
        get
        {
            if (index < 0 || index >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return source[index];
        }
    }

    public IEnumerator<Candle> GetEnumerator()
    {
        for (var i = 0; i < count; i++)
        {
            yield return source[i];
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
