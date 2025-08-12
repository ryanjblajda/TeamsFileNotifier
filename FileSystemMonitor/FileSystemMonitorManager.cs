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
        private List<FileSystemWatcher> _watchers;
        private readonly MessageBroker _messaging;
        private Configuration.Configuration _config;
        private readonly ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>();
        ConcurrentDictionary<string, byte[]> fileHashes = new ConcurrentDictionary<string, byte[]>();

        public FileSystemMonitorManager(Configuration.Configuration configuration, MessageBroker messaging) {
            _messaging = messaging;
            _watchers = new List<FileSystemWatcher>();
            _config = configuration;

            ConfigureWatchers();
        }

        private void ConfigureWatchers()
        {
            _config.WatchedFolders.ForEach(delegate (WatchedFolder folder) {
                if (!Directory.Exists(folder.Path)) { Log.Warning($"Warning: Directory does not exist: {folder.Path}"); }
                else
                {
                    var watcher = new FileSystemWatcher(folder.Path)
                    {
                        IncludeSubdirectories = true,
                        EnableRaisingEvents = true,
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
                    };

                    watcher.Changed += (s, e) => OnFileChanged(e, folder);
                    watcher.Created += (s, e) => OnFileChanged(e, folder);
                    watcher.Renamed += (s, e) => OnFileRenamed(e, folder);
                    watcher.Deleted += (s, e) => OnFileDeleted(e, folder);

                    _watchers.Add(watcher);

                    Log.Information($"Started watching folder: {folder.Path}, {folder.Extensions.Count} Extensions To Watch: {String.Join(',', folder.Extensions.Select(item => item.Extension).ToArray())}");
                }
            });
        }

        private bool AreHashesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i]) return false;

            return true;
        }

        private void OnDebounceTimerExpired(string path)
        {
            Log.Debug($"Debounce timer fired for: {path}");

            if (!File.Exists(path)) return;

            byte[] newHash;

            using (var stream = File.OpenRead(path)) { newHash = SHA256.HashData(stream); }

            if (fileHashes.TryGetValue(path, out var oldHash))
            {
                if (AreHashesEqual(newHash, oldHash))
                {
                    // No real content change
                    Log.Debug("Hashes are equal, ignoring changes");
                    return;
                }
            }

            FileInfo fileInfo = new FileInfo(path);
            long sizeInBytes = fileInfo.Length;
            Log.Debug($"File Current Size: {sizeInBytes}");

            // Content changed: update hash and handle event
            fileHashes[path] = newHash;

            Log.Debug($"Hashes are not equal, file content HAS changed: {path}");

            _messaging.Publish(new FileChangedMessage("", path));
        }

        private void OnFileDeleted(FileSystemEventArgs e, WatchedFolder folder)
        {
            //filter by extensions defined in config
            if (folder.Extensions.Count > 0)
            {
                string extension = Path.GetExtension(e.FullPath);
                string extensionLower = extension.ToLower();

                if (folder.Extensions.Find(item => item.Extension == extensionLower) != null) { Log.Information($"File deleted: {e.FullPath}"); }
            }
        }

        private void OnFileRenamed(RenamedEventArgs e, WatchedFolder folder)
        {
            //filter by extensions defined in config
            if (folder.Extensions.Count > 0)
            {
                string extension = Path.GetExtension(e.FullPath);
                string extensionLower = extension.ToLower();

                if (folder.Extensions.Find(item => item.Extension == extensionLower) != null) { Log.Information($"File renamed: {e.FullPath}"); }
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
                    Log.Information($"Raw file changed: {e.FullPath}");
                    FileChanged(e.FullPath);
                }
            }
        }

        private void FileChanged(string filePath)
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
                    OnDebounceTimerExpired(filePath);
                    // Dispose and remove timer after firing
                    if (_timers.TryRemove(filePath, out Timer? t)) { t.Dispose(); }
                }, null, Values.DebounceIntervalMS, Timeout.Infinite);
                //store the time in the array
                _timers[filePath] = timer;
            }
        }
    }
}
