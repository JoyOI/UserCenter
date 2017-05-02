﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

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
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public Guid ExtensionStamp { get; set; }

        public JsonObject<Dictionary<string, long>> Extensions { get; set; } = "[]";

        public AvatarSource AvatarSource { get; set; }

        [MaxLength(64)]
        public string WeChatOpenId { get; set; }

        [MaxLength(128)]
        public string GravatarEmail{ get; set; }

        [MaxLength(32)]
        public string School { get; set; }

        [MaxLength(64)]
        public string Address { get; set; }

        public JsonObject<List<Guid>> AuthorizedApplications { get; set; } = "[]";

        public Sex Sex { get; set; }
    }
}