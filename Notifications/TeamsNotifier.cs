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
using Microsoft.Graph.Models;
using AdaptiveCards;
using static System.Collections.Specialized.BitVector32;
using teams_file_notifier.Notifications;

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

                Notify(team, channel, ChatMessageBuilder.GenerateChatMessage(message), message);
            }
        }

        private async void Notify(string team, string channel, ChatMessage message, UpdateTeamsRequestMessage request)
        {
            try
            {
                ChatMessage? response = await Values.GraphService.Teams[team].Channels[channel].Messages.PostAsync(message);
                
                if (response != null)
                {
                    string log = GenerateSuccessfulUpdateBalloonContent(request, response);
                    Values.MessageBroker.Publish(new BalloonMessage("success", $"Notification Sent: {request.Filename}", log, ToolTipIcon.Info));
                    Log.Information($"TeamsNotifier | {log}");
                }
            }
            catch (Exception ex)
            {
                Values.MessageBroker.Publish(new BalloonMessage("failure", $"Notification Failed: {request.Filename}", ex.Message, ToolTipIcon.Error));
                Log.Fatal($"TeamsNotifier | Exception posting to Teams: {ex.Message}");
            }
        }

        private string GenerateSuccessfulUpdateBalloonContent(UpdateTeamsRequestMessage message, ChatMessage response)
        {
            StringBuilder builder = new StringBuilder();
            //start off with the team name
            builder.Append($"Post created @ {response.CreatedDateTime?.LocalDateTime} ");
            //if tag team members exists, add their info to the content, if not just append empty
            builder.Append(message.CustomActions.TagTeamMembers.Count > 0 ? $"{String.Join(", ", message.CustomActions.TagTeamMembers.ToArray())} were mentioned" : "");

            return builder.ToString();
        }
    }
}
