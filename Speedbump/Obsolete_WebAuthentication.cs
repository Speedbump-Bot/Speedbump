using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

using Newtonsoft.Json.Linq;

using System.Diagnostics;
using System.Net;
using System.Web;

namespace Speedbump
{
    [Obsolete]
    public static class WebAuthentication
    {
        private static IConfiguration Configuration;

        public static void UseDiscordAuth(this WebApplication app, IConfiguration config)
        {
            Configuration = config;
            app.Use(Discord);
            app.Use(Auth);
        }

        private static async Task Discord(HttpContext ctx, Func<Task> next)
        {
            ctx.Items.Add("RequestTime", DateTimeOffset.UtcNow);

            if (ctx.Request.Path != "/fromdiscord" || ctx.Request.Query["code"] == StringValues.Empty)
            {
                await next();
                return;
            }

            var client = new HttpClient();
            var content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>()
            {
                new("client_id", Configuration.Get<string>("discord.clientId")),
                new("client_secret", Configuration.Get<string>("discord.clientSecret")),
                new("grant_type", "authorization_code"),
                new("code", ctx.Request.Query["code"][0]),
                new("redirect_uri", Configuration.Get<string>("discord.redirect")),
            });

            var api = Configuration.Get<string>("discord.api");

            var res = await client.PostAsync(api + "/oauth2/token", content);
            if (res.StatusCode != HttpStatusCode.OK)
            {
                await next();
                return;
            }

            var token = JObject.Parse(await res.Content.ReadAsStringAsync())["access_token"].Value<string>();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

            res = await client.GetAsync(api + "/users/@me");
            var userInfo = JObject.Parse(await res.Content.ReadAsStringAsync());

            var jwt = JWT.Issue(JWT.CreatePayload(userInfo), Configuration.Get<string>("web.jwt"));

            ctx.Response.Cookies.Delete("speedbumpAuth");
            ctx.Response.Cookies.Append("speedbumpAuth", jwt, new CookieOptions()
            {
                Expires = DateTimeOffset.UtcNow + TimeSpan.FromDays(6),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = !Debugger.IsAttached,
            });

            ctx.Response.Redirect("/");
            return;
        }

        private static async Task Auth(HttpContext ctx, Func<Task> next)
        {
            var clientId = Configuration.Get<string>("discord.clientId");
            var redirect = HttpUtility.UrlEncode(Configuration.Get<string>("discord.redirect"));

            var cookie = ctx.Request.Cookies["speedbumpAuth"];

            if (cookie is null)
            {
                ctx.Response.Redirect($"https://discord.com/api/oauth2/authorize?client_id={clientId}&redirect_uri={redirect}&response_type=code&scope=identify&prompt=consent");
                return;
            }

            var payload = JWT.GetPayload(cookie, Configuration.Get<string>("web.jwt"));
            if (payload is null)
            {
                ctx.Response.Redirect($"https://discord.com/api/oauth2/authorize?client_id={clientId}&redirect_uri={redirect}&response_type=code&scope=identify&prompt=consent");
                return;
            }

            ctx.Items.Add("userData", payload);

            await next();
        }
    }
}
