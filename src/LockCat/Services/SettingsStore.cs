using LockPig.Models;
using System.IO;
using System.Text.Json;

namespace LockPig.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _settingsPath;

    public SettingsStore()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _settingsPath = CreateSettingsPath(appData);
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            string json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
            // LockCat should keep running even if Windows blocks settings writes.
        }
    }

    private static string CreateSettingsPath(string appData)
    {
        string? settingsPath = TryCreateSettingsPath(appData, migrateFrom: appData);
        if (settingsPath is not null)
        {
            return settingsPath;
        }

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        settingsPath = TryCreateSettingsPath(localAppData, migrateFrom: appData);
        if (settingsPath is not null)
        {
            return settingsPath;
        }

        string tempDirectory = Path.Combine(Path.GetTempPath(), "LockCat");
        Directory.CreateDirectory(tempDirectory);
        return Path.Combine(tempDirectory, "settings.json");
    }

    private static string? TryCreateSettingsPath(string baseDirectory, string migrateFrom)
    {
        try
        {
            string directory = Path.Combine(baseDirectory, "LockCat");
            Directory.CreateDirectory(directory);
            string settingsPath = Path.Combine(directory, "settings.json");
            MigrateLegacySettings(migrateFrom, settingsPath);
            return settingsPath;
        }
        catch
        {
            return null;
        }
    }

    private static void MigrateLegacySettings(string appData, string settingsPath)
    {
        if (File.Exists(settingsPath))
        {
            return;
        }

        string legacyPath = Path.Combine(appData, "LockPig", "settings.json");
        if (File.Exists(legacyPath))
        {
            File.Copy(legacyPath, settingsPath, overwrite: false);
        }
    }
}
