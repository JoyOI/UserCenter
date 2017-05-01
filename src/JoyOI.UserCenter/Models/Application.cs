using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace JoyOI.UserCenter.Models
{
    public class Application
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        [MaxLength(64)]
        public string Secret { get; set; }

        public JsonObject<List<string>> ExtensionPermissions { get; set; } = "[]";
    }
}
