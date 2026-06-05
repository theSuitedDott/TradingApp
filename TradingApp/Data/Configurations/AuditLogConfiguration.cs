using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingApp.Entities;

namespace TradingApp.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Action)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.EntityType)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasMaxLength(64);

        builder.Property(a => a.ChangesJson)
            .HasColumnType("jsonb");

        builder.Property(a => a.IpAddress)
            .HasMaxLength(64);

        builder.Property(a => a.UserAgent)
            .HasMaxLength(512);

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(64);

        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => new { a.UserId, a.CreatedAt });
        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.CreatedAt });
        builder.HasIndex(a => a.CorrelationId);

        builder.HasOne(a => a.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
