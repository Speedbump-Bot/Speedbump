using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Speedbump
{
    public static class Extensions
    {
        public static Stream StreamFromString(this string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static DiscordEmbedBuilder Embed() => 
            new DiscordEmbedBuilder().WithColor(DiscordColor.CornflowerBlue).WithTimestamp(DateTimeOffset.Now);

        public static async Task EditAsync(this InteractionContext ctx, string con) =>
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(con));

        public static string Discord(this DateTimeOffset offset, DiscordTimeFormat format = DiscordTimeFormat.ShortDateTime_f)
        {
            var f = format.ToString().Split("_")[1];
            return $"<t:{offset.ToUnixTimeSeconds()}:{f}>";
        }
    }

    public enum DiscordTimeFormat
    {
        /// <summary>
        /// 9:41 PM
        /// </summary>
        ShortTime_t,
        /// <summary>
        /// 9:41:30 PM
        /// </summary>
        LongTime_T,
        /// <summary>
        /// 30/06/2021
        /// </summary>
        ShortDate_d,
        /// <summary>
        /// 30 June 2021
        /// </summary>
        LongTime_D,
        /// <summary>
        /// 30 June 2021 9:41 PM
        /// </summary>
        ShortDateTime_f,
        /// <summary>
        /// Wednesday, June, 30, 2021 9:41 PM
        /// </summary>
        LongDateTime_F,
        /// <summary>
        /// 2 months ago
        /// </summary>
        Relative_R
    }
}
