using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MusicManager.Application.Services;
using MusicManager.Core.ViewModules;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MusicManager.Infrastructure.Repository
{
    public class Authentication : IAuthentication
    {
        private readonly IConfiguration _config;

        public Authentication(IConfiguration config)
        {
            _config = config;
        }

        private string GenerateJSONWebToken()
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
              _config["Jwt:Issuer"],
              null,
              expires: DateTime.Now.AddMinutes(60),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> AuthenticateUser(UserModel userModel)
        {
            string token = string.Empty;
            var fbModel = new FireBaseUserModel();

            var res = await httpPOSTRequest(string.Format("{0}{1}", _config["Jwt:firebaseUrl"], _config["Jwt:apiKey"]), userModel);
            using (var reader = new System.IO.StreamReader(res.GetResponseStream()))
            {
                var responseText = reader.ReadToEnd();
                fbModel = JsonConvert.DeserializeObject<FireBaseUserModel>(responseText);
            }
            if (fbModel != null && !string.IsNullOrEmpty(fbModel.idToken))
            {
                token = GenerateJSONWebToken();
            }

            return token;
        }

        private async Task<HttpWebResponse> httpPOSTRequest(string url, dynamic requestPayload)
        {
            HttpWebRequest httpWebRequest;
            WebHeaderCollection webHeaderCollection = new WebHeaderCollection();

            try
            {
                httpWebRequest = WebRequest.Create(url) as HttpWebRequest;
                httpWebRequest.Headers.Add(webHeaderCollection);
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentType = "application/json";

                if (requestPayload != null)
                {
                    byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestPayload, Formatting.Indented, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));

                    using (var stream = httpWebRequest.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                }

                return await httpWebRequest.GetResponseAsync() as HttpWebResponse;
            }
            catch (WebException ex)
            {
                using (var reader = new System.IO.StreamReader(ex.Response.GetResponseStream()))
                {
                    string responseText = reader.ReadToEnd();
                }
                return (HttpWebResponse)ex.Response;
            }
            catch (Exception e)
            {
                return null;
            }
        }

    }
}
