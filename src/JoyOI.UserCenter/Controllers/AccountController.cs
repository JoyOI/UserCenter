using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pomelo.Net.Smtp;
using Newtonsoft.Json;
using JoyOI.UserCenter.Models;

namespace JoyOI.UserCenter.Controllers
{
    public class AccountController : BaseController
    {
        private static Regex emailRegex = new Regex("^\\s*([A-Za-z0-9_-]+(\\.\\w+)*@(\\w+\\.)+\\w{2,5})\\s*$");
        private const string usernameRegexString = "[A-Za-z0-9_-]{4,32}";
        private static Regex usernameRegex = new Regex("^(" + usernameRegexString + ")$");
        private static MD5 _md5 = MD5.Create();

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var users = DB.Users
                .Where(x => x.UserName == username || x.Email == username)
                .OrderByDescending(x => x.UserName == username);

            if (users.Count() == 0)
            {
                return Prompt(x => 
                {
                    x.Title = SR["Login Failed"];
                    x.Details = SR["The username or password is incorrect."];
                });
            }

            var result = await SignInManager.PasswordSignInAsync(users.First(), password, true, false);
            if (!result.Succeeded && users.Count() > 1)
            {
                result = await SignInManager.PasswordSignInAsync(users.Last(), password, true, false);
            }

            if (!result.Succeeded)
            {
                return Prompt(x =>
                {
                    x.Title = SR["Login Failed"];
                    x.Details = SR["The username or password is incorrect."];
                });
            }

            return RedirectToAction("Index");
        }

        [NonAction]
        private IActionResult _Prompt(Action<Prompt> setupPrompt)
        {
            var prompt = new Prompt();
            setupPrompt(prompt);
            Response.StatusCode = prompt.StatusCode;
            return View("_Prompt", prompt);
        }

        [HttpGet("/Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("/Register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(
            string email,
            [FromHeader] string Referer,
            [FromServices] IEmailSender EmailSender,
            [FromServices] AesCrypto Aes)
        {
            if (string.IsNullOrEmpty(email))
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Email is invalid"];
                    x.Details = SR["The email address cannot be null.", email];
                    x.StatusCode = 400;
                });
            }
            else if (!emailRegex.IsMatch(email))
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Email is invalid"];
                    x.Details = SR["The email address <{0}> is invalid.", email];
                    x.StatusCode = 400;
                });
            }
            else if (DB.Users.Any(x => x.Email == email))
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Email is invalid"];
                    x.Details = SR["The email address <{0}> is already existed.", email];
                    x.StatusCode = 400;
                });
            }

            var code = Aes.Encrypt(JsonConvert.SerializeObject(new Tuple<string, DateTime, string>(email, DateTime.Now.AddHours(2), Referer)));
            await EmailSender.SendEmailAsync(email, SR["JoyOI Register Verification"], SR["<p>Please click the following link to continue register.</p><p><a href='{0}'>Click here.</a></p>", Request.Scheme + "://" + Request.Host + Url.Action("VerifyEmail", new { code = code })]);

            return _Prompt(x =>
            {
                x.Title = SR["Succeeded"];
                x.Details = SR["We have sent you an email which contains a URL to continue registering operations."];
                x.RedirectText = SR["Go to email"];
                x.RedirectUrl = "//mail." + email.Split('@')[1];
            });
        }

        [HttpGet("/Register/VerifyEmail")]
        public IActionResult VerifyEmail(string code, [FromServices] AesCrypto Aes)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<Tuple<string, DateTime, string>>(Aes.Decrypt(code));
                if (obj.Item2 < DateTime.Now)
                {
                    return _Prompt(x =>
                    {
                        x.Title = SR["Invalid Code"];
                        x.Details = SR["Your code is out of date, please retry to register."];
                        x.HideBack = true;
                        x.RedirectText = SR["Retry"];
                        x.RedirectUrl = Url.Action("Index");
                    });
                }
                ViewBag.Email = obj.Item1;
                ViewBag.AesEmail = Aes.Encrypt(obj.Item1);
                ViewBag.Referer = obj.Item3;
                return View();
            }
            catch
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Invalid Code"];
                    x.Details = SR["Your code is invalid, please retry to register."];
                    x.HideBack = true;
                    x.RedirectText = SR["Retry"];
                    x.RedirectUrl = Url.Action("Index");
                });
            }
        }

        [HttpPost("/Register/VerifyEmail")]
        public async Task<IActionResult> VerifyEmail(
            string email,
            string username,
            string password,
            string confirm,
            string nickname,
            string referer,
            [FromServices] AesCrypto Aes)
        {
            string parsedEmail = null;
            try
            {
                parsedEmail = Aes.Decrypt(email);
            }
            catch
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Register Failed"];
                    x.Details = SR["Your email address is invalid, please open this page from the verification email content."];
                    x.StatusCode = 400;
                    x.HideBack = true;
                });
            }

            if (DB.Users.Any(x => x.UserName == username))
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Register Failed"];
                    x.Details = SR["The username <{0}> is already exists. Please pick another one.", username];
                    x.StatusCode = 400;
                });
            }
            else if (!usernameRegex.IsMatch(username))
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Register Failed"];
                    x.Details = SR["The username must match the rule {0}.", usernameRegexString];
                    x.StatusCode = 400;
                });
            }
            else if (password != confirm)
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Register Failed"];
                    x.Details = SR["The confirm password is not match the password."];
                    x.StatusCode = 400;
                });
            }
            else if (DB.Users.Any(x => x.Email == parsedEmail))
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Register Failed"];
                    x.Details = SR["The email address <{0}> is already existed.", email];
                    x.StatusCode = 400;
                });
            }

            var user = new User
            {
                UserName = username,
                Nickname = nickname,
                Email = parsedEmail.ToLower(),
                EmailConfirmed = true,
                AvatarSource = AvatarSource.GravatarPolling,
                AvatarData = parsedEmail
            };

            var result = await UserManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Register Failed"];
                    x.Details = string.Join("<br/>", result.Errors.Select(y => SR[y.Description]));
                    x.StatusCode = 400;
                });
            }

            await SignInManager.SignInAsync(user, true);
            if (!string.IsNullOrEmpty(referer))
            {
                return Redirect(referer);
            }
            else
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Register Succeeded"];
                    x.Details = SR["{0}, welcome to JoyOI!", username];
                    x.HideBack = true;
                });
            }
        }

        [HttpGet]
        [ResponseCache(Duration = 1000 * 60 * 60 * 24 * 7)]
        public async Task<IActionResult> GetAvatar(Guid id, int size = 230)
        {
            var user = DB.Users
                .SingleOrDefault(x => x.Id == id);
            if (user == null)
            {
                return File(System.IO.File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "non-avatar.png")), "image/png", "avatar.png");
            }
            try
            {
                if (user.AvatarSource == AvatarSource.GravatarPolling)
                {
                    var md5_email = string.Join("", _md5.ComputeHash(Encoding.UTF8.GetBytes(user.AvatarData)).Select(x => x.ToString("x2")));
                    using (var client = new HttpClient() { BaseAddress = new Uri("https://www.gravatar.com") })
                    {
                        var result = await client.GetAsync($"/avatar/{ md5_email }?d={ HttpContext.Request.Scheme }://{ HttpContext.Request.Host }/images/non-avatar.png&s={ size }");
                        var bytes = await result.Content.ReadAsByteArrayAsync();
                        return File(bytes, "image/png", "avatar.png");
                    }
                }
                else if (user.AvatarSource == AvatarSource.WeChatPolling)
                {
                    // TODO: support wechat avatar
                    throw new NotSupportedException();
                }
                else // Local storage
                {
                    var bytes = (await DB.Blobs.SingleAsync(x => x.Id == Guid.Parse(user.AvatarData))).Bytes;
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
    }
}
