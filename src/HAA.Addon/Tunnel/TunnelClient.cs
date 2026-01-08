using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using HAA.Shared;
using HAA.Shared.Protocol;

namespace HAA.Addon.Tunnel;

/// <summary>
/// Client that maintains a tunnel connection to the server
/// </summary>
public class TunnelClient : IAsyncDisposable
{
    private readonly string _serverUrl;
    private readonly string _connectionKey;
    private readonly string _homeAssistantUrl;
    private readonly HttpClient _haHttpClient;
    private readonly ILogger<TunnelClient> _logger;
    private readonly ConcurrentDictionary<string, ClientWebSocket> _activeWebSockets = new();

    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;
    private Task? _heartbeatTask;
    private int _reconnectAttempts;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;
    public string? Subdomain { get; private set; }

    public TunnelClient(
        string serverUrl,
        string connectionKey,
        string homeAssistantUrl,
        ILogger<TunnelClient> logger)
    {
        _serverUrl = serverUrl.TrimEnd('/');
        _connectionKey = connectionKey;
        _homeAssistantUrl = homeAssistantUrl.TrimEnd('/');
        _logger = logger;

        _haHttpClient = new HttpClient
        {
            BaseAddress = new Uri(_homeAssistantUrl),
            Timeout = TimeSpan.FromSeconds(Constants.DefaultRequestTimeoutSeconds)
        };
    }

    /// <summary>
    /// Connects to the server and maintains the connection
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await ConnectAndRunAsync(_cts.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Tunnel client shutting down");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection error, will retry...");
            }

            if (!_cts.Token.IsCancellationRequested)
            {
                var delay = CalculateReconnectDelay();
                _logger.LogInformation("Reconnecting in {Delay} seconds...", delay.TotalSeconds);
                await Task.Delay(delay, _cts.Token);
                _reconnectAttempts++;
            }
        }
    }

    private TimeSpan CalculateReconnectDelay()
    {
        // Exponential backoff with jitter
        var baseDelay = Math.Min(
            Constants.InitialReconnectDelaySeconds * Math.Pow(2, _reconnectAttempts),
            Constants.MaxReconnectDelaySeconds);

        var jitter = Random.Shared.NextDouble() * 0.3 * baseDelay;
        return TimeSpan.FromSeconds(baseDelay + jitter);
    }

    private async Task ConnectAndRunAsync(CancellationToken cancellationToken)
    {
        _webSocket = new ClientWebSocket();
        _webSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

        var wsUrl = $"{_serverUrl}{Constants.TunnelWebSocketPath}";

        _logger.LogInformation("Connecting to {Url}", wsUrl);

        await _webSocket.ConnectAsync(new Uri(wsUrl), cancellationToken);
        _logger.LogInformation("WebSocket connected, authenticating...");

        // Send authentication
        if (!await AuthenticateAsync(cancellationToken))
        {
            throw new Exception("Authentication failed");
        }

        _reconnectAttempts = 0; // Reset on successful connection
        _logger.LogInformation("Authenticated as {Subdomain}, tunnel is active", Subdomain);

        // Start heartbeat
        _heartbeatTask = RunHeartbeatLoopAsync(cancellationToken);

        // Run receive loop (blocking until disconnected)
        await RunReceiveLoopAsync(cancellationToken);
    }

    private async Task<bool> AuthenticateAsync(CancellationToken cancellationToken)
    {
        var authMessage = new AuthenticateMessage
        {
            ConnectionKey = _connectionKey,
            AddonVersion = Constants.Version,
            HaVersion = await GetHomeAssistantVersionAsync()
        };

        await SendMessageAsync(authMessage, cancellationToken);

        // Wait for response
        var response = await ReceiveMessageAsync(cancellationToken);

        if (response is AuthenticateResponseMessage authResponse)
        {
            if (authResponse.Success)
            {
                Subdomain = authResponse.Subdomain;
                return true;
            }

            _logger.LogError("Authentication failed: {Code} - {Message}",
                authResponse.ErrorCode, authResponse.ErrorMessage);
            return false;
        }

        _logger.LogError("Unexpected response during authentication: {Type}", response?.Type);
        return false;
    }

    private async Task<string?> GetHomeAssistantVersionAsync()
    {
        try
        {
            var response = await _haHttpClient.GetStringAsync("/api/config");
            // Parse version from response if needed
            return null; // Simplified
        }
        catch
        {
            return null;
        }
    }

    private async Task RunReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[Constants.MaxWebSocketMessageSize];

        try
        {
            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await _webSocket!.ReceiveAsync(buffer, cancellationToken);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation("Server closed connection");
                        return;
                    }

                    ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    // Use GetBuffer() to avoid allocation, parse directly from bytes
                    var message = TunnelMessage.FromBytes(ms.GetBuffer().AsSpan(0, (int)ms.Length));
                    if (message != null)
                    {
                        _ = HandleMessageAsync(message, cancellationToken);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse message ({Length} bytes)", ms.Length);
                    }
                }
            }
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            _logger.LogWarning("Connection closed prematurely");
        }
    }

    private async Task HandleMessageAsync(TunnelMessage message, CancellationToken cancellationToken)
    {
        try
        {
            switch (message)
            {
                case TunnelHttpRequest request:
                    await HandleHttpRequestAsync(request, cancellationToken);
                    break;

                case HeartbeatMessage heartbeat:
                    await HandleHeartbeatAsync(heartbeat, cancellationToken);
                    break;

                case DisconnectMessage disconnect:
                    _logger.LogInformation("Server requested disconnect: {Reason}", disconnect.Reason);
                    await _cts!.CancelAsync();
                    break;

                case WebSocketOpenMessage wsOpen:
                    _ = HandleWebSocketOpenAsync(wsOpen, cancellationToken);
                    break;

                case WebSocketDataMessage wsData:
                    await HandleWebSocketDataAsync(wsData, cancellationToken);
                    break;

                case WebSocketCloseMessage wsClose:
                    await HandleWebSocketCloseAsync(wsClose, cancellationToken);
                    break;

                default:
                    _logger.LogDebug("Received message type {Type}", message.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message");
        }
    }

    private async Task HandleHttpRequestAsync(TunnelHttpRequest request, CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            // Build HTTP request to Home Assistant
            using var haRequest = new HttpRequestMessage(new HttpMethod(request.Method), request.Path);

            // Copy body as-is
            var body = request.GetBodyBytes();
            if (body != null && body.Length > 0)
            {
                haRequest.Content = new ByteArrayContent(body);
            }

            // Copy headers - always to request headers, content-type to content if we have content
            foreach (var (key, values) in request.Headers)
            {
                if (IsRestrictedHeader(key))
                    continue;

                // Content-Type should go to content headers if we have content
                if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) && haRequest.Content != null)
                {
                    haRequest.Content.Headers.TryAddWithoutValidation(key, values);
                }
                else
                {
                    haRequest.Headers.TryAddWithoutValidation(key, values);
                }
            }

            // Send to Home Assistant - use ResponseHeadersRead to avoid waiting for streaming responses
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Check if this is a streaming endpoint (logs, events, etc.)
            var isStreamingEndpoint = request.Path.Contains("/logs") ||
                                      request.Path.Contains("/stream") ||
                                      request.Path.Contains("/events");

            // Use longer timeout for streaming endpoints, but not infinite
            var timeout = isStreamingEndpoint ? 120 : request.TimeoutSeconds;
            cts.CancelAfter(TimeSpan.FromSeconds(timeout));

            using var haResponse = await _haHttpClient.SendAsync(
                haRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            // Build tunnel response
            var response = new TunnelHttpResponse
            {
                RequestId = request.RequestId,
                StatusCode = (int)haResponse.StatusCode,
                ReasonPhrase = haResponse.ReasonPhrase,
                ProcessingTimeMs = (long)(DateTimeOffset.UtcNow - startTime).TotalMilliseconds
            };

            // Copy response headers as-is
            foreach (var header in haResponse.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }
            foreach (var header in haResponse.Content.Headers)
            {
                response.Headers[header.Key] = header.Value.ToArray();
            }

            // Copy response body - for streaming endpoints, read available data with timeout
            byte[] responseBody;
            if (isStreamingEndpoint)
            {
                // For streaming endpoints, read chunks until timeout
                using var ms = new MemoryStream();
                var buffer = new byte[8192];
                var streamReadTimeout = TimeSpan.FromSeconds(3);
                var readStartTime = DateTime.UtcNow;

                try
                {
                    await using var stream = await haResponse.Content.ReadAsStreamAsync(cts.Token);

                    // Reuse CTS across iterations to avoid allocations
                    using var readCts = new CancellationTokenSource();

                    while (DateTime.UtcNow - readStartTime < streamReadTimeout && !cts.Token.IsCancellationRequested)
                    {
                        readCts.CancelAfter(TimeSpan.FromMilliseconds(500));

                        try
                        {
                            var bytesRead = await stream.ReadAsync(buffer, readCts.Token);
                            if (bytesRead == 0)
                                break; // End of stream

                            ms.Write(buffer, 0, bytesRead);

                            // Reset for next iteration
                            if (!readCts.TryReset())
                                break; // CTS exhausted, stop reading
                        }
                        catch (OperationCanceledException) when (readCts.IsCancellationRequested && !cts.Token.IsCancellationRequested)
                        {
                            // Read timeout - check if we have data
                            if (ms.Length > 0)
                                break; // We have some data, return it

                            // No data yet, try to reset and continue waiting
                            if (!readCts.TryReset())
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading streaming response for {Path}", request.Path);
                }

                responseBody = ms.ToArray();
            }
            else
            {
                responseBody = await haResponse.Content.ReadAsByteArrayAsync(cts.Token);
            }

            if (responseBody.Length > 0)
            {
                response.SetBody(responseBody);
            }

            await SendMessageAsync(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling request {RequestId}", request.RequestId);

            var errorResponse = TunnelHttpResponse.CreateError(
                request.RequestId,
                502,
                "Bad Gateway - Could not reach Home Assistant");

            await SendMessageAsync(errorResponse, cancellationToken);
        }
    }

    private async Task HandleHeartbeatAsync(HeartbeatMessage heartbeat, CancellationToken cancellationToken)
    {
        var ack = new HeartbeatAckMessage
        {
            Sequence = heartbeat.Sequence
        };

        await SendMessageAsync(ack, cancellationToken);
    }

    #region WebSocket Proxying

    private async Task HandleWebSocketOpenAsync(WebSocketOpenMessage open, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Opening WebSocket connection {ConnectionId} to {Path}", open.ConnectionId, open.Path);

        var response = new WebSocketOpenResponseMessage
        {
            ConnectionId = open.ConnectionId
        };

        try
        {
            var wsClient = new ClientWebSocket();
            wsClient.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);

            // Build the WebSocket URL
            var wsUrl = _homeAssistantUrl.Replace("http://", "ws://").Replace("https://", "wss://");
            wsUrl = $"{wsUrl}{open.Path}";

            _logger.LogDebug("Connecting WebSocket to {Url}", wsUrl);
            await wsClient.ConnectAsync(new Uri(wsUrl), cancellationToken);
            _logger.LogDebug("WebSocket connected successfully to {Url}", wsUrl);

            if (!_activeWebSockets.TryAdd(open.ConnectionId, wsClient))
            {
                await wsClient.CloseAsync(WebSocketCloseStatus.InternalServerError, "Duplicate connection", cancellationToken);
                response.Success = false;
                response.ErrorMessage = "Duplicate connection ID";
            }
            else
            {
                response.Success = true;

                // Start receiving data from HA and relaying to server
                _ = RelayWebSocketFromHaAsync(open.ConnectionId, wsClient, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open WebSocket {ConnectionId}", open.ConnectionId);
            response.Success = false;
            response.ErrorMessage = ex.Message;
        }

        await SendMessageAsync(response, cancellationToken);
    }

    private async Task RelayWebSocketFromHaAsync(string connectionId, ClientWebSocket wsClient, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        try
        {
            while (!cancellationToken.IsCancellationRequested && wsClient.State == WebSocketState.Open)
            {
                var result = await wsClient.ReceiveAsync(buffer, cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    var closeMessage = new WebSocketCloseMessage
                    {
                        ConnectionId = connectionId,
                        StatusCode = (int)(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure),
                        Reason = result.CloseStatusDescription
                    };
                    await SendMessageAsync(closeMessage, cancellationToken);
                    break;
                }

                var dataMessage = new WebSocketDataMessage
                {
                    ConnectionId = connectionId,
                    IsText = result.MessageType == WebSocketMessageType.Text,
                    EndOfMessage = result.EndOfMessage
                };
                // Use Span overload to avoid allocation
                dataMessage.SetData(buffer.AsSpan(0, result.Count));

                await SendMessageAsync(dataMessage, cancellationToken);
            }
        }
        catch (WebSocketException ex)
        {
            _logger.LogDebug(ex, "WebSocket error for {ConnectionId}", connectionId);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            _activeWebSockets.TryRemove(connectionId, out _);
            if (wsClient.State == WebSocketState.Open)
            {
                try
                {
                    await wsClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
                }
                catch { }
            }
            wsClient.Dispose();
        }
    }

    private async Task HandleWebSocketDataAsync(WebSocketDataMessage data, CancellationToken cancellationToken)
    {
        if (!_activeWebSockets.TryGetValue(data.ConnectionId, out var wsClient))
        {
            _logger.LogDebug("Received WebSocket data for unknown connection {ConnectionId}", data.ConnectionId);
            return;
        }

        if (wsClient.State != WebSocketState.Open)
            return;

        var bytes = data.GetDataBytes();
        if (bytes == null)
            return;

        try
        {
            await wsClient.SendAsync(
                bytes,
                data.IsText ? WebSocketMessageType.Text : WebSocketMessageType.Binary,
                data.EndOfMessage,
                cancellationToken);
        }
        catch (WebSocketException ex)
        {
            _logger.LogDebug(ex, "Failed to send to HA WebSocket {ConnectionId}", data.ConnectionId);
        }
    }

    private async Task HandleWebSocketCloseAsync(WebSocketCloseMessage close, CancellationToken cancellationToken)
    {
        if (_activeWebSockets.TryRemove(close.ConnectionId, out var wsClient))
        {
            if (wsClient.State == WebSocketState.Open)
            {
                try
                {
                    await wsClient.CloseAsync(
                        (WebSocketCloseStatus)close.StatusCode,
                        close.Reason,
                        cancellationToken);
                }
                catch { }
            }
            wsClient.Dispose();
        }
    }

    #endregion

    private async Task RunHeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        // Client doesn't send heartbeats, only responds to server heartbeats
        // This task just monitors the connection
        while (!cancellationToken.IsCancellationRequested && IsConnected)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }

    private async Task SendMessageAsync(TunnelMessage message, CancellationToken cancellationToken)
    {
        if (!IsConnected)
            return;

        var bytes = message.ToBytes();
        await _webSocket!.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private async Task<TunnelMessage?> ReceiveMessageAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[Constants.MaxWebSocketMessageSize];
        using var ms = new MemoryStream();

        WebSocketReceiveResult result;
        do
        {
            result = await _webSocket!.ReceiveAsync(buffer, cancellationToken);
            ms.Write(buffer, 0, result.Count);
        }
        while (!result.EndOfMessage);

        if (result.MessageType == WebSocketMessageType.Text)
        {
            // Use GetBuffer() to avoid allocation
            return TunnelMessage.FromBytes(ms.GetBuffer().AsSpan(0, (int)ms.Length));
        }

        return null;
    }

    private static bool IsRestrictedHeader(string header)
    {
        // Headers that should not be forwarded to Home Assistant
        return header.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
               header.Equals("Connection", StringComparison.OrdinalIgnoreCase) ||
               header.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
               header.Equals("Accept-Encoding", StringComparison.OrdinalIgnoreCase) ||
               header.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
               // Filter sec-* headers that can cause issues
               header.StartsWith("sec-", StringComparison.OrdinalIgnoreCase) ||
               // Filter X-Forwarded-* and X-Real-IP headers that cause 400 errors
               header.StartsWith("X-Forwarded-", StringComparison.OrdinalIgnoreCase) ||
               header.Equals("X-Real-IP", StringComparison.OrdinalIgnoreCase);
    }

    public async ValueTask DisposeAsync()
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
        }

        // Close all active WebSocket connections
        foreach (var (_, ws) in _activeWebSockets)
        {
            if (ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Addon shutdown", CancellationToken.None);
                }
                catch { }
            }
            ws.Dispose();
        }
        _activeWebSockets.Clear();

        if (_heartbeatTask != null)
        {
            await _heartbeatTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        if (_webSocket != null)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                try
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Addon shutdown", CancellationToken.None);
                }
                catch { }
            }
            _webSocket.Dispose();
        }

        _haHttpClient.Dispose();
    }
}
