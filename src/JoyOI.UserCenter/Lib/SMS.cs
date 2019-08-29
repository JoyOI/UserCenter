using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JoyOI.UserCenter.Lib
{
    public static class SMS
    {
        public static async Task<bool> SendSmsAsync(string corpId, string pwd, string phone, string content)
        {
            System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls | SecurityProtocolType.SystemDefault;
            var bytes = Encoding.GetEncoding("gb2312").GetBytes(content);
            var hexString = BitConverter.ToString(bytes);
            var urlString = string.Join("", hexString.Split('-').Select(x => "%" + x));
            using (var client = new HttpClient() { BaseAddress = new Uri("https://inolink.com") })
            {
                using (var response = await client.GetAsync($"/ws/BatchSend.aspx?CorpID={ corpId }&Pwd={ pwd }&Mobile={ phone }&Content={ urlString }"))
                {
                    var text = await response.Content.ReadAsStringAsync();
                    return text == "1";
                }
            }
        }
    }
}
