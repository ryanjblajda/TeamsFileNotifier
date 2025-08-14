using TeamsFileNotifier.Messaging;
using TeamsFileNotifier.Global;
using Serilog;

namespace TeamsFileNotifier.Parsing.Crestron
{
    internal class CrestronParser : GenericParser
    {
        private static List<string> _validExtensions = new List<string>() { Extensions.CrestronCompiledCode, Extensions.CrestronCompiledUserInterface };

        public CrestronParser() : base() { }

        internal override void OnFileChanged(FileChangedMessage message)
        {
            //we only want to parse valid extensions for us
            if (_validExtensions.Contains(message.FileExtension)) {
                if (message.CustomAction == String.Empty && message.FileExtension == Extensions.CrestronCompiledCode) { LPZFileReader.ParseLPZFile(message.FilePath); }
                else if(message.FileExtension == Extensions.CrestronCompiledUserInterface) { Values.MessageBroker.Publish(new UpdateTeamsRequestMessage("vtz changed", Path.GetFileName(message.FilePath), Path.GetDirectoryName(message.FilePath), $"{message.FileExtension.ToUpper()} File Updated", $"{message.FileExtension.ToUpper()} Updated\r\n\r\n\tAssuming New Version/Import Change Was Made @ {DateTime.Now.ToString()}", Values.CrestronIconURL)); }
                else { Log.Warning($"Crestron .lpz file changed, but custom action is not empty! Custom Action: {message.CustomAction}"); }
            }
        }
    }
}
