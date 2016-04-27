using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace BotApp.Controllers
{
    public class AuthController : ApiController
    {
        ICredentialStore _creds;
        public AuthController()
        {
            ClientId = Environment.GetEnvironmentVariable("MSHEALTHBOT_HEALTHAPI_CLIENTID");
            ClientSecret = Environment.GetEnvironmentVariable("MSHEALTHBOT_HEALTHAPI_CLIENTSECRET");

            _creds = MyDependencies._store;
        }

        private static string RedirectUri = "http://localhost:3978/api/auth/receivetoken";
        private readonly string ClientId;
        private static string Scopes = "mshealth.ReadDevices mshealth.ReadActivityHistory mshealth.ReadActivityLocation mshealth.ReadDevices mshealth.ReadProfile offline_access";
        private readonly string ClientSecret;

        [Route("api/auth/home")]
        [HttpGet]
        public HttpResponseMessage Home(string UserId)
        {
            var resp = Request.CreateResponse(System.Net.HttpStatusCode.Found);
            resp.Headers.Location = CreateOAuthCodeRequestUri(UserId);
            return resp;
        }

        private Uri CreateOAuthCodeRequestUri(string UserId)
        {
            UriBuilder uri = new UriBuilder("https://login.live.com/oauth20_authorize.srf");
            var query = new StringBuilder();

            query.AppendFormat("redirect_uri={0}", Uri.EscapeUriString(RedirectUri));
            query.AppendFormat("&client_id={0}", Uri.EscapeUriString(ClientId));
            query.AppendFormat("&client_secret={0}", Uri.EscapeUriString(ClientSecret));

            query.AppendFormat("&scope={0}", Uri.EscapeUriString(Scopes));
            query.Append("&response_type=code");
            if (!string.IsNullOrEmpty(UserId))
                query.Append($"&state={UserId}");

            uri.Query = query.ToString();
            return uri.Uri;
        }

        private Uri CreateOAuthTokenRequestUri(string code, string refreshToken = "")
        {
            UriBuilder uri = new UriBuilder("https://login.live.com/oauth20_token.srf");
            var query = new StringBuilder();

            query.AppendFormat("redirect_uri={0}", Uri.EscapeUriString(RedirectUri));
            query.AppendFormat("&client_id={0}", Uri.EscapeUriString(ClientId));
            query.AppendFormat("&client_secret={0}", Uri.EscapeUriString(ClientSecret));

            string grant = "authorization_code";
            if (!string.IsNullOrEmpty(refreshToken))
            {
                grant = "refresh_token";
                query.AppendFormat("&refresh_token={0}", Uri.EscapeUriString(refreshToken));
            }
            else
            {
                query.AppendFormat("&code={0}", Uri.EscapeUriString(code));
            }

            query.Append(string.Format("&grant_type={0}", grant));
            uri.Query = query.ToString();
            return uri.Uri;
        }

        [Route("api/auth/receivetoken")]
        [HttpGet()]
        public async Task<string> ReceiveToken(string code = null, string state = null)
        {
            if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(state))
            {
                var tokenUri = CreateOAuthTokenRequestUri(code);
                string result = null;

                using (var http = new HttpClient())
                {
                    var c = tokenUri.Query.Remove(0, 1);
                    var content = new StringContent(c);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    var resp = await http.PostAsync(new Uri("https://login.live.com/oauth20_token.srf"), content);
                    result = await resp.Content.ReadAsStringAsync();
                }

                dynamic obj = JsonConvert.DeserializeObject(result);
                _creds.AddToken(state, obj.access_token.ToString());
                return "Done, thanks!";
            }
            return "Something went wrong - please try again!";
        }
    }
}
