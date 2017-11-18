using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Pomelo.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using JoyOI.UserCenter.Models;

namespace JoyOI.UserCenter.Controllers
{
    public class ApiController : BaseController<UserCenterContext, User, Guid>
    {
        private static Random _random = new Random();
        private static string _randomStringDictionary = "qwertyuiopasdfghjklzxcvbnm1234567890";
        private static MD5 _md5 = MD5.Create();

        private IActionResult ApiResult(object data) => Json(new ApiBody { code = 200, msg = "ok", data = data });
        private IActionResult ApiResult(string msg, int statusCode = 400)
        {
            Response.StatusCode = 200;
            return Json(new ApiBody { code = statusCode, msg = msg, data = null });
        }

        [NonAction]
        private IActionResult _Prompt(Action<Prompt> setupPrompt)
        {
            var prompt = new Prompt();
            setupPrompt(prompt);
            Response.StatusCode = prompt.StatusCode;
            return View("_Prompt", prompt);
        }


        private static string _generateString(int length)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                sb.Append(_randomStringDictionary[_random.Next() % _randomStringDictionary.Count()]);
            }
            return sb.ToString();
        }

        private static string _generateCallBackUrl(string url, string code)
        {
            var queryStringStartPosition = url.LastIndexOf('?');

            if (queryStringStartPosition >= 0)
            {
                var baseUrl = url.Substring(0, queryStringStartPosition);
                var queryString = new QueryString(url.Substring(queryStringStartPosition));
                queryString = queryString.Add("code", code);
                return baseUrl + queryString.ToString();
            }
            else
            {
                return url + "?code=" + code;
            }
        } 

        public Application Application
        {
            get
            {
                if (ControllerContext.RouteData.Values.ContainsKey("id"))
                {
                    var id = Guid.Parse(ControllerContext.RouteData.Values["id"].ToString());
                    return DB.Applications.SingleOrDefault(x => x.Id == id && x.Type != ApplicationType.Pending);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// 授权页
        /// </summary>
        /// <param name="id"></param>
        /// <param name="CallBackUrl"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Authorize(Guid id, string CallBackUrl)
        {
            if (Application == null)
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Request denied"];
                    x.Details = SR["The application is not found."];
                    x.StatusCode = 404;
                });
            }
            else if (!CallBackUrl.StartsWith(Application.CallBackUrl))
            {
                return _Prompt(x => 
                {
                    x.Title = SR["Request denied"];
                    x.Details = SR["The callback URL is invalid."];
                    x.StatusCode = 400;
                });
            }
            return View(Application);
        }

        /// <summary>
        /// 授权页
        /// </summary>
        /// <param name="id"></param>
        /// <param name="CallBackUrl"></param>
        /// <param name="Username"></param>
        /// <param name="Password"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Authorize(Guid id, string CallBackUrl, string Username, string Password, CancellationToken token)
        {
            if (Application == null)
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Request denied"];
                    x.Details = SR["The application is not found."];
                    x.StatusCode = 404;
                });
            }
            else if (!CallBackUrl.StartsWith(Application.CallBackUrl))
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Request denied"];
                    x.Details = SR["The callback URL is invalid."];
                    x.StatusCode = 400;
                });
            }
            else if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Request denied"];
                    x.Details = SR["Username or password cannot be null."];
                    x.StatusCode = 400;
                });
            }

            var result = await SignInManager.PasswordSignInAsync(Username, Password, true, false);
            if (!result.Succeeded)
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Sign in failed"];
                    x.Details = SR[result.ToString()];
                    x.StatusCode = 401;
                });
            }
            else
            {
                var user = await DB.Users.SingleAsync(x => x.UserName == Username, token);
                var openId = await DB.OpenIds.SingleOrDefaultAsync(x => x.UserId == user.Id && !x.Disabled, token);
                if (openId == null)
                {
                    openId = new OpenId
                    {
                        AccessToken = _generateString(64),
                        ApplicationId = Application.Id,
                        ExpireTime = DateTime.Now.AddDays(15),
                        Code = _generateString(64),
                        UserId = user.Id
                    };
                    DB.OpenIds.Add(openId);
                }
                else
                {
                    if (DateTime.Now > openId.ExpireTime)
                    {
                        openId.ExpireTime = DateTime.Now.AddDays(15);
                        openId.AccessToken = _generateString(64);
                        openId.Code = _generateString(64);
                    }
                }
                await DB.SaveChangesAsync(token);
                return Redirect(_generateCallBackUrl(CallBackUrl, openId.Code));
            }
        }

        public async Task<IActionResult> CheckUser(
            Guid id, 
            string username, 
            string password, 
            string secret,
            CancellationToken token)
        {
            if (Application == null)
            {
                return ApiResult(SR["Application is not found."], 404);
            }
            else if (Application.Secret != secret)
            {
                return ApiResult(SR["Application secret is invalid."]);
            }
            else if (!(await SignInManager.CheckPasswordSignInAsync(new User { UserName = username }, password, false)).Succeeded)
            {
                return ApiResult(SR["The username or password is incorrect."]);
            }
            else
            {
                var user = await UserManager.FindByNameAsync(username);
                var openId = await DB.OpenIds.SingleOrDefaultAsync(x => x.ApplicationId == id && x.UserId == user.Id && !x.Disabled, token);
                if (openId == null)
                {
                    openId = new OpenId
                    {
                        AccessToken = _generateString(64),
                        ApplicationId = id,
                        Code = null,
                        ExpireTime = DateTime.Now.AddDays(15),
                        UserId = user.Id
                    };
                    DB.OpenIds.Add(openId);
                }
                else
                {
                    openId.AccessToken = _generateString(64);
                    openId.ExpireTime = DateTime.Now.AddDays(15);
                    openId.Code = null;
                }
                
                await DB.SaveChangesAsync(token);

                return ApiResult(new
                {
                    access_token = openId.AccessToken,
                    expire_time = openId.ExpireTime
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TrustedAuthorize(Guid id, string secret, string username, string password, CancellationToken token)
        {
            if (Application == null)
            {
                return ApiResult(SR["Application is not found."], 404);
            }
            else if (Application.Secret != secret)
            {
                return ApiResult(SR["Application secret is invalid."]);
            }
            else if (!await UserManager.CheckPasswordAsync(await UserManager.FindByNameAsync(username), password))
            {
                return ApiResult(SR["The username or password is incorrect."], 401);
            }
            else
            {
                var user = await UserManager.FindByNameAsync(username);
                var openId = await DB.OpenIds.SingleOrDefaultAsync(x => x.ApplicationId == id && x.UserId == user.Id && !x.Disabled, token);
                if (openId == null)
                {
                    openId = new OpenId
                    {
                        AccessToken = _generateString(64),
                        ApplicationId = id,
                        Code = null,
                        ExpireTime = DateTime.Now.AddDays(15),
                        UserId = user.Id
                    };
                    DB.OpenIds.Add(openId);
                }
                else
                {
                    openId.AccessToken = _generateString(64);
                    openId.ExpireTime = DateTime.Now.AddDays(15);
                    openId.Code = null;
                }

                await DB.SaveChangesAsync(token);

                return ApiResult(new
                {
                    open_id = openId.Id,
                    access_token = openId.AccessToken,
                    expire_time = openId.ExpireTime,
                    is_root = (await UserManager.GetRolesAsync(openId.User)).Any(x => x == "Root")
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetUserProfile(Guid id, string secret, Guid openId, string accessToken, CancellationToken token)
        {
            if (Application == null)
            {
                return ApiResult(SR["Application is not found."], 404);
            }
            else if (Application.Secret != secret)
            {
                return ApiResult(SR["Application secret is invalid."]);
            }
            else
            {
                var _openId = await DB.OpenIds
                    .Include(x => x.User)
                    .SingleOrDefaultAsync(x => x.Id == openId, token);
                if (_openId == null)
                {
                    return ApiResult(SR["The open id is not found.", 404]);
                }
                else if (_openId.AccessToken != accessToken || DateTime.Now > _openId.ExpireTime)
                {
                    return ApiResult(SR["Your access token is invalid."], 403);
                }
                else
                {
                    return ApiResult(new
                    {
                        open_id = _openId.Id,
                        nickname = _openId.User.Nickname,
                        phone = _openId.User.PhoneNumber,
                        email = _openId.User.Email,
                        sex = _openId.User.Sex
                    });
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetExtensionCoin(
            Guid id, 
            string field, 
            string accessToken, 
            Guid openId,
            string secret,
            CancellationToken token)
        {
            if (Application == null)
            {
                return ApiResult(SR["Application is not found."], 404);
            }
            else if (Application.Secret != secret)
            {
                return ApiResult(SR["Application secret is invalid."]);
            }
            else if (Application.ExtensionPermissions.Object.Any(x => x == field.ToLower()))
            {
                return ApiResult(SR["This application does not have the permission to access this field"]);
            }
            else if (!await DB.OpenIds.AnyAsync(x => x.Id == openId && !x.Disabled, token))
            {
                return ApiResult(SR["The user is not found."], 404);
            }
            else
            {
                var _openId = await DB.OpenIds
                    .Include(x => x.User)
                    .SingleAsync(x => x.Id == openId, token);

                if (_openId.AccessToken != accessToken || DateTime.Now > _openId.ExpireTime)
                {
                    return ApiResult(SR["Your access token is invalid."], 403);
                }
                else
                {
                    return ApiResult(_openId.User.Extensions.Object[field.ToLower()]);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetExtensionCoin(
            Guid id,
            string field,
            long value,
            string accessToken,
            Guid openId,
            string secret,
            CancellationToken token)
        {
            if (Application == null)
            {
                return ApiResult(SR["Application is not found."], 404);
            }
            else if (Application.Secret != secret)
            {
                return ApiResult(SR["Application secret is invalid."]);
            }
            else if (Application.ExtensionPermissions.Object.Any(x => x == field.ToLower()))
            {
                return ApiResult(SR["This application does not have the permission to access this field"]);
            }
            else if (!await DB.OpenIds.AnyAsync(x => x.Id == openId && !x.Disabled, token))
            {
                return ApiResult(SR["The user is not found."], 404);
            }
            else
            {
                var _openId = await DB.OpenIds
                    .Include(x => x.User)
                    .SingleAsync(x => x.Id == openId, token);
                
                if (_openId.AccessToken != accessToken || DateTime.Now > _openId.ExpireTime)
                {
                    return ApiResult(SR["Your access token is invalid."], 403);
                }
                else
                {
                    _openId.User.Extensions.Object[field] = value;
                    _openId.User.Extensions = JsonConvert.SerializeObject(_openId.User.Extensions.Object);

                    var result = await DB.Users
                        .Where(x => x.Id == _openId.UserId && x.ConcurrencyStamp == _openId.User.ConcurrencyStamp)
                        .SetField(x => x.Extensions).WithValue(_openId.User.Extensions)
                        .UpdateAsync(token);

                    if (result == 0)
                    {
                        return ApiResult(SR["The concurrency stamp was out of date."]);
                    }

                    DB.Users.Attach(_openId.User);

                    return ApiResult(null);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> IncreaseExtensionCoin(
            Guid id,
            string field,
            long value,
            string accessToken,
            Guid openId,
            string secret,
            CancellationToken token)
        {
            if (Application == null)
            {
                return ApiResult(SR["Application is not found."], 404);
            }
            else if (Application.Secret != secret)
            {
                return ApiResult(SR["Application secret is invalid."]);
            }
            else if (Application.ExtensionPermissions.Object.Any(x => x == field.ToLower()))
            {
                return ApiResult(SR["This application does not have the permission to access this field"]);
            }
            else if (!await DB.OpenIds.AnyAsync(x => x.Id == openId && !x.Disabled, token))
            {
                return ApiResult(SR["The user is not found."], 404);
            }
            else
            {
updateExtensionCoin:
                var _openId = await DB.OpenIds
                    .Include(x => x.User)
                    .SingleAsync(x => x.Id == openId, token);

                if (_openId.AccessToken != accessToken || DateTime.Now > _openId.ExpireTime)
                {
                    return ApiResult(SR["Your access token is invalid."], 403);
                }
                else
                {
                    _openId.User.Extensions.Object[field] = _openId.User.Extensions.Object[field] + value;
                    _openId.User.Extensions = JsonConvert.SerializeObject(_openId.User.Extensions.Object);

                    var result = await DB.Users
                        .Where(x => x.Id == _openId.UserId && x.ConcurrencyStamp == _openId.User.ConcurrencyStamp)
                        .SetField(x => x.Extensions).WithValue(_openId.User.Extensions)
                        .UpdateAsync(token);

                    if (result == 0)
                    {
                        goto updateExtensionCoin;
                    }

                    DB.Users.Attach(_openId.User);

                    return ApiResult(null);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> DecreaseExtensionCoin(
            Guid id,
            string field,
            long value,
            string accessToken,
            Guid OpenId,
            string secret,
            CancellationToken token)
        {
            if (Application == null)
            {
                return ApiResult(SR["Application is not found."], 404);
            }
            else if (Application.Secret != secret)
            {
                return ApiResult(SR["Application secret is invalid."]);
            }
            else if (Application.ExtensionPermissions.Object.Any(x => x == field.ToLower()))
            {
                return ApiResult(SR["This application does not have the permission to access this field"]);
            }
            else if (!DB.OpenIds.Any(x => x.Id == OpenId && !x.Disabled))
            {
                return ApiResult(SR["The user is not found."], 404);
            }
            else
            {
                updateExtensionCoin:
                var openId = await DB.OpenIds
                    .Include(x => x.User)
                    .SingleAsync(x => x.Id == OpenId, token);

                if (openId.AccessToken != accessToken || DateTime.Now > openId.ExpireTime)
                {
                    return ApiResult(SR["Your access token is invalid."], 403);
                }
                else
                {
                    openId.User.Extensions.Object[field] = openId.User.Extensions.Object[field] - value;
                    openId.User.Extensions = JsonConvert.SerializeObject(openId.User.Extensions.Object);

                    var result = await DB.Users
                        .Where(x => x.Id == openId.UserId && x.ConcurrencyStamp == openId.User.ConcurrencyStamp)
                        .SetField(x => x.Extensions).WithValue(openId.User.Extensions)
                        .UpdateAsync(token);

                    if (result == 0)
                    {
                        goto updateExtensionCoin;
                    }

                    DB.Users.Attach(openId.User);

                    return ApiResult(null);
                }
            }
        }

        [HttpGet]
        [ResponseCache(Duration = 1000 * 60 * 60 * 24 * 7)]
        public async Task<IActionResult> GetAvatar(Guid id, CancellationToken token, int size = 230)
        {
            var openId = DB.OpenIds
                .Include(x => x.User)
                .SingleOrDefault(x => x.Id == id);
            if (openId == null)
            {
                return File(System.IO.File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "non-avatar.png")), "image/png", "avatar.png");
            }
            try
            {
                if (openId.User.AvatarSource == AvatarSource.GravatarPolling)
                {
                    var md5_email = string.Join("", _md5.ComputeHash(Encoding.UTF8.GetBytes(openId.User.AvatarData)).Select(x => x.ToString("x2")));
                    using (var client = new HttpClient() { BaseAddress = new Uri("https://www.gravatar.com") })
                    {
                        var result = await client.GetAsync($"/avatar/{ md5_email }?d={ HttpContext.Request.Scheme }://{ HttpContext.Request.Host }/images/non-avatar.png&s={ size }", token);
                        var bytes = await result.Content.ReadAsByteArrayAsync();
                        if (bytes.Length > 0)
                        {
                            return File(bytes, "image/png", "avatar.png");
                        }
                        else
                        {
                            return File(System.IO.File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "non-avatar.png")), "image/gif", "avatar.png");
                        }
                    }
                }
                else if (openId.User.AvatarSource == AvatarSource.WeChatPolling)
                {
                    // TODO: support wechat avatar
                    throw new NotSupportedException();
                }
                else // Local storage
                {
                    var bytes = (await DB.Blobs.SingleAsync(x => x.Id == Guid.Parse(openId.User.AvatarData), token)).Bytes;
                    if (bytes.Length > 0)
                    {
                        return File(bytes, "image/png", "avatar.png");
                    }
                    else
                    {
                        return File(System.IO.File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "non-avatar.png")), "image/gif", "avatar.png");
                    }
                }
            }
            catch
            {
                return File(System.IO.File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "non-avatar.png")), "image/gif", "avatar.png");
            }
        }

        public async Task<IActionResult> GetUsername(
            Guid id,
            string secret,
            Guid openId,
            string accessToken,
            CancellationToken token)
        {
            if (Application == null)
            {
                return ApiResult(SR["Application is not found."], 404);
            }
            else if (Application.Secret != secret)
            {
                return ApiResult(SR["Application secret is invalid."]);
            }
            else if (Application.Type != ApplicationType.Official)
            {
                return ApiResult(SR["This application does not have the permission to access usernames."]);
            }
            else if (!await DB.OpenIds.AnyAsync(x => x.Id == openId && !x.Disabled, token))
            {
                return ApiResult(SR["The user is not found."], 404);
            }
            else
            {
                var _openId = await DB.OpenIds
                    .Include(x => x.User)
                    .SingleAsync(x => x.Id == openId, token);

                if (_openId.AccessToken != accessToken || DateTime.Now > _openId.ExpireTime)
                {
                    return ApiResult(SR["Your access token is invalid."], 403);
                }
                else
                {
                    return ApiResult(data: _openId.User.UserName);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendSmsToUser(
            Guid id,
            string secret,
            Guid OpenId,
            string accessToken,
            string content,
            CancellationToken token)
        {
            if (Application == null)
            {
                return ApiResult(SR["Application is not found."], 404);
            }
            else if (Application.Secret != secret)
            {
                return ApiResult(SR["Application secret is invalid."]);
            }
            else
            {
                var openId = await DB.OpenIds
                    .Include(x => x.User)
                    .SingleAsync(x => x.Id == OpenId, token);

                if (openId.AccessToken != accessToken || DateTime.Now > openId.ExpireTime)
                {
                    return ApiResult(SR["Your access token is invalid."], 403);
                }

                if (!openId.User.PhoneNumberConfirmed)
                {
                    return ApiResult(SR["The phone number is not comfirmed."], 400);
                }

                var phone = openId.User.PhoneNumber;
                using (var client = new HttpClient() { BaseAddress = new Uri("http://www.inolink.com") })
                using (var response = await client.GetAsync($"/ws/BatchSend.aspx?CorpID={ Configuration["SMS:CorpId"] }&Pwd={ Configuration["SMS:Pwd"] }&Mobile={ phone }&Content={ System.Net.WebUtility.UrlEncode(content) }"))
                {
                    var text = await response.Content.ReadAsStringAsync();
                    if (text == "1")
                    {
                        return ApiResult(SR["The SMS sent successfully"]);
                    }
                    else
                    {
                        return ApiResult(SR["The SMS sent failed with code {0}", text], 500);
                    }
                }
            }
        }

        public async Task<IActionResult> SendSms(
            Guid id,
            string secret,
            string phone,
            string content,
            CancellationToken token)
        {
            if (Application == null)
            {
                return ApiResult(SR["Application is not found."], 404);
            }
            else if (Application.Secret != secret)
            {
                return ApiResult(SR["Application secret is invalid."]);
            }
            else
            {
                using (var client = new HttpClient() { BaseAddress = new Uri("http://www.inolink.com") })
                using (var response = await client.GetAsync($"/ws/BatchSend.aspx?CorpID={ Configuration["SMS:CorpId"] }&Pwd={ Configuration["SMS:Pwd"] }&Mobile={ phone }&Content={ System.Net.WebUtility.UrlEncode(content) }"))
                {
                    var text = await response.Content.ReadAsStringAsync();
                    if (text == "1")
                    {
                        return ApiResult(SR["The SMS sent successfully"]);
                    }
                    else
                    {
                        return ApiResult(SR["The SMS sent failed with code {0}", text], 500);
                    }
                }
            }
        }
    }
}
