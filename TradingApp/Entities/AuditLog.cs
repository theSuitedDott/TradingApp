using TradingApp.Entities.Enums;

namespace TradingApp.Entities;

/// <summary>
/// Immutable audit trail. Append-only at application level.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public User? User { get; set; }

    public AuditAction Action { get; set; }

    public required string EntityType { get; set; }

    public string? EntityId { get; set; }

    public string? ChangesJson { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? CorrelationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
