using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using JoyOI.UserCenter.Models;
using System;

namespace JoyOI.UserCenter.Hubs
{
    public class MessageHub : Hub
    {
        private UserCenterContext DB;

        public MessageHub(UserCenterContext db)
        {
            DB = db;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();

            DB.Users
                .Where(x => x.UserName == Context.User.Identity.Name)
                .SetField(x => x.Online).Plus(1)
                .Update();

            await Groups.AddAsync(Context.ConnectionId, Context.User.Identity.Name);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await base.OnDisconnectedAsync(exception);

            DB.Users
                .Where(x => x.UserName == Context.User.Identity.Name && x.Online > 0)
                .SetField(x => x.Online).Subtract(1)
                .Update();

            await Groups.RemoveAsync(Context.ConnectionId, Context.User.Identity.Name);
        }
    }
}
