using System;
using GitlabTelegramBot.DB;
using GitlabTelegramBot.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NLog.Web;

namespace GitlabTelegramBot
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            using (var client = new TelegramBotDBContext())
            {
                if (client.Database.EnsureCreated())
                {
                    client.Migrate();
                }
            }


            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEntityFrameworkSqlite().AddDbContext<TelegramBotDBContext>();

            services.AddSingleton<ITelegramBot, Bot>();
            services.Configure<GitlabConfig>(options =>
            {
                options.Host = Configuration.GetSection("GitlabHost")?.Value;
                options.Token = Configuration.GetSection("GitlabToken")?.Value;
                options.Admin = Configuration.GetSection("GitlabAdmin")?.Value;
            });
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, ITelegramBot bot)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();
            loggerFactory.AddNLog();
            app.UseMvc();

            app.AddNLogWeb();
            env.ConfigureNLog("nlog.config");

            var accessToken = Configuration.GetSection("TelegramAccessToken")?.Value;
            var botname = Configuration.GetSection("TelegramBotName")?.Value;
            bot.Connect(accessToken, botname);
            bot.Start();
        }
    }
}
