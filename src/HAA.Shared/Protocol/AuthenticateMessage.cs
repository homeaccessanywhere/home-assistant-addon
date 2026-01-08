using System.Text.Json.Serialization;

namespace HAA.Shared.Protocol;

/// <summary>
/// Sent by addon to authenticate with the server
/// </summary>
public class AuthenticateMessage : TunnelMessage
{
    public override MessageType Type => MessageType.Authenticate;

    /// <summary>
    /// The connection key in format: subdomain-secret
    /// </summary>
    [JsonPropertyName("connectionKey")]
    public required string ConnectionKey { get; set; }

    /// <summary>
    /// Version of the addon for compatibility checking
    /// </summary>
    [JsonPropertyName("addonVersion")]
    public string? AddonVersion { get; set; }

    /// <summary>
    /// Home Assistant version (optional, for diagnostics)
    /// </summary>
    [JsonPropertyName("haVersion")]
    public string? HaVersion { get; set; }
}

/// <summary>
/// Response to authentication attempt
/// </summary>
public class AuthenticateResponseMessage : TunnelMessage
{
    public override MessageType Type => MessageType.AuthenticateResponse;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("subdomain")]
    public string? Subdomain { get; set; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Heartbeat interval in seconds that the addon should use
    /// </summary>
    [JsonPropertyName("heartbeatIntervalSeconds")]
    public int HeartbeatIntervalSeconds { get; set; } = 30;

    public static AuthenticateResponseMessage CreateSuccess(string subdomain)
    {
        return new AuthenticateResponseMessage
        {
            Success = true,
            Subdomain = subdomain
        };
    }

    public static AuthenticateResponseMessage CreateError(string errorCode, string errorMessage)
    {
        return new AuthenticateResponseMessage
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage
        };
    }
}
