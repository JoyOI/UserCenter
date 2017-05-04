using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Pomelo.AspNetCore.Localization;
using JoyOI.UserCenter.Models;

namespace JoyOI.UserCenter.Controllers
{
    public class ApiController : BaseController
    {
        #region Non-public members
        [Inject]
        public UserCenterContext DB { get; set; }

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
        #endregion

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
            else if (CallBackUrl.StartsWith(Application.CallBackUrl))
            {
                return Prompt(x => 
                {
                    x.Title = SR["Request denied"];
                    x.Details = SR["The callback URL is invalid."];
                    x.StatusCode = 400;
                });
            }
            return View();
        }
    }
}
