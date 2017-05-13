using System;
using Microsoft.EntityFrameworkCore;
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

        public void Initialize()
        {
            if (Database.EnsureCreated())
            {
                Applications.Add(new Application
                {
                    Id = Guid.Parse("b453aa01-680e-49ca-a332-9d3ae296af9f"),
                    CallBackUrl = "http://callback",
                    Name = "Empty",
                    Secret = "0",
                    Type = ApplicationType.Official
                });
                SaveChanges();
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
