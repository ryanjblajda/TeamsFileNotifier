using Newtonsoft.Json;
using Serilog;
using Serilog.Core;
using TeamsFileNotifier.Global;

namespace TeamsFileNotifier.Configuration
{
    public class ConfigurationLoader
    {
        private readonly ILogger _logger;

        public ConfigurationLoader(ILogger logger) {
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }       

        public string GetConfigFilePath(string folderName = Values.Namespace, string fileName = Values.DefaultConfigFilename)
        {
            return Path.Combine(Functions.GetDefaultTempPathLocation(_logger, folderName), fileName);
        }

        public Configuration? LoadConfig(string folderName = Values.Namespace, string fileName = Values.DefaultConfigFilename)
        { 
            string configFile = GetConfigFilePath(folderName, fileName);

            if (!File.Exists(configFile))
                return null;

            try
            {
                string json = File.ReadAllText(configFile);
                Log.Debug(json);
                var config = JsonConvert.DeserializeObject<Configuration>(json);
                return config;
            }
            catch (Exception ex)
            {
                Log.Warning($"Error loading config file: {ex.Message}");
                return null;
            }
        }

        public void SaveConfig(Configuration config, string folderName = Values.Namespace, string fileName = Values.DefaultConfigFilename)
        {
            string configFile = GetConfigFilePath(folderName, fileName);

            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configFile, json);
                Log.Information($"Config saved to: {configFile}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error saving config file: {ex.Message}");
            }
        }
    }
}
