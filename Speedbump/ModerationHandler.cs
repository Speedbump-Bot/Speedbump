using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Speedbump
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

        private async Task Discord_GuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs ev)
        {
            var modinfo = ev.Guild.Channels[ulong.Parse(GuildConfigConnector.Get(ev.Guild.Id, "channel.modinfo").Value)];
            var e = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.CornflowerBlue)
                .WithTitle("Member Left")
                .WithDescription(ev.Member.Mention)
                .AddField("Joined At", $"<t:{ev.Member.JoinedAt.ToUnixTimeSeconds()}:F>", true)
                .AddField("Account Created At", $"<t:{ev.Member.CreationTimestamp.ToUnixTimeSeconds()}:F>", true)
                .WithTimestamp(DateTimeOffset.Now);
            await modinfo.SendMessageAsync(e);
        }

        private async Task Discord_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs ev)
        {
            var modinfo = ev.Guild.Channels[ulong.Parse(GuildConfigConnector.Get(ev.Guild.Id, "channel.modinfo").Value)];
            var e = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.CornflowerBlue)
                .WithTitle("Member Joined")
                .WithDescription(ev.Member.Mention)
                .AddField("Account Created At", $"<t:{ev.Member.CreationTimestamp.ToUnixTimeSeconds()}:F>", true)
                .WithTimestamp(DateTimeOffset.Now);
            await modinfo.SendMessageAsync(e);

            var general = ev.Guild.Channels[ulong.Parse(GuildConfigConnector.Get(ev.Guild.Id, "channel.general").Value)];
            var joinMessage = Tag.GenerateFromTemplate(GuildConfigConnector.Get(ev.Guild.Id, "text.joinmessage").Value, ev.Member, general);
            await general.SendMessageAsync(joinMessage);
        }

        private async Task Discord_ComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
        {
            await ModerationUtility.HandleComponent(e, Discord);
        }

        private async Task Discord_MessageDeleted(DiscordClient sender, MessageDeleteEventArgs e) =>
            await ModerationUtility.HandleDelete(new List<DiscordMessage>() { e.Message }, e.Channel, null);

        private async Task Discord_MessageUpdated(DiscordClient sender, MessageUpdateEventArgs e)
        {
            await HandleMessage(e.Message);
        }

        private async Task Discord_MessageCreated(DiscordClient sender, MessageCreateEventArgs e) => await HandleMessage(e.Message);

        private async Task HandleMessage(DiscordMessage message)
        {
            if (message.Author.IsBot || (message.Author.IsSystem is not null && (bool)message.Author.IsSystem) || message.Channel.GuildId is null) { return; }

            (var matches, var action) = ModerationUtility.GetMatches(message);
            if (action == FilterMatchType.None) { return; }

            if (action == FilterMatchType.Mute)
            {
                await ModerationUtility.MuteUser(message.Author.Id, (ulong)message.Channel.GuildId, Discord, Discord.CurrentUser);
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
                SystemMessage = action == FilterMatchType.Mute ? "The user was automatically muted." : "The message matched a filter.",
                SourceMessage = message.Id,
                SourceGuild = message.Channel.Guild.Id,
                FlaggedBy = Discord.CurrentUser.Id,
                ResolutionPoints = action == FilterMatchType.Mute ? 3 : 0,
                ResolutionTime = action == FilterMatchType.Mute ? default : DateTime.Now,
                ResolutionType = action == FilterMatchType.Mute ? FlagResolutionType.Muted : FlagResolutionType.None,
                ResolutionUser = action == FilterMatchType.Mute ? Discord.CurrentUser.Id : null,
            };

            flag = await ModerationUtility.RenderFlag(flag, Discord);
            flag = FlagConnector.Create(flag, Discord.CurrentUser.Id);
        }
    }
}
