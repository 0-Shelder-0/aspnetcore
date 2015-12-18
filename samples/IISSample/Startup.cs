using System;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.Logging;

namespace IISSample
{
    public class Startup
    {
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerfactory)
        {
            loggerfactory.AddConsole(LogLevel.Debug);

            var logger = loggerfactory.CreateLogger("Requests");

            app.UseIISPlatformHandler();
            
            app.Run(async (context) =>
            {
                logger.LogDebug("Received request: " + context.Request.Method + " " + context.Request.Path);

                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync("Hello World - " + DateTimeOffset.Now + Environment.NewLine);
                await context.Response.WriteAsync("User - " + context.User.Identity.Name + Environment.NewLine);
                foreach (var header in context.Request.Headers)
                {
                    await context.Response.WriteAsync(header.Key + ": " + header.Value + Environment.NewLine);
                }
            });
        }

        public static void Main(string[] args)
        {
            var application = new WebApplicationBuilder()
                .UseConfiguration(WebApplicationConfiguration.GetDefault(args))
                .UseStartup<Startup>()
                .Build();

            application.Run();
        }
    }
}
