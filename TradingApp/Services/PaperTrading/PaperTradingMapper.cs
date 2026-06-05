using TradingApp.DTOs.Paper;
using TradingApp.Entities;

namespace TradingApp.Services.PaperTrading;

internal static class PaperTradingMapper
{
    public static PaperAccountResponse ToAccountResponse(PaperTradeAccount account) =>
        new(
            account.Id,
            account.Name,
            account.BaseCurrency,
            account.InitialBalance,
            account.IsDefault,
            account.Status.ToString(),
            account.CreatedAt);

    public static PortfolioSummaryResponse ToPortfolioSummary(PaperTradeAccount account, Portfolio portfolio)
    {
        var unrealized = portfolio.Positions.Sum(p => p.UnrealizedPnL);
        var realized = portfolio.Positions.Sum(p => p.RealizedPnL);

        return new PortfolioSummaryResponse(
            portfolio.Id,
            account.Id,
            portfolio.CashBalance,
            portfolio.ReservedCash,
            portfolio.CashBalance - portfolio.ReservedCash,
            portfolio.TotalEquity,
            unrealized,
            realized,
            portfolio.BaseCurrency);
    }

    public static OrderResponse ToOrderResponse(Order order) =>
        new(
            order.Id,
            order.PaperTradeAccountId ?? Guid.Empty,
            order.Symbol,
            order.Exchange,
            order.Side.ToString(),
            order.Type.ToString(),
            order.Status.ToString(),
            order.Quantity,
            order.FilledQuantity,
            order.LimitPrice,
            order.AverageFillPrice,
            order.Commission,
            order.RejectReason,
            order.CreatedAt,
            order.FilledAt);

    public static PositionResponse ToPositionResponse(Position position) =>
        new(
            position.Id,
            position.Symbol,
            position.Exchange,
            position.Side.ToString(),
            position.Status.ToString(),
            position.Quantity,
            position.AverageEntryPrice,
            position.CurrentPrice,
            position.UnrealizedPnL,
            position.RealizedPnL,
            position.OpenedAt,
            position.ClosedAt);
}
