using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamsFileNotifier.Messaging;
using TeamsFileNotifier.Parsing.Crestron;

namespace TeamsFileNotifier.Parsing.Biamp
{
    internal class BiampParser : GenericParser
    {
        private static List<string> _validExtensions = new List<string>() {  };

        public BiampParser() : base() { }

        internal override void OnFileChanged(FileChangedMessage message)
        {
            //we only want to parse valid extensions for us
            if (_validExtensions.Contains(Path.GetExtension(message.FilePath))) { /* DO STUFF */}
        }
    }
}
