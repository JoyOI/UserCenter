using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace JoyOI.UserCenter.Models
{
    public enum ApplicationType
    {
        Official,
        ThirdParty
    }

    public class Application
    {
        public Guid Id { get; set; }

        [MaxLength(64)]
        public string Name { get; set; }

        [MaxLength(64)]
        public string Secret { get; set; }

        [MaxLength(128)]
        public string CallBackUrl { get; set; }

        public ApplicationType Type { get; set; }

        public JsonObject<List<string>> ExtensionPermissions { get; set; } = "[]";

        public JsonObject<List<Guid>> Owners { get; set; } = "[]";

        [ConcurrencyCheck]
        public Guid ConcurrencyStamp { get; set; }
    }
}
