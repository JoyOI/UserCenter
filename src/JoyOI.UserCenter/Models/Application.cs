using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using Pomelo.AspNetCore.Extensions.BlobStorage.Models;

namespace JoyOI.UserCenter.Models
{
    public enum ApplicationType
    {
        Official,
        ThirdParty,
        Pending
    }

    public class Application
    {
        public Guid Id { get; set; }

        [MaxLength(64)]
        public string Name { get; set; }

        [MaxLength(256)]
        public string Description { get; set; }

        [MaxLength(64)]
        public string Secret { get; set; }

        [ForeignKey("Icon")]
        public Guid IconId { get; set; }

        public virtual Blob Icon { get; set; }

        [MaxLength(128)]
        public string CallBackUrl { get; set; }

        public ApplicationType Type { get; set; }

        public JsonObject<List<string>> ExtensionPermissions { get; set; } = "[]";

        [ConcurrencyCheck]
        public Guid ConcurrencyStamp { get; set; }

        public string RequestText { get; set; }
    }
}
