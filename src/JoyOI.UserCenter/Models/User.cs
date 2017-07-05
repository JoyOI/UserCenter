using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace JoyOI.UserCenter.Models
{
    public enum AvatarSource
    {
        LocalStorage,
        WeChatPolling,
        GravatarPolling
    }

    public enum Sex
    {
        Unknown,
        Male,
        Female
    }

    public class User : IdentityUser<Guid>
    {
        [MaxLength(64)]
        public string Nickname { get; set; }

        public JsonObject<Dictionary<string, long>> Extensions { get; set; } = "{}";

        public AvatarSource AvatarSource { get; set; }

        [MaxLength(256)]
        public string AvatarData { get; set; }

        [MaxLength(64)]
        public string WeChatOpenId { get; set; }

        [MaxLength(32)]
        public string School { get; set; }

        [MaxLength(64)]
        public string Address { get; set; }

        public Sex Sex { get; set; }

        public DateTime RegisterTime { get; set; } = DateTime.Now;

        public DateTime ActiveTime { get; set; } = DateTime.Now;
    }
}
