using Microsoft.Extensions.Options;
using TradingApp.Configuration;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Commission calculator based on <see cref="PaperTradingSettings"/>.
/// </summary>
public sealed class CommissionCalculator(IOptions<PaperTradingSettings> options) : ICommissionCalculator
{
    private readonly PaperTradingSettings _settings = options.Value;

    /// <inheritdoc />
    public decimal Calculate(decimal notional)
    {
        if (notional <= 0)
        {
            return 0m;
        }

        var commission = notional * _settings.CommissionRate;
        return Math.Max(commission, _settings.MinimumCommission);
    }
}
