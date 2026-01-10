using System.Text.Json.Serialization;

namespace HAA.Shared.Protocol;

/// <summary>
/// HTTP request to be forwarded to Home Assistant
/// </summary>
public class TunnelHttpRequest : TunnelMessage
{
    public override MessageType Type => MessageType.HttpRequest;

    /// <summary>
    /// Unique ID for this request, used to correlate with response
    /// </summary>
    [JsonPropertyName("requestId")]
    public required string RequestId { get; set; }

    [JsonPropertyName("method")]
    public required string Method { get; set; }

    /// <summary>
    /// The path and query string (e.g., "/api/states?filter=light")
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; set; }

    /// <summary>
    /// HTTP headers as dictionary
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string[]> Headers { get; set; } = new();

    /// <summary>
    /// Request body as base64 encoded string (for binary safety)
    /// </summary>
    [JsonPropertyName("bodyBase64")]
    public string? BodyBase64 { get; set; }

    /// <summary>
    /// Timeout in seconds for this request
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 30;

    public void SetBody(byte[] body)
    {
        BodyBase64 = Convert.ToBase64String(body);
    }

    public void SetBody(string body)
    {
        BodyBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(body));
    }

    public byte[]? GetBodyBytes()
    {
        return string.IsNullOrEmpty(BodyBase64) ? null : Convert.FromBase64String(BodyBase64);
    }
}

/// <summary>
/// HTTP response from Home Assistant
/// </summary>
public class TunnelHttpResponse : TunnelMessage
{
    public override MessageType Type => MessageType.HttpResponse;

    /// <summary>
    /// The request ID this is responding to
    /// </summary>
    [JsonPropertyName("requestId")]
    public required string RequestId { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("reasonPhrase")]
    public string? ReasonPhrase { get; set; }

    /// <summary>
    /// Response headers
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string[]> Headers { get; set; } = new();

    /// <summary>
    /// Response body as base64 encoded string
    /// </summary>
    [JsonPropertyName("bodyBase64")]
    public string? BodyBase64 { get; set; }

    /// <summary>
    /// Processing time in milliseconds (for diagnostics)
    /// </summary>
    [JsonPropertyName("processingTimeMs")]
    public long ProcessingTimeMs { get; set; }

    /// <summary>
    /// True if this response has more chunks coming (for large responses)
    /// </summary>
    [JsonPropertyName("hasMoreChunks")]
    public bool HasMoreChunks { get; set; }

    public void SetBody(byte[] body)
    {
        BodyBase64 = Convert.ToBase64String(body);
    }

    public void SetBody(ReadOnlySpan<byte> body)
    {
        BodyBase64 = Convert.ToBase64String(body);
    }

    public byte[]? GetBodyBytes()
    {
        return string.IsNullOrEmpty(BodyBase64) ? null : Convert.FromBase64String(BodyBase64);
    }

    public static TunnelHttpResponse CreateError(string requestId, int statusCode, string message)
    {
        return new TunnelHttpResponse
        {
            RequestId = requestId,
            StatusCode = statusCode,
            ReasonPhrase = message
        };
    }
}

/// <summary>
/// Notification that an HTTP request was aborted (e.g., client disconnected)
/// </summary>
public class HttpRequestAbortedMessage : TunnelMessage
{
    public override MessageType Type => MessageType.HttpRequestAborted;

    [JsonPropertyName("requestId")]
    public required string RequestId { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
