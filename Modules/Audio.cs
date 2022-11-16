using BodzioWithVictoria.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Text;
using Victoria;
using Victoria.Enums;

namespace BodzioWithVictoria.Modules
{
    public class Audio : ModuleBase<SocketCommandContext>
    {
        private AudioService _audioService;
        private LavaNode _lavaNode;


        public Audio(AudioService audioService, DiscordSocketClient client, LavaNode lavaNode, LogService logService)
        {
            _audioService = audioService;
            _lavaNode = lavaNode;
        }

        [Command("Join")]
        public async Task Join()
        {
            var user = (SocketGuildUser)Context.User;
            if (user.VoiceChannel is null && _lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("Problem is with voice channel I'm in or you are out.");
                return;
            }
            try
            {
                await _audioService.ConnectAsync(user.VoiceChannel, Context.Channel as ITextChannel);
                await ReplyAsync($"Now connected to '{user.VoiceChannel.Name}'");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }
        [Command("Leave")]
        public async Task Leave()
        {
            var user = (SocketGuildUser)Context.User;
            if (user.VoiceChannel is null && !_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("Problem is with voice channel.");
                return;
            }
            try
            {
                await _audioService.LeaveAsync(user.VoiceChannel);
                await ReplyAsync($"Bot has now left {user.VoiceChannel.Name}");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Play")]
        public async Task PlayAsync([Remainder] string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                await ReplyAsync("Please provide search terms.");
                return;
            }

            if (!_lavaNode.HasPlayer(Context.Guild))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            var queries = searchQuery.Split(' ');
            foreach (var query in queries)
            {
                var searchResponse = await _lavaNode.SearchAsync(Victoria.Responses.Search.SearchType.Direct, query);
                if (searchResponse.Status == Victoria.Responses.Search.SearchStatus.LoadFailed ||
                    searchResponse.Status == Victoria.Responses.Search.SearchStatus.NoMatches)
                {
                    await ReplyAsync($"I wasn't able to find anything for `{query}`.");
                    return;
                }

                var player = _lavaNode.GetPlayer(Context.Guild);

                if (player.PlayerState == PlayerState.Playing || player.PlayerState == PlayerState.Paused)
                {
                    if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                    {
                        foreach (var track in searchResponse.Tracks)
                        {
                            player.Queue.Enqueue(track);
                        }

                        await ReplyAsync($"Enqueued {searchResponse.Tracks.Count} tracks.");
                    }
                    else
                    {
                        var track = searchResponse.Tracks.FirstOrDefault();
                        player.Queue.Enqueue(track);
                        await ReplyAsync($"Enqueued: {track.Title}");
                    }
                }
                else
                {
                    var track = searchResponse.Tracks.FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(searchResponse.Playlist.Name))
                    {
                        for (var i = 0; i < searchResponse.Tracks.Count; i++)
                        {
                            if (i == 0)
                            {
                                await player.PlayAsync(track);
                                await ReplyAsync($"Now Playing: {track.Title}");
                            }
                            else
                            {
                                player.Queue.Enqueue(searchResponse.Tracks.Take(i));
                            }
                        }

                        await ReplyAsync($"Enqueued {searchResponse.Tracks.Count} tracks.");
                    }
                    else
                    {
                        await player.PlayAsync(track);
                        await ReplyAsync($"Now Playing: {track.Title}");
                    }
                }
            }
        }
        [Command("Pause")]
        public async Task PauseAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("I cannot pause when I'm not playing anything!");
                return;
            }

            try
            {
                await player.PauseAsync();
                await ReplyAsync($"Paused: {player.Track.Title}");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Resume")]
        public async Task ResumeAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Paused)
            {
                await ReplyAsync("I cannot resume when I'm not playing anything!");
                return;
            }

            try
            {
                await player.ResumeAsync();
                await ReplyAsync($"Resumed: {player.Track.Title}");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }
        [Command("Stop")]
        public async Task StopAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState == PlayerState.Stopped)
            {
                await ReplyAsync("Woaaah there, I can't stop the stopped forced.");
                return;
            }

            try
            {
                await player.StopAsync();
                await ReplyAsync("No longer playing anything.");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Skip")]
        public async Task SkipAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("Woaaah there, I can't skip when nothing is playing.");
                return;
            }

            var voiceChannelUsers = (player.VoiceChannel as SocketVoiceChannel).Users.Where(x => !x.IsBot).ToArray();
            if (_audioService.VoteQueue.Contains(Context.User.Id))
            {
                await ReplyAsync("You can't vote again.");
                return;
            }

            _audioService.VoteQueue.Add(Context.User.Id);
            var percentage = _audioService.VoteQueue.Count / voiceChannelUsers.Length * 100;
            if (percentage < 85)
            {
                await ReplyAsync("You need more than 85% votes to skip this song.");
                return;
            }

            try
            {
                var oldTrack = player.Track;
                var currenTrack = await player.SkipAsync();
                await ReplyAsync($"Skipped: {oldTrack.Title}\nNow Playing: {currenTrack.Current.Title}");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Seek")]
        public async Task SeekAsync(TimeSpan timeSpan)
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("Woaaah there, I can't seek when nothing is playing.");
                return;
            }

            try
            {
                await player.SeekAsync(timeSpan);
                await ReplyAsync($"I've seeked `{player.Track.Title}` to {timeSpan}.");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("Volume")]
        public async Task VolumeAsync(ushort volume)
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            try
            {
                await player.UpdateVolumeAsync(volume);
                await ReplyAsync($"I've changed the player volume to {volume}.");
            }
            catch (Exception exception)
            {
                await ReplyAsync(exception.Message);
            }
        }

        [Command("NowPlaying"), Alias("Np")]
        public async Task NowPlayingAsync()
        {
            if (!_lavaNode.TryGetPlayer(Context.Guild, out var player))
            {
                await ReplyAsync("I'm not connected to a voice channel.");
                return;
            }

            if (player.PlayerState != PlayerState.Playing)
            {
                await ReplyAsync("Woaaah there, I'm not playing any tracks.");
                return;
            }

            var track = player.Track;
            var artwork = await track.FetchArtworkAsync();

            var embed = new EmbedBuilder
            {
                Title = $"{track.Author} - {track.Title}",
                ThumbnailUrl = artwork,
                Url = track.Url
            }
                .AddField("Id", track.Id)
                .AddField("Duration", track.Duration)
                .AddField("Position", track.Position);

            await ReplyAsync(embed: embed.Build());
        }
    }
}
