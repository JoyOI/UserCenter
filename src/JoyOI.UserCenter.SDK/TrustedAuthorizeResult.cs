using System;

namespace JoyOI.UserCenter.SDK
{
    public class TrustedAuthorizeResult
    {
        public Guid open_id { get; set; }

        public string access_token { get; set; }

        public DateTime expire_time { get; set; }
    }
}
