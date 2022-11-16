using Discord;
using Discord.WebSocket;
using Victoria.EventArgs;

namespace BodzioWithVictoria.Services
{
    public class LogService
    {
        internal async Task LogAsync(LogMessage message)
        {
            await Task.Run(() => Console.WriteLine(message.ToString()));
        }

        internal async Task LogAsync(PlayerUpdateEventArgs message)
        {
            await Task.Run(() => Console.WriteLine(message.ToString()));
        }

        internal async Task LogAsync(StatsEventArgs message)
        {
            await Task.Run(() => Console.WriteLine(message.ToString()));
        }

        internal async Task LogAsync(TrackExceptionEventArgs message)
        {
            await Task.Run(() => Console.WriteLine(message.ToString()));
        }

        internal async Task LogAsync(TrackStuckEventArgs message)
        {
            await Task.Run(() => Console.WriteLine(message.ToString()));
        }

        internal async Task LogAsync(WebSocketClosedEventArgs message)
        {
            await Task.Run(() => Console.WriteLine(message.ToString()));
        }
    }
}
