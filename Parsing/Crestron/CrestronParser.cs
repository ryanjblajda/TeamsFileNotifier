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
                if (message.FileExtension == Extensions.CrestronCompiledCode) { Values.MessageBroker.Publish(new UpdateTeamsRequestMessage("", Path.GetFileName(message.FilePath), Path.GetDirectoryName(message.FilePath), "Crestron Code Updated", LPZFileReader.ParseLPZFile(message.FilePath), Values.CrestronIconURL, message.CustomAction)); }
                else if(message.FileExtension == Extensions.CrestronCompiledUserInterface) { Values.MessageBroker.Publish(new UpdateTeamsRequestMessage("vtz changed", Path.GetFileName(message.FilePath), Path.GetDirectoryName(message.FilePath), $"{message.FileExtension.ToUpper()} File Updated", $"{message.FileExtension.ToUpper()} Updated\r\n\r\n\tAssuming New Version/Import Change Was Made @ {DateTime.Now.ToString()}", Values.CrestronIconURL, message.CustomAction)); }
                else { Log.Warning($"CrestronParser | {message.FileExtension} unknown!"); }
            }
        }
    }
}
