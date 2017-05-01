using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.UserCenter.Models
{
    public class TransferMoneyLog
    {
        public Guid Id { get; set; }

        public DateTime Time { get; set; }

        [ForeignKey("Application")]
        public Guid ApplicationId { get; set; }

        public virtual Application Application { get; set; }

        public float Price { get; set; }

        [MaxLength(256)]
        public string Hint { get; set; }
    }
}
