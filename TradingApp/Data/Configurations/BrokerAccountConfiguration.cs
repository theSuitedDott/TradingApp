using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingApp.Entities;

namespace TradingApp.Data.Configurations;

public sealed class BrokerAccountConfiguration : IEntityTypeConfiguration<BrokerAccount>
{
    public void Configure(EntityTypeBuilder<BrokerAccount> builder)
    {
        builder.ToTable("broker_accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.BrokerName)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.ExternalAccountId)
            .HasMaxLength(128);

        builder.Property(a => a.BaseCurrency)
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.CreatedAt).IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();

        builder.HasIndex(a => new { a.UserId, a.Name })
            .IsUnique();

        builder.HasIndex(a => new { a.UserId, a.BrokerName, a.ExternalAccountId });

        builder.HasIndex(a => a.UserId);

        builder.HasOne(a => a.User)
            .WithMany(u => u.BrokerAccounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Portfolio)
            .WithOne(p => p.BrokerAccount)
            .HasForeignKey<Portfolio>(p => p.BrokerAccountId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Orders)
            .WithOne(o => o.BrokerAccount)
            .HasForeignKey(o => o.BrokerAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
