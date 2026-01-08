using System.Text.Json;
using System.Text.Json.Serialization;

namespace HAA.Shared.Protocol;

/// <summary>
/// Base class for all tunnel messages
/// </summary>
public abstract class TunnelMessage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    [JsonPropertyName("type")]
    public abstract MessageType Type { get; }

    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, GetType(), JsonOptions);
    }

    public byte[] ToBytes()
    {
        return JsonSerializer.SerializeToUtf8Bytes(this, GetType(), JsonOptions);
    }

    public static TunnelMessage? FromJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return FromJsonDocument(doc.RootElement);
    }

    private static TunnelMessage? FromJsonDocument(JsonElement root)
    {
        if (!root.TryGetProperty("type", out var typeElement))
            return null;

        var messageType = typeElement.ValueKind == JsonValueKind.String
            ? Enum.Parse<MessageType>(typeElement.GetString()!)
            : (MessageType)typeElement.GetInt32();

        return messageType switch
        {
            MessageType.Authenticate => root.Deserialize<AuthenticateMessage>(JsonOptions),
            MessageType.AuthenticateResponse => root.Deserialize<AuthenticateResponseMessage>(JsonOptions),
            MessageType.Heartbeat => root.Deserialize<HeartbeatMessage>(JsonOptions),
            MessageType.HeartbeatAck => root.Deserialize<HeartbeatAckMessage>(JsonOptions),
            MessageType.Disconnect => root.Deserialize<DisconnectMessage>(JsonOptions),
            MessageType.HttpRequest => root.Deserialize<TunnelHttpRequest>(JsonOptions),
            MessageType.HttpResponse => root.Deserialize<TunnelHttpResponse>(JsonOptions),
            MessageType.HttpRequestAborted => root.Deserialize<HttpRequestAbortedMessage>(JsonOptions),
            MessageType.WebSocketOpen => root.Deserialize<WebSocketOpenMessage>(JsonOptions),
            MessageType.WebSocketOpenResponse => root.Deserialize<WebSocketOpenResponseMessage>(JsonOptions),
            MessageType.WebSocketData => root.Deserialize<WebSocketDataMessage>(JsonOptions),
            MessageType.WebSocketClose => root.Deserialize<WebSocketCloseMessage>(JsonOptions),
            MessageType.Error => root.Deserialize<ErrorMessage>(JsonOptions),
            _ => null
        };
    }

    public static TunnelMessage? FromBytes(ReadOnlySpan<byte> bytes)
    {
        var reader = new Utf8JsonReader(bytes);
        using var doc = JsonDocument.ParseValue(ref reader);
        return FromJsonDocument(doc.RootElement);
    }
}
