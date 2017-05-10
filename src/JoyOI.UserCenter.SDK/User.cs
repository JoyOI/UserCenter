using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JoyOI.UserCenter.SDK
{
    public class User
    {
        public Guid OpenId { get; set; }

        public string AccessToken { get; set; }

        public string Nickname { get; set; }

        public string Email { get; set; }

        public string AvatarUrl { get; set; }

        public DateTime Expire { get; set; }
    }
}
