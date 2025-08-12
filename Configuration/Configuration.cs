using Newtonsoft.Json;
using TeamsFileNotifier.FileSystemMonitor;

namespace TeamsFileNotifier.Configuration
{
    public class Configuration
    {
        [JsonProperty("folders")]
        public List<WatchedFolder> WatchedFolders { get; set; } = new List<WatchedFolder>();
    }
}