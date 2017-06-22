using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Pomelo.Net.Smtp;
using Newtonsoft.Json;
using JoyOI.UserCenter.Models;

namespace JoyOI.UserCenter.Controllers
{
    public class RegisterController : BaseController
    {
        private static Regex emailRegex = new Regex("^\\s*([A-Za-z0-9_-]+(\\.\\w+)*@(\\w+\\.)+\\w{2,5})\\s*$");
        private const string usernameRegexString = "[A-Za-z0-9_-]{4,32}";
        private static Regex usernameRegex = new Regex("^(" + usernameRegexString + ")$");

        [NonAction]
        private IActionResult _Prompt(Action<Prompt> setupPrompt)
        {
            var prompt = new Prompt();
            setupPrompt(prompt);
            Response.StatusCode = prompt.StatusCode;
            return View("_Prompt", prompt);
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
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

        [HttpGet]
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

        [HttpPost]
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
    }
}
