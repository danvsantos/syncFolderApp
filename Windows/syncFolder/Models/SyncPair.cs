using System.Text.Json.Serialization;

namespace syncFolder.Models;

public enum SyncMode
{
    [JsonPropertyName("oneWay")]
    OneWay,

    [JsonPropertyName("bidirectional")]
    Bidirectional
}

public class SyncPair
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public int IntervalMinutes { get; set; } = 5;
    public bool DeleteOrphans { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastSyncDate { get; set; }
    public List<string> FilterPatterns { get; set; } = new();
    public SyncMode SyncMode { get; set; } = SyncMode.OneWay;

    [JsonIgnore]
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name)
        && !string.IsNullOrEmpty(SourcePath)
        && !string.IsNullOrEmpty(DestinationPath)
        && !string.Equals(SourcePath, DestinationPath, StringComparison.OrdinalIgnoreCase)
        && IntervalMinutes >= 1
        && IntervalMinutes <= 120;

    public SyncPair Clone()
    {
        return new SyncPair
        {
            Id = Id,
            Name = Name,
            SourcePath = SourcePath,
            DestinationPath = DestinationPath,
            IntervalMinutes = IntervalMinutes,
            DeleteOrphans = DeleteOrphans,
            IsEnabled = IsEnabled,
            LastSyncDate = LastSyncDate,
            FilterPatterns = new List<string>(FilterPatterns),
            SyncMode = SyncMode
        };
    }
}

public class SyncResult
{
    public int FilesCopied { get; set; }
    public int FilesDeleted { get; set; }
    public ulong BytesTransferred { get; set; }
    public List<string> Errors { get; set; } = new();

    public bool HasErrors => Errors.Count > 0;
    public bool IsClean => FilesCopied == 0 && FilesDeleted == 0 && Errors.Count == 0;

    public string Summary
    {
        get
        {
            var parts = new List<string>();
            if (FilesCopied > 0) parts.Add($"{FilesCopied} copied");
            if (FilesDeleted > 0) parts.Add($"{FilesDeleted} deleted");
            if (BytesTransferred > 0) parts.Add(FormatBytes(BytesTransferred));
            if (parts.Count == 0) return Strings.Resources.NoChanges;
            return string.Join(", ", parts);
        }
    }

    private static string FormatBytes(ulong bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

public class PairStatus
{
    public enum StateType
    {
        Idle,
        Syncing,
        Error
    }

    public StateType State { get; set; } = StateType.Idle;
    public string? ErrorMessage { get; set; }
    public SyncResult? LastResult { get; set; }
    public DateTime? LastSyncDate { get; set; }
}
