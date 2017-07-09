using System;

namespace JoyOI.UserCenter.Models.ViewModels
{
    public class ApplicationManagerViewModel
    {
        public Guid UserId { get; set; }
        public string Role { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
    }
}
