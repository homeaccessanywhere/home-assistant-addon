namespace HAA.Shared.Models;

/// <summary>
/// Result of a user registration attempt
/// </summary>
public class RegistrationResult
{
    public bool Success { get; set; }
    public string? Subdomain { get; set; }
    public string? ConnectionKey { get; set; }
    public string? FullUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }

    public static RegistrationResult Succeeded(string subdomain, string connectionKey, string baseDomain)
    {
        return new RegistrationResult
        {
            Success = true,
            Subdomain = subdomain,
            ConnectionKey = connectionKey,
            FullUrl = $"https://{subdomain}.{baseDomain}"
        };
    }

    public static RegistrationResult Failed(string errorCode, string errorMessage)
    {
        return new RegistrationResult
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Common error codes for registration
/// </summary>
public static class RegistrationErrorCodes
{
    public const string SubdomainTaken = "SUBDOMAIN_TAKEN";
    public const string SubdomainInvalid = "SUBDOMAIN_INVALID";
    public const string SubdomainReserved = "SUBDOMAIN_RESERVED";
    public const string EmailTaken = "EMAIL_TAKEN";
    public const string EmailInvalid = "EMAIL_INVALID";
    public const string PasswordTooWeak = "PASSWORD_TOO_WEAK";
    public const string ServerError = "SERVER_ERROR";
    public const string EmailNotVerified = "EMAIL_NOT_VERIFIED";
    public const string InvalidToken = "INVALID_TOKEN";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string RateLimited = "RATE_LIMITED";
}
