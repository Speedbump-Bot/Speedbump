using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Speedbump.Commands
{
    [SlashCommandGroup("manager", "Manager Controls")]
    [SlashCommandPermissions(DSharpPlus.Permissions.ManageGuild)]
    public class ManagerCommand : ApplicationCommandModule
    {
        [SlashCommandGroup("filter", "Filter Controls")]
        [SlashCommandPermissions(DSharpPlus.Permissions.ManageGuild)]
        public class FilterCommand : ApplicationCommandModule
        {
            [SlashCommand("add", "Add to the guild filter.")]
            public async Task Add(InteractionContext ctx, [Option("match", "The phrase to match against.")] string match, [Option("Type", "The severity of the match.")] FilterMatchType type)
            {
                if (type == FilterMatchType.None)
                {
                    await ctx.CreateResponseAsync("Why would you set it to none?", true);
                    return;
                }

                await ctx.DeferAsync(true);
                var m = new FilterMatch()
                {
                    Match = match.ToLower().Trim(),
                    Guild = ctx.Guild.Id,
                    Type = type
                };

                var res = FilterConnector.AddMatch(m);
                await ctx.EditAsync(res ?
                    "I've added the phrase ||" + match + "|| to the filter." :
                    "||" + match + "|| is already on the filter.");

                if (res)
                {
                    var modinfo = GuildConfigConnector.GetChannel(ctx.Guild.Id, "channel.modinfo", ctx.Client);
                    if (modinfo is null) { return; }

                    var e = Extensions.Embed()
                        .WithTitle("Filter Modified")
                        .AddField("Added", $"||{m.Match}||");
                    await modinfo.SendMessageAsync(e);
                }
            }

            [SlashCommand("remove", "Remove from the guild filter.")]
            public async Task Remove(InteractionContext ctx, [Option("match", "The phrase to match against.")][Autocomplete(typeof(FilterMatchAutocompleteProvider))] string match)
            {
                await ctx.DeferAsync(true);
                match = match.ToLower().Trim();

                var res = FilterConnector.RemoveMatch(ctx.Guild.Id, match);
                await ctx.EditAsync(res ?
                    "I've removed the phrase ||" + match + "|| from the filter." :
                    "||" + match + "|| is not on the filter.");

                if (res)
                {
                    var modinfo = GuildConfigConnector.GetChannel(ctx.Guild.Id, "channel.modinfo", ctx.Client);
                    if (modinfo is null) { return; }

                    var e = Extensions.Embed()
                        .WithTitle("Filter Modified")
                        .AddField("Removed", $"||{match}||");
                    await modinfo.SendMessageAsync(e);
                }
            }

            [SlashCommand("list", "List filtered words.")]
            public async Task List(InteractionContext ctx)
            {
                await ctx.DeferAsync(true);
                var res = await ModerationUtility.ConfirmAction(ctx, "Warning", "You are about to view all filters. This may including many bad words. Proceed?");
                if (!res) { return; }
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(Extensions.Embed().WithDescription("```\n" +
                    string.Join('\n', FilterConnector.GetMatches(ctx.Guild.Id)
                        .OrderBy(o => o.Type)
                        .ThenBy(o => o.Match)
                        .Select(m => $"{m.Match,-15} ({m.Type})")) + "\n```"
                    ).WithColor(DiscordColor.Orange).WithTitle("Filtered Phrases")));
            }
        }

        [SlashCommandGroup("tag", "Tag Controls", true)]
        [SlashCommandPermissions(DSharpPlus.Permissions.ManageGuild)]
        public class TagCommand
        {
            [SlashCommand("create", "Create a tag.")]
            public async Task Add(InteractionContext ctx, 
                [Option("name", "The name of the tag.")]string name, 
                [Option("content", "The text content of the tag. Supports placeholders. Optional.")]string content = "", 
                [Option("media", "Attachment. Optional.")]DiscordAttachment attachment = null)
            {
                content = content.Trim().Replace("\\n", "\n");
                name = name.Trim();

                if (TagConnector.GetByNameAndGuild(name, ctx.Guild.Id) is not null)
                {
                    await ctx.CreateResponseAsync("That tag name already exists.", true);
                    return;
                }
                else if ((content is null || content == "") && attachment is null)
                {
                    await ctx.CreateResponseAsync("You must have either content or an attachment.", true);
                    return;
                }

                var tag = new Tag()
                {
                    Guild = ctx.Guild.Id,
                    Name = name,
                    TagID = Snowflake.Generate(),
                    Attachment = attachment?.Url,
                    Template = content,
                };
                TagConnector.Create(tag);

                await ctx.CreateResponseAsync("Tag created.", true);
            }

            [SlashCommand("delete", "Deletes a tag.")]
            public async Task Delete(InteractionContext ctx, [Option("name", "The name of the tag")][Autocomplete(typeof(TagAutocompleteProvider))] string tagName)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(TagConnector.Delete(ctx.Guild.Id, tagName) ?
                    "Tag deleted." : "That tag does not exist.").AsEphemeral(true));
            }
        }

        [SlashCommandGroup("level", "XP Level Controls", true)]
        [SlashCommandPermissions(DSharpPlus.Permissions.ManageGuild)]
        public class LevelCommand
        {
            [SlashCommand("add", "Adds a level.")]
            public async Task Add(InteractionContext ctx, 
                [Option("level", "The XP level to add a role for")]long level, 
                [Option("role", "The role to use for this level.")]DiscordRole role)
            {
                var l = new XPLevel()
                {
                    Guild = ctx.Guild.Id,
                    Level = (int)level,
                    Role = role.Id
                };

                await ctx.CreateResponseAsync(XPConnector.AddLevel(l) ? "Level created." : "Failed to create level. Either the level already exists, or you have the max of 10 levels.");
            }

            [SlashCommand("remove", "Removes a level.")]
            public async Task Remove(InteractionContext ctx, 
                [Option("level", "The level to remove.")][Autocomplete(typeof(XPLevelAutocompleteProvider))]long level)
            {
                var l = new XPLevel()
                {
                    Guild = ctx.Guild.Id,
                    Level = (int)level,
                };

                await ctx.CreateResponseAsync(XPConnector.DeleteLevel(l) ? "Level deleted" : "That level does not exist.");
            }
        }

        [SlashCommandGroup("role", "Control Toggleable Roles", true)]
        [SlashCommandPermissions(DSharpPlus.Permissions.ManageGuild)]
        public class RoleCommand
        {
            [SlashCommand("add", "Adds a toggleable role.")]
            public async Task Add(InteractionContext ctx,
                [Option("role", "The role")] DiscordRole role)
            {
                await ctx.CreateResponseAsync(RoleConnector.Add(ctx.Guild.Id, role.Id) ? "Role added." : "I couldn't add that role. Is it already added?", true);
            }

            [SlashCommand("remove", "Removes a toggleable role.")]
            public async Task Remove(InteractionContext ctx,
                [Option("role", "The role")] DiscordRole role)
            {
                await ctx.CreateResponseAsync(RoleConnector.Remove(ctx.Guild.Id, role.Id) ? "Role removed." : "I couldn't find that role.", true);
            }
        }

        [SlashCommandGroup("config", "Guild Configuration")]
        [SlashCommandPermissions(DSharpPlus.Permissions.ManageGuild)]
        public class ConfigCommand : ApplicationCommandModule
        {
            [SlashCommand("list", "List all config items")]
            public async Task List(InteractionContext ctx)
            {
                var configs = GuildConfigConnector.GetAll(ctx.Guild.Id);
                var categories = configs.GroupBy(c => c.Label.Split('.')[0]);
                var e = Extensions.Embed()
                    .WithTitle("Guild Configuration");

                foreach (var c in categories)
                {
                    var toPost = "";

                    foreach (var item in c)
                    {
                        var valKey = item.Value ?? item.Default ?? null;

                        var front = $"`{item.Label.Split('.')[1],-15}`";

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
                            toPost += front + ": " + (val?.ToString() ?? "~") + "\n";
                            continue;
                        }

                        toPost += front + ": " + (val?.ToString() ?? "") + "\n";
                    }
                    toPost = toPost.Trim();

                    e.AddField($"__{c.Key}__", toPost);
                }

                await ctx.CreateResponseAsync(e, true);
            }

            [SlashCommand("default", "Sets the config item to the default value.")]
            public async Task Default(InteractionContext ctx,
                [Option("item", "The configuration item to default")][Autocomplete(typeof(ConfigAutocompleteProvider))] string item)
            {
                var i = GuildConfigConnector.Get(ctx.Guild.Id, item);
                if (i is null)
                {
                    await ctx.CreateResponseAsync("Invalid item.", true);
                    return;
                }

                GuildConfigConnector.Set(ctx.Guild.Id, item, i.Default);
                await ctx.CreateResponseAsync("I set it to default.", true);
            }

            [SlashCommand("setrole", "Set a role-type configuration item")]
            public async Task SetRole(InteractionContext ctx,
                [Option("itemrole", "The configuration item to set")][Autocomplete(typeof(ConfigAutocompleteProvider))] string item,
                [Option("value", "The value of the item")] DiscordRole value)
            {
                var configs = GuildConfigConnector.GetAll(ctx.Guild.Id).Where(c => c.Type == GuildConfigType.Role);
                if (!configs.Select(c => c.Item).Contains(item)) { await ctx.CreateResponseAsync("Invalid item/value pair.", true); return; }

                GuildConfigConnector.Set(ctx.Guild.Id, item, value.Id.ToString());
                await List(ctx);
            }

            [SlashCommand("setchannel", "Set a channel or category type configuration item")]
            public async Task SetTextChannel(InteractionContext ctx,
                [Option("itemchannel", "The configuration item to set")][Autocomplete(typeof(ConfigAutocompleteProvider))] string item,
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
    }

    public class FilterMatchAutocompleteProvider : IAutocompleteProvider
    {
        public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx) =>
            Task.FromResult(
                FilterConnector.GetMatches(ctx.Guild.Id)
                    .Where(m => ctx.OptionValue.ToString().Trim() == "" || m.Match.ToLower().Contains(ctx.OptionValue.ToString().ToLower()))
                    .OrderBy(o => o.Type)
                    .ThenBy(o => o.Match)
                    .Select(m => new DiscordAutoCompleteChoice(m.Match + $" ({m.Type})", m.Match))
            );
    }

    public class XPLevelAutocompleteProvider : IAutocompleteProvider
    {
        public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx) =>
            Task.FromResult(
                XPConnector.GetLevels(ctx.Guild.Id)
                    .OrderBy(o => o.Level)
                    .Select(m => new DiscordAutoCompleteChoice($"{ctx.Guild.Roles[m.Role].Name} ({m.Level})", m.Level))
            );
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
                        (ctx.FocusedOption.Name == "itemtext" && c.Type == GuildConfigType.Text) ||
                        ctx.FocusedOption.Name == "item";
                })
                .Where(c => ctx.OptionValue.ToString().Trim() == "" || c.Label.ToLower().Contains(ctx.OptionValue.ToString().ToLower()))
                .Select(c => new DiscordAutoCompleteChoice(c.Label, c.Item)));
        }
    }
}
