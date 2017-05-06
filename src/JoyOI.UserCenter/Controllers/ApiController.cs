using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Pomelo.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using JoyOI.UserCenter.Models;

namespace JoyOI.UserCenter.Controllers
{
    public class ApiController : BaseController<UserCenterContext, User, Guid>
    {
        private static Random _random = new Random();
        private static string _randomStringDictionary = "qwertyuiopasdfghjklzxcvbnm1234567890";

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
    }
}
