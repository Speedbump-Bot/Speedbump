using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Speedbump
{
    [SlashCommandGroup("rank", "Rank Information")]
    public class RankCommand : ApplicationCommandModule
    {
        [SlashCommand("leaderboard", "View the rank leaderboard")]
        public async Task Rank(InteractionContext ctx)
        {
            var leaderboard = XPConnector.Leaderboard(ctx.Guild.Id);

            var toDisplay = leaderboard.Take(10).ToList();
            if (!toDisplay.Any(l => l.user == ctx.User.Id))
            {
                if (leaderboard.Any(l => l.user == ctx.User.Id))
                {
                    toDisplay.Add(leaderboard.First(l => l.user == ctx.User.Id));
                }
                else
                {
                    toDisplay.Add((-1, ctx.User.Id, 0));
                }
            }

            var d = "";
            foreach (var l in toDisplay)
            {
                d += $"{(l.rank == -1 ? "?" : l.rank.ToString())}: {(await ctx.Guild.GetMemberAsync(l.user)).Mention} ({string.Format("{0:n0}", l.xp)} XP)\n";
            }
            d = d.Trim();

            var e = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.CornflowerBlue)
                .WithTimestamp(DateTimeOffset.Now)
                .WithTitle("Chat Rank Leaderboard")
                .WithDescription(d);

            await ctx.CreateResponseAsync(e);
        }

        [SlashCommand("view", "View your current rank")]
        public async Task View(InteractionContext ctx,
            [Option("user", "The user to view the rank of.")]DiscordUser user = null)
        {
            var leaderboard = XPConnector.Leaderboard(ctx.Guild.Id);
            var id = user?.Id ?? ctx.User.Id;
            var toUse = leaderboard.FirstOrDefault(l => l.user == id);
            if (!leaderboard.Any(l => l.user == id))
            {
                toUse = (-1, id, 0);
            }

            var level = XPConnector.GetLevel(ctx.Guild.Id, ctx.User.Id);
            var levelXp = XPConnector.GetMinXP(level);
            var nextLevel = XPConnector.GetMinXP(level + 1);

            var e = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.CornflowerBlue)
                .WithTitle("Current Rank")
                .AddField("Rank", $"{(toUse.rank == -1 ? "?" : toUse.rank.ToString())}/{leaderboard.Count}", true)
                .AddField("Level", $"Current level: {level} ({string.Format("{0:n0}", levelXp)} XP)\nNext level: {level+1} ({string.Format("{0:n0}", nextLevel)} XP)", true)
                .AddField("Current XP", toUse.xp.ToString() + " XP", true);

            await ctx.CreateResponseAsync(e);
        }
    }
}
