using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamsFileNotifier.Messaging
{
    interface IMessage
    {
        string DebugMessage { get; }
    }

    public class FileChangedMessage : IMessage 
    {
        public string DebugMessage { get; private set; }
        public string FilePath { get; private set; }

        public FileChangedMessage(string debugMessage, string path)
        {
            FilePath = path;
            DebugMessage = debugMessage;
        }
    }
}
