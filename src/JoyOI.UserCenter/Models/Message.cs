using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace JoyOI.UserCenter.Models
{
    public class Message
    {
        public Guid Id { get; set; }

        [ForeignKey("Sender")]
        public Guid SenderId { get; set; }

        public virtual User Sender { get; set; }

        [ForeignKey("Receiver")]
        public Guid ReceiverId { get; set; }

        public virtual User Receiver { get; set; }

        public bool IsRead { get; set; }

        public DateTime SendTime { get; set; }

        public DateTime ReceiveTime { get; set; }

        public string Content { get; set; }
    }
}
