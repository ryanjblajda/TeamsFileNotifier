using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Web;
using TeamsFileNotifier.FileSystemMonitor;
using TeamsFileNotifier.Global;
using TeamsFileNotifier.Messaging;
using Microsoft.Graph;

namespace TeamsFileNotifier.Notifications
{
    internal class TeamsNotifier
    {
        private readonly HttpClient client = new HttpClient();
        private readonly Dictionary<string, UpdateTeamsRequestMessage> _sentMessages = new Dictionary<string, UpdateTeamsRequestMessage>();
        private readonly MessageBroker _messaging;

        public TeamsNotifier(MessageBroker messaging)
        {
            _messaging = messaging;
            _messaging.Subscribe<UpdateTeamsRequestMessage>(OnUpdateTeamsRequestMessage);
        }

        private void OnUpdateTeamsRequestMessage(UpdateTeamsRequestMessage message)
        {
            bool sendUpdate = false;

            //need to check if content is actually different, and only update if it is
            if (this._sentMessages.ContainsKey(message.Filename)) {
                if (this._sentMessages[message.Filename].Content != message.Content) {
                    Log.Information($"TeamsNotifier | Content for {message.Filename} Has Changed, Do Update");
                    sendUpdate = true; 
                }
                else { Log.Debug($"TeamsNotifier | Content For {message.Filename} Has Not Changed, Doing Nothing"); }
            }
            else //if not this is a completely new request, so fire it
            {
                Log.Information($"TeamsNotifier | No {message.Filename} Key Found, First Update Since Start, Updating & Storing Key");
                sendUpdate = true;
            }
            //store the most recent change to the message contents so we are always updating against the last change
            this._sentMessages[message.Filename] = message;

            //only send update if required
            if (sendUpdate)
            {
                (string team, string channel) = Functions.ParseDetailsFromWebhook(Functions.GetWebhook(message.Path));

                (string url, StringContent content) = GenerateRequest(team, channel, AdaptiveCardBuilder.BuildAdaptiveCard(message.Title, message.Filename, "", message.Content, message.IconURL, message.CustomActions));

                Notify(content, url);
            }
        }        

        private void Notify(StringContent message, string url)
        {
            try
            {
                HttpStatusCode responseCode = HttpStatusCode.Forbidden;

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Values.AccessToken);
                
                var response = client.PostAsync(url, message).ContinueWith(task =>
                {
                    if (task.IsCanceled) { Log.Warning("TeamsNotifier | canceled"); }
                    else if (task.IsFaulted) { Log.Error("TeamsNotifier | faulted"); }
                    else if (task.IsCompletedSuccessfully) {
                        var response = task.Result;
                        Log.Debug(response.ToString());
                        response.Content.ReadAsStringAsync().ContinueWith(task => { var response = task.Result; Log.Debug(response); });
                        responseCode = response.StatusCode;
                    }

                    if (responseCode == HttpStatusCode.OK || responseCode == HttpStatusCode.Created) {
                        Log.Information($"TeamsNotifier | succesful update to teams --> {responseCode}");
                        Values.MessageBroker.Publish(new BalloonMessage("show success", "Update Successs", " ", ToolTipIcon.Info, 1000)); }
                    else if (responseCode == HttpStatusCode.Unauthorized) { 
                        Log.Warning($"TeamsNotifier | failure to update {(task.Exception == null ? task.Result.ToString() : task.Exception.Message)} --> http {responseCode.ToString()}");
                        Values.MessageBroker.Publish(new AuthenticationFailureMessage());
                    }
                    else {
                        Values.MessageBroker.Publish(new BalloonMessage("show failure", "Update Failed", task.Exception == null ? task.Result.ToString() : task.Exception.Message, ToolTipIcon.Error, 1000));
                        Log.Warning($"TeamsNotifier | failure to update {(task.Exception == null ? task.Result.ToString() : task.Exception.Message)} --> http {responseCode.ToString()}");
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Fatal($"TeamsNotifier | Exception posting to Teams: {ex.Message}");
            }
        }

        private (string, StringContent) GenerateRequest(string teamId, string channelId, JObject payload)
        {
            var json = JsonConvert.SerializeObject(payload, Formatting.Indented);

            Log.Debug(json);
            
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://graph.microsoft.com/v1.0/teams/{teamId}/channels/{channelId}/messages";

            Log.Information($"TeamsNotifier | generated microsoft graph url {url}");
            Log.Debug($"TeamsNotifier | generated card content {json}");

            return (url, content);
        }
    }
}
