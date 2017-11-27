using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace JoyOI.UserCenter.SDK
{
    public class JoyOIUC
    {
        private Guid _appId;
        private Uri _baseUri;
        private string _secret;
        private IConfiguration _configuration;

        public JoyOIUC(IConfiguration configuration)
        {
            _configuration = configuration;
            _appId = Guid.Parse(configuration["JoyOI:AppId"]);
            _secret = configuration["JoyOI:Secret"];
            _baseUri = new Uri(configuration["JoyOI:UcUrl"] ?? "http://api.uc.joyoi.cn");
        }

        public async Task<ResponseBody<long>> GetExtensionCoinAsync(
            Guid openId,
            string accessToken,
            string field)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/GetExtensionCoin/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken },
                    { "field", field }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<long>>(ret);
            }
        }

        public async Task<ResponseBody<long>> SetExtensionCoinAsync(
            Guid openId,
            string accessToken,
            string field,
            long value)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/SetExtensionCoin/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken },
                    { "field", field },
                    { "value", value.ToString() }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<long>>(ret);
            }
        }

        public async Task<ResponseBody<long>> IncreaseExtensionCoinAsync(
            Guid openId,
            string accessToken,
            string field,
            long value)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/IncreaseExtensionCoin/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken },
                    { "field", field },
                    { "value", value.ToString() }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<long>>(ret);
            }
        }

        public async Task<ResponseBody<long>> DecreaseExtensionCoinAsync(
            Guid openId,
            string accessToken,
            string field,
            long value)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/DecreaseExtensionCoin/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken },
                    { "field", field },
                    { "value", value.ToString() }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<long>>(ret);
            }
        }

        public async Task<ResponseBody<TrustedAuthorizeResult>> TrustedAuthorizeAsync(string username, string password)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/TrustedAuthorize/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "username", username },
                    { "password", password }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<TrustedAuthorizeResult>>(ret);
            }
        }

        public async Task<ResponseBody<UserProfileResult>> GetUserProfileAsync(
            Guid openId,
            string accessToken)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/GetUserProfile/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<UserProfileResult>>(ret);
            }
        }

        public string GetAvatarUrl(Guid openId, int size = 230)
        {
            return $"{ _baseUri.ToString() }getavatar/{ openId }?size={ size }";
        }

        public async Task<byte[]> GetAvatarBytesAsync(Guid openId, int size = 230)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.GetAsync($"/getavatar/{ openId }?size={ size }");
                return await result.Content.ReadAsByteArrayAsync();
            }
        }

        public async Task<ResponseBody<string>> GetUsernameAsync(
            Guid openId,
            string accessToken)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/GetUsername/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<string>>(ret);
            }
        }

        public async Task<bool> SendSmsToUserAsync(
            Guid openId,
            string accessToken,
            string content)
        {

            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/SendSmsToUser/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken },
                    { "content", content }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return result.IsSuccessStatusCode;
            }
        }

        public async Task<bool> SendSmsAsync(
            string phone,
            string content)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/SendSms/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "phone", phone },
                    { "content", content }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return result.IsSuccessStatusCode;
            }
        }

        public async Task<Guid> InsertUserAsync(
            string username,
            string password,
            string phone,
            string email)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/InsertUser/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "username", username },
                    { "password", password },
                    { "email", email },
                    { "phone", phone }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<Guid>>(ret).data;
            }
        }

        public async Task<bool> IsPhoneExistAsync(
            string phone)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/IsPhoneExist/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "phone", phone }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<bool>>(ret).data;
            }
        }

        public async Task<bool> IsEmailExistAsync(
            string email)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/IsEmailExist/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "email", email }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<bool>>(ret).data;
            }
        }

        public async Task<bool> IsUsernameExistAsync(
            string username)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/IsUsernameExist/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "username", username }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<bool>>(ret).data;
            }
        }

        public async Task<bool> HasUnreadMessageAsync(Guid openId)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/HasUnreadMessage/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openId", openId.ToString() }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<bool>>(ret).data;
            }
        }

        public string GenerateChatWindowUrl(Guid openId, string toUser = null)
        {
            var pk = _configuration["Chat:PrivateKey"];
            var iv = _configuration["Chat:IV"];
            var appid = _configuration["JoyOI:AppId"];
            var secret = _configuration["JoyOI:Secret"];
            var baseUrl = _configuration["JoyOI:ChatUrl"];
            var aes = new AesCrypto(pk, iv);
            if (toUser == null)
                return $"{ baseUrl }/{ appid }/{ openId }/{ aes.Encrypt(secret) }";
            else
                return $"{ baseUrl }/{ appid }/{ openId }/{ aes.Encrypt(secret) }#{ toUser }";
        }

        public async Task<bool> SendSystemMessageToUserAsync(Guid openId, string content)
        {
            using (var client = new HttpClient() { BaseAddress = _baseUri })
            {
                var result = await client.PostAsync("/SendSystemMessageToUser/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openId", openId.ToString() },
                    { "content", content }
                }));
                return result.IsSuccessStatusCode;
            }
        }
    }
}
