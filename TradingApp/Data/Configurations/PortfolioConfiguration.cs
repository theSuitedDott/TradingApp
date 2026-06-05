using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingApp.Entities;

namespace TradingApp.Data.Configurations;

public sealed class PortfolioConfiguration : IEntityTypeConfiguration<Portfolio>
{
    public void Configure(EntityTypeBuilder<Portfolio> builder)
    {
        builder.ToTable("portfolios", t =>
            t.HasCheckConstraint(
                "CK_portfolios_single_account",
                "(\"PaperTradeAccountId\" IS NOT NULL AND \"BrokerAccountId\" IS NULL) OR " +
                "(\"PaperTradeAccountId\" IS NULL AND \"BrokerAccountId\" IS NOT NULL)"));

        builder.HasKey(p => p.Id);

        builder.Property(p => p.CashBalance).HasPrecision(18, 4);
        builder.Property(p => p.ReservedCash).HasPrecision(18, 4);
        builder.Property(p => p.TotalEquity).HasPrecision(18, 4);

        builder.Property(p => p.BaseCurrency)
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => p.PaperTradeAccountId)
            .IsUnique();

        builder.HasIndex(p => p.BrokerAccountId)
            .IsUnique();

        builder.HasMany(p => p.Positions)
            .WithOne(pos => pos.Portfolio)
            .HasForeignKey(pos => pos.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
