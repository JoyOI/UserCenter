using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Pomelo.AspNetCore.Extensions.BlobStorage.Models;

namespace JoyOI.UserCenter.Models
{
    public class UserCenterContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IBlobStorageDbContext
    {
        public DbSet<Application> Applications { get; set; }

        public DbSet<ExtensionLog> ExtensionLogs { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<OpenId> OpenIds { get; set; }

        public DbSet<TransferMoneyLog> TransferMoneyLogs { get; set; }

        public DbSet<UserLog> UserLogs { get; set; }

        public DbSet<Blob> Blobs { get; set; }

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
                e.HasIndex(x => x.RequestToken);
            });

            builder.Entity<TransferMoneyLog>(e => 
            {
                e.HasIndex(x => x.Price);
                e.HasIndex(x => x.Time);
            });

            builder.Entity<User>(e =>
            {
                e.HasIndex(x => x.Sex);
            });

            builder.Entity<UserLog>(e => 
            {
                e.HasIndex(x => x.Time);
            });
        }
    }
}
