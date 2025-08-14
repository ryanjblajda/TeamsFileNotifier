using System.Text.RegularExpressions;

namespace TeamsFileNotifier.Parsing.Crestron
{
    internal class CloudInformation
    {
        public int NumDevices { get; set; }
        public List<Device> Devices { get; set; } = new List<Device>();
    }

    internal static class CloudInfoParser
    {
        public static CloudInformation Parse(string[] lines)
        {
            var cloudInfo = new CloudInformation();

            var deviceMap = new Dictionary<int, Device>();

            var regex = new Regex(@"^(DeviceIndex|DeviceType|DeviceName|DeviceComment)(\d+)=(.*)$");

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("["))
                    continue;

                if (line.StartsWith("NumDevices=", StringComparison.OrdinalIgnoreCase))
                {
                    cloudInfo.NumDevices = int.Parse(line.Split('=')[1]);
                    continue;
                }

                var match = regex.Match(line);
                if (!match.Success) continue;

                string key = match.Groups[1].Value;
                int number = int.Parse(match.Groups[2].Value);
                string value = match.Groups[3].Value.Trim();

                if (!deviceMap.TryGetValue(number, out var device))
                {
                    device = new Device();
                    deviceMap[number] = device;
                }

                switch (key)
                {
                    case "DeviceIndex":
                        device.DeviceIndex = int.Parse(value);
                        break;
                    case "DeviceType":
                        device.DeviceType = int.Parse(value);
                        break;
                    case "DeviceName":
                        device.DeviceName = value;
                        break;
                    case "DeviceComment":
                        device.DeviceComment = value;
                        break;
                }
            }

            // Preserve order by device number
            for (int i = 1; i <= cloudInfo.NumDevices; i++)
            {
                if (deviceMap.TryGetValue(i, out var dev))
                    cloudInfo.Devices.Add(dev);
            }

            return cloudInfo;
        }
    }
}
