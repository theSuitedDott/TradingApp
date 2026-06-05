namespace TradingApp.TradingEngine.Setup;

/// <summary>
/// Outcome of a single institutional setup condition.
/// </summary>
public sealed class ConditionCheck
{
    /// <summary>
    /// Creates a condition check result.
    /// </summary>
    /// <param name="name">Stable condition name.</param>
    /// <param name="passed">Whether the condition is satisfied.</param>
    /// <param name="detail">Human-readable explanation.</param>
    public ConditionCheck(string name, bool passed, string detail)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);

        Name = name;
        Passed = passed;
        Detail = detail;
    }

    /// <summary>Stable condition name.</summary>
    public string Name { get; }

    /// <summary>Whether the condition is satisfied.</summary>
    public bool Passed { get; }

    /// <summary>Human-readable explanation.</summary>
    public string Detail { get; }
}
