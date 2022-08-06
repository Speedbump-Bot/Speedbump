using DSharpPlus;
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

            Client = new DiscordClient(new DiscordConfiguration()
            {
                Token = Configuration.Get<string>("discord.token"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Trace,
                LoggerFactory = new ConverterILoggerFactory(Logger, "Discord")
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

        private Task Slash_SlashCommandErrored(SlashCommandsExtension sender, DSharpPlus.SlashCommands.EventArgs.SlashCommandErrorEventArgs e)
        {
            Logger.Error(e.Exception);
            return Task.CompletedTask;
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
    }
}
