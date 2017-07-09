using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Pomelo.AspNetCore.Extensions.BlobStorage.Models;

namespace JoyOI.UserCenter.Models
{
    public class UserCenterContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IBlobStorageDbContext
    {
        public UserCenterContext(DbContextOptions opt) : base(opt)
        {
        }

        public DbSet<Application> Applications { get; set; }

        public DbSet<ExtensionLog> ExtensionLogs { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<OpenId> OpenIds { get; set; }

        public DbSet<RelationShip> RelationShips { get; set; }

        public DbSet<TransferMoneyLog> TransferMoneyLogs { get; set; }

        public DbSet<UserLog> UserLogs { get; set; }

        public DbSet<Blob> Blobs { get; set; }

        public async void InitializeAsync(IServiceProvider services, CancellationToken token = default(CancellationToken))
        {
            if (await Database.EnsureCreatedAsync(token))
            {
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                await roleManager.CreateAsync(new IdentityRole<Guid>("Root"));

                var bytes = await File.ReadAllBytesAsync(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "non-avatar.png"), token);
                var icon = new Blob
                {
                    ContentType = "image/png",
                    FileName = "icon.png",
                    Time = DateTime.Now
                };

                Blobs.Add(icon);

                Applications.Add(new Application
                {
                    Id = Guid.Parse("b453aa01-680e-49ca-a332-9d3ae296af9f"),
                    CallBackUrl = "http://callback",
                    Name = "Empty",
                    Secret = "0",
                    Type = ApplicationType.Official,
                    IconId = icon.Id
                });
                await SaveChangesAsync(token);
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.SetupBlobStorage();

            builder.Entity<ExtensionLog>(e => 
            {
                e.HasIndex(x => x.Time);
            });

            builder.Entity<Message>(e =>
            {
                e.HasIndex(x => x.ReceiveTime);
                e.HasIndex(x => x.SendTime);
                e.HasIndex(x => x.IsRead);
            });

            builder.Entity<OpenId>(e =>
            {
                e.HasIndex(x => x.AccessToken);
                e.HasIndex(x => x.ExpireTime);
                e.HasIndex(x => x.Code);
                e.HasIndex(x => x.Disabled);
            });

            builder.Entity<RelationShip>(e =>
            {
                e.HasKey(x => new { x.FocuserId, x.FocuseeId });
                e.HasIndex(x => x.Time);
            });

            builder.Entity<TransferMoneyLog>(e => 
            {
                e.HasIndex(x => x.Price);
                e.HasIndex(x => x.Time);
            });

            builder.Entity<User>(e =>
            {
                e.HasIndex(x => x.Sex);
                e.HasIndex(x => x.Email).IsUnique();
            });

            builder.Entity<UserLog>(e => 
            {
                e.HasIndex(x => x.Time);
            });
        }
    }
}
