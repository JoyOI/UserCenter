using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Pomelo.AspNetCore.Extensions.BlobStorage.Models;
using Pomelo.Net.Smtp;
using Newtonsoft.Json;
using JoyOI.UserCenter.Models;
using JoyOI.UserCenter.Models.ViewModels;

namespace JoyOI.UserCenter.Controllers
{
    public class AccountController : BaseController
    {
        private static Random _random = new Random();
        private static Regex emailRegex = new Regex("^\\s*([A-Za-z0-9_-]+(\\.\\w+)*@(\\w+\\.)+\\w{2,5})\\s*$");
        private static Regex phoneRegex = new Regex("^[+0-9]*$");
        private const string usernameRegexString = "[\u3040-\u309F\u30A0-\u30FF\u4e00-\u9fa5A-Za-z0-9_-]{4,32}";
        private static Regex usernameRegex = new Regex("^(" + usernameRegexString + ")$");
        private static MD5 _md5 = MD5.Create();

        [NonAction]
        private IActionResult _Prompt(Action<Prompt> setupPrompt)
        {
            var prompt = new Prompt();
            setupPrompt(prompt);
            Response.StatusCode = prompt.StatusCode;
            return View("_Prompt", prompt);
        }
        
        [Authorize]
        [HttpGet("[controller]/{userId:Guid?}")]
        public async Task<IActionResult> Index(Guid? userId, CancellationToken token)
        {
            ViewBag.ImageId = _random.Next() % 21 + 1;

            if (!userId.HasValue)
            {
                userId = User.Current.Id;
            }

            var user = await DB.Users
                .SingleOrDefaultAsync(x => x.Id == userId.Value, token);

            if (user == null)
            {
                return Prompt(x =>
                {
                    x.Title = SR["User Not Found"];
                    x.Details = SR["The specified user is not found."];
                    x.StatusCode = 404;
                });
            }

            ViewBag.Role = (await User.Manager.GetRolesAsync(user)).SingleOrDefault();

            return View(user);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, CancellationToken token)
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
                    x.StatusCode = 400;
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
                    x.StatusCode = 400;
                });
            }

            return RedirectToAction("Index", "Account");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            Guid id, 
            string nickName, 
            string school, 
            Sex sex, 
            AvatarSource avatarSource,
            IFormFile avatar, 
            string gravatar,
            CancellationToken token)
        {
            var user = await DB.Users
                .SingleOrDefaultAsync(x => x.Id == id, token);
            if (user == null)
            {
                return Prompt(x =>
                {
                    x.Title = SR["User Not Found"];
                    x.Details = SR["The specified user is not found."];
                    x.StatusCode = 404;
                });
            }

            if (avatarSource == AvatarSource.WeChatPolling)
            {
                return Prompt(x =>
                {
                    x.Title = SR["Operation Failed"];
                    x.Details = SR["WeChat avatar polling has not been supported."];
                    x.StatusCode = 400;
                });
            }

            user.Nickname = nickName;
            user.School = school;
            user.Sex = sex;
            user.AvatarSource = avatarSource;
            switch (avatarSource)
            {
                case AvatarSource.GravatarPolling:
                    user.AvatarData = gravatar;
                    break;
                case AvatarSource.LocalStorage:
                    var bytes = avatar.ReadAllBytes();
                    var blob = new Blob
                    {
                        Id = Guid.NewGuid(),
                        Bytes = bytes,
                        ContentLength = bytes.Length,
                        FileName = avatar.FileName,
                        Time = DateTime.Now,
                        ContentType = avatar.ContentType
                    };
                    DB.Blobs.Add(blob);
                    user.AvatarData = blob.Id.ToString();
                    break;
            }
            await DB.SaveChangesAsync(token);
            return Prompt(x => 
            {
                x.Title = SR["Succeeded"];
                x.Details = SR["Profile updated successfully."];
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Password(Guid id, string oldPassword, string newPassword, string confirm, string role, CancellationToken token)
        {
            var user = await DB.Users
                .SingleOrDefaultAsync(x => x.Id == id, token);

            if (user == null)
            {
                return Prompt(x =>
                {
                    x.Title = SR["User Not Found"];
                    x.Details = SR["The specified user is not found."];
                    x.StatusCode = 404;
                });
            }

            if (!User.IsInRole("Root"))
            {
                if (!await User.Manager.CheckPasswordAsync(user, oldPassword))
                {
                    return Prompt(x =>
                    {
                        x.Title = SR["Operation Failed"];
                        x.Details = SR["The old password is incorrect."];
                        x.StatusCode = 400;
                    });
                }
            }
            else if (User.Current.Id != id)
            {
                if (role == "Member")
                {
                    await User.Manager.RemoveFromRolesAsync(user, await User.Manager.GetRolesAsync(user));
                }
                else
                {
                    await User.Manager.RemoveFromRolesAsync(user, await User.Manager.GetRolesAsync(user));
                    await User.Manager.AddToRoleAsync(user, role);
                }
            }

            var result = await User.Manager.ChangePasswordAsync(user, oldPassword, newPassword);
            if (!result.Succeeded)
            {
                return Prompt(x =>
                {
                    x.Title = SR["Operation Failed"];
                    x.Details = string.Join("\r\n", result.Errors.Select(y => y.Description));
                    x.StatusCode = 400;
                });
            }

            return Prompt(x =>
            {
                x.Title = SR["Password Updated"];
                x.Details = SR["The new password is active now."];
            });
        }

        [HttpGet("/Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("/Register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string phone,
            [FromHeader] string Referer,
            [FromServices] AesCrypto Aes,
            CancellationToken token)
        {
            if (string.IsNullOrEmpty(phone))
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Phone number is invalid"];
                    x.Details = SR["The phone number cannot be null.", phone];
                    x.StatusCode = 400;
                });
            }
            else if (!phoneRegex.IsMatch(phone))
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Phone number is invalid"];
                    x.Details = SR["The phone number {0} is invalid.", phone];
                    x.StatusCode = 400;
                });
            }
            else if (await DB.Users.AnyAsync(x => x.PhoneNumber == phone, token))
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Phone number is invalid"];
                    x.Details = SR["The phone number {0} is already existed.", phone];
                    x.StatusCode = 400;
                });
            }
            else if (!string.IsNullOrEmpty(Request.Cookies["register"]))
            {
                var data = JsonConvert.DeserializeObject<dynamic>(Aes.Decrypt(Request.Cookies["register"]));
                var send = (DateTime)data.send;
                if (DateTime.Now < send.AddMinutes(2))
                {
                    return Prompt(x => 
                    {
                        x.Title = SR["Verify Code Send Failed"];
                        x.Details = SR["Please wait {0} seconds.", (send.AddMinutes(2) - DateTime.Now).TotalSeconds];
                    });
                }
            }

            var code = _random.Next(100000, 999999);
            Response.Cookies.Append("register", Aes.Encrypt(JsonConvert.SerializeObject(new
            {
                code = code.ToString(),
                phone = phone,
                expire = DateTime.Now.AddMinutes(30),
                send = DateTime.Now
            })));

            var content = SR["You are registering a Joy OI ID. The verify code is: {0}", code];
            await Lib.SMS.SendSmsAsync(Configuration["SMS:CorpId"], Configuration["SMS:Pwd"], phone, content);

            return View("VerifyCode");
        }

        [HttpGet("/Register/VerifyPhone")]
        public IActionResult VerifyPhone(string code, [FromServices] AesCrypto Aes)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(Aes.Decrypt(Request.Cookies["register"]));
                if (obj.expire < DateTime.Now)
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
                else if (obj.code != code)
                {
                    return _Prompt(x =>
                    {
                        x.Title = SR["Invalid Code"];
                        x.Details = SR["Your code is invalid."];
                        x.HideBack = false;
                    });
                }
                ViewBag.Phone = obj.phone;
                ViewBag.AesPhone = Aes.Encrypt(obj.phone.ToString());
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

        [HttpPost("/Register/VerifyPhone")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhone(
            string phone,
            string email,
            string username,
            string password,
            string confirm,
            string nickname,
            string referer,
            [FromServices] AesCrypto Aes,
            CancellationToken token)
        {
            string parsedPhone = null;
            try
            {
                parsedPhone = Aes.Decrypt(phone);
            }
            catch
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Register Failed"];
                    x.Details = SR["Your phone number is invalid."];
                    x.StatusCode = 400;
                    x.HideBack = true;
                });
            }

            if (await DB.Users.AnyAsync(x => x.PhoneNumber == parsedPhone, token))
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Register Failed"];
                    x.Details = SR["The phone number <{0}> is already exists. Please pick another one.", parsedPhone];
                    x.StatusCode = 400;
                });
            }
            else if (await DB.Users.AnyAsync(x => x.Email == email, token))
            {
                return _Prompt(x =>
                {
                    x.Title = SR["Register Failed"];
                    x.Details = SR["The email <{0}> is already exists. Please pick another one.", email];
                    x.StatusCode = 400;
                });
            }
            else if (await DB.Users.AnyAsync(x => x.UserName == username, token))
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

            var user = new User
            {
                UserName = username,
                Nickname = nickname,
                PhoneNumber = parsedPhone,
                PhoneNumberConfirmed = true,
                Email = email.ToLower(),
                EmailConfirmed = false,
                AvatarSource = AvatarSource.GravatarPolling,
                AvatarData = email
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
        public async Task<IActionResult> GetAvatar(Guid id, CancellationToken token, int size = 230)
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
                        var result = await client.GetAsync($"/avatar/{ md5_email }?d={ HttpContext.Request.Scheme }://{ HttpContext.Request.Host }/images/non-avatar.png&s={ size }", token);
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

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Authorization(Guid? id, CancellationToken token)
        {
            if (!id.HasValue)
            {
                id = User.Current.Id;
            }

            if (id.Value != User.Current.Id && !User.IsInRole("Root"))
            {
                return Prompt(x =>
                {
                    x.Title = SR["No permission"];
                    x.Details = SR["You do not have the permission to access this resource."];
                    x.StatusCode = 401;
                });
            }

            var applications = await DB.OpenIds
                .Include(x => x.Application)
                .Where(x => x.UserId == id)
                .OrderBy(x => x.Disabled)
                .ToListAsync(token);

            return View(applications);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Authorization(Guid? id, Guid openId, bool disabled, CancellationToken token)
        {
            if (!id.HasValue)
            {
                id = User.Current.Id;
            }

            var _openId = await DB.OpenIds
                .Include(x => x.Application)
                .SingleOrDefaultAsync(x => x.Id == openId, token);

            if (_openId == null)
            {
                return Prompt(x =>
                {
                    x.Title = SR["Open ID Not Found"];
                    x.Details = SR["Please check the open id."];
                    x.StatusCode = 404;
                });
            }

            if (!User.IsInRole("Root") && User.Current.Id != id.Value)
            {
                return Prompt(x =>
                {
                    x.Title = SR["No permission"];
                    x.Details = SR["You do not have the permission to access this resource."];
                    x.StatusCode = 401;
                });
            }

            if (_openId.Application.Type == ApplicationType.Official)
            {
                return Prompt(x =>
                {
                    x.Title = SR["Invalid Operation"];
                    x.Details = SR["You cannot modify the official application active status."];
                    x.StatusCode = 403;
                });
            }

            _openId.Disabled = disabled;
            await DB.SaveChangesAsync(token);

            return Prompt(x =>
            {
                x.Title = SR["Operation Succeeded"];
                x.Details = SR["The {0} has been {1} successfully.", _openId.Application.Name, disabled ? SR["inactived"] : SR["reactived"]];
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Application(Guid? id, CancellationToken token)
        {
            if (!id.HasValue)
            {
                id = User.Current.Id;
            }

            IEnumerable<ApplicationRoleViewModel> applicationClaims;
            if (!User.IsInRole("Root"))
            {
                applicationClaims = await DB.UserClaims
                    .Where(x => x.UserId == id.Value)
                    .Where(x => x.ClaimType == "OwnedApplication" || x.ClaimType == "SuperviseApplication")
                    .Select(x => new ApplicationRoleViewModel { Role = x.ClaimType, ApplicationId = Guid.Parse(x.ClaimValue) })
                    .ToListAsync(token);
            }
            else
            {
                applicationClaims = await DB.Applications
                    .Select(x => new ApplicationRoleViewModel { Role = "Root", ApplicationId = x.Id })
                    .ToListAsync(token);
            }

            var ids = applicationClaims.Select(x => x.ApplicationId);

            return PagedView(await DB.Applications
                .Where(x => ids.Contains(x.Id))
                .Join(applicationClaims, x => x.Id, x => x.ApplicationId, (x,y) => new ApplicationViewModel
                {
                    Name = x.Name,
                    IconId = x.IconId,
                    Description = x.Description,
                    Id = x.Id,
                    Role = y.Role,
                    Type = x.Type,
                    ExtensionPermissions = x.ExtensionPermissions
                })
                .ToListAsync(token), 20);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ApplicationManager(Guid id, CancellationToken token)
        {
            var application = await DB.Applications.SingleOrDefaultAsync(x => x.Id == id, token);

            if (application == null)
            {
                return Prompt(x =>
                {
                    x.Title = SR["Application Not Found"];
                    x.Details = SR["The specified application is not found."];
                    x.StatusCode = 404;
                });
            }

            if (!await User.Manager.IsInAnyRolesOrClaimsAsync(User.Current, "Root", "OwnedApplication", application.Id.ToString()))
            {
                return Prompt(x =>
                {
                    x.Title = SR["No permission"];
                    x.Details = SR["You do not have the permission to access this resource."];
                    x.StatusCode = 401;
                });
            }

            ViewBag.Managers = await DB.UserClaims
                .Where(x => x.ClaimValue == application.Id.ToString())
                .Where(x => x.ClaimType == "OwnedApplication" || x.ClaimType == "OwnedApplication")
                .Join(DB.Users, x => x.UserId, x => x.Id, (x, y) => new ApplicationManagerViewModel
                {
                    Role = x.ClaimType == "OwnedApplication" ? "Owner" : "Manager",
                    Nickname = y.Nickname,
                    Username = y.UserName,
                    UserId = y.Id
                })
                .ToListAsync(token);

            return View(application);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplicationManagerRemove(Guid id, Guid userId, CancellationToken token)
        {
            if (userId == User.Current.Id)
            {
                return Prompt(x => 
                {
                    x.Title = SR["Invalid Operation"];
                    x.Details = SR["You cannot remove yourself from this application."];
                    x.StatusCode = 403;
                });
            }

            var application = await DB.Applications.SingleOrDefaultAsync(x => x.Id == id, token);

            if (application == null)
            {
                return Prompt(x =>
                {
                    x.Title = SR["Application Not Found"];
                    x.Details = SR["The specified application is not found."];
                    x.StatusCode = 404;
                });
            }

            if (!await User.Manager.IsInAnyRolesOrClaimsAsync(User.Current, "Root", "OwnedApplication", application.Id.ToString()))
            {
                return Prompt(x =>
                {
                    x.Title = SR["No permission"];
                    x.Details = SR["You do not have the permission to access this resource."];
                    x.StatusCode = 401;
                });
            }

            var user = await DB.Users.SingleOrDefaultAsync(x => x.Id == userId, token);
            if (user == null)
            {
                return Prompt(x =>
                {
                    x.Title = SR["User Not Found"];
                    x.Details = SR["The specified user is not found."];
                    x.StatusCode = 404;
                });
            }

            await User.Manager.RemoveClaimAsync(user, new Claim("SuperviseApplication", application.Id.ToString()));

            return Prompt(x =>
            {
                x.Title = SR["Suceeded"];
                x.Details = SR["You have removed {0} from {1} successfully.", user.UserName, application.Name];
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await SignInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Authorize]
        public IActionResult ApplicationCreate()
        {
            return View();
        }
    }
}
