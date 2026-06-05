namespace TradingApp.Entities.Enums;

public enum AuditAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    Login = 3,
    Logout = 4,
    OrderPlaced = 5,
    OrderCancelled = 6,
    OrderFilled = 7,
    SignalGenerated = 8,
    BacktestStarted = 9,
    BacktestCompleted = 10,
    AccountStatusChanged = 11
}
