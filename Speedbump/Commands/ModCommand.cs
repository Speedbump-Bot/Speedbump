using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;

namespace Speedbump
{
    [SlashCommandGroup("mod", "Moderation Commands")]
    [SlashCommandPermissions(Permissions.ManageMessages)]
    public class ModCommand : ApplicationCommandModule
    {
        [SlashCommand("trust", "Toggles the trusted role for a user")]
        public async Task Trust(InteractionContext ctx, [Option("user", "The user to trust.")] DiscordUser user)
        {
            var modinfo = GuildConfigConnector.GetChannel(ctx.Guild.Id, "channel.modinfo", ctx.Client);
            var modlogs = GuildConfigConnector.GetChannel(ctx.Guild.Id, "channel.modlogs", ctx.Client);
            if (modinfo is null || modlogs is null)
            {
                await ctx.CreateResponseAsync("The modinfo or modlogs channel has not been assigned.");
                return;
            }

            var member = (DiscordMember)user;
            var roleIdS = GuildConfigConnector.Get(ctx.Guild.Id, "role.trusted").Value;
            if (roleIdS is null || roleIdS == "")
            {
                await ctx.CreateResponseAsync("The Trusted role has not been configured for this server.", true);
                return;
            }

            await ctx.DeferAsync(true);

            var roleId = ulong.Parse(roleIdS);
            var role = ctx.Guild.GetRole(roleId);
            try
            {
                if (member.Roles.Any(r => r.Id == roleId))
                {
                    await member.RevokeRoleAsync(role);
                    await ctx.EditAsync($"I've removed the {role.Name} role from {member.Mention}.");
                    await modlogs.SendMessageAsync(Extensions.Embed().WithTitle("User Untrusted").AddField("By", ctx.User.Mention).WithDescription(user.Mention));
                }
                else
                {
                    await member.GrantRoleAsync(role);
                    await ctx.EditAsync($"I've added the {role.Name} role to {member.Mention}.");
                    await modinfo.SendMessageAsync(Extensions.Embed().WithTitle("User Trusted").AddField("By", ctx.User.Mention).WithDescription(user.Mention));
                }
            }
            catch (UnauthorizedException)
            {
                await ctx.EditAsync("I'm missing the permissions to do that.");
            }
        }

        [SlashCommand("mute", "Mute a user", false)]
        public async Task Mute(InteractionContext ctx, 
            [Option("user", "The user to mute.")] DiscordUser user, 
            [Option("reason", "The reason for the mute")] string reason)
        {
            await ctx.DeferAsync(true);
            var res = await ModerationUtility.MuteUser(user.Id, ctx.Guild.Id, ctx.Client, ctx.User, reason);
            await ctx.EditAsync(res ? "I've muted " + user.Mention + "." : user.Mention + " is already muted.");
        }

        [SlashCommand("unmute", "Unmute a user", false)]
        public async Task Unmute(InteractionContext ctx, [Option("user", "The user to unmute.")] DiscordUser user)
        {
            await ctx.DeferAsync(true);
            var res = await ModerationUtility.UnmuteUser(user.Id, ctx.Guild.Id, ctx.Client, ctx.User);
            await ctx.EditAsync(res ? "I've unmuted " + user.Mention + "." : user.Mention + " is already unmuted.");
        }

        [SlashCommand("kick", "Kick a user", false)]
        public async Task Kick(InteractionContext ctx, 
            [Option("user", "The user to kick.")] DiscordUser user,
            [Option("reason", "The reason")]string reason)
        {
            await ctx.DeferAsync(true);

            var res = await ModerationUtility.ConfirmAction(ctx, "Kick Confirmation", "Are you sure you want to kick " + user.Mention + "?");

            if (res)
            {
                await ModerationUtility.KickUser(user.Id, ctx.Guild.Id, ctx.Client, ctx.User, reason);
            }
        }

        [SlashCommand("ban", "Ban a user", false)]
        public async Task Ban(InteractionContext ctx, 
            [Option("user", "The user to ban.")] DiscordUser user,
            [Option("reason", "The reason")] string reason)
        {
            await ctx.DeferAsync(true);

            var res = await ModerationUtility.ConfirmAction(ctx, "Ban Confirmation", "Are you sure you want to ban " + user.Mention + "?");

            if (res)
            {
                await ModerationUtility.BanUser(user.Id, ctx.Guild.Id, ctx.Client, ctx.User, reason);
            }
        }

        [SlashCommand("purge", "Purge messages")]
        public async Task Purge(InteractionContext ctx, [Option("count", "The number of messages to purge (1-100).")]long count)
        {
            if (count > 100 || count < 1)
            {
                await ctx.CreateResponseAsync("Invalid count.");
            }
            await ctx.DeferAsync(true);
            var res = await ModerationUtility.ConfirmAction(ctx, "Purge Confirmation", "Are you sure you want to purge " + count + " messages?");
            if (res)
            {
                var list = (await ctx.Channel.GetMessagesAsync((int)count)).Reverse();
                await ctx.Channel.DeleteMessagesAsync(list, "Purge by " + ctx.User.Id);
                await ModerationUtility.HandleDelete(list, ctx.Channel, ctx.User, ctx.Client);
                await ctx.EditAsync("Purge complete.");
            }
        }

        [SlashCommand("info", "Get information about a user")]
        public async Task Info(InteractionContext ctx, [Option("user", "The user")] DiscordUser user = null)
        {
            await ctx.DeferAsync(true);
            var member = await ctx.Guild.GetMemberAsync((user ?? ctx.User).Id, true);
            var xp = XPConnector.GetXP(ctx.Guild.Id, member.Id);
            var level = XPConnector.GetLevel(ctx.Guild.Id, member.Id);
            var points = FlagConnector.GetPointsByUserInGuild(ctx.Guild.Id, member.Id, DateTime.Now - TimeSpan.FromDays(30), DateTime.Now);
            var count = FlagConnector.GetCountByUserInGuild(ctx.Guild.Id, member.Id, DateTime.Now - TimeSpan.FromDays(30), DateTime.Now);

            var e = Extensions.Embed()
                .WithAuthor(member.Username + "#" + member.Discriminator, iconUrl: member.GetAvatarUrl(ImageFormat.Auto))
                .WithDescription($"**Flag Activity - Last 30 days**\n{points} points\n{count} flags with points")
                .AddField("ID", member.Id.ToString(), true)
                .AddField("Joined At", member.JoinedAt.Discord(), true)
                .AddField("Account Created At", member.CreationTimestamp.Discord(), true)
                .AddField("Activity", $"{xp} XP\n{level} Level", true)
                .AddField("Roles", string.Join(", ", member.Roles.Select(r => r.Name)), true);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(e));
        }

        [SlashCommand("slowmode", "Enable slowmode in a channel")]
        public async Task Slowmode(InteractionContext ctx, 
            [Option("channel", "The channel to enable slowmode in")] DiscordChannel channel,
            [Option("reason", "The reason for the slowmode")]string reason,
            [Option("duration", "How long the slowmode will last, in minutes (1-1440)")]long duration,
            [Option("timer", "How long between each message, in seconds (0-21600)")]long timer)
        {
            if (channel.Type == ChannelType.Category)
            {
                await ctx.CreateResponseAsync("You can't modify slowmode on a category, silly!", true);
                return;
            }

            await ctx.DeferAsync(true);
            var res = await ModerationUtility.Slowmode(timer, duration, channel, ctx.Client, ctx.User, reason);
            await ctx.EditAsync(res ? "Slowmode Has Been Modified." : "Failed to modify slowmode.");
        }

        [SlashCommand("warn", "Warn a user.")]
        public async Task Warn(InteractionContext ctx,
            [Option("user", "The user to warn")] DiscordUser user,
            [Option("reason", "The reason the user is being warned. This reason is sent to the user.")]string reason,
            [Option("points", "The points to give the user. This is *not* sent to the user.")]long points = 1,
            [Option("notify", "Whether to send the user a DM about this warning. Using false is useful for making notes.")]bool notify = true)
        {
            await ctx.DeferAsync(true);
            var res = await ModerationUtility.Warn(user, (int)points, reason, ctx.User, ctx.Client, notify);            
            await ctx.EditAsync("The user has been given a warning." + (res ? "" : " I was unable to direct message the user. (Blocked or no longer on the server?)"));
        }
    }
}
