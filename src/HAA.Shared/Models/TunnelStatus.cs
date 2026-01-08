namespace HAA.Shared.Models;

/// <summary>
/// Status of a tunnel connection
/// </summary>
public enum TunnelConnectionStatus
{
    /// <summary>
    /// No addon is connected
    /// </summary>
    Offline = 0,

    /// <summary>
    /// Addon is connecting/authenticating
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// Addon is connected and ready
    /// </summary>
    Online = 2,

    /// <summary>
    /// Connection issue detected (missed heartbeats)
    /// </summary>
    Unstable = 3
}

/// <summary>
/// Information about a tunnel's current status
/// </summary>
public class TunnelStatusInfo
{
    public required string Subdomain { get; set; }
    public TunnelConnectionStatus Status { get; set; }
    public DateTimeOffset? LastConnected { get; set; }
    public DateTimeOffset? LastActivity { get; set; }
    public string? AddonVersion { get; set; }
    public string? HomeAssistantVersion { get; set; }
    public string? ClientIpAddress { get; set; }
    public long TotalRequestsHandled { get; set; }
    public TimeSpan? Uptime { get; set; }
    public int? LatencyMs { get; set; }
}
