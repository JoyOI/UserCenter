using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace JoyOI.UserCenter.Hubs
{
    public class MessageHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddAsync(Context.ConnectionId, groupName);
        }
    }
}
