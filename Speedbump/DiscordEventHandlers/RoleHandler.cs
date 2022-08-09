using DSharpPlus;

namespace Speedbump
{
    public class RoleHandler
    {
        DiscordClient Discord;

        public RoleHandler(DiscordManager discord)
        {
            Discord = discord.Client;
            Discord.GuildRoleDeleted += Discord_GuildRoleDeleted;
        }

        private Task Discord_GuildRoleDeleted(DiscordClient sender, DSharpPlus.EventArgs.GuildRoleDeleteEventArgs e)
        {
            RoleConnector.Remove(e.Guild.Id, e.Role.Id);
            return Task.CompletedTask;
        }
    }
}
