using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using syncFolder.Models;

namespace syncFolder.Services;

public sealed class ManifestManager
{
    public static ManifestManager Shared { get; } = new();

    private readonly string _baseDir;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private ManifestManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _baseDir = Path.Combine(appData, "syncFolder", "manifests");
        Directory.CreateDirectory(_baseDir);
    }

    private string GetPath(Guid pairId) => Path.Combine(_baseDir, $"{pairId}.json");
    private string GetBiDirPath(Guid pairId) => Path.Combine(_baseDir, $"{pairId}_bidir.json");

    public FileManifest Load(Guid pairId)
    {
        lock (_lock)
        {
            try
            {
                var path = GetPath(pairId);
                if (!File.Exists(path)) return new FileManifest();
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<FileManifest>(json, JsonOptions) ?? new FileManifest();
            }
            catch
            {
                return new FileManifest();
            }
        }
    }

    public void Save(FileManifest manifest, Guid pairId)
    {
        lock (_lock)
        {
            try
            {
                var path = GetPath(pairId);
                var json = JsonSerializer.Serialize(manifest, JsonOptions);
                File.WriteAllText(path, json);
            }
            catch { }
        }
    }

    public BiDirectionalManifest LoadBiDir(Guid pairId)
    {
        lock (_lock)
        {
            try
            {
                var path = GetBiDirPath(pairId);
                if (!File.Exists(path)) return new BiDirectionalManifest();
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<BiDirectionalManifest>(json, JsonOptions) ?? new BiDirectionalManifest();
            }
            catch
            {
                return new BiDirectionalManifest();
            }
        }
    }

    public void SaveBiDir(BiDirectionalManifest manifest, Guid pairId)
    {
        lock (_lock)
        {
            try
            {
                var path = GetBiDirPath(pairId);
                var json = JsonSerializer.Serialize(manifest, JsonOptions);
                File.WriteAllText(path, json);
            }
            catch { }
        }
    }

    public void DeleteManifest(Guid pairId)
    {
        lock (_lock)
        {
            try { File.Delete(GetPath(pairId)); } catch { }
            try { File.Delete(GetBiDirPath(pairId)); } catch { }
        }
    }
}
