using System;

namespace JoyOI.UserCenter.SDK
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

    public class UserProfileResult
    {
        public Guid open_id { get; set; }

        public string nickname { get; set; }

        public string phone { get; set; }

        public string email { get; set; }

        public Sex sex { get; set; }
    }
}
