using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace SteamInventoryAIR.Services
{
    public static class EnvironmentService
    {
        private static Dictionary<string, string> _variables = new Dictionary<string, string>();

        public static async void LoadEnvironmentVariables()
        {
            try
            {
                Debug.WriteLine("Loading environment variables from embedded asset");

                // Read from embedded resource
                using var stream = await FileSystem.Current.OpenAppPackageFileAsync("dotenv");
                using var reader = new StreamReader(stream);

                string envContent = await reader.ReadToEndAsync();
                string[] lines = envContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    // Skip comments and empty lines
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    // Parse KEY=VALUE format
                    int equalPos = line.IndexOf('=');
                    if (equalPos > 0)
                    {
                        string key = line.Substring(0, equalPos).Trim();
                        string value = line.Substring(equalPos + 1).Trim();

                        // Remove quotes if present
                        if (value.StartsWith("\"") && value.EndsWith("\""))
                            value = value.Substring(1, value.Length - 2);

                        _variables[key] = value;
                        Debug.WriteLine($"Loaded env variable: {key}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading environment variables: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }


        public static string GetVariable(string key, string defaultValue = "")
        {
            if (_variables.TryGetValue(key, out string value))
                return value;

            return defaultValue;
        }
    }
}
