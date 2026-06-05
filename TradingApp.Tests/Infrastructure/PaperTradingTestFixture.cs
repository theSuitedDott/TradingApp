using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TradingApp.Configuration;
using TradingApp.Data;
using TradingApp.Services.PaperTrading;
using TradingApp.Trading.Execution;

namespace TradingApp.Tests.Infrastructure;

/// <summary>
/// Builds an in-memory database context for paper trading tests.
/// </summary>
public sealed class PaperTradingTestFixture : IDisposable
{
    public PaperTradingTestFixture()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        DbContext = new ApplicationDbContext(options);
        DbContext.Database.EnsureCreated();

        QuoteStore = new MarketQuoteStore();
        var paperSettings = Options.Create(new PaperTradingSettings
        {
            CommissionRate = 0.001m,
            MinimumCommission = 0.01m
        });

        var commission = new CommissionCalculator(paperSettings);
        var pnl = new PnlCalculator();
        var valuation = new PortfolioValuationService(DbContext, pnl);
        var settlement = new PortfolioSettlementService(DbContext, commission, pnl, valuation);
        var paperExecutor = new PaperOrderExecutor(settlement);
        var executorFactory = new OrderExecutorFactory([paperExecutor]);
        var notifier = new NoOpPaperTradingNotifier();

        Commission = commission;
        Pnl = pnl;
        Valuation = valuation;
        Settlement = settlement;
        ExecutorFactory = executorFactory;
        Notifier = notifier;
        var riskMonitor = new PositionRiskMonitor();
        var riskExit = new PaperRiskExitService(DbContext, ExecutorFactory, Notifier);
        QuoteProcessor = new MarketQuoteProcessor(
            DbContext,
            QuoteStore,
            ExecutorFactory,
            Valuation,
            riskMonitor,
            riskExit,
            Notifier);
        AccountService = new PaperTradeAccountService(DbContext);
        OrderService = new PaperOrderService(
            DbContext,
            QuoteStore,
            ExecutorFactory,
            Commission,
            Notifier);
    }

    public ApplicationDbContext DbContext { get; }

    public IMarketQuoteStore QuoteStore { get; }

    public ICommissionCalculator Commission { get; }

    public IPnlCalculator Pnl { get; }

    public IPortfolioValuationService Valuation { get; }

    public IPortfolioSettlementService Settlement { get; }

    public IOrderExecutorFactory ExecutorFactory { get; }

    public IMarketQuoteProcessor QuoteProcessor { get; }

    public IPaperTradeAccountService AccountService { get; }

    public IPaperOrderService OrderService { get; }

    public IPaperTradingNotifier Notifier { get; }

    public void Dispose() => DbContext.Dispose();

    private sealed class NoOpPaperTradingNotifier : IPaperTradingNotifier
    {
        public Task NotifyOrderUpdatedAsync(Guid accountId, Entities.Order order, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task NotifyPortfolioUpdatedAsync(Guid accountId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
