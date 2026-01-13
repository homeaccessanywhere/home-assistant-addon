using HAA.Addon.Tunnel;

namespace HAA.Addon;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly ILoggerFactory _loggerFactory;

    public Worker(ILogger<Worker> logger, IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _configuration = configuration;
        _loggerFactory = loggerFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionKey = _configuration.GetValue<string>("ConnectionKey")
            ?? throw new InvalidOperationException("ConnectionKey is required");

        var serverUrl = _configuration.GetValue<string>("ServerUrl")
            ?? "wss://api.homeaccessanywhere.com";

        var homeAssistantUrl = _configuration.GetValue<string>("HomeAssistantUrl")
            ?? "http://homeassistant:8123";

        _logger.LogInformation("Starting Home Access Anywhere Addon");
        _logger.LogInformation("Server: {ServerUrl}", serverUrl);
        _logger.LogInformation("Home Assistant: {HAUrl}", homeAssistantUrl);

        await using var client = new TunnelClient(
            serverUrl,
            connectionKey,
            homeAssistantUrl,
            _loggerFactory.CreateLogger<TunnelClient>());

        await client.RunAsync(stoppingToken);
    }
}
