using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsFileNotifier.FileSystemMonitor
{
    public class FileExtensionAction
    {
        [JsonProperty("extension")]
        public string Extension { get; set; }
        
        [JsonProperty("custom_action")]
        public string CustomActionMessage { get; set; }

        private FileExtensionAction() { this.Extension = String.Empty; this.CustomActionMessage = String.Empty; }
    }
}
