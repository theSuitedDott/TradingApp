using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingApp.Entities;
using TradingApp.Entities.Enums;

namespace TradingApp.Data.Configurations;

public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("positions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Symbol)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.Exchange)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.Isin)
            .HasMaxLength(12);

        builder.Property(p => p.Side)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(p => p.Quantity).HasPrecision(18, 8);
        builder.Property(p => p.AverageEntryPrice).HasPrecision(18, 6);
        builder.Property(p => p.CurrentPrice).HasPrecision(18, 6);
        builder.Property(p => p.StopLossPrice).HasPrecision(18, 6);
        builder.Property(p => p.TakeProfitPrice).HasPrecision(18, 6);
        builder.Property(p => p.UnrealizedPnL).HasPrecision(18, 4);
        builder.Property(p => p.RealizedPnL).HasPrecision(18, 4);

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(p => p.OpenedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => new { p.PortfolioId, p.Symbol, p.Exchange, p.Side })
            .IsUnique()
            .HasFilter($"\"Status\" = '{PositionStatus.Open}'");

        builder.HasIndex(p => new { p.PortfolioId, p.Status });

        builder.HasIndex(p => new { p.Symbol, p.Exchange });
    }
}
