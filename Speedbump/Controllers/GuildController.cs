using DSharpPlus;

using Microsoft.AspNetCore.Mvc;

namespace Speedbump
{
    [Route("api/{controller}")]
    public class GuildController : ControllerBase
    {
        DiscordClient Discord;

        public GuildController(DiscordManager discord)
        {
            Discord = discord.Client;
        }

        [HttpGet][Route("list")]
        public ContentResult List()
        {
            var id = this.Discord().Value<ulong>("id");
            var guilds = PermissionConnector.GetPermitted(id, Discord);
            return this.Respond(guilds, System.Net.HttpStatusCode.OK);
        }

        [HttpGet][Route("config")]
        public ContentResult GetConfig([FromQuery]ulong guild, [FromQuery]string item)
        {
            var v = GuildConfigConnector.Get(guild, item);
            if (v is null || guild == 0) { return this.Respond("Invalid item or guild", System.Net.HttpStatusCode.BadRequest); }

            var id = this.Discord().Value<ulong>("id");
            if (!PermissionConnector.HasGuildEditPermission(id, guild, Discord))
            {
                return this.Respond(null, System.Net.HttpStatusCode.Unauthorized);
            }

            return this.Respond(v, System.Net.HttpStatusCode.OK);
        }

        [HttpPost][Route("config")]
        public ContentResult SetConfig([FromQuery]ulong guild, [FromQuery]string item, [FromQuery]string value)
        {
            var v = GuildConfigConnector.Get(guild, item);
            if (v is null || guild == 0) { return this.Respond("Invalid item or guild", System.Net.HttpStatusCode.BadRequest); }

            var id = this.Discord().Value<ulong>("id");
            if (!PermissionConnector.HasGuildEditPermission(id, guild, Discord))
            {
                return this.Respond(null, System.Net.HttpStatusCode.Unauthorized);
            }

            if (value is not null)
            {
                var i = GuildConfigConnector.Get(guild, item);
                var g = Discord.Guilds.First(g => g.Key == guild).Value;
                if ((i.Type == GuildConfigType.Role && !g.Roles.Any(r => r.Key.ToString() == value)) || 
                    (i.Type == GuildConfigType.TextChannel && !g.Channels.Any(c => c.Key.ToString() == value)))
                {
                    return this.Respond($"Invalid value `{value}` for type `{i.Type}`", System.Net.HttpStatusCode.BadRequest);
                }
            }

            return this.Respond(GuildConfigConnector.Set(guild, item, value), System.Net.HttpStatusCode.OK);
        }

        [HttpGet][Route("configs")]
        public ContentResult GetConfigs([FromQuery]ulong guild)
        {
            var id = this.Discord().Value<ulong>("id");
            if (!PermissionConnector.HasGuildEditPermission(id, guild, Discord))
            {
                return this.Respond(null, System.Net.HttpStatusCode.Unauthorized);
            }

            return this.Respond(GuildConfigConnector.GetAll(guild), System.Net.HttpStatusCode.OK);
        }

        [HttpGet][Route("roles")]
        public ContentResult GetRoles([FromQuery]ulong guild)
        {
            var id = this.Discord().Value<ulong>("id");
            if (!PermissionConnector.HasGuildEditPermission(id, guild, Discord))
            {
                return this.Respond(null, System.Net.HttpStatusCode.Unauthorized);
            }

            return this.Respond(Discord.Guilds.First(g => g.Key == guild).Value.Roles.Select(r => new RoleInfo()
            {
                ID = r.Key,
                Name = r.Value.Name,
                Color = r.Value.Color.ToString(),
                Position = r.Value.Position
            }), System.Net.HttpStatusCode.OK);
        }

        [HttpGet][Route("channels")]
        public ContentResult GetChannels([FromQuery]ulong guild)
        {
            var id = this.Discord().Value<ulong>("id");
            if (!PermissionConnector.HasGuildEditPermission(id, guild, Discord))
            {
                return this.Respond(null, System.Net.HttpStatusCode.Unauthorized);
            }

            return this.Respond(Discord.Guilds.First(g => g.Key == guild).Value.Channels.Select(c => new ChannelInfo()
            {
                ID = c.Key,
                Name = c.Value.Name,
                Description = c.Value.Topic,
                Category = c.Value.Parent?.Name,
                Position = c.Value.Position,
                Type = c.Value.Type.ToString(),
            }), System.Net.HttpStatusCode.OK);
        }

        [HttpGet][Route("filter")]
        public ContentResult GetFilters([FromQuery]ulong guild)
        {
            var id = this.Discord().Value<ulong>("id");
            if (!PermissionConnector.HasGuildEditPermission(id, guild, Discord))
            {
                return this.Respond(null, System.Net.HttpStatusCode.Unauthorized);
            }

            return this.Respond(FilterConnector.GetMatches(guild), System.Net.HttpStatusCode.OK);
        }

        [HttpPost][Route("filter")]
        public ContentResult AddFilter([FromQuery]ulong guild, [FromQuery]string match, [FromQuery]FilterMatchType type)
        {
            var id = this.Discord().Value<ulong>("id");
            if (!PermissionConnector.HasGuildEditPermission(id, guild, Discord))
            {
                return this.Respond(null, System.Net.HttpStatusCode.Unauthorized);
            }

            var f = new FilterMatch()
            {
                Guild = guild,
                Match = match,
                Type = type,
            };

            return this.Respond(FilterConnector.AddMatch(f), System.Net.HttpStatusCode.OK);
        }

        [HttpDelete][Route("filter")]
        public ContentResult DeleteFilter([FromQuery]ulong guild, [FromQuery]string match)
        {
            var id = this.Discord().Value<ulong>("id");
            if (!PermissionConnector.HasGuildEditPermission(id, guild, Discord))
            {
                return this.Respond(null, System.Net.HttpStatusCode.Unauthorized);
            }

            return this.Respond(FilterConnector.RemoveMatch(guild, match), System.Net.HttpStatusCode.OK);
        }
    }
}
