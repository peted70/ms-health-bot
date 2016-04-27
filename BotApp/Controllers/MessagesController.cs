using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Utilities;
using System.Net.Http;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using NodaTime.Text;
using NodaTime;
using BotApp.Model;

namespace BotApp
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private const string ApiVersion = "v1";

        ICredentialStore _creds;
        public MessagesController()
        {
            _creds = MyDependencies._store;
        }

        private async Task<MSHealthUserText> ParseUserInput(string input)
        {
            string escaped = Uri.EscapeDataString(input);

            using (var http = new HttpClient())
            {
                string key = Environment.GetEnvironmentVariable("MSHEALTHBOT_LUIS_API_KEY");
                string id = Environment.GetEnvironmentVariable("MSHEALTHBOT_LUIS_APP_ID");

                string uri = $"https://api.projectoxford.ai/luis/v1/application?id={id}&subscription-key={key}&q={escaped}";
                var resp = await http.GetAsync(uri);
                resp.EnsureSuccessStatusCode();

                var strRes = await resp.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<MSHealthUserText>(strRes);
                return data;
            }
        }

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<Message> Post([FromBody]Message message)
        {
            if (message.Type == "Message")
            {
                var userid = message?.From?.Id;
                if (string.IsNullOrEmpty(userid))
                    return message.CreateReplyMessage("Struggling to get a user id...");

                // Lookup the user id to see if we have a token already..
                var token = _creds.GetToken(userid);

                if (message.Text == "token")
                {
                    return message.CreateReplyMessage($"Token is {token}");
                }

                string prompt = "";
                if (string.IsNullOrEmpty(token))
                {
                    var loginUri = new Uri($"http://localhost:3978/api/auth/home?UserId={userid}");
                    prompt = $"Please pay a visit to {loginUri.ToString()} to associate your user identity with your Microsoft Health identity.";
                }
                else
                {
                    var data = await ParseUserInput(message.Text);

                    if (data.intents.Length <= 0 || data.entities.Length <= 0)
                    {
                        return message.CreateReplyMessage("I don't have enough information to understand the question - please try again...");
                    }

                    var topIntent = data.intents[0].intent;

                    switch (topIntent)
                    {
                        case "SummariseActivity":
                            var entityStr = data.entities.FirstOrDefault(e => e.type == "ActivityType").entity;

                            // This could be either date, time or duration..
                            var entityTime = data.entities.FirstOrDefault(e =>
                                e.type == "builtin.datetime.time" ||
                                e.type == "builtin.datetime.duration" ||
                                e.type == "builtin.datetime.date");

                            ParseResult<Period> res = null;

                            // TODO: parse the time formats correctly...
                            if (entityTime.type == "builtin.datetime.duration")
                            {
                                res = PeriodPattern.NormalizingIsoPattern.Parse(entityTime.resolution.duration);
                            }
                            else if (entityTime.type == "builtin.datetime.time")
                            {
                                res = PeriodPattern.NormalizingIsoPattern.Parse(entityTime.resolution.time);
                            }
                            else if (entityTime.type == "builtin.datetime.date")
                            {
                                var pattern = LocalDatePattern.CreateWithInvariantCulture("yyyy-MM-dd");
                                LocalDate parseResult = pattern.Parse(entityTime.resolution.date).Value;
                            }

                            var entity = data.entities[0].entity;

                            // Now call the relevant Microsoft Health API and respond to the user...
                            if (entityTime.type == "builtin.datetime.duration")
                            {
                                try
                                {
                                    var st = SystemClock.Instance.GetCurrentInstant().InUtc().LocalDateTime - res.Value;

                                    DateTime start = st.ToDateTimeUnspecified();
                                    DateTime end = DateTime.Now;
                                    var res2 = await GetActivity(token, entityStr, start, end);
                                    var sleep = JsonConvert.DeserializeObject<Sleep>(res2);

                                    // create a textual summary of sleep in that period...
                                    int num = sleep.itemCount;
                                    if (num <= 0)
                                    {
                                        prompt = "You didn't track any sleep";
                                        break;
                                    }
                                    var total = sleep.sleepActivities.Sum((a) =>
                                    {
                                        if (a.sleepDuration != null)
                                        {
                                            var dur = PeriodPattern.NormalizingIsoPattern.Parse(a.sleepDuration);
                                            return dur.Value.ToDuration().Ticks;
                                        }
                                        else
                                            return 0;
                                    });

                                    var av = total / num;
                                    var sleepSpan = TimeSpan.FromTicks((long)av);
                                    var totalSpan = TimeSpan.FromTicks(total);

                                    var avSleepStr = $"{sleepSpan.ToString(@"%h")} hrs {sleepSpan.ToString(@"%m")} mins";
                                    var totalSleepStr = $"{totalSpan.ToString(@"%h")} hrs {totalSpan.ToString(@"%m")} mins";

                                    prompt = $"You have tracked {num} sleeps - average sleep per night {avSleepStr} for a total of {totalSleepStr}";
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                            break;
                    }

                    if (string.IsNullOrEmpty(prompt))
                        prompt = "Please ask a question to the MS Health Bot";
                }
                // return our reply to the user
                return message.CreateReplyMessage(prompt);
            }
            else
            {
                return HandleSystemMessage(message);
            }
        }

        private Message HandleSystemMessage(Message message)
        {
            if (message.Type == "Ping")
            {
                Message reply = message.CreateReplyMessage();
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == "BotAddedToConversation")
            {
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
            }

            return null;
        }

        private async Task<string> MakeRequestAsync(string token, string path, string query = "")
        {
            var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var ub = new UriBuilder("https://api.microsofthealth.net");

            ub.Path = ApiVersion + "/" + path;
            ub.Query = query;

            string resStr = string.Empty;

            var resp = await http.GetAsync(ub.Uri);

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
            {
                // If we are unauthorized here assume that our token may have expired and use the  
                // refresh token to get a new one and then try the request again.. 
                // TODO: handle this - we can cache the refresh token in the same flow as the access token
                // just haven't done it.
                return "";

                // Re-issue the same request (will use new auth token now) 
                //return await MakeRequestAsync(path, query);
            }

            if (resp.IsSuccessStatusCode)
            {
                resStr = await resp.Content.ReadAsStringAsync();
            }
            return resStr;
        }

        private async Task<string> GetActivity(string token, string activity, DateTime Start, DateTime end)
        {
            string res = string.Empty;
            try
            {
                res = await MakeRequestAsync(token, "me/Activities/",
                    string.Format("startTime={0}&endTime={1}&activityTypes={2}&ActivityIncludes=Details",
                    Start.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"),
                    end.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"),
                    activity));
            }
            catch (Exception ex)
            {
                return $"API Request Error - {ex.Message}";
            }

            await Task.Run(() =>
            {
                // Format the JSON string 
                var obj = JsonConvert.DeserializeObject(res);
                res = JsonConvert.SerializeObject(obj, Formatting.Indented);
            });

            return res;
        }
    }
}