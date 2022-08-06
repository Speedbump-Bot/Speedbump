using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using DSharpPlus.SlashCommands;

namespace Speedbump
{
    [SlashCommandGroup("mod", "Moderation Commands")]
    [SlashCommandPermissions(DSharpPlus.Permissions.ManageMessages)]
    public class ModCommand : ApplicationCommandModule
    {
        [SlashCommand("trust", "Toggles the trusted role for a user")]
        public async Task Trust(InteractionContext ctx, [Option("user", "The user to trust.")] DiscordUser user)
        {
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
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"I've removed the {role.Name} role from {member.Mention}."));
                }
                else
                {
                    await member.GrantRoleAsync(role);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"I've added the {role.Name} role to {member.Mention}."));
                }
            }
            catch (UnauthorizedException)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I'm missing the permissions to do that."));
            }
        }

        [SlashCommand("mute", "Mute a user", false)]
        public async Task Mute(InteractionContext ctx, [Option("user", "The user to mute.")] DiscordUser user)
        {
            await ctx.DeferAsync(true);
            var res = await ModerationUtility.MuteUser(user.Id, ctx.Guild.Id, ctx.Client, ctx.User);
            if (res)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I've muted " + user.Mention + "."));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(user.Mention + " is already muted."));
            }
        }

        [SlashCommand("unmute", "Unmute a user", false)]
        public async Task Unmute(InteractionContext ctx, [Option("user", "The user to unmute.")] DiscordUser user)
        {
            await ctx.DeferAsync(true);
            var res = await ModerationUtility.UnmuteUser(user.Id, ctx.Guild.Id, ctx.Client, ctx.User);
            if (res)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I've unmuted " + user.Mention + "."));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(user.Mention + " is already unmuted."));
            }
        }

        [SlashCommand("kick", "Kick a user", false)]
        public async Task Kick(InteractionContext ctx, [Option("user", "The user to kick.")] DiscordUser user)
        {
            await ctx.DeferAsync(true);

            var res = await ModerationUtility.ConfirmAction(ctx, "Ban Confirmation", "Are you sure you want to ban " + user.Mention + "?");

            if (res)
            {
                await ModerationUtility.KickUser(user.Id, ctx.Guild.Id, ctx.Client, ctx.User);
            }
        }

        [SlashCommand("ban", "Ban a user", false)]
        public async Task Ban(InteractionContext ctx, [Option("user", "The user to ban.")] DiscordUser user)
        {
            await ctx.DeferAsync(true);

            var res = await ModerationUtility.ConfirmAction(ctx, "Ban Confirmation", "Are you sure you want to ban " + user.Mention + "?");

            if (res)
            {
                await ModerationUtility.BanUser(user.Id, ctx.Guild.Id, ctx.Client, ctx.User);
            }
        }

        [SlashCommand("purge", "Purge messages")]
        public async Task Purge(InteractionContext ctx, [Option("count", "The number of messages to purge.")]long count)
        {
            if (count > 100 || count < 1)
            {
                await ctx.CreateResponseAsync("Invalid count.");
            }
            await ctx.DeferAsync(true);
            var list = (await ctx.Channel.GetMessagesAsync((int)count)).Reverse();
            await ctx.Channel.DeleteMessagesAsync(list, "Purge by " + ctx.User.Id);
            await ModerationUtility.HandleDelete(list, ctx.Channel, ctx.User);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Purge complete."));
        }
    }
}
