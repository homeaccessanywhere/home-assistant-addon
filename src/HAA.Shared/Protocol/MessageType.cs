namespace HAA.Shared.Protocol;

/// <summary>
/// Types of messages exchanged between server and addon
/// </summary>
public enum MessageType
{
    // Connection lifecycle
    Authenticate = 1,
    AuthenticateResponse = 2,
    Heartbeat = 10,
    HeartbeatAck = 11,
    Disconnect = 20,

    // HTTP tunneling
    HttpRequest = 100,
    HttpResponse = 101,
    HttpRequestAborted = 102,

    // WebSocket tunneling
    WebSocketOpen = 110,
    WebSocketOpenResponse = 111,
    WebSocketData = 112,
    WebSocketClose = 113,

    // Errors
    Error = 200
}
