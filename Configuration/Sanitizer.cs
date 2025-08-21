using Serilog;
using System.IO;
using System.Text.RegularExpressions;

namespace TeamsFileNotifier.Configuration
{
    internal static class Sanitizer
    {
        internal static string SanitizeFileContents(string content)
        {
            string result = content;
            
            Log.Information($"Sanitizer | attempting to sanitize file path strings");

            try {  result = Regex.Replace(content, @"(?<=""path""\s*:\s*"")(.*?)(?="")", match =>
                {
                    string path = match.Value;
                    //only replace single backslashes, that have no preceding backslash, preventing previously fixed strings from being falsely fixed twice
                    return Regex.Replace(path, @"(?<!\\)\\(?![\\])", @"\\");
                });
            }
            catch(Exception e) { Log.Warning($"Sanitizer | failed to sanitize input -> {e.Message}"); }
            finally { }
            
            return result;
        }
    }
}
