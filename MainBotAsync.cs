using BodzioWithVictoria;
using BodzioWithVictoria.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Victoria;

public class MainBotAsync
{
    //Log cmd
    private LogService _logService;

    //Discord connect
    private DiscordSocketClient _client;

    //Setup service
    private IServiceProvider _serviceProvider;

    //Comand handler service
    private CommandService _commandService;

    //Bot Token
    private ConfigService _configService;
    private Config _config;

    public MainBotAsync()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            MessageCacheSize = 100,
            LogLevel = LogSeverity.Debug
            
        });

        _commandService = new CommandService(new CommandServiceConfig
        {
            LogLevel = LogSeverity.Debug,
            CaseSensitiveCommands = false
        });

        _logService = new LogService();
        _configService = new ConfigService();
        _config = _configService.GetConfig();

    }

    public async Task InitializeAsync()
    {
        await _client.LoginAsync(TokenType.Bot, _config.Token);
        await _client.StartAsync();

        _client.Log += LogAsync;
        _serviceProvider = SetupServices();

        var commandService = new CommandHandler(_client, _serviceProvider, _commandService);

        await commandService.InitializeAsync();

        await _serviceProvider.GetRequiredService<AudioService>().InitializeAsync();
        await _serviceProvider.GetRequiredService<AudioService>().InitializeAsync();

        await Task.Delay(-1);
    }
    private IServiceProvider SetupServices()
        => new ServiceCollection()
        .AddSingleton(_logService)
        .AddSingleton(_client)
        .AddSingleton(_commandService)
        .AddLavaNode(x => { x.SelfDeaf = false; })
        .AddSingleton<AudioService>()
        .AddSingleton<AudioService>()
        .BuildServiceProvider();

    private async Task LogAsync(LogMessage msg)
    {
        await _logService.LogAsync(msg);
    }

}