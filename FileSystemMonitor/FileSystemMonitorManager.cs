using Serilog;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Windows.Forms;
using TeamsFileNotifier.Messaging;
using TeamsFileNotifier.Global;
using Timer = System.Threading.Timer;

namespace TeamsFileNotifier.FileSystemMonitor
{    
    public class FileSystemMonitorManager
    {
        //a list of the watchers to be stored for later use, and unsubscribing
        private readonly List<FileSystemWatcher> _watchers;
        private readonly Dictionary<FileSystemWatcher, (FileSystemEventHandler changed, FileSystemEventHandler created, RenamedEventHandler renamed, FileSystemEventHandler deleted)> _handlers;
        //a reference to the message broker...which is not needed but i dont feel like changing functionality
        private readonly MessageBroker _messaging;
        //stores timers for use to debounce each file when it changes and restart the timers as needed
        private readonly ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>();
        //stores the hashes so we can make sure that the file's contents actually changed
        private readonly ConcurrentDictionary<string, byte[]> fileHashes = new ConcurrentDictionary<string, byte[]>();

        public FileSystemMonitorManager(MessageBroker messaging) {
            _messaging = messaging;
            _watchers = new List<FileSystemWatcher>();
            _handlers = new Dictionary<FileSystemWatcher, (FileSystemEventHandler changed, FileSystemEventHandler created, RenamedEventHandler renamed, FileSystemEventHandler deleted)> { };

            StartWatchers();
        }

        private void ConfigureWatchers()
        {
            Values.Configuration?.WatchedFolders.ForEach(delegate (WatchedFolder folder) {
                if (!Directory.Exists(folder.Path)) { Log.Warning($"FileSystemMonitorManager | directory does not exist: {folder.Path}"); }
                else
                {
                    try
                    {
                        var watcher = new FileSystemWatcher(folder.Path)
                        {
                            IncludeSubdirectories = true,
                            EnableRaisingEvents = true,
                            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
                        };

                        FileSystemEventHandler  changedHandler = (s, e) => OnFileChanged(e, folder);
                        FileSystemEventHandler  createdHandler = (s, e) => OnFileChanged(e, folder);
                        RenamedEventHandler     renamedHandler = (s, e) => OnFileRenamed(e, folder);
                        FileSystemEventHandler  deletedHandler = (s, e) => OnFileDeleted(e, folder);

                        watcher.Changed += changedHandler;
                        watcher.Created += createdHandler;
                        watcher.Renamed += renamedHandler;
                        watcher.Deleted += deletedHandler;

                        //store in the list
                        _watchers.Add(watcher);
                        //store handlers in the dict so we can stop monitoring later
                        _handlers[watcher] = (changedHandler, createdHandler, renamedHandler, deletedHandler);

                        Log.Information($"FileSystemMonitorManager | Started watching folder: {folder.Path}, {folder.Extensions.Count} Extensions To Watch: {String.Join(", ", folder.Extensions.Select(item => item.Extension).ToArray())}");

                    }
                    catch (Exception e) { Log.Fatal($"FileSystemMonitorManager | exception encountered attempting to monitor folder {folder.Path} -> {e.Message}"); }
                }
            });

            if (_watchers.Count > 0) {
                Log.Information($"FileSystemMonitorManager | Started monitoring {_watchers.Count} folders");
                Values.MessageBroker.Publish(new BalloonMessage("success started monitoring", "Monitoring Started", $"Monitoring {_watchers.Count} Folders", ToolTipIcon.Info)); }
            else {
                Log.Information($"FileSystemMonitorManager | Failure to start monitoring..no folders available");
                Values.MessageBroker.Publish(new BalloonMessage("no folders to monitor", "Monitoring Failed", "No Folders Configured", ToolTipIcon.Error)); }
        }

        public void StartWatchers()
        {
            StopWatchers();
            ConfigureWatchers();
        }

        public void StopWatchers()
        {
            bool result = false;
            string error = $"{_watchers.Count} Folders";

            if (_watchers.Count != 0) {
                try
                {
                    _watchers.ForEach(delegate (FileSystemWatcher watcher)
                    {
                        try
                        {
                            if (_handlers.ContainsKey(watcher))
                            {
                                watcher.Changed -= _handlers[watcher].changed;
                                watcher.Created -= _handlers[watcher].created;
                                watcher.Renamed -= _handlers[watcher].renamed;
                                watcher.Deleted -= _handlers[watcher].deleted;

                            }

                            watcher.EnableRaisingEvents = false;
                            watcher.Dispose();

                            Log.Information($"FileSystemMonitorManager | unsubscribe from {watcher.Path}");
                        }
                        catch(Exception e) { 
                            Log.Fatal($"FileSystemMonitorManager | exception unsubscribing from {watcher.Path}");
                            error = e.Message;
                        }
                    });

                    _watchers.Clear();
                    _handlers.Clear();

                    result = true;
                }
                catch(Exception e) { 
                    Log.Fatal($"FileSystemMonitorManager | exception attempting to unsubscribe");
                    error = e.Message;
                }
                finally { Values.MessageBroker.Publish(new BalloonMessage("results", result ? "Stopped Monitoring" : "Failed To Stop Monitoring", error, result ? ToolTipIcon.Info : ToolTipIcon.Error)); }
            }
            else { Log.Information("FileSystemMonitorManager | no watchers to unsubscribe from");  }
        }

        private bool AreHashesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;

            return true;
        }

        private void OnDebounceTimerExpired(string path, CustomActions action)
        {
            Log.Debug($"FileSystemMonitorManager | Debounce timer fired for: {path}");

            if (!File.Exists(path)) return;

            byte[] newHash;

            using (var stream = File.OpenRead(path)) { newHash = SHA256.HashData(stream); }

            if (fileHashes.TryGetValue(path, out var oldHash))
            {
                if (AreHashesEqual(newHash, oldHash))
                {
                    // No real content change
                    Log.Debug("FileSystemMonitorManager | Hashes are equal, ignoring changes");
                    return;
                }
            }

            FileInfo fileInfo = new FileInfo(path);
            long sizeInBytes = fileInfo.Length;
            Log.Debug($"FileSystemMonitorManager | File Current Size: {sizeInBytes}");

            // Content changed: update hash and handle event
            fileHashes[path] = newHash;

            Log.Debug($"FileSystemMonitorManager | Hashes are not equal, file content HAS changed: {path}");

            _messaging.Publish(new FileChangedMessage("pass to parser", path, action));
        }

        private void OnFileDeleted(FileSystemEventArgs e, WatchedFolder folder)
        {
            //filter by extensions defined in config
            if (folder.Extensions.Count > 0)
            {
                string extension = Path.GetExtension(e.FullPath);
                string extensionLower = extension.ToLower();

                if (folder.Extensions.Find(item => item.Extension == extensionLower) != null) { Log.Debug($"FileSystemMonitorManager | File deleted: {e.FullPath}"); }
            }
        }

        private void OnFileRenamed(RenamedEventArgs e, WatchedFolder folder)
        {
            //filter by extensions defined in config
            if (folder.Extensions.Count > 0)
            {
                string extension = Path.GetExtension(e.FullPath);
                string extensionLower = extension.ToLower();

                if (folder.Extensions.Find(item => item.Extension == extensionLower) != null) { Log.Debug($"FileSystemMonitorManager | File renamed: {e.FullPath}"); }
            }
        }

        private void OnFileChanged(FileSystemEventArgs e, WatchedFolder folder)
        {
            //filter by extensions defined in config
            if (folder.Extensions.Count > 0)
            {
                string extension = Path.GetExtension(e.FullPath);
                string extensionLower = extension.ToLower();

                if (folder.Extensions.Find(item => item.Extension == extensionLower) != null) { 
                    Log.Debug($"FileSystemMonitorManager | Raw file changed: {e.FullPath}");
                    FileChanged(e.FullPath, folder.Extensions.Find(item => item.Extension == extensionLower)?.CustomAction);
                }
            }
        }

        private void FileChanged(string filePath, CustomActions? action)
        {
            //find an existing timer
            if (_timers.TryGetValue(filePath, out Timer? existingTimer)) { 
                existingTimer.Change(Values.DebounceIntervalMS, Timeout.Infinite); 
            }
            //no existing timer found
            else
            {
                //create a timer with a lamda callback so we can dispose of this timer after firing
                Timer timer = new Timer(_ => {
                    //fire the callback
                    OnDebounceTimerExpired(filePath, action);
                    // Dispose and remove timer after firing
                    if (_timers.TryRemove(filePath, out Timer? t)) { t.Dispose(); }
                }, null, Values.DebounceIntervalMS, Timeout.Infinite);
                //store the time in the array
                _timers[filePath] = timer;
            }
        }
    }
}
