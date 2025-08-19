using Newtonsoft.Json;

namespace TeamsFileNotifier.FileSystemMonitor
{
    public class CustomActions
    {
        [JsonProperty("notify_team_members")]
        public List<string> TagTeamMembers { get; private set; }
        [JsonProperty("always_update")] 
        public bool OverrideUpdateChecks { get; private set; }
        public CustomActions() { 
            TagTeamMembers = new List<string>();
            OverrideUpdateChecks = false;
        }
    }
}