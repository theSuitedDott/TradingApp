using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingApp.Entities;

namespace TradingApp.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", t =>
            t.HasCheckConstraint(
                "CK_orders_single_account",
                "(\"PaperTradeAccountId\" IS NOT NULL AND \"BrokerAccountId\" IS NULL) OR " +
                "(\"PaperTradeAccountId\" IS NULL AND \"BrokerAccountId\" IS NOT NULL)"));

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Symbol)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(o => o.Exchange)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(o => o.Side)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(o => o.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(o => o.Quantity).HasPrecision(18, 8);
        builder.Property(o => o.FilledQuantity).HasPrecision(18, 8);
        builder.Property(o => o.LimitPrice).HasPrecision(18, 6);
        builder.Property(o => o.StopPrice).HasPrecision(18, 6);
        builder.Property(o => o.AverageFillPrice).HasPrecision(18, 6);
        builder.Property(o => o.Commission).HasPrecision(18, 4);

        builder.Property(o => o.RejectReason)
            .HasMaxLength(512);

        builder.Property(o => o.ClientOrderId)
            .HasMaxLength(64);

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();

        builder.HasIndex(o => new { o.PaperTradeAccountId, o.Status, o.CreatedAt });
        builder.HasIndex(o => new { o.BrokerAccountId, o.Status, o.CreatedAt });
        builder.HasIndex(o => new { o.Symbol, o.Exchange, o.CreatedAt });
        builder.HasIndex(o => o.TradeSignalId);
        builder.HasIndex(o => o.StrategyId);

        builder.HasIndex(o => o.ClientOrderId)
            .IsUnique()
            .HasFilter("\"ClientOrderId\" IS NOT NULL");

        builder.HasOne(o => o.Strategy)
            .WithMany(s => s.Orders)
            .HasForeignKey(o => o.StrategyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.TradeSignal)
            .WithMany(s => s.Orders)
            .HasForeignKey(o => o.TradeSignalId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
