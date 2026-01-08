using System.Text.Json.Serialization;

namespace HAA.Shared.Protocol;

/// <summary>
/// Request to open a WebSocket connection to Home Assistant
/// </summary>
public class WebSocketOpenMessage : TunnelMessage
{
    public override MessageType Type => MessageType.WebSocketOpen;

    /// <summary>
    /// Unique ID for this WebSocket connection
    /// </summary>
    [JsonPropertyName("connectionId")]
    public required string ConnectionId { get; set; }

    /// <summary>
    /// The path to connect to (e.g., "/api/websocket")
    /// </summary>
    [JsonPropertyName("path")]
    public required string Path { get; set; }

    /// <summary>
    /// HTTP headers from the original WebSocket upgrade request
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string[]> Headers { get; set; } = new();
}

/// <summary>
/// Response to a WebSocket open request
/// </summary>
public class WebSocketOpenResponseMessage : TunnelMessage
{
    public override MessageType Type => MessageType.WebSocketOpenResponse;

    [JsonPropertyName("connectionId")]
    public required string ConnectionId { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// WebSocket data frame
/// </summary>
public class WebSocketDataMessage : TunnelMessage
{
    public override MessageType Type => MessageType.WebSocketData;

    [JsonPropertyName("connectionId")]
    public required string ConnectionId { get; set; }

    /// <summary>
    /// Data as base64 encoded string
    /// </summary>
    [JsonPropertyName("dataBase64")]
    public string? DataBase64 { get; set; }

    /// <summary>
    /// Whether this is a text or binary message
    /// </summary>
    [JsonPropertyName("isText")]
    public bool IsText { get; set; } = true;

    /// <summary>
    /// Whether this is the final frame of a message
    /// </summary>
    [JsonPropertyName("endOfMessage")]
    public bool EndOfMessage { get; set; } = true;

    public void SetData(byte[] data)
    {
        DataBase64 = Convert.ToBase64String(data);
    }

    public void SetData(ReadOnlySpan<byte> data)
    {
        DataBase64 = Convert.ToBase64String(data);
    }

    public void SetData(string text)
    {
        DataBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(text));
    }

    public byte[]? GetDataBytes()
    {
        return string.IsNullOrEmpty(DataBase64) ? null : Convert.FromBase64String(DataBase64);
    }

    public string? GetDataString()
    {
        var bytes = GetDataBytes();
        return bytes == null ? null : System.Text.Encoding.UTF8.GetString(bytes);
    }
}

/// <summary>
/// Request to close a WebSocket connection
/// </summary>
public class WebSocketCloseMessage : TunnelMessage
{
    public override MessageType Type => MessageType.WebSocketClose;

    [JsonPropertyName("connectionId")]
    public required string ConnectionId { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; } = 1000; // Normal closure

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
