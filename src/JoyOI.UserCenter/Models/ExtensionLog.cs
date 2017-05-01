using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.UserCenter.Models
{
    public class ExtensionLog
    {
        public Guid Id { get; set; }

        public DateTime Time { get; set; }

        [ForeignKey("Application")]
        public Guid ApplicationId { get; set; }

        public virtual Application Application { get; set; }

        public long Count { get; set; }

        [MaxLength(256)]
        public string Hint { get; set; }
    }
}
