using Serilog;

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
                logger.Information($"Created config folder at: {folderPath}");
            }

            return folderPath;
        }
    }
}
