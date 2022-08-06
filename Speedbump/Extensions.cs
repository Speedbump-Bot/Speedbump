using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Net;

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
    }
}
