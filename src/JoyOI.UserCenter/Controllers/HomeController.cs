using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JoyOI.UserCenter.Models;

namespace JoyOI.UserCenter.Controllers
{
    public class HomeController : BaseController
    {
        // GET: /<controller>/
        public IActionResult Index()
        {
            if (User.Current == null)
                return RedirectToAction("Login", "Account");
            return View();
        }
    }
}
