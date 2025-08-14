using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsFileNotifier.Parsing.Crestron
{
    internal class Device
    {
        public int DeviceIndex { get; set; }
        public int DeviceType { get; set; }
        public string DeviceName { get; set; }
        public string DeviceComment { get; set; }
        public int IPID { get; set; }
        public string IPAddress { get; set; }

        internal Device()
        {
            DeviceName = String.Empty;
            DeviceComment = String.Empty;
            IPAddress = String.Empty;
        }

        public override string ToString()
        {
            string HexIPID = $"0x{IPID:X}"; // uppercase hex with 0x
            return $"{DeviceName} ({DeviceComment}) [Index: {DeviceIndex}, Type: {DeviceType}] {HexIPID} -> {IPAddress}";
        }
    }
}
