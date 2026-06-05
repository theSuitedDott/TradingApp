using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingApp.Entities;

namespace TradingApp.Data.Configurations;

public sealed class BacktestConfiguration : IEntityTypeConfiguration<Backtest>
{
    public void Configure(EntityTypeBuilder<Backtest> builder)
    {
        builder.ToTable("backtests");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(b => b.InitialCapital).HasPrecision(18, 4);

        builder.Property(b => b.BaseCurrency)
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(b => b.StrategySnapshotJson)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(b => b.ResultsJson)
            .HasColumnType("jsonb");

        builder.Property(b => b.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(b => b.CreatedAt).IsRequired();

        builder.HasIndex(b => new { b.StrategyId, b.Status, b.CreatedAt });
        builder.HasIndex(b => new { b.UserId, b.CreatedAt });

        builder.HasOne(b => b.Strategy)
            .WithMany(s => s.Backtests)
            .HasForeignKey(b => b.StrategyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.User)
            .WithMany(u => u.Backtests)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
