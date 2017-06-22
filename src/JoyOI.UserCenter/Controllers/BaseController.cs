using System;
using Microsoft.AspNetCore.Mvc;
using JoyOI.UserCenter.Models;

namespace JoyOI.UserCenter.Controllers
{
    public class BaseController : BaseController<UserCenterContext, User, Guid>
    {
    }
}
