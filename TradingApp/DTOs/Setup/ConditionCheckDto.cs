namespace TradingApp.DTOs.Setup;

/// <summary>
/// Outcome of a single institutional setup condition for API/UI consumption.
/// </summary>
/// <param name="Name">Stable condition name.</param>
/// <param name="Passed">Whether the condition is satisfied.</param>
/// <param name="Detail">Human-readable explanation.</param>
public sealed record ConditionCheckDto(string Name, bool Passed, string Detail);
