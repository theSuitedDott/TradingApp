using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingApp.Entities;

namespace TradingApp.Data.Configurations;

public sealed class MarketQuoteConfiguration : IEntityTypeConfiguration<MarketQuote>
{
    public void Configure(EntityTypeBuilder<MarketQuote> builder)
    {
        builder.ToTable("market_quotes");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Symbol)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(q => q.Exchange)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(q => q.Price)
            .HasPrecision(18, 6);

        builder.Property(q => q.Timestamp).IsRequired();
        builder.Property(q => q.UpdatedAt).IsRequired();

        builder.HasIndex(q => new { q.Symbol, q.Exchange })
            .IsUnique();
    }
}
