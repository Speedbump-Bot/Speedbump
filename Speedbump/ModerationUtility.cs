using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

using System.Text.RegularExpressions;

namespace Speedbump
{
    public static class ModerationUtility
    {
        public static (List<FilterMatch>, FilterMatchType) GetMatches(DiscordMessage message)
        {
            if (message.Channel.Guild is null) { return (new List<FilterMatch>(), FilterMatchType.None); }
            var guildFilters = FilterConnector.GetMatches((ulong)message.Channel.GuildId);
            var member = (DiscordMember)message.Author;

            var matches = guildFilters.Where(f =>
            {
                if (f.Type == FilterMatchType.Mute)
                {
                    return message.Content.Contains(f.Match);
                }
                else if (f.Type == FilterMatchType.Flag) 
                {
                    var content = message.Content;
                    var rgx = new Regex("[^a-zA-Z]");
                    content = rgx.Replace(content, "");
                    return content.Contains(f.Match);
                }

                return false;
            }).ToList();

            return (matches, matches.Count > 0 ? matches.Max(m => m.Type) : FilterMatchType.None);
        }

        public static async Task<Flag> RenderFlag(Flag f, DiscordClient discord)
        {
            var guild = discord.Guilds[(ulong)f.SourceGuild];
            var modlogs = guild.Channels[ulong.Parse(GuildConfigConnector.Get((ulong)f.SourceGuild, "channel.modlogs").Value)];

            var embed = Extensions.Embed()
                .WithColor(f.ResolutionType == FlagResolutionType.Warned || f.ResolutionType == FlagResolutionType.Muted ? DiscordColor.DarkBlue : f.ResolutionType == FlagResolutionType.Cleared ? DiscordColor.Green : DiscordColor.Red)
                .WithDescription(f.SourceContent)
                .AddField("Flag Reason", f.SystemMessage);

            if (f.SourceMatches is not null && f.SourceMatches != "")
            {
                embed.AddField("Matches", f.SourceMatches);
            }

            embed.AddField("User", (await guild.GetMemberAsync((ulong)f.SourceUser)).Mention, true)
                .AddField("Channel", guild.GetChannel((ulong)f.SourceChannel).Mention, true)
                .AddField("Flagged By", f.FlaggedBy is null ? discord.CurrentUser.Mention : (await guild.GetMemberAsync((ulong)f.FlaggedBy)).Mention, true)
                .AddField("Points Last 30 Days", FlagConnector.GetPointsByUserInGuild(guild.Id, (ulong)f.SourceUser, DateTime.Now - TimeSpan.FromDays(30), DateTime.Now).ToString())
                .AddField("Rolled Back?", (FlagConnector.GetHistory(f).Count > 2).ToString())
                .AddField("Resolution", f.ResolutionType.ToString(), true)
                .WithTimestamp(DateTimeOffset.Now)
                .WithAuthor((await guild.GetMemberAsync((ulong)f.SourceUser)).Username, iconUrl: (await guild.GetMemberAsync((ulong)f.SourceUser)).GetAvatarUrl(ImageFormat.Auto));

            if (f.ResolutionType != FlagResolutionType.None)
            {
                embed.AddField("Resolved By", (await guild.GetMemberAsync((ulong)f.ResolutionUser)).Mention, true)
                    .AddField("Points Issued", f.ResolutionPoints.ToString(), true);
            }

            var rows = new List<DiscordActionRowComponent>();
            if (f.ResolutionType == FlagResolutionType.None)
            {
                rows.Add(new DiscordActionRowComponent(new List<DiscordComponent>()
                {
                    new DiscordButtonComponent(ButtonStyle.Success, "flag-clear", "", emoji: new DiscordComponentEmoji("✅")),
                    new DiscordButtonComponent(ButtonStyle.Danger, "flag-warn", "", emoji: new DiscordComponentEmoji("⚠")),
                    new DiscordButtonComponent(ButtonStyle.Danger, "flag-mute", "", emoji: new DiscordComponentEmoji("🔇")),
                    new DiscordButtonComponent(ButtonStyle.Danger, "flag-history", "", emoji: new DiscordComponentEmoji("🗒")),
                }));
            }
            else
            {
                rows.Add(new DiscordActionRowComponent(new List<DiscordComponent>()
                {
                    new DiscordButtonComponent(ButtonStyle.Secondary, "flag-rollback", "", emoji: new DiscordComponentEmoji("⏪")),
                    new DiscordButtonComponent(ButtonStyle.Danger, "flag-history", "", emoji: new DiscordComponentEmoji("🗒")),
                }));
            }

            if (f.Message is not null)
            {
                var message = await modlogs.GetMessageAsync((ulong)f.Message);
                await message.ModifyAsync(new DiscordMessageBuilder()
                {
                    Embed = embed,
                }.AddComponents(rows));
            }
            else
            {
                f.Message = (await modlogs.SendMessageAsync(new DiscordMessageBuilder()
                {
                    Embed = embed,
                }.AddComponents(rows))).Id;
            }

            return f;
        }

        public static async Task HandleComponent(ComponentInteractionCreateEventArgs e, DiscordClient discord)
        {
            var modlogs = GuildConfigConnector.Get(e.Guild.Id, "channel.modlogs").Value;
            if (e.Channel.Id.ToString() != modlogs) { return; }

            var guild = e.Message.Channel.Guild;
            var flag = FlagConnector.GetByMessage(e.Message.Id);

            var message = "ERROR";

            if (e.Id == "flag-history")
            {
                var history = FlagConnector.GetHistory(flag);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    Content = $"__***History***__\n\n{string.Join('\n', history.Select(h => $"<t:{((DateTimeOffset)h.Time).ToUnixTimeSeconds()}:F> - {discord.GetUserAsync((ulong)h.User).GetAwaiter().GetResult().Username} - {h.Type}"))}",
                    IsEphemeral = true,
                });
            }
            else if (flag.ResolutionType == FlagResolutionType.None)
            {
                if (e.Id == "flag-clear")
                {
                    flag.ResolutionType = FlagResolutionType.Cleared;
                    flag.ResolutionPoints = 0;
                    flag.ResolutionTime = DateTime.Now;
                    flag.ResolutionUser = e.User.Id;
                    flag = FlagConnector.UpdateFlag(flag, e.User.Id);
                    await RenderFlag(flag, discord);
                    message = "Cleared the flag.";
                }
                else if (e.Id == "flag-warn")
                {
                    flag.ResolutionType = FlagResolutionType.Warned;
                    flag.ResolutionPoints = 1;
                    flag.ResolutionTime = DateTime.Now;
                    flag.ResolutionUser = e.User.Id;
                    flag = FlagConnector.UpdateFlag(flag, e.User.Id);
                    await RenderFlag(flag, discord);
                    message = "Gave the user a warning.";
                }
                else if (e.Id == "flag-mute")
                {
                    await MuteUser((ulong)flag.SourceUser, (ulong)flag.SourceGuild, discord, e.User);

                    flag.ResolutionType = FlagResolutionType.Muted;
                    flag.ResolutionPoints = 3;
                    flag.ResolutionTime = DateTime.Now;
                    flag.ResolutionUser = e.User.Id;
                    flag = FlagConnector.UpdateFlag(flag, e.User.Id);
                    await RenderFlag(flag, discord);
                    message = "Muted the user.";
                }

                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    Content = message,
                    IsEphemeral = true,
                });
            }
            else if (e.Id == "flag-rollback")
            {
                if (flag.ResolutionType == FlagResolutionType.Muted)
                {
                    await UnmuteUser((ulong)flag.SourceUser, (ulong)flag.SourceGuild, discord, e.User);
                }

                flag.ResolutionType = FlagResolutionType.None;
                flag.ResolutionPoints = 0;
                flag.ResolutionTime = DateTime.Now;
                flag.ResolutionUser = e.User.Id;
                flag = FlagConnector.UpdateFlag(flag, e.User.Id);
                await RenderFlag(flag, discord);
                message = "Rolled back the flag.";

                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                {
                    Content = message,
                    IsEphemeral = true,
                });
            }
        }

        public static async Task<bool> MuteUser(ulong user, ulong guildId, DiscordClient discord, DiscordUser cause)
        {
            var guild = discord.Guilds[guildId];
            var member = await guild.GetMemberAsync(user);

            var mutedRole = GuildConfigConnector.GetRole(guildId, "role.muted", discord);
            var muteCategory = GuildConfigConnector.GetChannel(guildId, "channel.mutecategory", discord);
            var modlogs = GuildConfigConnector.GetChannel(guildId, "channel.modlogs", discord);
            if (mutedRole is null || muteCategory is null || modlogs is null) { return false; }

            if (!member.Roles.Any(r => r.Id == mutedRole.Id))
            {
                await member.GrantRoleAsync(mutedRole);
            }
            else
            {
                return false;
            }

            var existing = (await guild.GetChannelsAsync()).FirstOrDefault(c => c.Name == user.ToString());
            if (existing is not null) { return false; }

            var muteChannel = guild.CreateChannelAsync(user.ToString(), ChannelType.Text, muteCategory, "Mute - " + member.Username, overwrites: new List<DiscordOverwriteBuilder>()
            {
                new DiscordOverwriteBuilder(member)
                .Allow(Permissions.AccessChannels)
            });

            var e = Extensions.Embed()
                .WithTitle("Member Muted")
                .AddField("Member", member.Mention, true)
                .AddField("Cause", cause.Mention, true)
                .WithAuthor(member.Username, iconUrl: member.GetAvatarUrl(ImageFormat.Auto));

            await modlogs.SendMessageAsync(e);

            return true;
        }

        public static async Task<bool> UnmuteUser(ulong user, ulong guildId, DiscordClient discord, DiscordUser cause)
        {
            var guild = discord.Guilds[guildId];
            var member = await guild.GetMemberAsync(user);

            var mutedRole = GuildConfigConnector.GetRole(guildId, "role.muted", discord);
            var muteCategory = GuildConfigConnector.GetChannel(guildId, "channel.mutecategory", discord);
            var modlogs = GuildConfigConnector.GetChannel(guildId, "channel.modlogs", discord);
            if (mutedRole is null || muteCategory is null || modlogs is null) { return false; }

            if (!member.Roles.Any(r => r.Id == mutedRole.Id))
            {
                return false;
            }
            else
            {
                await member.RevokeRoleAsync(mutedRole);
            }

            var memberMuteChannel = (await guild.GetChannelsAsync()).FirstOrDefault(c => c.Name == user.ToString());

            var messages = new List<DiscordMessage>();
            var lastCount = 0;
            do
            {
                var toAdd = await memberMuteChannel.GetMessagesAfterAsync(messages.Count == 0 ? 0 : messages.Last().Id);
                messages.AddRange(toAdd.Reverse());
                lastCount = toAdd.Count;
            } while (lastCount > 0);

            var time = DateTimeOffset.Now;

            var archive = $"Mute Channel History - {user} ({member.Mention}) - Generated At {time}\n\n" + 
                string.Join("\n\n", messages.Select(m => $"{m.CreationTimestamp.ToUnixTimeSeconds()}:{m.Author.Id} {m.CreationTimestamp.ToLocalTime()} {m.Author.Username} [{m.Attachments.Count}] {m.Content}"));

            var e = Extensions.Embed()
                .WithTitle("Member Unmuted")
                .AddField("Member", member.Mention, true)
                .AddField("Cause", cause.Mention, true)
                .WithAuthor(member.Username, iconUrl: member.GetAvatarUrl(ImageFormat.Auto));
            await modlogs.SendMessageAsync(new DiscordMessageBuilder().WithEmbed(e).WithFile($"{time.ToUnixTimeMilliseconds()} - {user}.txt", archive.StreamFromString()));

            await memberMuteChannel.DeleteAsync();

            return true;
        }

        public static async Task<bool> KickUser(ulong user, ulong guildId, DiscordClient discord, DiscordUser cause)
        {
            var guild = discord.Guilds[guildId];
            var member = await guild.GetMemberAsync(user);
            var modlogs = GuildConfigConnector.GetChannel(guildId, "channel.modlogs", discord);
            if (modlogs is null) { return false; }

            await member.RemoveAsync();

            var e = Extensions.Embed()
                .WithTitle("Member Kicked")
                .AddField("Member", member.Mention, true)
                .AddField("Cause", cause.Mention, true)
                .WithAuthor(member.Username, iconUrl: member.GetAvatarUrl(ImageFormat.Auto));

            await modlogs.SendMessageAsync(e);
            return true;
        }

        public static async Task<bool> BanUser(ulong user, ulong guildId, DiscordClient discord, DiscordUser cause)
        {
            var guild = discord.Guilds[guildId];
            var member = await guild.GetMemberAsync(user);
            var modlogs = GuildConfigConnector.GetChannel(guildId, "channel.modlogs", discord);
            if (modlogs is null) { return false; }

            await member.BanAsync();

            var e = Extensions.Embed()
                .WithTitle("Member Banned")
                .AddField("Member", member.Mention, true)
                .AddField("Cause", cause.Mention, true)
                .WithAuthor(member.Username, iconUrl: member.GetAvatarUrl(ImageFormat.Auto));

            await modlogs.SendMessageAsync(e);
            return true;
        }

        public static async Task<bool> ConfirmAction(InteractionContext ctx, string title, string content)
        {
            var embed = Extensions.Embed()
                .WithColor(DiscordColor.Red)
                .WithTitle(title)
                .WithDescription(content);

            var t = Snowflake.Generate();
            var f = Snowflake.Generate();

            var components = new List<DiscordComponent>()
            { 
                new DiscordButtonComponent(ButtonStyle.Success, t.ToString(), "Confirm", emoji: new DiscordComponentEmoji("✅")),
                new DiscordButtonComponent(ButtonStyle.Danger, f.ToString(), "Cancel", emoji: new DiscordComponentEmoji("❌"))
            };

            var msg = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed).AddComponents(components));

            var res = await msg.WaitForButtonAsync(i => i.Id == f.ToString() || i.Id == t.ToString());
            if (res.TimedOut)
            {
                embed.Title = "[CANCELED] " + embed.Title;
                embed.Color = DiscordColor.Gray;

                await res.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().AddEmbed(embed));

                return false;
            }

            var c = res.Result.Id == t.ToString();

            embed.Title = "[" + (c ? "CONFIRMED" : "CANCELED") + "] " + embed.Title;
            embed.Color = c ? DiscordColor.DarkGreen : DiscordColor.Gray;
            await res.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                new DiscordInteractionResponseBuilder().AddEmbed(embed)
            );

            return c;
        }

        public static async Task<bool> HandleDelete(IEnumerable<DiscordMessage> messages, DiscordChannel channel, DiscordUser cause, DiscordClient discord)
        {
            var modlogs = GuildConfigConnector.GetChannel(channel.Guild.Id, "channel.modinfo", discord);
            if (modlogs is null) { return false; }

            DiscordEmbed Create(DiscordMessage msg)
            {
                var embed = Extensions.Embed()
                    .WithTitle("Message Deletion")
                    .WithDescription(msg?.Content ?? "```\nA message was deleted, but the content was not cached in the bot.\nThis flag is purely informational.\n```")
                    .WithColor(DiscordColor.Orange)
                    .AddField("Channel", channel.Mention, true)
                    .AddField("Sent At", $"<t:{msg.CreationTimestamp.ToUnixTimeSeconds()}:F>", true)
                    .AddField("Message ID", msg.Id.ToString());

                if (msg.Author is not null)
                {
                    embed.AddField("Author", msg.Author.Mention, true)
                        .AddField("Edited At", msg.EditedTimestamp is null ? "Never" : $"<t:{msg.EditedTimestamp.Value.ToUnixTimeSeconds()}:F>", true)
                        .AddField("Attachments", msg?.Attachments.Count.ToString(), true)
                        .WithAuthor(msg.Author.Username, iconUrl: msg.Author.GetAvatarUrl(ImageFormat.Auto));
                }

                return embed;
            }

            var chunks = messages.Chunk(10);

            foreach (var c in chunks)
            {
                var c2 = c.Select(m => Create(m));

                if (messages.Count() > 1)
                {
                    await modlogs.SendMessageAsync(new DiscordMessageBuilder().AddEmbeds(c2).WithContent($"Purge by {cause.Mention}"));
                }
                else
                {
                    await modlogs.SendMessageAsync(new DiscordMessageBuilder().AddEmbeds(c2));
                }
            }
            return true;
        }
    }
}