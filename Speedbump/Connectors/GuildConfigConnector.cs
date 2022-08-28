using DSharpPlus;
using DSharpPlus.Entities;

namespace Speedbump
{
    public static class GuildConfigConnector
    {
        public static GuildConfig Get(ulong guild, string item)
        {
            var i = new SqlInstance();
            return i.Read(@"
                select * from
                (
	                select c.item, c.value, d.label, d.type, d.value as `default` from @p0guildconfig c
	                join @p0guildconfig_default d using (item)
                    where c.guild = @p1
                    and c.item = @p2
                    union
	                select
		                d.item, d.value, d.label, d.type, d.value
	                from @p0guildconfig_default d
                    where d.item = @p2
                ) configs
                limit 1;",
                guild, item).Bind<GuildConfig>().FirstOrDefault();
        }

        public static List<GuildConfig> GetAll(ulong guild)
        {
            var i = new SqlInstance();
            return i.Read(@"
                select * from
                (
	                select c.item, c.value, d.label, d.type, d.value as `default` from @p0guildconfig c
	                join @p0guildconfig_default d using (item)
                    where c.guild = @p1
                    union
	                select
		                d.item, d.value, d.label, d.type, d.value
	                from @p0guildconfig_default d
                ) configs
                group by configs.item", 
                guild).Bind<GuildConfig>();
        }

        public static GuildConfig Set(ulong guild, string item, string value)
        {
            var i = new SqlInstance();
            i.Execute("delete from @p0guildconfig where guild=@p1 and item=@p2; insert into @p0guildconfig (guild, item, value) values (@p1, @p2, @p3)", 
                guild, item, value);

            return Get(guild, item);
        }

        public static DiscordChannel GetChannel(ulong guildId, string item, DiscordClient client)
        {
            try
            {
                var c = Get(guildId, item);
                if (c is null || c.Value is null || c.Value.ToString().Trim() == "") { return null; }
                return client.Guilds[guildId].Channels[ulong.Parse(c.Value.ToString())];
            }
            catch
            {
                return null;
            }
        }

        public static DiscordRole GetRole(ulong guild, string item, DiscordClient client)
        {
            try
            {
                var c = Get(guild, item);
                if (c is null || c.Value is null || c.Value.ToString().Trim() == "") { return null; }
                return client.Guilds[guild].Roles[ulong.Parse(c.Value.ToString())];
            }
            catch
            {
                return null;
            }
        }

        public enum GuildConfigType
        {
            Role = 0,
        }
    }
}
