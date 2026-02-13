using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using syncFolder.Models;

namespace syncFolder.Services;

public sealed class ConfigManager
{
    public static ConfigManager Shared { get; } = new();

    private readonly string _configPath;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private ConfigManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDir = Path.Combine(appData, "syncFolder");
        Directory.CreateDirectory(appDir);
        _configPath = Path.Combine(appDir, "config.json");
    }

    public List<SyncPair> LoadPairs()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_configPath)) return new List<SyncPair>();
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<List<SyncPair>>(json, JsonOptions) ?? new List<SyncPair>();
            }
            catch
            {
                return new List<SyncPair>();
            }
        }
    }

    public void SavePairs(List<SyncPair> pairs)
    {
        lock (_lock)
        {
            try
            {
                var json = JsonSerializer.Serialize(pairs, JsonOptions);
                var tempPath = _configPath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, _configPath, overwrite: true);
            }
            catch
            {
                // Silently fail, matching the macOS behavior
            }
        }
    }
}
