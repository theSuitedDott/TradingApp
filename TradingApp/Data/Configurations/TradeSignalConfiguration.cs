using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingApp.Entities;

namespace TradingApp.Data.Configurations;

public sealed class TradeSignalConfiguration : IEntityTypeConfiguration<TradeSignal>
{
    public void Configure(EntityTypeBuilder<TradeSignal> builder)
    {
        builder.ToTable("trade_signals");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Symbol)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(s => s.Exchange)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(s => s.SignalType)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(s => s.Confidence).HasPrecision(5, 4);

        builder.Property(s => s.Rationale)
            .HasMaxLength(4000);

        builder.Property(s => s.MetadataJson)
            .HasColumnType("jsonb");

        builder.Property(s => s.GeneratedAt).IsRequired();

        builder.HasIndex(s => new { s.StrategyId, s.Status, s.GeneratedAt });
        builder.HasIndex(s => new { s.UserId, s.GeneratedAt });
        builder.HasIndex(s => new { s.Symbol, s.Exchange, s.Status });

        builder.HasOne(s => s.Strategy)
            .WithMany(st => st.TradeSignals)
            .HasForeignKey(s => s.StrategyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.User)
            .WithMany(u => u.TradeSignals)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
