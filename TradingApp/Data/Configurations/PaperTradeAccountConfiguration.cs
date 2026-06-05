using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingApp.Entities;

namespace TradingApp.Data.Configurations;

public sealed class PaperTradeAccountConfiguration : IEntityTypeConfiguration<PaperTradeAccount>
{
    public void Configure(EntityTypeBuilder<PaperTradeAccount> builder)
    {
        builder.ToTable("paper_trade_accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.BaseCurrency)
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(a => a.InitialBalance)
            .HasPrecision(18, 4);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();

        builder.HasIndex(a => new { a.UserId, a.Name })
            .IsUnique();

        builder.HasIndex(a => new { a.UserId, a.IsDefault })
            .IsUnique()
            .HasFilter("\"IsDefault\" = true");

        builder.HasIndex(a => a.UserId);

        builder.HasOne(a => a.User)
            .WithMany(u => u.PaperTradeAccounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Portfolio)
            .WithOne(p => p.PaperTradeAccount)
            .HasForeignKey<Portfolio>(p => p.PaperTradeAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Orders)
            .WithOne(o => o.PaperTradeAccount)
            .HasForeignKey(o => o.PaperTradeAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
