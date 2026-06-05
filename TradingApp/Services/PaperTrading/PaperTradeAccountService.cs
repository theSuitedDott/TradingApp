using Microsoft.EntityFrameworkCore;
using TradingApp.Data;
using TradingApp.DTOs.Paper;
using TradingApp.Entities;
using TradingApp.Entities.Enums;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Paper trade account lifecycle and portfolio queries.
/// </summary>
public sealed class PaperTradeAccountService(ApplicationDbContext dbContext) : IPaperTradeAccountService
{
    /// <inheritdoc />
    public async Task<ServiceResult<PaperAccountResponse>> CreateAccountAsync(
        Guid userId,
        CreatePaperAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var exists = await dbContext.PaperTradeAccounts
            .AnyAsync(a => a.UserId == userId && a.Name == name, cancellationToken);

        if (exists)
        {
            return ServiceResult<PaperAccountResponse>.Failure(
                PaperTradingErrorCodes.AccountNameExists,
                "A paper account with this name already exists.");
        }

        var hasAccounts = await dbContext.PaperTradeAccounts
            .AnyAsync(a => a.UserId == userId, cancellationToken);

        var isDefault = request.IsDefault || !hasAccounts;
        if (isDefault)
        {
            var existingDefaults = await dbContext.PaperTradeAccounts
                .Where(a => a.UserId == userId && a.IsDefault)
                .ToListAsync(cancellationToken);

            foreach (var existingDefault in existingDefaults)
            {
                existingDefault.IsDefault = false;
            }
        }

        var now = DateTimeOffset.UtcNow;
        var accountId = Guid.NewGuid();
        var portfolioId = Guid.NewGuid();

        var account = new PaperTradeAccount
        {
            Id = accountId,
            UserId = userId,
            Name = name,
            BaseCurrency = request.BaseCurrency.ToUpperInvariant(),
            InitialBalance = request.InitialBalance,
            IsDefault = isDefault,
            Status = AccountStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            Portfolio = new Portfolio
            {
                Id = portfolioId,
                PaperTradeAccountId = accountId,
                CashBalance = request.InitialBalance,
                ReservedCash = 0,
                TotalEquity = request.InitialBalance,
                BaseCurrency = request.BaseCurrency.ToUpperInvariant(),
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        dbContext.PaperTradeAccounts.Add(account);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ServiceResult<PaperAccountResponse>.Success(PaperTradingMapper.ToAccountResponse(account));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<PaperAccountResponse>>> GetAccountsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var accounts = await dbContext.PaperTradeAccounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.Name)
            .ToListAsync(cancellationToken);

        var response = accounts.Select(PaperTradingMapper.ToAccountResponse).ToList();
        return ServiceResult<IReadOnlyList<PaperAccountResponse>>.Success(response);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<PortfolioSummaryResponse>> GetPortfolioSummaryAsync(
        Guid userId,
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await dbContext.PaperTradeAccounts
            .AsNoTracking()
            .Include(a => a.Portfolio)
            .ThenInclude(p => p.Positions)
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);

        if (account is null)
        {
            return ServiceResult<PortfolioSummaryResponse>.Failure(
                PaperTradingErrorCodes.AccountNotFound,
                "Paper trade account not found.");
        }

        return ServiceResult<PortfolioSummaryResponse>.Success(
            PaperTradingMapper.ToPortfolioSummary(account, account.Portfolio));
    }
}
