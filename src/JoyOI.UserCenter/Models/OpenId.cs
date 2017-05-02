using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.UserCenter.Models
{
    public class OpenId
    {
        public Guid Id { get; set; }

        [MaxLength(64)]
        public string AccessToken { get; set; }

        [MaxLength(64)]
        public string RequestToken { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public virtual User User { get; set; }

        [ForeignKey("Application")]
        public Guid ApplicationId { get; set; }

        public virtual Application Application { get; set; }

        public DateTime ExpireTime { get; set; }
    }
}
