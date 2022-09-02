using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

using System.Diagnostics;
using System.Reflection;

namespace Speedbump
{
    public class DiscordManager
    {
        private IConfiguration Configuration;
        private ILogger Logger;

        public DiscordClient Client;

        public DiscordManager(IConfiguration config, ILogger logger, Lifetime lifetime)
        {
            lifetime.Add(End, Lifetime.ExitOrder.Normal);

            Configuration = config;
            Logger = logger;

            var token = Configuration.Get<string>("discord.token");

            Logger.AddRedacted(token);
            ((Logger)Logger).LogHandlers.Add(LogHandler);

            Client = new DiscordClient(new DiscordConfiguration()
            {
                Token = token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Trace,
                LoggerFactory = new ConverterILoggerFactory(Logger, "Discord", lifetime)
            });

            var slash = Client.UseSlashCommands();
            slash.SlashCommandErrored += Slash_SlashCommandErrored;
            if (Debugger.IsAttached)
            {
                slash.RegisterCommands(Assembly.GetExecutingAssembly(), Configuration.Get<ulong>("discord.debugGuild"));
            }
            else
            {
                slash.RegisterCommands(Assembly.GetExecutingAssembly());
            }

            Client.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromSeconds(30)
            });

            Client.Ready += Client_Ready;
            Client.ConnectAsync().GetAwaiter().GetResult();
            Logger.Information("Discord connected.");
        }

        private async Task Slash_SlashCommandErrored(SlashCommandsExtension sender, DSharpPlus.SlashCommands.EventArgs.SlashCommandErrorEventArgs e)
        {
            try
            {
                await e.Context.CreateResponseAsync("I've run into an error. I've let my devs know.");
            }
            catch
            {
                try
                {
                    await e.Context.EditAsync("I've run into an error. I've let my devs know.");
                } catch { }
            }

            Logger.Error($"tickControl ```cs\n {e.Exception}\n```\n\nUser: {e.Context.Member.Mention}\nCommand: {e.Context.CommandName}\nChannel: {e.Context.Channel.Mention}\nServer: {e.Context.Guild.Name}\n" +
                $"Type: {e.Context.Type}\nInteraction: {e.Context.InteractionId}");
        }

        private Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs e)
        {
            Logger.Information("Discord ready.");
            return Task.CompletedTask;
        }

        private void End(Lifetime.ExitCause cause)
        {
            if (Client is not null)
            {
                try
                {
                    Client.DisconnectAsync();
                } catch { }
                try
                {
                    Client.Dispose();
                }
                catch { }
            }
        }

        private void LogHandler(Logger.LogMessage msg)
        {
            var ignore = new List<string>()
            {
                "Connection terminated",
                "Connection closed",
                "DSharpPlus, version",
                "Session resumed",
            };

            if (ignore.Any(i => msg.Content.Contains(i))) { return; }

            if (msg.Level < Enum.Parse<LogLevel>(Configuration.Get<string>("logging.levels.discord"))) { return; }
            var webhookUrl = Configuration.Get<string>("discord.logWebhook");

            var client = new DiscordWebhookClient();
            var webhook = client.AddWebhookAsync(new Uri(webhookUrl)).GetAwaiter().GetResult();

            var embed = Extensions.Embed()
                .WithColor(DiscordColor.Orange)
                .WithTitle(msg.Level.ToString())
                .WithTimestamp(msg.Time)
                .WithDescription(msg.Content.StartsWith("tickControl ") ? msg.Content.Replace("tickControl ", "") : $"```\n{msg.Content}\n```")
                .AddField("Location", msg.File + ":" + msg.Line);

            webhook.ExecuteAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }
    }
}
