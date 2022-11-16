using Discord;
using Discord.Rest;
using Discord.WebSocket;
using System.Collections.Concurrent;
using Victoria;
using Victoria.EventArgs;

namespace BodzioWithVictoria.Services
{
    public class AudioService
    {
        private LavaNode _lavaNode;
        private DiscordSocketClient _client;
        private LogService _logService;

        public HashSet<ulong> VoteQueue;
        private ConcurrentDictionary<ulong, CancellationTokenSource> _disconnectTokens;

        public AudioService(DiscordSocketClient client, LavaNode lavaNode, LogService logService)
        {
            _client = client;
            _lavaNode = lavaNode;
            _logService = logService;

            VoteQueue = new HashSet<ulong>();
        }
        public Task InitializeAsync()
        {
            _client.Ready += ClientReadyAsync;
            _lavaNode.OnLog += LogAsync;

            _lavaNode.OnPlayerUpdated += OnPlayerUpdated;
            _lavaNode.OnStatsReceived += OnStatsReceived;
            _lavaNode.OnTrackEnded += OnTrackEnded;
            _lavaNode.OnTrackStarted += OnTrackStarted;
            _lavaNode.OnTrackException += OnTrackException;
            _lavaNode.OnTrackStuck += OnTrackStuck;
            _lavaNode.OnWebSocketClosed += OnWebSocketClosed;

            return Task.CompletedTask;
        }
        public async Task ClientReadyAsync()
        {
            await _lavaNode.ConnectAsync();
        }


        public async Task ConnectAsync(SocketVoiceChannel voiceChannel, ITextChannel channel)
        {
            await _lavaNode.JoinAsync(voiceChannel, channel);
        }

        public async Task LeaveAsync(SocketVoiceChannel voiceChannel)
        {
            await _lavaNode.LeaveAsync(voiceChannel);
        }

        public async Task LogAsync(LogMessage msg)
        {
            await _logService.LogAsync(msg);
        }

        private Task OnPlayerUpdated(PlayerUpdateEventArgs arg)
        {
            _logService.LogAsync(arg);
            //_logger.LogInformation($"Track update received for {arg.Track.Title}: {arg.Position}");
            return Task.CompletedTask;
        }

        private Task OnStatsReceived(StatsEventArgs arg)
        {
            _logService.LogAsync(arg);
            //_logger.LogInformation($"Lavalink has been up for {arg.Uptime}.");
            return Task.CompletedTask;
        }

        private async Task OnTrackStarted(TrackStartEventArgs arg)
        {
            if (!_disconnectTokens.TryGetValue(arg.Player.VoiceChannel.Id, out var value))
            {
                return;
            }

            if (value.IsCancellationRequested)
            {
                return;
            }

            value.Cancel(true);
            await arg.Player.TextChannel.SendMessageAsync("Auto disconnect has been cancelled!");
        }

        private async Task OnTrackEnded(TrackEndedEventArgs args)
        {
            if (args.Reason != Victoria.Enums.TrackEndReason.Finished)
            {
                return;
            }

            var player = args.Player;
            if (!player.Queue.TryDequeue(out var queueable))
            {
                await player.TextChannel.SendMessageAsync("Queue completed! Please add more tracks to rock n' roll!");
                _ = InitiateDisconnectAsync(args.Player, TimeSpan.FromSeconds(10));
                return;
            }

            if (!(queueable is LavaTrack track))
            {
                await player.TextChannel.SendMessageAsync("Next item in queue is not a track.");
                return;
            }

            await args.Player.PlayAsync(track);
            await args.Player.TextChannel.SendMessageAsync(
                $"{args.Reason}: {args.Track.Title}\nNow playing: {track.Title}");
        }

        private async Task InitiateDisconnectAsync(LavaPlayer player, TimeSpan timeSpan)
        {
            if (!_disconnectTokens.TryGetValue(player.VoiceChannel.Id, out var value))
            {
                value = new CancellationTokenSource();
                _disconnectTokens.TryAdd(player.VoiceChannel.Id, value);
            }
            else if (value.IsCancellationRequested)
            {
                _disconnectTokens.TryUpdate(player.VoiceChannel.Id, new CancellationTokenSource(), value);
                value = _disconnectTokens[player.VoiceChannel.Id];
            }

            await player.TextChannel.SendMessageAsync($"Auto disconnect initiated! Disconnecting in {timeSpan}...");
            var isCancelled = SpinWait.SpinUntil(() => value.IsCancellationRequested, timeSpan);
            if (isCancelled)
            {
                return;
            }

            await _lavaNode.LeaveAsync(player.VoiceChannel);
            await player.TextChannel.SendMessageAsync("Invite me again sometime, sugar.");
        }

        private async Task OnTrackException(TrackExceptionEventArgs arg)
        {
            _logService.LogAsync(arg);

            //_logger.LogError($"Track {arg.Track.Title} threw an exception. Please check Lavalink console/logs.");
            arg.Player.Queue.Enqueue(arg.Track);
            await arg.Player.TextChannel?.SendMessageAsync(
                $"{arg.Track.Title} has been re-added to queue after throwing an exception.");
        }

        private async Task OnTrackStuck(TrackStuckEventArgs arg)
        {
            _logService.LogAsync(arg);

            //_logger.LogError(
            //    $"Track {arg.Track.Title} got stuck for {arg.Threshold}ms. Please check Lavalink console/logs.");
            //arg.Player.Queue.Enqueue(arg.Track);
            //await arg.Player.TextChannel?.SendMessageAsync(
            //    $"{arg.Track.Title} has been re-added to queue after getting stuck.");
        }

        private Task OnWebSocketClosed(WebSocketClosedEventArgs arg)
        {
            _logService.LogAsync(arg);

            //_logger.LogCritical($"Discord WebSocket connection closed with following reason: {arg.Reason}");
            return Task.CompletedTask;
        }

    }
}
