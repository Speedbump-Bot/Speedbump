using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Speedbump.DiscordEventHandlers
{
    public class ModerationHandler
    {
        DiscordClient Discord;

        public ModerationHandler(DiscordManager discord)
        {
            Discord = discord.Client;
            Discord.MessageCreated += Discord_MessageCreated;
            Discord.MessageUpdated += Discord_MessageUpdated;
            Discord.MessageDeleted += Discord_MessageDeleted;

            Discord.GuildMemberAdded += Discord_GuildMemberAdded;
            Discord.GuildMemberRemoved += Discord_GuildMemberRemoved;

            Discord.ComponentInteractionCreated += Discord_ComponentInteractionCreated;
        }

        private Task Discord_GuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs ev)
        {
            _ = Task.Run(async () =>
            {
                var modinfo = GuildConfigConnector.GetChannel(ev.Guild.Id, "channel.modinfo", Discord);
                if (modinfo is null) { return; }
                var e = Extensions.Embed()
                    .WithTitle("Member Left")
                    .WithDescription(ev.Member.Mention)
                    .AddField("Joined At", $"<t:{ev.Member.JoinedAt.ToUnixTimeSeconds()}:F>", true)
                    .AddField("Account Created At", $"<t:{ev.Member.CreationTimestamp.ToUnixTimeSeconds()}:F>", true);
                await modinfo.SendMessageAsync(e);
            });
            return Task.CompletedTask;
        }

        private Task Discord_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs ev)
        {
            _ = Task.Run(async () =>
            {
                var general = GuildConfigConnector.GetChannel(ev.Guild.Id, "channel.general", Discord);
                if (general is null) { return; }

                var modinfo = GuildConfigConnector.GetChannel(ev.Guild.Id, "channel.modinfo", Discord);
                if (modinfo is null) { return; }

                var e = Extensions.Embed()
                    .WithTitle("Member Joined")
                    .WithDescription(ev.Member.Mention)
                    .AddField("Account Created At", $"<t:{ev.Member.CreationTimestamp.ToUnixTimeSeconds()}:F>", true);
                await modinfo.SendMessageAsync(e);

                var joinMessage = Tag.GenerateFromTemplate(GuildConfigConnector.Get(ev.Guild.Id, "text.joinmessage").Value, ev.Member, general);
                await general.SendMessageAsync(joinMessage);
            });
            return Task.CompletedTask;
        }

        private Task Discord_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            _ = ModerationUtility.HandleComponent(e, Discord);
            return Task.CompletedTask;
        }

        private Task Discord_MessageDeleted(DiscordClient sender, MessageDeleteEventArgs e)
        {
            _ = ModerationUtility.HandleDelete(new List<DiscordMessage>() { e.Message }, e.Channel, null, Discord);
            return Task.CompletedTask;
        }

        private Task Discord_MessageUpdated(DiscordClient sender, MessageUpdateEventArgs e)
        {
            _ = HandleMessage(e.Message);
            return Task.CompletedTask;
        }

        private Task Discord_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            _ = HandleMessage(e.Message);
            return Task.CompletedTask;
        }

        private async Task HandleMessage(DiscordMessage message)
        {
            if (message.Author.IsBot || (message.Author.IsSystem is not null && (bool)message.Author.IsSystem) || message.Channel.GuildId is null) { return; }

            var modlogs = GuildConfigConnector.GetChannel(message.Channel.Guild.Id, "channel.modlogs", Discord);
            if (modlogs is null) { return; }

            (var matches, var action) = ModerationUtility.GetMatches(message);
            if (action == FilterMatchType.None) { return; }

            if (action == FilterMatchType.Mute)
            {
                await message.DeleteAsync();
                await ModerationUtility.MuteUser(message.Author.Id, (ulong)message.Channel.GuildId, Discord, Discord.CurrentUser, "Automatic - Filter");
            }
            else if (action == FilterMatchType.Warn)
            {
                await message.DeleteAsync();
                var msg = GuildConfigConnector.Get(message.Channel.Guild.Id, "text.autowarnmessage").Value;
                await message.Channel.SendMessageAsync(Tag.GenerateFromTemplate(msg, message.Author, message.Channel));
            }

            var flag = new Flag()
            {
                Guild = message.Channel.Guild.Id,
                SourceChannel = message.ChannelId,
                SourceContent = message.Content,
                SourceMatches = string.Join(',', matches.Select(m => m.Match)),
                SourceUser = message.Author.Id,
                Time = message.Timestamp.LocalDateTime,
                Type = FlagType.Message,
                SystemMessage = action == FilterMatchType.Mute ? "The user was automatically muted. The message was deleted." : action == FilterMatchType.Warn ? "The user was automatically warned. The message was deleted." : "The message matched a filter.",
                SourceMessage = message.Id,
                SourceGuild = message.Channel.Guild.Id,
                FlaggedBy = Discord.CurrentUser.Id,
                ResolutionPoints = action == FilterMatchType.Mute ? 3 : action == FilterMatchType.Warn ? 1 : 0,
                ResolutionTime = action == FilterMatchType.Mute || action == FilterMatchType.Warn ? DateTime.Now : default,
                ResolutionType = action == FilterMatchType.Mute ? FlagResolutionType.Muted : action == FilterMatchType.Warn ? FlagResolutionType.Warned : FlagResolutionType.None,
                ResolutionUser = action == FilterMatchType.Mute || action == FilterMatchType.Warn ? Discord.CurrentUser.Id : null,
            };

            flag = await ModerationUtility.RenderFlag(flag, Discord);
            flag = FlagConnector.Create(flag, Discord.CurrentUser.Id);
        }
    }
}
