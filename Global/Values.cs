using TeamsFileNotifier.Parsing.Crestron;
using TeamsFileNotifier.Notifications;
using TeamsFileNotifier.Parsing;
using TeamsFileNotifier.Parsing.QSC;
using TeamsFileNotifier.Parsing.Biamp;
using TeamsFileNotifier.Parsing.Custom;
using TeamsFileNotifier.Parsing.Extron;

namespace TeamsFileNotifier.Global
{
    internal static class Values
    {
        public const string Namespace = "teams-file-notifier";
        public const string DefaultConfigFilename = "config.json";
        public const string DefaultTokenCacheFilename = "msal.dat";
        public const int DebounceIntervalMS = 1000;

        public const string CrestronIconURL = "https://kenticoprod.azureedge.net/kenticoblob/crestron/media/crestron/generalsiteimages/crestron-logo.png";
        public const string CCSIconURL = "https://ccsnewengland.com/wp-content/uploads/2020/09/CCS-Logo.png";

        public static string AccessToken = String.Empty;

        public static Mutex SingleInstanceMutex = new Mutex(false);

        public static Configuration.Configuration Configuration = new Configuration.Configuration();

        public static readonly MessageBroker MessageBroker = new MessageBroker();

        public static readonly TeamsNotifier Notifier = new TeamsNotifier(MessageBroker);
        private static readonly GenericParser GenericParser = new GenericParser();
        private static readonly CrestronParser CrestronParser = new CrestronParser();
        private static readonly ExtronParser ExtronParser = new ExtronParser();
        private static readonly BiampParser BiampParser = new BiampParser();
        private static readonly QSCParser QSCParser = new QSCParser();
        private static readonly CustomParser CustomMessageParser = new CustomParser();

        public const string DefaultConfigContents = """
        {
            "folders":[
                {
                    "path":"",
                    "webhook":"",
                    "extensions" : [
                        { "extension":".lpz" }, { "extension":".tmf" }, { "extension":".qsys" }, { "extension":".gcp" }, { "extension":".txt" }, { "extension":".json" }
                    ]
                },
                {
                    "path" : "",
                    "webhook" : "",
                    "extensions" : [
                        { "extension":".lpz" }, { "extension":".tmf" }
                    ]
                }
            ]   
        }
        """;
    }
}
