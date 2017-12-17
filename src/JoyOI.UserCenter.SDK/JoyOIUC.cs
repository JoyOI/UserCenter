using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace JoyOI.UserCenter.SDK
{
    public class JoyOIUC : IDisposable
    {
        private Guid _appId;
        private HttpClient _client;
        private string _secret;
        private string _baseUrl;
        private IConfiguration _configuration;

        public JoyOIUC(IConfiguration configuration)
        {
            _configuration = configuration;
            _appId = Guid.Parse(configuration["JoyOI:AppId"]);
            _secret = configuration["JoyOI:Secret"];
            _baseUrl = configuration["JoyOI:UcUrl"] ?? "http://api.uc.joyoi.cn";
            _client = new HttpClient() { BaseAddress = new Uri(_baseUrl) };
        }

        public async Task<ResponseBody<long>> GetExtensionCoinAsync(
            Guid openId,
            string accessToken,
            string field)
        {
            using (var result = await _client.PostAsync("/GetExtensionCoin/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken },
                    { "field", field }
                })))
            {
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
            using (var result = await _client.PostAsync("/SetExtensionCoin/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken },
                    { "field", field },
                    { "value", value.ToString() }
                })))
            {
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
            using (var result = await _client.PostAsync("/IncreaseExtensionCoin/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken },
                    { "field", field },
                    { "value", value.ToString() }
                })))
            {
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
            using (var result = await _client.PostAsync("/DecreaseExtensionCoin/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken },
                    { "field", field },
                    { "value", value.ToString() }
                })))
            {
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<long>>(ret);
            }
        }

        public async Task<ResponseBody<TrustedAuthorizeResult>> TrustedAuthorizeAsync(string username, string password)
        {
            using (var result = await _client.PostAsync("/TrustedAuthorize/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "username", username },
                    { "password", password }
                })))
            {
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<TrustedAuthorizeResult>>(ret);
            }
        }

        public async Task<ResponseBody<UserProfileResult>> GetUserProfileAsync(
            Guid openId,
            string accessToken)
        {
            using (var result = await _client.PostAsync("/GetUserProfile/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken }
                })))
            {
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<UserProfileResult>>(ret);
            }
        }

        public string GetAvatarUrl(Guid openId, int size = 230)
        {
            return $"{ _baseUrl }getavatar/{ openId }?size={ size }";
        }

        public async Task<byte[]> GetAvatarBytesAsync(Guid openId, int size = 230)
        {
            using (var result = await _client.GetAsync($"/getavatar/{ openId }?size={ size }"))
            {
                return await result.Content.ReadAsByteArrayAsync();
            }
        }

        public async Task<ResponseBody<string>> GetUsernameAsync(
            Guid openId,
            string accessToken)
        {
            using (var result = await _client.PostAsync("/GetUsername/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken }
                })))
            {
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<string>>(ret);
            }
        }

        public async Task<bool> SendSmsToUserAsync(
            Guid openId,
            string accessToken,
            string content)
        {
            using (var result = await _client.PostAsync("/SendSmsToUser/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openid", openId.ToString() },
                    { "accessToken", accessToken },
                    { "content", content }
                })))
            {
                var ret = await result.Content.ReadAsStringAsync();
                return result.IsSuccessStatusCode;
            }
        }

        public async Task<bool> SendSmsAsync(
            string phone,
            string content)
        {
            using (var result = await _client.PostAsync("/SendSms/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "phone", phone },
                    { "content", content }
                })))
            {
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
            using (var result = await _client.PostAsync("/InsertUser/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "username", username },
                    { "password", password },
                    { "email", email },
                    { "phone", phone }
                })))
            {
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<Guid>>(ret).data;
            }
        }

        public async Task<bool> IsPhoneExistAsync(
            string phone)
        {
            using (var result = await _client.PostAsync("/IsPhoneExist/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "phone", phone }
                })))
            {
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<bool>>(ret).data;
            }
        }

        public async Task<bool> IsEmailExistAsync(
            string email)
        {
            using (var result = await _client.PostAsync("/IsEmailExist/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "email", email }
                })))
            {
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<bool>>(ret).data;
            }
        }

        public async Task<bool> IsUsernameExistAsync(
            string username)
        {
            using (var result = await _client.PostAsync("/IsUsernameExist/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "username", username }
                })))
            {
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<bool>>(ret).data;
            }
        }

        public async Task<bool> HasUnreadMessageAsync(Guid openId)
        {
            using (var result = await _client.PostAsync("/HasUnreadMessage/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openId", openId.ToString() }
                })))
            {
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
            using (var result = await _client.PostAsync("/SendSystemMessageToUser/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "secret", _secret },
                    { "openId", openId.ToString() },
                    { "content", content }
                })))
            {
                return result.IsSuccessStatusCode;
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
