using System;

namespace JoyOI.UserCenter.Models
{
    public class RelationShip
    {
        public Guid FocuserId { get; set; }

        public virtual User Focuser { get; set; }

        public Guid FocuseeId { get; set; }

        public virtual User Focusee { get; set; }

        public DateTime Time { get; set; }
    }
}
