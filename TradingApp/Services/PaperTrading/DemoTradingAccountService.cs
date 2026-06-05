using Microsoft.EntityFrameworkCore;
using TradingApp.Data;
using TradingApp.DTOs.Paper;
using TradingApp.Entities;
using TradingApp.Entities.Enums;

namespace TradingApp.Services.PaperTrading;

/// <summary>
/// Provisions demo paper accounts and demo broker links for onboarding.
/// </summary>
public sealed class DemoTradingAccountService(
    ApplicationDbContext dbContext,
    IPaperTradeAccountService paperAccountService) : IDemoTradingAccountService
{
    /// <inheritdoc />
    public IReadOnlyList<DemoAccountPresetDto> GetPaperPresets() =>
        DemoAccountPresets.Paper.Select(p => new DemoAccountPresetDto(
            p.Id,
            p.Name,
            p.Description,
            p.InitialBalance,
            p.BaseCurrency,
            "Paper")).ToList();

    /// <inheritdoc />
    public IReadOnlyList<DemoAccountPresetDto> GetBrokerPresets() =>
        DemoAccountPresets.Brokers.Select(p => new DemoAccountPresetDto(
            p.Id,
            p.BrokerName,
            p.Description,
            0m,
            p.BaseCurrency,
            "Broker")).ToList();

    /// <inheritdoc />
    public async Task<ServiceResult<PaperAccountResponse>> ProvisionPaperAccountAsync(
        Guid userId,
        ProvisionDemoAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var preset = DemoAccountPresets.FindPaper(request.PresetId.Trim());
        if (preset is null)
        {
            return ServiceResult<PaperAccountResponse>.Failure(
                PaperTradingErrorCodes.InvalidDemoPreset,
                "Unknown demo account preset.");
        }

        var accountName = string.IsNullOrWhiteSpace(request.CustomName)
            ? preset.Name
            : request.CustomName.Trim();

        var createResult = await paperAccountService.CreateAccountAsync(
            userId,
            new CreatePaperAccountRequest
            {
                Name = accountName,
                InitialBalance = preset.InitialBalance,
                BaseCurrency = preset.BaseCurrency,
                IsDefault = request.IsDefault
            },
            cancellationToken);

        if (!createResult.IsSuccess)
        {
            return createResult;
        }

        await PromoteToTraderIfNeededAsync(userId, cancellationToken);
        return createResult;
    }

    /// <inheritdoc />
    public async Task<ServiceResult<PaperAccountResponse>> EnsureDefaultPaperAccountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.PaperTradeAccounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return ServiceResult<PaperAccountResponse>.Success(PaperTradingMapper.ToAccountResponse(existing));
        }

        return await ProvisionPaperAccountAsync(
            userId,
            new ProvisionDemoAccountRequest { PresetId = "starter-eur", IsDefault = true },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ServiceResult<BrokerAccountResponse>> LinkDemoBrokerAsync(
        Guid userId,
        LinkDemoBrokerRequest request,
        CancellationToken cancellationToken = default)
    {
        var preset = DemoAccountPresets.FindBroker(request.PresetId.Trim());
        if (preset is null)
        {
            return ServiceResult<BrokerAccountResponse>.Failure(
                PaperTradingErrorCodes.InvalidDemoPreset,
                "Unknown demo broker preset.");
        }

        var externalId = request.ExternalAccountId?.Trim();
        if (preset.RequiresExternalAccountId && string.IsNullOrWhiteSpace(externalId))
        {
            return ServiceResult<BrokerAccountResponse>.Failure(
                PaperTradingErrorCodes.MissingBrokerAccountId,
                "This broker preset requires an external demo account ID.");
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? $"{preset.BrokerName} Demo"
            : request.DisplayName.Trim();

        var duplicate = await dbContext.BrokerAccounts
            .AnyAsync(
                a => a.UserId == userId &&
                     a.BrokerName == preset.BrokerName &&
                     a.ExternalAccountId == externalId,
                cancellationToken);

        if (duplicate)
        {
            return ServiceResult<BrokerAccountResponse>.Failure(
                PaperTradingErrorCodes.BrokerLinkExists,
                "This demo broker connection already exists.");
        }

        var nameExists = await dbContext.BrokerAccounts
            .AnyAsync(a => a.UserId == userId && a.Name == displayName, cancellationToken);

        if (nameExists)
        {
            return ServiceResult<BrokerAccountResponse>.Failure(
                PaperTradingErrorCodes.BrokerNameExists,
                "A broker connection with this display name already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var brokerId = Guid.NewGuid();
        var portfolioId = Guid.NewGuid();

        var broker = new BrokerAccount
        {
            Id = brokerId,
            UserId = userId,
            Name = displayName,
            BrokerName = preset.BrokerName,
            ExternalAccountId = externalId,
            BaseCurrency = preset.BaseCurrency,
            IsLiveTradingEnabled = false,
            Status = AccountStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            Portfolio = new Portfolio
            {
                Id = portfolioId,
                BrokerAccountId = brokerId,
                CashBalance = 0,
                ReservedCash = 0,
                TotalEquity = 0,
                BaseCurrency = preset.BaseCurrency,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        dbContext.BrokerAccounts.Add(broker);
        await dbContext.SaveChangesAsync(cancellationToken);
        await PromoteToTraderIfNeededAsync(userId, cancellationToken);

        return ServiceResult<BrokerAccountResponse>.Success(ToBrokerResponse(broker));
    }

    /// <inheritdoc />
    public async Task<ServiceResult<IReadOnlyList<BrokerAccountResponse>>> GetLinkedBrokersAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var brokers = await dbContext.BrokerAccounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        return ServiceResult<IReadOnlyList<BrokerAccountResponse>>.Success(
            brokers.Select(ToBrokerResponse).ToList());
    }

    /// <inheritdoc />
    public async Task<ServiceResult<DemoTradingSetupStatusDto>> GetSetupStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var paperAccounts = await dbContext.PaperTradeAccounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var brokers = await dbContext.BrokerAccounts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        var defaultPaper = paperAccounts.FirstOrDefault(a => a.IsDefault) ?? paperAccounts.FirstOrDefault();
        var recommended = ResolveRecommendedAction(paperAccounts.Count, brokers.Count);

        return ServiceResult<DemoTradingSetupStatusDto>.Success(new DemoTradingSetupStatusDto(
            paperAccounts.Count > 0,
            paperAccounts.Count,
            brokers.Count,
            defaultPaper is null ? null : PaperTradingMapper.ToAccountResponse(defaultPaper),
            brokers.Select(ToBrokerResponse).ToList(),
            recommended));
    }

    private static string ResolveRecommendedAction(int paperCount, int brokerCount)
    {
        if (paperCount == 0)
        {
            return "Create a demo paper account to start virtual trading.";
        }

        if (brokerCount == 0)
        {
            return "Optionally link a demo broker feed for market data routing.";
        }

        return "Setup complete — you can trade on your paper account.";
    }

    private async Task PromoteToTraderIfNeededAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || user.Role != UserRole.Viewer)
        {
            return;
        }

        user.Role = UserRole.Trader;
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static BrokerAccountResponse ToBrokerResponse(BrokerAccount broker) =>
        new(
            broker.Id,
            broker.Name,
            broker.BrokerName,
            broker.ExternalAccountId,
            broker.BaseCurrency,
            broker.IsLiveTradingEnabled,
            broker.Status.ToString(),
            broker.IsLiveTradingEnabled ? "Live" : "Demo",
            broker.CreatedAt);
}
