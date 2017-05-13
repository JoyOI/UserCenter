using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private IActionResult ApiResult(object data) => Json(new ApiBody { code = 200, msg = "ok", data = data });
        private IActionResult ApiResult(string msg, int statusCode = 400)
        {
            Response.StatusCode = 200;
            return Json(new ApiBody { code = statusCode, msg = msg, data = null });
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
                    return DB.Applications.SingleOrDefault(x => x.Id == id);
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
                return Prompt(x =>
                {
                    x.Title = SR["Request denied"];
                    x.Details = SR["The application is not found."];
                    x.StatusCode = 404;
                });
            }
            else if (!CallBackUrl.StartsWith(Application.CallBackUrl))
            {
                return Prompt(x => 
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
        public async Task<IActionResult> Authorize(Guid id, string CallBackUrl, string Username, string Password)
        {
            if (Application == null)
            {
                return Prompt(x =>
                {
                    x.Title = SR["Request denied"];
                    x.Details = SR["The application is not found."];
                    x.StatusCode = 404;
                });
            }
            else if (!CallBackUrl.StartsWith(Application.CallBackUrl))
            {
                return Prompt(x =>
                {
                    x.Title = SR["Request denied"];
                    x.Details = SR["The callback URL is invalid."];
                    x.StatusCode = 400;
                });
            }
            else if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                return Prompt(x =>
                {
                    x.Title = SR["Request denied"];
                    x.Details = SR["Username or password cannot be null."];
                    x.StatusCode = 400;
                });
            }

            var result = await SignInManager.PasswordSignInAsync(Username, Password, true, false);
            if (!result.Succeeded)
            {
                return Prompt(x =>
                {
                    x.Title = SR["Sign in failed"];
                    x.Details = SR[result.ToString()];
                    x.StatusCode = 401;
                });
            }
            else
            {
                var user = DB.Users.Single(x => x.UserName == Username);
                var openId = DB.OpenIds.SingleOrDefault(x => x.UserId == user.Id);
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
                DB.SaveChanges();
                return Redirect(_generateCallBackUrl(CallBackUrl, openId.Code));
            }
        }

        public async Task<IActionResult> CheckUser(
            Guid id, 
            string username, 
            string password, 
            string secret)
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
                var openId = DB.OpenIds.SingleOrDefault(x => x.ApplicationId == id && x.UserId == user.Id);
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
                
                await DB.SaveChangesAsync();

                return ApiResult(new
                {
                    access_token = openId.AccessToken,
                    expire_time = openId.ExpireTime
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TrustedAuthorize(Guid id, string secret, string username, string password)
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
                return ApiResult(SR["The username or password is incorrect."], 401);
            }
            else
            {
                var user = await UserManager.FindByNameAsync(username);
                var openId = DB.OpenIds.SingleOrDefault(x => x.ApplicationId == id && x.UserId == user.Id);
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

                await DB.SaveChangesAsync();

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
        public IActionResult GetUserProfile(Guid id, string secret, Guid openId, string accessToken)
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
                var _openId = DB.OpenIds
                    .Include(x => x.User)
                    .SingleOrDefault(x => x.Id == openId);
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
                        avatar_source = _openId.User.AvatarSource,
                        avatar_data = _openId.User.AvatarData,
                        phone = _openId.User.PhoneNumber,
                        email = _openId.User.Email,
                        sex = _openId.User.Sex
                    });
                }
            }
        }

        [HttpPost]
        public IActionResult GetExtensionCoin(
            Guid id, 
            string field, 
            string accessToken, 
            Guid OpenId,
            string secret)
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
            else if (!DB.OpenIds.Any(x => x.Id == OpenId))
            {
                return ApiResult(SR["The user is not found."], 404);
            }
            else
            {
                var openId = DB.OpenIds
                    .Include(x => x.User)
                    .Single(x => x.Id == OpenId);

                if (openId.AccessToken != accessToken || DateTime.Now > openId.ExpireTime)
                {
                    return ApiResult(SR["Your access token is invalid."], 403);
                }
                else
                {
                    return ApiResult(openId.User.Extensions.Object[field.ToLower()]);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetExtensionCoin(
            Guid id,
            string field,
            long value,
            string accessToken,
            Guid OpenId,
            string secret)
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
            else if (!DB.OpenIds.Any(x => x.Id == OpenId))
            {
                return ApiResult(SR["The user is not found."], 404);
            }
            else
            {
                var openId = DB.OpenIds
                    .Include(x => x.User)
                    .Single(x => x.Id == OpenId);
                
                if (openId.AccessToken != accessToken || DateTime.Now > openId.ExpireTime)
                {
                    return ApiResult(SR["Your access token is invalid."], 403);
                }
                else
                {
                    openId.User.Extensions.Object[field] = value;
                    openId.User.Extensions = JsonConvert.SerializeObject(openId.User.Extensions.Object);

                    var result = await DB.Users
                        .Where(x => x.Id == openId.UserId && x.ConcurrencyStamp == openId.User.ConcurrencyStamp)
                        .SetField(x => x.Extensions).WithValue(openId.User.Extensions)
                        .UpdateAsync();

                    if (result == 0)
                    {
                        return ApiResult(SR["The concurrency stamp was out of date."]);
                    }

                    DB.Users.Attach(openId.User);

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
            Guid OpenId,
            string secret)
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
            else if (!DB.OpenIds.Any(x => x.Id == OpenId))
            {
                return ApiResult(SR["The user is not found."], 404);
            }
            else
            {
updateExtensionCoin:
                var openId = DB.OpenIds
                    .Include(x => x.User)
                    .Single(x => x.Id == OpenId);

                if (openId.AccessToken != accessToken || DateTime.Now > openId.ExpireTime)
                {
                    return ApiResult(SR["Your access token is invalid."], 403);
                }
                else
                {
                    openId.User.Extensions.Object[field] = openId.User.Extensions.Object[field] + value;
                    openId.User.Extensions = JsonConvert.SerializeObject(openId.User.Extensions.Object);

                    var result = await DB.Users
                        .Where(x => x.Id == openId.UserId && x.ConcurrencyStamp == openId.User.ConcurrencyStamp)
                        .SetField(x => x.Extensions).WithValue(openId.User.Extensions)
                        .UpdateAsync();

                    if (result == 0)
                    {
                        goto updateExtensionCoin;
                    }

                    DB.Users.Attach(openId.User);

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
            string secret)
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
            else if (!DB.OpenIds.Any(x => x.Id == OpenId))
            {
                return ApiResult(SR["The user is not found."], 404);
            }
            else
            {
                updateExtensionCoin:
                var openId = DB.OpenIds
                    .Include(x => x.User)
                    .Single(x => x.Id == OpenId);

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
                        .UpdateAsync();

                    if (result == 0)
                    {
                        goto updateExtensionCoin;
                    }

                    DB.Users.Attach(openId.User);

                    return ApiResult(null);
                }
            }
        }
    }
}
