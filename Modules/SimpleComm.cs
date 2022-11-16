using Discord.Commands;

namespace BodzioWithVictoria.Modules
{
    public class SimpleComm : ModuleBase<SocketCommandContext>
    {
        [Command("Ping")]
        public async Task Pong()
        {
            await ReplyAsync("Pong");
        }

    }
}
