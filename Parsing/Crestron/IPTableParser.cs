using Serilog;

namespace TeamsFileNotifier.Parsing.Crestron
{
    internal static class IPTableParser
    {
        internal static Dictionary<int, string> Parse(string content)
        {
            var result = new Dictionary<int, string>();
            using var reader = new StringReader(content);

            var temp = new Dictionary<int, int>();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();

                var parts = line.Split('=', 2);

                if (parts.Length == 2)
                {
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    if (key.StartsWith("id"))
                    {
                        if (int.TryParse(key.Substring(2), out int idx))
                        {
                            // Hex or decimal parse
                            if (int.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out int ipid))
                            {
                                result[ipid] = "";
                                temp[idx] = ipid;
                                Log.Debug($"Adding IP Table Address Entry To Temp Map: {idx} == {ipid}");
                            }
                        }
                    }
                    else if (key.StartsWith("addr"))
                    {
                        if (int.TryParse(key.Substring(4), out int idx) && temp.ContainsKey(idx))
                        {
                            result[temp[idx]] = value;

                            Log.Debug($"Assigning Address To IP Table Entry: {temp[idx]} with value: {value} // idx == {idx}");
                        }
                    }
                }
            }
            return result;
        }
    }
}
