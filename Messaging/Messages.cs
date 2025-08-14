using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TeamsFileNotifier.Parsing.Crestron;

namespace TeamsFileNotifier.Messaging
{
    interface IMessage
    {
        string DebugMessage { get; }
    }

    public class BalloonMessage : IMessage
    {
        public string DebugMessage { get; private set; }
        public string Title { get; private set; }
        public string Text {  get; private set; }
        public ToolTipIcon Icon { get; private set; }
        public int Timeout { get; private set; }   

        public BalloonMessage(string debugMessage, string title, string text, ToolTipIcon icon, int timeout = 1000)
        {
            DebugMessage = debugMessage;
            Title = title;
            Text = text;
            Icon = icon;
            Timeout = timeout;
        }
    }

    public class FileChangedMessage : IMessage 
    {
        public string DebugMessage { get; private set; }
        public string FilePath { get; private set; }

        public string FileExtension { get; private set; }
        public string CustomAction { get; private set; }

        public FileChangedMessage(string debugMessage, string path, string action = "")
        {
            FilePath = path;
            DebugMessage = debugMessage;
            FileExtension = Path.GetExtension(FilePath);
            CustomAction = action;
        }
    }

    public class UpdateTeamsRequestMessage : IMessage
    {
        public string DebugMessage { get; private set; }
        public string Filename { get; private set; }

        public string Path { get; private set; }
        public string Title { get; private set; }  
        public string Content {  get; private set; }
        public string IconURL { get; private set; }

        internal UpdateTeamsRequestMessage(string debugMessage, string name, string path, string title, string content, string iconurl)
        {
            DebugMessage = debugMessage;
            Filename = name;
            Title = title;
            Path = path;
            Content = content;
            IconURL = iconurl;
        }
    }
}
