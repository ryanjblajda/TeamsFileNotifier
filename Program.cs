using Serilog;
using Serilog.Core;
using TeamsFileNotifier.Messaging;
using TeamsFileNotifier.Configuration;
using TeamsFileNotifier.FileSystemMonitor;
using TeamsFileNotifier.Global;

class Program
{
    public static ContextMenuStrip contextMenu;
    public static Configuration? configuration;
    public static NotifyIcon trayIcon;
    public static MessageBroker messaging;
    public static FileSystemMonitorManager manager;
    public static ILogger logger;
    public static readonly string tempPath = "./";

    [STAThread]
    static void Main()
    {
        Log.Logger = ConfigureLogging();
        logger = Log.Logger;

        Log.Information("Starting Logging...");

        contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Edit Config", null, OnAdjustConfig);
        contextMenu.Items.Add("Start Monitoring", null, OnStartMonitoring);
        contextMenu.Items.Add("Stop Monitoring", null, OnStopMonitoring);
        contextMenu.Items.Add("Exit", null, OnExit);

        Log.Debug("Configured Tray Options");

        trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            ContextMenuStrip = contextMenu,
            Visible = true,
            Text = "Teams File Monitor - Updater"
        };

        Log.Debug("Configured Icon");

        Log.Debug("Tray Starting");

        LoadConfiguration();

        Log.Debug("Beginning Monitoring");

        ShowBalloon("Successful App Start", "Monitoring Active");

        if (configuration != null) { OnStartMonitoring(null, new EventArgs()); }

        messaging = new MessageBroker();

        messaging.Subscribe<FileChangedMessage>(OnFileChangedMessage);

        Application.Run();
    }

    private static void OnFileChangedMessage(FileChangedMessage message)
    {
        string file = Path.GetFileName(message.FilePath);

        ShowBalloon("File Changed", file, ToolTipIcon.Warning);
    }

    private static Logger ConfigureLogging()
    {
        LoggerConfiguration config = new LoggerConfiguration();
        config.MinimumLevel.Debug();
        config.WriteTo.File(Path.Combine(Functions.GetDefaultTempPathLocation(logger), Values.Namespace + ".log"), rollingInterval: RollingInterval.Day);
        config.WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

        return config.CreateLogger();
    }

    private static void LoadConfiguration()
    {
        ConfigurationLoader loader = new ConfigurationLoader(logger);
        configuration = loader.LoadConfig();

        if (configuration != null) {
            Log.Information("Configuration Loaded Successfully");
            ShowBalloon("Success", "Config Loaded"); }
        else { 
            ShowBalloon("Error", "Config Not Loaded!"); 
        }
    }

    public static void ShowBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 3000)
    {
        trayIcon.BalloonTipTitle = title;
        trayIcon.BalloonTipText = text;
        trayIcon.BalloonTipIcon = icon;
        trayIcon.ShowBalloonTip(timeout);
    }

    private static void OnAdjustConfig(object? sender, EventArgs e) { }
    private static void OnStartMonitoring(object? sender, EventArgs e) {
        Task.Factory.StartNew(() => {
            manager = new FileSystemMonitorManager(configuration, messaging);
        });
    }
    private static void OnStopMonitoring(object? sender, EventArgs e) { }
    private static void OnExit(object? sender, EventArgs e) { Application.Exit(); }
}