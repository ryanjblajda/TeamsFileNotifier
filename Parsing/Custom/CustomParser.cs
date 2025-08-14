using TeamsFileNotifier.Global;
using TeamsFileNotifier.Messaging;
using Serilog;
using System.Text;

namespace TeamsFileNotifier.Parsing.Custom
{
    internal class CustomParser : GenericParser
    {
        public CustomParser() : base() { }

        internal override void OnFileChanged(FileChangedMessage message)
        {
            string content = String.Empty;
            //if we have a custom action string to generate the format, do so
            if (message.CustomAction != String.Empty) { content = GenerateCustomActionContent(message.CustomAction); }
        }
    }
}
