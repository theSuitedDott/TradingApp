namespace TradingApp.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Trader = "Trader";
    public const string Viewer = "Viewer";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Admin,
        Trader,
        Viewer
    };
}
