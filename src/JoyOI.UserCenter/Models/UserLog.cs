using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JoyOI.UserCenter.Models
{
    public enum UserOperation
    {
        SignIn,
        SignOut,
        ChangePassword
    }

    public class UserLog
    {
        public Guid Id { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public virtual User User { get; set; }

        public string Hint { get; set; }
    }
}
