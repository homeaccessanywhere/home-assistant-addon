using System.Text.Json.Serialization;

namespace HAA.Shared.Protocol;

/// <summary>
/// Graceful disconnect notification
/// </summary>
public class DisconnectMessage : TunnelMessage
{
    public override MessageType Type => MessageType.Disconnect;

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// Indicates if the addon should attempt to reconnect
    /// </summary>
    [JsonPropertyName("shouldReconnect")]
    public bool ShouldReconnect { get; set; }

    /// <summary>
    /// Delay in seconds before reconnecting (if applicable)
    /// </summary>
    [JsonPropertyName("reconnectDelaySeconds")]
    public int ReconnectDelaySeconds { get; set; } = 5;
}

/// <summary>
/// Error message for protocol or processing errors
/// </summary>
public class ErrorMessage : TunnelMessage
{
    public override MessageType Type => MessageType.Error;

    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("message")]
    public required string Message { get; set; }

    /// <summary>
    /// Optional reference to the message that caused the error
    /// </summary>
    [JsonPropertyName("relatedMessageId")]
    public string? RelatedMessageId { get; set; }

    /// <summary>
    /// Whether this error is fatal and connection will be closed
    /// </summary>
    [JsonPropertyName("fatal")]
    public bool Fatal { get; set; }

    public static ErrorMessage Create(string code, string message, bool fatal = false, string? relatedMessageId = null)
    {
        return new ErrorMessage
        {
            Code = code,
            Message = message,
            Fatal = fatal,
            RelatedMessageId = relatedMessageId
        };
    }
}
