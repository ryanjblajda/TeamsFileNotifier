using Serilog;
using System.Web;
using TeamsFileNotifier.FileSystemMonitor;
using TeamsFileNotifier.Messaging;

namespace TeamsFileNotifier.Global
{
    public static class Functions
    {
        public static string GetDefaultTempPathLocation(ILogger logger, string folderName = Values.Namespace)
        {
            string tempPath = Path.GetTempPath();
            string folderPath = Path.Combine(tempPath, folderName);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                logger.Information($"Functions | Created config folder at: {folderPath}");
            }

            return folderPath;
        }

        public static bool IsChildPath(string parentPath, string childPath)
        {
            string fullChild = String.Empty, fullParent = String.Empty;
            StringComparison comparison = StringComparison.OrdinalIgnoreCase;

            if (parentPath != String.Empty)
            {
                fullParent = Path.GetFullPath(parentPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

                fullChild = Path.GetFullPath(childPath);

                comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            }
            else { return false; }

            return fullChild.StartsWith(fullParent, comparison);
        }
    }
}
