using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Speedbump
{
    public class TagCommandUser : ApplicationCommandModule
    {
        [SlashCommand("tag", "Get a tag!")]
        public async Task TagAsync(InteractionContext ctx, [Option("name", "The name of the tag")][Autocomplete(typeof(TagAutocompleteProvider))]string tagName)
        {
            if (tagName == "- All -")
            {
                var tags = TagConnector.GetByGuild(ctx.Guild.Id);
                var e2 = Extensions.Embed()
                    .WithDescription(string.Join("\n", tags.Select(t => t.Name)))
                    .WithTitle("Tag List")
                    .WithAuthor(ctx.User.Username, iconUrl: ctx.User.GetAvatarUrl(DSharpPlus.ImageFormat.Auto));
                await ctx.CreateResponseAsync(e2, true);
                return;
            }

            var tag = TagConnector.GetByNameAndGuild(tagName, ctx.Guild.Id);
            if (tag is null)
            {
                await ctx.CreateResponseAsync("I was unable to find the tag `" + tagName + "`.", true);
                return;
            }

            var e = Extensions.Embed()
                .WithTitle(tag.Name);

            if (tag.Template is not null)
            {
                e.WithDescription(Tag.GenerateFromTemplate(tag.Template, ctx.User, ctx.Channel));
            }
            if (tag.Attachment is not null)
            {
                e.WithImageUrl(tag.Attachment);
            }

            await ctx.CreateResponseAsync(e);
        }
    }

    public class TagAutocompleteProvider : IAutocompleteProvider
    {
        public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx)
        {
            var list = TagConnector.GetByGuild(ctx.Guild.Id);
            list = list.OrderBy(o => o.Name).ToList();
            list.Insert(0, new Tag()
            {
                Name = "- All -"
            });

            list = list.Where(l => ctx.OptionValue.ToString().Trim() == "" || l.Name.ToLower().Contains(ctx.OptionValue.ToString().ToLower())).ToList();

            return Task.FromResult(list.Select(m => new DiscordAutoCompleteChoice(m.Name, m.Name)));
        }
    }
}
