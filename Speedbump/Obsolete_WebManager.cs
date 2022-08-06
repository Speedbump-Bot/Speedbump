using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

using Newtonsoft.Json.Linq;

using System.Diagnostics;
using System.Net;
using System.Web;

namespace Speedbump
{
    [Obsolete]
    public class WebManager
    {
        IConfiguration Configuration;
        ILogger Logger;
        DiscordManager DiscordManager;
        WebApplication app;

        public WebManager(IConfiguration config, ILogger logger, Lifetime lifetime, DiscordManager discord)
        {
            Configuration = config;
            Logger = logger;
            DiscordManager = discord;
            lifetime.Add(End, Lifetime.ExitOrder.Normal);

            Start();
        }

        private void Start()
        {
            var www = Configuration.Get<string>("web.www");
            var mode = Configuration.Get<string>("web.wwwMode");
            var port = Configuration.Get<int>("web.port");

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions()
            {
                ContentRootPath = Directory.GetCurrentDirectory(),
                WebRootPath = mode == "file" ? www : null,
            });

            builder.Logging.ClearProviders().AddProvider(new ConverterILoggerFactory(Logger, "ASPNET"));
            builder.Logging.SetMinimumLevel(LogLevel.Information);

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(port);
            });
            builder.Services.AddControllers();
            builder.Services.AddSingleton(Configuration);
            builder.Services.AddSingleton(Logger);
            builder.Services.AddSingleton(DiscordManager);
            app = builder.Build();

            if (Debugger.IsAttached)
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDiscordAuth(Configuration);

            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllerRoute("default", "{controller=api}/{action=docs}"));

            if (mode == "file")
            {
                app.UseFileServer();
            } 
            else if (mode == "redirect")
            {
                app.UseWebSockets();
                app.Use(Redirect);
            }

            app.RunAsync();

            Logger.Information("Webserver online.");
        }
        private async Task Redirect(HttpContext ctx, Func<Task> next)
        {
            var server = Configuration.Get<string>("web.www");

            if (ctx.WebSockets.IsWebSocketRequest)
            {
                ctx.Response.Redirect(server);
                return;
            }

            var path = ctx.Request.Path;

            var client = new HttpClient();
            var res = await client.GetAsync(server + path + (path == "/" ? "index.html" : "") + (ctx.Request.QueryString.HasValue ? ctx.Request.QueryString.Value : ""));

            if (res.StatusCode == HttpStatusCode.OK)
            {
                ctx.Response.ContentType = res.Content.Headers.ContentType.MediaType;
            }

            ctx.Response.StatusCode = (int)res.StatusCode;
            await ctx.Response.WriteAsync(await res.Content.ReadAsStringAsync());
        }

        private void End(Lifetime.ExitCause cause)
        {
            try
            {
                app.StopAsync().GetAwaiter().GetResult();
            } catch { }
            try
            {
                app.DisposeAsync().GetAwaiter().GetResult();
            } catch { }
        }
    }
}
