using DSharpPlus.Entities;

namespace Speedbump
{
    public class Tag
    {
        public ulong TagID { get; set; }
        public ulong Guild { get; set; }
        public string Name { get; set; }
        public string Template { get; set; }
        public string Attachment { get; set; }

        public static string GenerateFromTemplate(string text, DiscordUser user = null, DiscordChannel channel = null)
        {
            if (text is null) { return null; }

            return text
                .Replace("{user}", user?.Mention)
                .Replace("{channel}", channel?.Mention)
                .Replace("{guild}", channel?.Guild?.Name)
                .Replace("{timeshort}", $"<t:{DateTimeOffset.Now.ToUnixTimeSeconds()}:R>")
                .Replace("{timelong}", $"<t:{DateTimeOffset.Now.ToUnixTimeSeconds()}:F>");
        }
    }
}
