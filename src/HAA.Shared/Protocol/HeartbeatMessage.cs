using System.Text.Json.Serialization;

namespace HAA.Shared.Protocol;

/// <summary>
/// Heartbeat message to keep connection alive and detect disconnections
/// </summary>
public class HeartbeatMessage : TunnelMessage
{
    public override MessageType Type => MessageType.Heartbeat;

    /// <summary>
    /// Sequence number for tracking missed heartbeats
    /// </summary>
    [JsonPropertyName("sequence")]
    public long Sequence { get; set; }
}

/// <summary>
/// Acknowledgment of heartbeat
/// </summary>
public class HeartbeatAckMessage : TunnelMessage
{
    public override MessageType Type => MessageType.HeartbeatAck;

    /// <summary>
    /// Sequence number being acknowledged
    /// </summary>
    [JsonPropertyName("sequence")]
    public long Sequence { get; set; }

    /// <summary>
    /// Server timestamp for latency calculation
    /// </summary>
    [JsonPropertyName("serverTime")]
    public DateTimeOffset ServerTime { get; set; } = DateTimeOffset.UtcNow;
}
