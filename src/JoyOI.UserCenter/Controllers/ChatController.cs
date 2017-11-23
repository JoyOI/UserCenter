using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using JoyOI.UserCenter.Models;
using JoyOI.UserCenter.Hubs;

namespace JoyOI.UserCenter.Controllers
{
    public class ChatController : BaseController
    {
        [HttpPost]
        public async Task<IActionResult> Send(
            [FromServices] IHubContext<MessageHub> hub,
            Guid receiverId, 
            string text, 
            CancellationToken token)
        {
            if (receiverId == User.Current.Id)
                return Content("fail");

            DB.Messages
                .Add(new Message
                {
                    Content = text,
                    IsRead = false,
                    ReceiverId = receiverId,
                    SendTime = DateTime.Now,
                    SenderId = User.Current.Id
                });

            await DB.SaveChangesAsync(token);
            await hub.Clients.Group(receiverId.ToString()).InvokeAsync("onMessageReceived");
            return Content("ok");
        }

        [HttpGet]
        public async Task<IActionResult> GetContacts(CancellationToken token)
        {
            var query = await DB.Messages
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x => x.ReceiverId == User.Current.Id || x.SenderId == User.Current.Id)
                .OrderByDescending(x => x.SendTime)
                .Take(20)
                .ToListAsync(token);

            var ret = new List<object>(20);

            foreach (var x in query)
            {
                if (x.ReceiverId == User.Current.Id)
                {
                    ret.Add(new
                    {
                        username = x.Sender.UserName,
                        avatarUrl = Url.Action("GetAvatar", "Account", new { id = x.SenderId.HasValue ? x.SenderId : default(Guid) }),
                        isRoot = x.SenderId.HasValue ? await User.Manager.IsInAnyRolesAsync(x.Sender, "Root") : true,
                        time = x.SendTime
                    });
                }
                else
                {
                    ret.Add(new
                    {
                        username = x.Receiver.UserName,
                        avatarUrl = Url.Action("GetAvatar", "Account", new { id = x.ReceiverId }),
                        isRoot = await User.Manager.IsInAnyRolesAsync(x.Receiver, "Root"),
                        time = x.SendTime
                    });
                }
            }

            return Json(ret);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(Guid userId, int page, CancellationToken token)
        {
            var messages = await DB.Messages
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x => x.ReceiverId == User.Current.Id || x.SenderId == User.Current.Id)
                .OrderByDescending(x => x.ReceiveTime)
                .Skip(page * 20)
                .Take(20)
                .ToListAsync(token);

            var target = await User.Manager.FindByIdAsync(userId.ToString());
            var targetIsRoot = await User.Manager.IsInAnyRolesAsync(target, "Root");
            var me = User.Current;
            var iAmRoot = await User.Manager.IsInAnyRolesAsync(me, "Root");

            var ret = messages.Select(x => new
            {
                sender = new
                {
                    id = x.SenderId,
                    username = x.SenderId.HasValue ? x.Sender.UserName : "System",
                    avatarUrl = Url.Action("GetAvatar", "Account", new { id = x.SenderId.HasValue ? x.SenderId : default(Guid) }),
                    isRoot = User.Current.Id == x.SenderId ? iAmRoot : targetIsRoot,
                    isMe = User.Current.Id == x.SenderId
                },
                receiver = new
                {
                    id = x.ReceiverId,
                    username = x.Receiver.UserName,
                    avatarUrl = Url.Action("GetAvatar", "Account", new { id = x.ReceiverId }),
                    isRoot = User.Current.Id == x.ReceiverId ? iAmRoot : targetIsRoot,
                    isMe = User.Current.Id == x.ReceiverId
                },
                content = x.Content,
                time = x.SendTime,
                isRead = x.IsRead
            });

            DB.Messages
                .Where(x => x.ReceiverId == User.Current.Id && x.SenderId == userId)
                .SetField(x => x.IsRead).WithValue(true)
                .Update();

            return Json(ret);
        }

        [HttpGet]
        public async Task<IActionResult> FindContact(string name, CancellationToken token)
        {
            var ret = (await DB.Users
                .Where(x => x.UserName.Contains(name))
                .Take(5)
                .ToListAsync(token))
                .Select(x => new
                {
                    id = x.Id,
                    username = x.UserName,
                    avatarUrl = Url.Action("GetAvatar", "Account", new { id = x.Id }),
                    isRoot = User.Manager.IsInAnyRolesAsync(x, "Root")
                });

            return Json(ret);
        }
    }
}
