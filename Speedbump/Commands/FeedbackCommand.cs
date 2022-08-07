using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Speedbump
{
    public class FeedbackCommand : ApplicationCommandModule
    {
        [SlashCommand("feedback", "Give server feedback")]
        public async Task Trust(InteractionContext ctx)
        {
            var feedbackChannel = GuildConfigConnector.GetChannel(ctx.Guild.Id, "channel.feedback", ctx.Client);
            if (feedbackChannel is null) {
                await ctx.CreateResponseAsync("The feedback channel hasn't been setup.", true);
                return; 
            }

            var modal = new DiscordInteractionResponseBuilder()
                .WithTitle("Feedback")
                .WithCustomId("feedback-" + ctx.Guild.Id + "-" + ctx.User.Id)
                .AddComponents(new TextInputComponent("Feedback Input", "input", "I liked...\nI disliked...", null, true, DSharpPlus.TextInputStyle.Paragraph));

            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.Modal, modal);
        }
    }
}
