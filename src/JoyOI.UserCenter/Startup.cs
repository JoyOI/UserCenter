using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Pomelo.AspNetCore.Localization;
using Newtonsoft.Json;
using StackExchange.Redis;
using JoyOI.UserCenter.Hubs;
using JoyOI.UserCenter.Models;

namespace JoyOI.UserCenter
{
    public class Startup
    {
        public static IConfiguration Config;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddConfiguration(out Config);

            var redis = ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                EndPoints = { Config["Data:Redis:Host"] },
                Password = Config["Data:Redis:Password"],
                Ssl = false,
                ResponseTimeout = 100000,
                ChannelPrefix = "SHARED",
                DefaultDatabase = 1
            });

            services.AddDataProtection()
                .PersistKeysToRedis(redis, "DATA_PROTECTION_KEYS_");

            services.AddAesCrypto(Config["Security:PrivateKey"], Config["Security:IV"]);

            services.AddMvc();

            services.AddBlobStorage()
               .AddEntityFrameworkStorage<UserCenterContext>();

            services.AddSignalR()
                .AddRedis(x =>
                {
                    x.Options.EndPoints.Add(Config["Data:Redis:Host"]);
                    x.Options.Password = Config["Data:Redis:Password"];
                    x.Options.AbortOnConnectFail = false;
                    x.Options.Ssl = false;
                    x.Options.ResponseTimeout = 100000;
                    x.Options.ChannelPrefix = "USER_CENTER";
                    x.Options.DefaultDatabase = 3;
                });

            services.AddSmartUser<User, Guid>();

            services.AddEntityFrameworkMySql()
                .AddDbContextPool<UserCenterContext>(x => 
                {
                    x.UseMySql(Config["Data:MySQL"]);
                    x.UseMySqlLolita();
                });

            services.AddIdentity<User, Role>(x=> 
            {
                x.Password.RequireDigit = false;
                x.Password.RequiredLength = 0;
                x.Password.RequireLowercase = false;
                x.Password.RequireNonAlphanumeric = false;
                x.Password.RequireUppercase = false;
                x.User.AllowedUserNameCharacters = null;
            })
                .AddEntityFrameworkStores<UserCenterContext>()
                .AddDefaultTokenProviders();

            services.AddContextAccessor();

            services.AddPomeloLocalization(x =>
            {
                var cultures = JsonConvert.DeserializeObject<List<dynamic>>(File.ReadAllText(Path.Combine("Localization", "cultures.json")));
                foreach (dynamic c in cultures)
                    x.AddCulture(c.Cultures.ToObject<string[]>(), new JsonLocalizedStringStore(Path.Combine("Localization", c.Source.ToString())));
            });

            services.AddSmtpEmailSender("smtp.ym.163.com", 25, "Pomelo Foundation Test Account", "test@pomelo.cloud", "test@pomelo.cloud", "Test123456");
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.UseFrontendLocalizer();
            app.UseStaticFiles();
            app.UseDeveloperExceptionPage();
            app.UseWebSockets();
            app.UseSignalR(x => x.MapHub<MessageHub>("hubs"));
            app.UseBlobStorage("/js/jquery.pomelo.fileupload.js");
            app.UseAuthentication();
            app.UseSignalR(x =>
            {
                x.MapHub<MessageHub>("signalr/message");
            });
            app.MapWhen(x => x.Request.Host.ToString().StartsWith(Config["Domain:Api"]), x => x.UseMvc(y => y.MapRoute("apiRoute", "{action}/{id?}", new { controller = "Api" })));
            app.MapWhen(x => !x.Request.Host.ToString().StartsWith(Config["Domain:Api"]), x => x.UseMvcWithDefaultRoute());
            app.ApplicationServices.GetRequiredService<UserCenterContext>().InitializeAsync(app.ApplicationServices);
        }
    }
}
