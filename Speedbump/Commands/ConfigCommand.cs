using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Speedbump
{
    [SlashCommandGroup("config", "Guild Configuration")]
    [SlashCommandPermissions(DSharpPlus.Permissions.ManageGuild)]
    public class ConfigCommand : ApplicationCommandModule
    {
        [SlashCommand("list", "List all config items")]
        public async Task List(InteractionContext ctx)
        {
            var configs = GuildConfigConnector.GetAll(ctx.Guild.Id);
            var categories = configs.GroupBy(c => c.Label.Split('.')[0]);
            var e = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.CornflowerBlue)
                .WithTitle("Guild Configuration")
                .WithTimestamp(DateTimeOffset.Now);

            foreach (var c in categories)
            {
                e.AddField("-----", $"__{c.Key}__");
                foreach (var item in c)
                {
                    var valKey = item.Value ?? item.Default ?? null;

                    object val = null;

                    try
                    {
                        val = valKey is null ? null :
                            item.Type == GuildConfigType.Role ? ctx.Guild.Roles.First(r => r.Key.ToString() == valKey).Value.Mention :
                            item.Type == GuildConfigType.TextChannel || item.Type == GuildConfigType.Category ? ctx.Guild.Channels.First(c => c.Key.ToString() == valKey).Value.Mention :
                            item.Type == GuildConfigType.Text ? valKey :
                            "Error";
                    }
                    catch
                    {
                        e.AddField(item.Label.Split('.')[1], val?.ToString() ?? "~~", true);
                        continue;
                    }

                    e.AddField(item.Label.Split('.')[1], val?.ToString() ?? "~", true);
                }
            }

            await ctx.CreateResponseAsync(e, true);
        }

        [SlashCommand("setrole", "Set a role-type configuration item")]
        public async Task SetRole(InteractionContext ctx, 
            [Option("itemrole", "The configuration item to set")][Autocomplete(typeof(ConfigAutocompleteProvider))] string item,
            [Option("value", "The value of the item")]DiscordRole value)
        {
            var configs = GuildConfigConnector.GetAll(ctx.Guild.Id).Where(c => c.Type == GuildConfigType.Role);
            if (!configs.Select(c => c.Item).Contains(item)) { await ctx.CreateResponseAsync("Invalid item/value pair.", true); return;  }

            GuildConfigConnector.Set(ctx.Guild.Id, item, value.Id.ToString());
            await List(ctx);
        }

        [SlashCommand("setchannel", "Set a channel or category type configuration item")]
        public async Task SetTextChannel(InteractionContext ctx,
            [Option("itemchannel", "The configuration item to set")][Autocomplete(typeof(ConfigAutocompleteProvider))]string item,
            [Option("value", "The value of the item")] DiscordChannel value)
        {
            var configs = GuildConfigConnector.GetAll(ctx.Guild.Id).Where(c => c.Type == GuildConfigType.TextChannel || c.Type == GuildConfigType.Category);

            var selected = configs.FirstOrDefault(c => c.Item == item);

            if (selected is null || 
                !(
                    (value.Type == DSharpPlus.ChannelType.Text && selected.Type == GuildConfigType.TextChannel) || 
                    (value.Type == DSharpPlus.ChannelType.Category && selected.Type == GuildConfigType.Category)
                )) 
            { 
                await ctx.CreateResponseAsync("Invalid item/value pair.", true);
                return;
            }

            GuildConfigConnector.Set(ctx.Guild.Id, item, value.Id.ToString());
            await List(ctx);
        }

        [SlashCommand("settext", "Set a text-type configuration item")]
        public async Task SetText(InteractionContext ctx,
            [Option("itemtext", "The configuration item to set")][Autocomplete(typeof(ConfigAutocompleteProvider))] string item,
            [Option("value", "The value of the item")] string value)
        {
            var configs = GuildConfigConnector.GetAll(ctx.Guild.Id).Where(c => c.Type == GuildConfigType.Text);
            if (!configs.Select(c => c.Item).Contains(item)) { await ctx.CreateResponseAsync("Invalid item/value pair.", true); return; }

            GuildConfigConnector.Set(ctx.Guild.Id, item, value);
            await List(ctx);
        }
    }

    public class ConfigAutocompleteProvider : IAutocompleteProvider
    {
        public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var configs = GuildConfigConnector.GetAll(ctx.Guild.Id);
            return Task.FromResult(configs.Where(c =>
            {
                return
                    (ctx.FocusedOption.Name == "itemrole" && c.Type == GuildConfigType.Role) ||
                    (ctx.FocusedOption.Name == "itemchannel" && (c.Type == GuildConfigType.TextChannel || c.Type == GuildConfigType.Category)) ||
                    (ctx.FocusedOption.Name == "itemtext" && c.Type == GuildConfigType.Text);
            }).Select(c => new DiscordAutoCompleteChoice(c.Label, c.Item)));
        }
    }
}
