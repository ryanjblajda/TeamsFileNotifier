using Serilog;
using System.Text.RegularExpressions;
using TeamsFileNotifier.Messaging;
using TeamsFileNotifier.FileSystemMonitor;
using System.Web;

namespace TeamsFileNotifier.Global
{
    internal static class URL
    {
        private const string channelLinkPattern = @"teams\.microsoft\.com\/l\/channel\/(?<channelId>[^\/]+)\/(?<channelName>[^?]+)\?groupId=(?<teamId>[^&]+)&tenantId=(?<tenantId>[^&]+)";

        public static string GetWebhook(string path)
        {
            Log.Debug($"TeamsNotifier | file changed in path: {path}");

            string result = String.Empty;

            WatchedFolder? folder = Values.Configuration?.WatchedFolders.Find(item => Functions.IsChildPath(item.Path, path) || Path.GetFullPath(item.Path) == Path.GetFullPath(path));

            if (folder != null) { result = folder.TeamsWebhook; }

            Log.Debug($"TeamsNotifier | raw webook from config -> result: {result}");

            return result;
        }

        public static (string teamId, string channelId) ParseDetailsFromWebhook(string webhook)
        {
            string teamId = String.Empty;
            string tenantId = String.Empty;
            string channelId = String.Empty;

            if (string.IsNullOrWhiteSpace(webhook))
            {
                Values.MessageBroker.Publish(new BalloonMessage("cannot be null exception", "URL Error!", "Teams URL cannot be empty!", ToolTipIcon.Error, 1000));
                Log.Warning($"TeamsNotifier | {webhook} --> resulted in teams URL empty!");
                return ("", "");
            }

            var uri = new Uri(webhook);
            var queryParams = HttpUtility.ParseQueryString(uri.Query);

            if (queryParams != null)
            {
                // Extract groupId and tenantId from query string
                teamId = queryParams["groupId"];
                tenantId = queryParams["tenantId"]; // Might be useful for future


                if (string.IsNullOrEmpty(teamId))
                {
                    Values.MessageBroker.Publish(new BalloonMessage("team id null exception", "Team ID Error!", "Team ID cannot be empty!", ToolTipIcon.Error, 1000));
                    Log.Warning($"TeamsNotifier | {webhook} --> resulted in teams ID empty!");
                    return ("", "");
                }

                // The channelId is the last segment in the path after "/l/channel/"
                var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                // Typical path: "l/channel/{channelId}/{channelName}"
                // So channelId should be segments[2]
                if (segments.Length < 3 || segments[0] != "l" || segments[1] != "channel")
                {
                    Values.MessageBroker.Publish(new BalloonMessage("invalid format exception", "Channel URL Error!", "Invalid Teams channel URL format!", ToolTipIcon.Error, 1000));
                    Log.Warning($"TeamsNotifier | {webhook} --> resulted in teams channel url format invalid!");
                    return ("", "");
                }

                channelId = segments[2];

                if (string.IsNullOrEmpty(channelId))
                {
                    Values.MessageBroker.Publish(new BalloonMessage("team id null exception", "Channel ID Error!", "Channel ID not found in URL!", ToolTipIcon.Error, 1000));
                    Log.Warning($"TeamsNotifier | {webhook} --> resulted in teams channel id empty!");
                    return ("", "");
                }
            }
            Log.Debug($"TeamsNotifier | retrieved team: {teamId} - channel: {channelId}");
            return (teamId, channelId);
        }
    }
}
