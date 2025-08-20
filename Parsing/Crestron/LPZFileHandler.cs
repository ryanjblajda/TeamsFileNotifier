using Newtonsoft.Json.Linq;
using Serilog;
using System.IO.Compression;
using System.Text;
using TeamsFileNotifier.Global;
using TeamsFileNotifier.Messaging;

namespace TeamsFileNotifier.Parsing.Crestron
{
    public static class LPZFileReader
    {
        public static string ParseLPZFile(string lpzFilePath)
        {
            CloudInformation cloudInfo = null;
            Dictionary<int, int> deviceIndexToIPIDMap = null;
            Dictionary<int, string> ipAddressMap = null;

            string filename = Path.GetFileNameWithoutExtension(lpzFilePath);

            string cloudInformationFilename = filename + ".cloud";
            string ipIDFilename = filename + ".dsc";
            string ipAddressFilename = filename + ".dip";

            using (ZipArchive archive = ZipFile.OpenRead(lpzFilePath))
            {

                cloudInfo = ParseCloudFile(archive, cloudInformationFilename);

                deviceIndexToIPIDMap = ParseDSCFile(archive, ipIDFilename);

                ipAddressMap = ParseDIPFile(archive, ipAddressFilename);
            }

            string content = "No IP Table Available";

            if (cloudInfo != null && deviceIndexToIPIDMap != null && ipAddressMap != null)
            {
                cloudInfo.Devices.ForEach(device => {
                    if (deviceIndexToIPIDMap.ContainsKey(device.DeviceIndex)) { device.IPID = deviceIndexToIPIDMap[device.DeviceIndex]; }
                    if (ipAddressMap.ContainsKey(device.IPID)) { device.IPAddress = ipAddressMap[device.IPID]; }

                    Log.Debug($"LPZFileHandler | {device.DeviceName} - {device.DeviceComment} - {device.DeviceIndex} // {device.IPID} - {device.IPAddress}");
                });

                content = GenerateTeamsMessageContent(cloudInfo.Devices);
            }

            return content;
        }

        private static string GenerateTeamsMessageContent(List<Device> devices)
        {
            string result = String.Empty;

            StringBuilder builder = new StringBuilder();

            devices.ForEach(device => {
                builder.AppendLine($"{device.DeviceName}: {device.DeviceComment} @ IP ID: {device.IPID.ToString("X")} [{(device.IPAddress == String.Empty ? "Not Set!" : device.IPAddress)}]");
            });

            result = builder.ToString();

            //Log.Debug(result);

            return result;
        }

        private static Dictionary<int, string> ParseDIPFile(ZipArchive archive, string filename)
        {
            var ipAddressEntry = archive.GetEntry(filename);
            if (ipAddressEntry == null) { 
                Log.Information($"LPZFileHandler | File '{filename}' not found in archive.");
                //Values.MessageBroker.Publish(new BalloonMessage("file not found", "File Not Found!", $"File '{filename}' not found in archive.", ToolTipIcon.Error));
                return null;
            }

            using (var stream = ipAddressEntry.Open())
            using (var reader = new StreamReader(stream))
            {
                string ipAddressContent = reader.ReadToEnd();
                return ParseIPTableAddressesFromString(ipAddressContent);
            }
        }

        private static Dictionary<int, int> ParseDSCFile(ZipArchive archive, string filename)
        {
            //find ip id file
            var ipidEntry = archive.GetEntry(filename);
            if (ipidEntry == null) { 
                Log.Information($"LPZFileHandler | File '{filename}' not found in archive.");
                //Values.MessageBroker.Publish(new BalloonMessage("file not found", "File Not Found!", $"File '{filename}' not found in archive.", ToolTipIcon.Error));
                return null;
            }

            using (var stream = ipidEntry.Open())
            using (var reader = new StreamReader(stream)) {
                string ipidContent = reader.ReadToEnd();
                return ParseIPTableDescriptionFromString(ipidContent);
            }
        }

        private static CloudInformation ParseCloudFile(ZipArchive archive, string filename)
        {
            //find cloud info file
            var devicesEntry = archive.GetEntry(filename);
            if (devicesEntry == null) {
                Log.Information($"LPZFileHandler | File '{filename}' not found in archive.");
                //Values.MessageBroker.Publish(new BalloonMessage("file not found", "File Not Found!", $"File '{filename}' not found in archive.", ToolTipIcon.Error));
                return null;
            }

            using (var stream = devicesEntry.Open())
            using (var reader = new StreamReader(stream))
            {
                string devicesContent = reader.ReadToEnd();
                return ParseCloudInformationFromString(devicesContent);
            }
        }

        // Helper to parse the .cloud file
        private static CloudInformation ParseCloudInformationFromString(string content)
        {
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return CloudInfoParser.Parse(lines);
        }

        // Helper to parse the .dip file
        private static Dictionary<int, string> ParseIPTableAddressesFromString(string content)
        {
            return IPTableParser.Parse(content);
        }

        // Helper to parse the .dsc file
        private static Dictionary<int, int> ParseIPTableDescriptionFromString(string content)
        {
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            return DescriptionParser.Parse(lines);
        }
    }

}
