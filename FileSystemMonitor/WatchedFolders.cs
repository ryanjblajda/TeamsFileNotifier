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
        public string LocalFilePath { get; private set; }

        public string OnlineFilePathURL { get; private set; }
        
        [JsonProperty("webhook")]
        public string TeamsWebhook { get; private set; }

        [JsonProperty("extensions")]
        public List<FileExtensionAction> Extensions { get; private set; }

        public WatchedFolder() { 
            this.Extensions = new List<FileExtensionAction>();
            this.TeamsWebhook = String.Empty;
            this.LocalFilePath = String.Empty;
            this.OnlineFilePathURL = String.Empty;
        }
    }
}
