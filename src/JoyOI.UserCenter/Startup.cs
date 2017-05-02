using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using JoyOI.UserCenter.Models;

namespace JoyOI.UserCenter
{
    public class Startup
    {
        public static IConfiguration Config;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddConfiguration(out Config);

            var redis = ConnectionMultiplexer.Connect(Config["Data:Redis"]);
            services.AddDataProtection()
                .PersistKeysToRedis(redis, "DATA_PROTECTION_KEYS_");

            services.AddAesCrypto(Config["Security:PrivateKey"], Config["Security:IV"]);

            services.AddRedis();

            services.AddMvc();

            services.AddBlobStorage()
               .AddEntityFrameworkStorage<UserCenterContext>();

            services.AddRedis(x =>
            {
                x.ConnectionString = Config["Host:Redis"];
                x.Database = 1;
                x.EventKey = "HD_QUICK_RESPONSE_SIGNALR_INSTANCE";
            })
               .AddSignalR(options =>
               {
                   options.Hubs.EnableDetailedErrors = true;
               });

            services.AddSmartUser<User, Guid>();

            services.AddDistributedRedisCache(x =>
            {
                x.Configuration = Config["Host:Redis"];
                x.InstanceName = "HD_QR_";
            });

            services.AddEntityFrameworkMySql()
                .AddDbContext<UserCenterContext>(x => x.UseMySql(Config["Data:MySQL"]));

            services.AddIdentity<User, IdentityRole<Guid>>(x=> 
            {
                x.Password.RequireDigit = false;
                x.Password.RequiredLength = 0;
                x.Password.RequireLowercase = false;
                x.Password.RequireNonAlphanumeric = false;
                x.Password.RequireUppercase = false;
                x.User.AllowedUserNameCharacters = null;
            })
                .AddEntityFrameworkStores<UserCenterContext, Guid>()
                .AddDefaultTokenProviders();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();
            app.UseDeveloperExceptionPage();
            app.UseWebSockets();
            app.UseSignalR();
            app.UseBlobStorage("/js/jquery.pomelo.fileupload.js");
            app.UseIdentity();

            // TODO: Map to mvc
        }
    }
}
