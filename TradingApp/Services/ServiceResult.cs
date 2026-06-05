namespace TradingApp.Services;

public sealed class ServiceResult<T>
{
    public bool IsSuccess { get; init; }

    public T? Value { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static ServiceResult<T> Success(T value) => new()
    {
        IsSuccess = true,
        Value = value
    };

    public static ServiceResult<T> Failure(string errorCode, string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}

public static class AuthErrorCodes
{
    public const string InvalidCredentials = "invalid_credentials";
    public const string EmailAlreadyExists = "email_already_exists";
    public const string UserInactive = "user_inactive";
    public const string InvalidRefreshToken = "invalid_refresh_token";
    public const string UserNotFound = "user_not_found";
}
