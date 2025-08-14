using System.Text;
using TeamsFileNotifier.Global;
using TeamsFileNotifier.Messaging;
using Serilog;

namespace TeamsFileNotifier.Parsing
{
    internal class GenericParser
    {
        private static List<string> _validExtensions = new List<string>() {
            Extensions.GenericPlainText, Extensions.GenericJSON, Extensions.GenericCSV, Extensions.GenericRichText, Extensions.GenericMD,
            Extensions.BiampTesiraCode, Extensions.BiampTesiraControlDialogCode,
            Extensions.ExtronGlobalConfiguratorCode, Extensions.ExtronGlobalConfiguratorProCode,
            Extensions.QSYSDesignerCode
        };

        internal static List<string> _readableFileExtensions = new List<string>() { Extensions.GenericPlainText, Extensions.GenericJSON, Extensions.GenericCSV, Extensions.GenericRichText, Extensions.GenericMD };
        internal GenericParser() { Values.MessageBroker.Subscribe<FileChangedMessage>(OnFileChanged); }
        internal virtual void OnFileChanged(FileChangedMessage message)
        {
            string content = String.Empty;
            //we only want to parse valid extensions for us
            if (_validExtensions.Contains(message.FileExtension))
            {

                if (_readableFileExtensions.Contains(message.FileExtension))
                {
                    var task = GetReadableContent(message.FilePath);
                    content = task.Result;
                }
                else { content = $"!! Hash Changed !!\r\n\r\n\tAssuming New Revision/Important Change Was Made @ {DateTime.Now.ToString()}"; }
                
                Values.MessageBroker.Publish(new UpdateTeamsRequestMessage($"{message.FileExtension} changed", Path.GetFileName(message.FilePath), Path.GetDirectoryName(message.FilePath), $"{message.FileExtension.ToUpper()} File Updated", content, Values.CCSIconURL));
            }
        }

        internal async Task<string> GetReadableContent(string file)
        {
            string result = $"Unable To Read File Contents @ {DateTime.Now.ToString()}";
            try
            {
                using (FileStream generic = File.OpenRead(file))
                {
                    byte[] buffer = new byte[generic.Length];
                    if (generic.CanRead)
                    {
                        int read = await generic.ReadAsync(buffer);
                        Log.Information($"GenericParser | read {read} bytes from {file}");

                        if (read > 0) { result = Encoding.ASCII.GetString(buffer); }
                    }
                }
            }
            catch (Exception e) { Log.Warning($"GenericParser | failed to open & read file {file} -> {e.Message}"); }

            return result;
        }

        internal string GenerateCustomActionContent(string format)
        {
            string result = String.Empty;

            return result;
        }
    }
}
