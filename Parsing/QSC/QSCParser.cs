using TeamsFileNotifier.Global;
using TeamsFileNotifier.Messaging;
using TeamsFileNotifier.Parsing.Crestron;

namespace TeamsFileNotifier.Parsing.QSC
{
    internal class QSCParser : GenericParser
    {
        private static List<string> _validExtensions = new List<string>() { };

        public QSCParser() : base() { }

        internal override void OnFileChanged(FileChangedMessage message)
        {
            //we only want to parse valid extensions for us
            if (_validExtensions.Contains(Path.GetExtension(message.FilePath))) { /* DO STUFF */ }
        }
    }
}
