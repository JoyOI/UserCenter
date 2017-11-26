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
        [HttpGet("[controller]/{appId:Guid}/{openId:Guid}/{secret}")]
        [HttpGet("[controller]/window")]
        public async Task<IActionResult> Index(Guid appId, Guid openId, string secret, CancellationToken token)
        {
            if (User.Current != null)
            {
                return View();
            }
            else
            {
                var aes = new AesCrypto(Startup.Config["Chat:PrivateKey"], Startup.Config["Chat:IV"]);
                var open = await DB.OpenIds
                    .Include(x => x.User)
                    .Where(x => x.ApplicationId == appId)
                    .Where(x => x.Application.Type == ApplicationType.Official)
                    .Where(x => x.Application.Secret == aes.Decrypt(secret))
                    .Where(x => x.Id == openId)
                    .SingleOrDefaultAsync(token);

                if (open == null)
                {
                    return Content("登录失败");
                }

                var user = open.User;
                await SignInManager.SignInAsync(user, false);
                return View();
            }
        }

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

            var user = await DB.Users.SingleAsync(x => x.Id == receiverId, token);

            await hub.Clients.Group(user.UserName).InvokeAsync("onMessageReceived", User.Current.Id.ToString());
            return Content("ok");
        }

        [HttpGet]
        public async Task<IActionResult> GetId(string username, CancellationToken token)
        {
            var user = await DB.Users.Where(x => x.UserName == username).SingleAsync(token);
            return Content(user.Id.ToString());
        }

        [HttpGet]
        public async Task<IActionResult> GetContacts(CancellationToken token)
        {
            var query = DB.Messages
                .Include(x => x.Sender)
                .Include(x => x.Receiver)
                .Where(x => x.ReceiverId == User.Current.Id || x.SenderId == User.Current.Id)
                .OrderByDescending(x => x.SendTime)
                .DistinctBy(x => x.SenderId == User.Current.Id ? x.ReceiverId : x.SenderId)
                .Take(20)
                .ToList();

            var ret = new List<object>(20);

            foreach (var x in query)
            {
                if (x.ReceiverId == User.Current.Id)
                {
                    ret.Add(new
                    {
                        id = x.SenderId,
                        username = x.Sender.UserName,
                        avatarUrl = Url.Action("GetAvatar", "Account", new { id = x.SenderId.HasValue ? x.SenderId : default(Guid) }),
                        isRoot = x.SenderId.HasValue ? await User.Manager.IsInAnyRolesAsync(x.Sender, "Root") : true,
                        time = x.SendTime,
                        unread = DB.Messages.Where(y => !y.IsRead && y.ReceiverId == User.Current.Id && y.SenderId == x.SenderId).Count(),
                        message = x.Content
                    });
                }
                else
                {
                    ret.Add(new
                    {
                        id = x.ReceiverId,
                        username = x.Receiver.UserName,
                        avatarUrl = Url.Action("GetAvatar", "Account", new { id = x.ReceiverId }),
                        isRoot = await User.Manager.IsInAnyRolesAsync(x.Receiver, "Root"),
                        time = x.SendTime,
                        unread = DB.Messages.Where(y => !y.IsRead && y.ReceiverId == User.Current.Id && y.SenderId == x.ReceiverId).Count(),
                        message = x.Content
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
                .Where(x => x.ReceiverId == User.Current.Id && x.SenderId == userId || x.SenderId == User.Current.Id && x.ReceiverId == userId)
                .OrderByDescending(x => x.ReceiveTime)
                .Skip(page * 50)
                .Take(50)
                .ToListAsync(token);

            var target = await User.Manager.FindByIdAsync(userId.ToString());
            var targetIsRoot = await User.Manager.IsInAnyRolesAsync(target, "Root");
            var me = User.Current;
            var iAmRoot = await User.Manager.IsInAnyRolesAsync(me, "Root");
            
            var ret = messages.OrderBy(x => x.SendTime).Select(x => new
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
                time = new DateTime(x.SendTime.Year, x.SendTime.Month, x.SendTime.Day, x.SendTime.Hour, x.SendTime.Minute, 0),
                isRead = x.IsRead
            })
            .GroupBy(x => x.time)
            .Select(x => new { time = x.Key, messages = x.ToList() })
            .ToList();

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
                .Where(x => x.UserName != User.Current.UserName)
                .Where(x => x.UserName.Contains(name))
                .Take(5)
                .ToListAsync(token))
                .Select(x => new
                {
                    id = x.Id,
                    username = x.UserName,
                    avatarUrl = Url.Action("GetAvatar", "Account", new { id = x.Id }),
                    isRoot = User.Manager.IsInAnyRolesAsync(x, "Root"),
                    isOnline = x.Online > 0
                });

            return Json(ret);
        }
    }
}
