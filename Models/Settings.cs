using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace better_saving.Models
{
    public class Settings
    {
        private static readonly string SettingsFilePath = Path.Combine(System.AppContext.BaseDirectory, "EasySave33.settings");
        private static readonly object fileLock = new();

        public List<string> BlockedSoftware { get; set; } = [];
        public List<string> FileExtensions { get; set; } = [];
        public string Language { get; set; } = "en-US";

        /// <summary>
        /// Saves the current settings to the settings file
        /// </summary>
        public static void SaveSettings(Settings settings)
        {
            lock (fileLock)
            {
                try
                {
                    // Create a temporary file path for safe writing
                    string tempFilePath = SettingsFilePath + ".tmp";

                    // Serialize the settings to JSON with proper indentation
                    string jsonSettings = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(tempFilePath, jsonSettings);

                    // If successful, replace the original file
                    if (File.Exists(tempFilePath))
                    {
                        // If original file exists, delete it
                        if (File.Exists(SettingsFilePath))
                        {
                            File.Delete(SettingsFilePath);
                        }
                        // Rename temp file to proper name
                        File.Move(tempFilePath, SettingsFilePath);
                    }
                }
                catch (System.Exception ex)
                {
                    System.Console.Error.WriteLine($"Error writing settings file: {ex.Message}");
                    // Log error but don't rethrow to avoid disrupting app flow
                }
            }
        }

        /// <summary>
        /// Loads settings from the settings file
        /// </summary>
        /// <returns>A Settings object with the loaded values or default values if the file doesn't exist</returns>
        public static Settings LoadSettings()
        {
            lock (fileLock)
            {
                try
                {
                    if (!File.Exists(SettingsFilePath))
                    {
                        return new Settings(); // Return default settings if the file doesn't exist
                    }

                    // Read the JSON content from the settings file
                    string jsonContent = File.ReadAllText(SettingsFilePath);
                    if (string.IsNullOrWhiteSpace(jsonContent))
                    {
                        return new Settings(); // Return default settings if the file is empty
                    }

                    // Deserialize the JSON content to a Settings object
                    var settings = JsonSerializer.Deserialize<Settings>(jsonContent);
                    return settings ?? new Settings(); // Return deserialized settings or default if deserialization fails
                }
                catch (System.Exception ex)
                {
                    System.Console.Error.WriteLine($"Error reading settings file: {ex.Message}");
                    return new Settings(); // Return default settings on error
                }
            }
        }
    }
}
