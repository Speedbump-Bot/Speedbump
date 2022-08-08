using DSharpPlus;

namespace Speedbump.DiscordEventHandlers
{
    public class FeedbackHandler
    {
        public DiscordClient Discord;

        public FeedbackHandler(DiscordManager discord)
        {
            Discord = discord.Client;
            Discord.ModalSubmitted += Discord_ModalSubmitted;
        }

        private async Task Discord_ModalSubmitted(DiscordClient sender, DSharpPlus.EventArgs.ModalSubmitEventArgs e)
        {
            var feedbackChannel = GuildConfigConnector.GetChannel(e.Interaction.Guild.Id, "channel.feedback", Discord);
            if (feedbackChannel is null) { return; }

            var em = Extensions.Embed()
                .WithTitle("Feedback Received")
                .WithAuthor(e.Interaction.User.Username + "#" + e.Interaction.User.Discriminator, iconUrl: e.Interaction.User.GetAvatarUrl(ImageFormat.Auto))
                .WithDescription(e.Values["input"]);

            await feedbackChannel.SendMessageAsync(em);

            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DSharpPlus.Entities.DiscordInteractionResponseBuilder()
                .WithContent("Your feedback has been submitted. Thank you!").AddEmbed(em).AsEphemeral(true));
        }
    }
}
