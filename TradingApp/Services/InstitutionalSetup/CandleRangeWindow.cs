using TradingApp.TradingEngine.Models;

namespace TradingApp.Services.InstitutionalSetup;

/// <summary>
/// Zero-copy view over a contiguous slice <c>[start, start + count)</c> of a candle list.
/// </summary>
internal sealed class CandleRangeWindow(IReadOnlyList<Candle> source, int start, int count) : IReadOnlyList<Candle>
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

            return source[start + index];
        }
    }

    public IEnumerator<Candle> GetEnumerator()
    {
        for (var i = 0; i < count; i++)
        {
            yield return source[start + i];
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
