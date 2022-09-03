using DSharpPlus;

using System.Collections.Concurrent;

namespace Speedbump.DiscordEventHandlers
{
    public class XPHandler
    {
        DiscordClient Discord;
        ConcurrentQueue<(ulong guild, ulong player)> Players = new();
        bool Closing;
        ILogger Logger;

        public XPHandler(DiscordManager discord, Lifetime lifetime, ILogger logger)
        {
            Discord = discord.Client;
            Logger = logger;
            lifetime.Add(cause => Closing = true);

            Discord.MessageCreated += Discord_MessageCreated;

            new Thread(HandlerThread).Start();
        }

        private Task Discord_MessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs e)
        {
            if (e.Author.IsBot || e.Author.IsSystem.HasValue && e.Author.IsSystem.Value) { return Task.CompletedTask; }

            Players.Enqueue((e.Guild.Id, e.Author.Id));
            return Task.CompletedTask;
        }

        private void HandlerThread()
        {
            while (!Closing)
            {
                for (var i = 0; i < 30; i++)
                {
                    Thread.Sleep(2000);
                    if (Closing) { break; }
                }

                var list = new List<(ulong guild, ulong user)>();
                while (!Players.IsEmpty)
                {
                    Players.TryDequeue(out var p);
                    list.Add(p);
                }

                var list2 = list.GroupBy(c => new
                {
                    c.guild,
                    c.user
                }).Select(p => p.First());

                foreach (var user in list2)
                {
                    try
                    {
                        XPConnector.Increment(user.guild, user.user);
                        var level = XPConnector.GetLevel(user.guild, user.user);
                        var levels = XPConnector.GetLevels(user.guild);

                        var guild = Discord.Guilds[user.guild];
                        var member = guild.GetMemberAsync(user.user, true).GetAwaiter().GetResult();

                        foreach (var l in levels)
                        {
                            if (level >= l.Level && !member.Roles.Any(r => r.Id == l.Role))
                            {
                                var role = guild.GetRole(l.Role);
                                member.GrantRoleAsync(role).GetAwaiter().GetResult();
                                try
                                {
                                    // This can error if the user blocked the bot, or due to user privacy settings, or if the user leaves immediately after sending a message.
                                    member.SendMessageAsync($"You've been given the role `{role.Name}` for reaching level {level} in {guild.Name}!").GetAwaiter().GetResult();
                                } catch { } 
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error handing out XP:\n\n" + ex);
                    }
                }
            }
        }
    }
}
