using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JoyOI.UserCenter.SDK
{
    public class UserCenter
    {
        private static Uri BaseUri = new Uri("http://api.uc.joyoi.com");

        public async Task<ResponseBody<User>> AuthorizeAsync(string username, string password)
        {
            using (var client = new HttpClient() { BaseAddress = BaseUri })
            {
                var result = await client.PostAsync("/Authorize", new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "username", username },
                    { "password", password }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<User>>(ret);
            }
        }
    }
}
