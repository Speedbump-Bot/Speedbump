using DSharpPlus.SlashCommands;

namespace Speedbump
{
    public class RequireModeratorAttribute : SlashCheckBaseAttribute
    {
        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            var modRole = ctx.Guild.GetRole(ulong.Parse(GuildConfigConnector.Get(ctx.Guild.Id, "role.moderator").Value));
            return Task.FromResult(ctx.Member.Roles.Any(r => r.Id == modRole.Id));
        }
    }

    public class RequireManagerAttribute : SlashCheckBaseAttribute
    {
        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx) =>
            Task.FromResult(PermissionConnector.HasGuildEditPermission(ctx.User.Id, ctx.Guild.Id, ctx.Client));
    }
}
