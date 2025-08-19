using Serilog;
using Serilog.Core;
using TeamsFileNotifier.Messaging;
using TeamsFileNotifier.Configuration;
using TeamsFileNotifier.FileSystemMonitor;
using TeamsFileNotifier.Global;
using TeamsFileNotifier.Authentication;
using System.Diagnostics;
using System.Reflection;
using System.Drawing;

class Program
{
    public static ContextMenuStrip contextMenu;
    public static NotifyIcon trayIcon;
    public static FileSystemMonitorManager manager;
    public static ILogger logger;
    public static readonly string tempPath = "./";

    [STAThread]
    static void Main()
    {
        Log.Logger = ConfigureLogging();
        logger = Log.Logger;

        Log.Information("Program | Starting Logging...");

        contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Edit Config", null, OnAdjustConfig);
        contextMenu.Items.Add("Open Log", null, OnOpenLog);
        contextMenu.Items.Add("Start Monitoring", null, OnStartMonitoring);
        contextMenu.Items.Add("Stop Monitoring", null, OnStopMonitoring);
        contextMenu.Items.Add("Exit", null, OnExit);

        Log.Debug("Program | Configured Tray Options");

        //configure a default icon
        Icon? icon = SystemIcons.Information;
        //if we can generate the icon, assign the result
        if (GenerateIcon() != null) { icon = GenerateIcon(); }
        //generate the full tray icon
        trayIcon = new NotifyIcon
        {
            Icon = icon,
            ContextMenuStrip = contextMenu,
            Visible = true,
            Text = "Teams File Monitor - Updater"
        };

        Log.Debug("Program | Configured Icon");

        Log.Debug("Program | Tray Starting");

        if (!IsNewInstance()) { DuplicateInstanceExit(); }
        else {

            if (LoadConfiguration()) { StartMonitorManager(); }

            Values.MessageBroker.Subscribe<FileChangedMessage>(OnFileChangedMessage);
            Values.MessageBroker.Subscribe<BalloonMessage>(OnBalloonMessage);

            Application.Run();
        }
    }

    private static Icon? GenerateIcon()
    {
        Icon? icon = null;

        var assembly = Assembly.GetExecutingAssembly();

        #if DEBUG
            Log.Debug("Program | Available Assembly Resources");
            foreach (string name in assembly.GetManifestResourceNames()) { Log.Debug($"Program | resource : {name}"); }
        #endif

        using (var stream = assembly.GetManifestResourceStream("teams_file_notifier.logo.ico"))
        {
            if (stream != null) { icon = new Icon(stream); }
        }

        return icon;
    }

    private static void DuplicateInstanceExit()
    {
        Log.Warning("Program | exiting, only one instance of the application can be running at once");
        ShowBalloon("Exiting....", "Only a single instance of the application can be running at a time.", ToolTipIcon.Warning, 2500);
        //creater a timer
        System.Threading.Timer timer = new System.Threading.Timer((o) => OnExit(o, new EventArgs()));
        //fire it off after 3 seconds so balloon can show
        timer.Change(3000, Timeout.Infinite);
    }

    private static bool IsNewInstance()
    {
        bool isNew = false;

        Mutex mutex = new Mutex(true, Values.Namespace, out isNew);
        Log.Information($"Program | this instance is {(isNew ? "new" : "a duplicate")}");
        return isNew;
    }

    private static void OnBalloonMessage(BalloonMessage message)
    {
        ShowBalloon(message.Title, message.Text, message.Icon, message.Timeout);
    }

    private static void OnFileChangedMessage(FileChangedMessage message)
    {
        string file = Path.GetFileName(message.FilePath);

        ShowBalloon("File Changed", file, ToolTipIcon.Warning);
    }

    private static Logger ConfigureLogging()
    {
        LoggerConfiguration config = new LoggerConfiguration();

        #if DEBUG
            config.MinimumLevel.Debug();
        #else
            config.MinimumLevel.Information();
        #endif
        config.WriteTo.File(Path.Combine(Functions.GetDefaultTempPathLocation(logger), Values.Namespace + "-.log"), rollingInterval: RollingInterval.Day);
        config.WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");

        return config.CreateLogger();
    }

    private static bool LoadConfiguration()
    {
        bool result = false;
        ConfigurationLoader loader = new ConfigurationLoader(logger);
        
        Values.Configuration = loader.LoadConfig();

        Authentication.AuthenticationRoutine();

        if (Values.Configuration != null)
        {
            Log.Information("Program | Configuration Loaded Successfully");
            ShowBalloon("Success", "Config Loaded");
            result = true;
        }
        else { ShowBalloon("Error", "Config Not Loaded!", ToolTipIcon.Warning); }

        return result;
    }

    public static void StartMonitorManager()
    {
        Task.Factory.StartNew(() =>
        {
            Log.Information("Program | Creating Monitor Manager");
            manager = new FileSystemMonitorManager(Values.MessageBroker);
        });
    }

    public static void ShowBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.Info, int timeout = 3000)
    {
        trayIcon.BalloonTipTitle = title;
        trayIcon.BalloonTipText = text;
        trayIcon.BalloonTipIcon = icon;
        trayIcon.ShowBalloonTip(timeout);
    }

    private static void OnOpenLog(object? sender, EventArgs e) {
        try { Process.Start("explorer.exe", Functions.GetDefaultTempPathLocation(Log.Logger)); }
        catch (Exception ex) { Log.Error($"Program | error opening temp directory {ex.Message}"); }
    }

    private static void OnAdjustConfig(object? sender, EventArgs e) {
        try
        {
            string path = Path.Combine(Functions.GetDefaultTempPathLocation(Log.Logger), Values.DefaultConfigFilename);
            Task.Factory.StartNew(() =>
            {
                if (!File.Exists(path)) {
                    Log.Warning("Program | no config file found, creating an empty one");
                    File.WriteAllText(path, Values.DefaultConfigContents);
                }

                Process editor = new Process();

                editor.StartInfo = new ProcessStartInfo(path, Values.DefaultConfigFilename) { UseShellExecute = true };
                editor.EnableRaisingEvents = true;

                editor.Exited += OnEditorExited;
                editor.Start();

                Log.Information($"Program | Opening Config File: {path}");
            });
        }
        catch (Exception ex)
        {
            // Handle errors here (e.g., file not found or no associated app)
            Log.Error($"Program | Error opening file: {ex.Message}");
        }
    }

    private static void OnEditorExited(object? sender, EventArgs e)
    {
        if (sender != null)
        {
            Process editor = (Process)sender;
            editor.Exited -= OnEditorExited;
        }

        LoadConfiguration();
        OnStartMonitoring(null, new EventArgs());
    }

    private static void OnStartMonitoring(object? sender, EventArgs e) {
        if (manager != null) { manager.StartWatchers(); }
        else { StartMonitorManager(); }
    }
    private static void OnStopMonitoring(object? sender, EventArgs e) {
        if (manager != null) manager.StopWatchers();
    }
    private static void OnExit(object? sender, EventArgs e) { Application.Exit(); }
}