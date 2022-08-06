using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace Speedbump
{
    public class RoleCommand : ApplicationCommandModule
    {
        [SlashCommand("role", "Toggles a role.")]
        public async Task Role(InteractionContext ctx,
            [Option("role", "The role")][Autocomplete(typeof(RoleAutocompleteProvider))]string roleS)
        {
            if (!ulong.TryParse(roleS, out var role))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Invalid role."));
                return;
            }

            await ctx.DeferAsync(true);
            var available = RoleConnector.GetRoles(ctx.Guild.Id);

            var r = ctx.Guild.GetRole(role);
            if (r is null || !available.Contains(role))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Invalid role."));
                return;
            }

            if (ctx.Member.Roles.Any(r2 => r2.Id == role))
            {
                await ctx.Member.RevokeRoleAsync(r);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Removed the role."));
            }
            else
            {
                await ctx.Member.GrantRoleAsync(r);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Added the role."));
            }
        }
    }

    public class RoleAutocompleteProvider : IAutocompleteProvider
    {
        public Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx) =>
            Task.FromResult(
                RoleConnector.GetRoles(ctx.Guild.Id)
                    .Select(m => new DiscordAutoCompleteChoice(ctx.Guild.Roles[m].Name, m.ToString()))
            );
    }
}
