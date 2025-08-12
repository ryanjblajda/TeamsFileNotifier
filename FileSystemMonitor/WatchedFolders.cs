using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsFileNotifier.FileSystemMonitor
{
    public class WatchedFolder
    {
        [JsonProperty("path")]
        public string Path { get; set; }
        
        [JsonProperty("webhook")]
        public string TeamsWebhook { get; set; }

        [JsonProperty("extensions")]
        public List<FileExtensionAction> Extensions { get; set; }

        public WatchedFolder() { 
            this.Extensions = new List<FileExtensionAction>();
            this.TeamsWebhook = String.Empty;
            this.Path = String.Empty;
        }
    }
}
