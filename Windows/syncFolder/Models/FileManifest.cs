namespace syncFolder.Models;

public class FileManifest
{
    public Dictionary<string, FileEntry> Entries { get; set; } = new();
}

public class FileEntry
{
    public ulong Size { get; set; }
    public DateTime ModificationDate { get; set; }
}

public class BiDirectionalManifest
{
    public Dictionary<string, FileEntry> SourceEntries { get; set; } = new();
    public Dictionary<string, FileEntry> DestEntries { get; set; } = new();
}

public enum SyncAction
{
    Copy,
    Delete,
    CopyReverse,
    DeleteReverse
}

public class SyncActionItem
{
    public SyncAction Action { get; set; }
    public string RelativePath { get; set; } = string.Empty;
}

public class SyncException : Exception
{
    public SyncException(string message) : base(message) { }
}
