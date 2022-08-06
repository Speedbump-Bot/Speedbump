using DSharpPlus;

namespace Speedbump
{
    public static class PermissionConnector
    {
        public static bool HasGuildEditPermission(ulong user, ulong guildId, DiscordClient discord)
        {
            var managerRole = GuildConfigConnector.Get(guildId, "role.manager").Value;

            var guild = discord.Guilds.Any(g => g.Key == guildId) ? discord.Guilds.First(g => g.Key == guildId).Value : null;
            if (guild is null) { return false;}
            var member = guild.GetMemberAsync(user).GetAwaiter().GetResult();

            if (guild is null || member is null) { return false; }

            if (guild.OwnerId == user || member.Roles.Any(r => r.Id.ToString() == managerRole)) { return true; }

            return false;
        }
    }
}
