using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace HAA.Shared.Utilities;

/// <summary>
/// Helper class for generating and validating connection keys
/// Format: subdomain-secret (e.g., "myhouse-a8f3b2c19d4e4f5a")
/// </summary>
public static partial class ConnectionKeyHelper
{
    private const int SecretLength = 32; // 32 hex characters = 128 bits of entropy
    private const int MinSubdomainLength = 3;
    private const int MaxSubdomainLength = 32;

    [GeneratedRegex(@"^[a-z0-9][a-z0-9\-]*[a-z0-9]$|^[a-z0-9]$", RegexOptions.Compiled)]
    private static partial Regex SubdomainRegex();

    /// <summary>
    /// Generates a new connection key for a given subdomain
    /// </summary>
    public static string GenerateKey(string subdomain)
    {
        ValidateSubdomain(subdomain);
        var secret = GenerateSecret();
        return $"{subdomain.ToLowerInvariant()}-{secret}";
    }

    /// <summary>
    /// Generates a cryptographically secure secret
    /// </summary>
    public static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(SecretLength / 2);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Parses a connection key into subdomain and secret
    /// </summary>
    public static (string Subdomain, string Secret)? ParseKey(string connectionKey)
    {
        if (string.IsNullOrWhiteSpace(connectionKey))
            return null;

        var lastDashIndex = connectionKey.LastIndexOf('-');
        if (lastDashIndex <= 0 || lastDashIndex >= connectionKey.Length - 1)
            return null;

        var subdomain = connectionKey[..lastDashIndex];
        var secret = connectionKey[(lastDashIndex + 1)..];

        if (!IsValidSubdomain(subdomain) || string.IsNullOrEmpty(secret))
            return null;

        return (subdomain.ToLowerInvariant(), secret.ToLowerInvariant());
    }

    /// <summary>
    /// Validates if a subdomain meets all requirements
    /// </summary>
    public static bool IsValidSubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            return false;

        if (subdomain.Length < MinSubdomainLength || subdomain.Length > MaxSubdomainLength)
            return false;

        // Must match pattern: alphanumeric, can contain hyphens but not at start/end
        if (!SubdomainRegex().IsMatch(subdomain.ToLowerInvariant()))
            return false;

        // No consecutive hyphens
        if (subdomain.Contains("--"))
            return false;

        return true;
    }

    /// <summary>
    /// Validates subdomain and throws if invalid
    /// </summary>
    public static void ValidateSubdomain(string subdomain)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            throw new ArgumentException("Subdomain cannot be empty", nameof(subdomain));

        if (subdomain.Length < MinSubdomainLength)
            throw new ArgumentException($"Subdomain must be at least {MinSubdomainLength} characters", nameof(subdomain));

        if (subdomain.Length > MaxSubdomainLength)
            throw new ArgumentException($"Subdomain cannot exceed {MaxSubdomainLength} characters", nameof(subdomain));

        if (!SubdomainRegex().IsMatch(subdomain.ToLowerInvariant()))
            throw new ArgumentException("Subdomain can only contain lowercase letters, numbers, and hyphens (not at start/end)", nameof(subdomain));

        if (subdomain.Contains("--"))
            throw new ArgumentException("Subdomain cannot contain consecutive hyphens", nameof(subdomain));
    }

    /// <summary>
    /// Gets validation errors for a subdomain (returns null if valid)
    /// </summary>
    public static string? GetSubdomainValidationError(string subdomain)
    {
        try
        {
            ValidateSubdomain(subdomain);
            return null;
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
    }

    /// <summary>
    /// List of reserved subdomains that cannot be registered
    /// </summary>
    private static readonly HashSet<string> ReservedSubdomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "www", "api", "admin", "app", "mail", "smtp", "pop", "imap",
        "ftp", "ssh", "test", "dev", "staging", "prod", "production",
        "localhost", "local", "homeassistant", "hass", "home", "ha",
        "support", "help", "status", "blog", "docs", "documentation"
    };

    /// <summary>
    /// Checks if a subdomain is reserved
    /// </summary>
    public static bool IsReservedSubdomain(string subdomain)
    {
        return ReservedSubdomains.Contains(subdomain.ToLowerInvariant());
    }

    /// <summary>
    /// Hashes a secret using SHA256 for storage
    /// </summary>
    public static string HashSecret(string secret)
    {
        var bytes = Encoding.UTF8.GetBytes(secret.ToLowerInvariant());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
