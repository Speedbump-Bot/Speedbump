using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

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

                if (FilterConnector.AddMatch(m))
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I've added the phrase ||" + match + "|| to the filter."));
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("||" + match + "|| is already on the filter."));
                }
            }

            [SlashCommand("remove", "Remove from the guild filter.")]
            public async Task Remove(InteractionContext ctx, [Option("match", "The phrase to match against.")][Autocomplete(typeof(FilterMatchAutocompleteProvider))] string match)
            {
                await ctx.DeferAsync(true);
                match = match.ToLower().Trim();

                if (FilterConnector.RemoveMatch(ctx.Guild.Id, match))
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I've removed the phrase ||" + match + "|| from the filter."));
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("||" + match + "|| is not on the filter."));
                }
            }

            [SlashCommand("list", "List filtered words.")]
            public async Task List(InteractionContext ctx)
            {
                await ctx.DeferAsync(true);
                var res = await ModerationUtility.ConfirmAction(ctx, "Warning", "You are about to view all filters. This may including many bad words. Proceed?");
                if (!res) { return; }
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(new DiscordEmbedBuilder().WithDescription(
                    string.Join('\n', FilterConnector.GetMatches(ctx.Guild.Id)
                        .OrderBy(o => o.Type)
                        .ThenBy(o => o.Match)
                        .Select(m => $"{m.Match} ({m.Type})"))
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
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("That tag name already exists.").AsEphemeral(true));
                    return;
                }
                else if ((content is null || content == "") && attachment is null)
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("You must have either content or an attachment.").AsEphemeral(true));
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

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Tag created.").AsEphemeral(true));
            }

            [SlashCommand("delete", "Deletes a tag.")]
            public async Task Delete(InteractionContext ctx, [Option("name", "The name of the tag")][Autocomplete(typeof(TagAutocompleteProvider))] string tagName)
            {
                if (TagConnector.Delete(ctx.Guild.Id, tagName))
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Tag deleted.").AsEphemeral(true));
                }
                else
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("That tag does not exist.").AsEphemeral(true));
                }
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

                if (XPConnector.AddLevel(l))
                {
                    await ctx.CreateResponseAsync("Level created.");
                }
                else
                {
                    await ctx.CreateResponseAsync("Failed to create level. Either the level already exists, or you have the max of 10 levels.");
                }
            }

            [SlashCommand("remove", "Removes a level.")]
            public async Task Remove(InteractionContext ctx, 
                [Option("level", "The level to remove.")][Autocomplete(typeof(XPLevelAutocompleteProvider))]long level)
            {
                if (XPConnector.DeleteLevel(new XPLevel()
                {
                    Guild = ctx.Guild.Id,
                    Level = (int)level,
                }))
                {
                    await ctx.CreateResponseAsync("Level deleted.");
                }
                else
                {
                    await ctx.CreateResponseAsync("That level does not exist.");
                }
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
                await ctx.CreateResponseAsync(RoleConnector.Add(ctx.Guild.Id, role.Id) ? "Role added." : "I couldn't add that role. Is it already added?");
            }

            [SlashCommand("remove", "Removes a toggleable role.")]
            public async Task Remove(InteractionContext ctx,
                [Option("role", "The role")] DiscordRole role)
            {
                await ctx.CreateResponseAsync(RoleConnector.Remove(ctx.Guild.Id, role.Id) ? "Role removed." : "I couldn't find that role.");
            }
        }
    }

    public class FilterMatchAutocompleteProvider : IAutocompleteProvider
    {
        public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx) =>
            Task.FromResult(
                FilterConnector.GetMatches(ctx.Guild.Id)
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
}
