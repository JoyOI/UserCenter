using System;
using Microsoft.AspNetCore.Mvc;
using JoyOI.UserCenter.Models;

namespace JoyOI.UserCenter.Controllers
{
    public class BaseController : BaseController<UserCenterContext, User, Guid>
    {
        public override void Prepare()
        {
            base.Prepare();
            User.Current.ActiveTime = DateTime.Now;
            DB.SaveChanges();
        }
    }
}
