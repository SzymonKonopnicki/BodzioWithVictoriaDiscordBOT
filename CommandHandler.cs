using BodzioWithVictoria.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;

namespace BodzioWithVictoria
{
    public class CommandHandler
    {

        //Discord connect
        private DiscordSocketClient _client;
        //Setup service
        private IServiceProvider _serviceProvider;
        //Comand handler service
        private CommandService _commandService;

        public CommandHandler(DiscordSocketClient client, IServiceProvider serviceProvider, CommandService commandService)
        {
            _client = client;
            _serviceProvider = serviceProvider;
            _commandService = commandService;
        }

        public async Task InitializeAsync()
        {
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
            _client.MessageReceived += ComandHandlerAsync;
        }

        private async Task ComandHandlerAsync(SocketMessage arg)
        {
            // Don't process the command if it was a system message
            var message = arg as SocketUserMessage;
            if (message == null) return;

            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commandService.ExecuteAsync(
                context: context,
                argPos: argPos,
                services: _serviceProvider);
        }
    }
}
