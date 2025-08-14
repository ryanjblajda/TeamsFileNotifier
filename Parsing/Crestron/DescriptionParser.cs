
using Serilog;

namespace TeamsFileNotifier.Parsing.Crestron
{
    internal static class DescriptionParser
    {
        internal static Dictionary<int, int> Parse(string[] lines)
        {
            var map = new Dictionary<int, int>();

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                int lastColon = line.LastIndexOf(':');

                string dataPart = line.Substring(lastColon + 1).Trim();

                var parts = dataPart.Split(';');

                string ipId = parts[0].Trim();

                if (1 < parts.Length) { 
                    string deviceIndexStr = parts[1].Trim();
                    if (int.TryParse(deviceIndexStr, out int deviceIndex)) {

                       int.TryParse(ipId, System.Globalization.NumberStyles.HexNumber, null, out int value);
                       map[deviceIndex] = value;

                       Log.Debug($"Adding Description Entry: {deviceIndex} with value: {value}");
                    }
                }
            }

            return map;
        }
    }
}
