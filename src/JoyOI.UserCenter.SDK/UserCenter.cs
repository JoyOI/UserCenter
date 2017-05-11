﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace JoyOI.UserCenter.SDK
{
    public class UserCenter
    {
        private Guid _appId;
        private string _secret;

        private static Uri BaseUri = new Uri("http://api.uc.joyoi.com");

        public async Task<ResponseBody<User>> AuthorizeAsync(string username, string password)
        {
            using (var client = new HttpClient() { BaseAddress = BaseUri })
            {
                var result = await client.PostAsync("/Authorize/" + _appId, new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "username", username },
                    { "password", password }
                }));
                var ret = await result.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ResponseBody<User>>(ret);
            }
        }

        public async Task<ResponseBody<long>> GetExtensionCoin(
            Guid openId,
            string accessToken,
            string field)
        {
            using (var client = new HttpClient() { BaseAddress = BaseUri })
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

        public async Task<ResponseBody<long>> SetExtensionCoin(
            Guid openId,
            string accessToken,
            string field,
            long value)
        {
            using (var client = new HttpClient() { BaseAddress = BaseUri })
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

        public async Task<ResponseBody<long>> IncreaseExtensionCoin(
            Guid openId,
            string accessToken,
            string field,
            long value)
        {
            using (var client = new HttpClient() { BaseAddress = BaseUri })
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

        public async Task<ResponseBody<long>> DecreaseExtensionCoin(
            Guid openId,
            string accessToken,
            string field,
            long value)
        {
            using (var client = new HttpClient() { BaseAddress = BaseUri })
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
    }
}
