using System.IO;
using System.Text.RegularExpressions;
using syncFolder.Models;

namespace syncFolder.Services;

public class SyncEngine
{
    private readonly ManifestManager _manifestManager = ManifestManager.Shared;

    public async Task<SyncResult> SyncAsync(SyncPair pair)
    {
        return pair.SyncMode switch
        {
            SyncMode.OneWay => await Task.Run(() => SyncOneWay(pair)),
            SyncMode.Bidirectional => await Task.Run(() => SyncBidirectional(pair)),
            _ => throw new SyncException("Unknown sync mode")
        };
    }

    private SyncResult SyncOneWay(SyncPair pair)
    {
        var sourcePath = pair.SourcePath;
        var destPath = pair.DestinationPath;

        if (!Directory.Exists(sourcePath))
            throw new SyncException($"Source folder not found: {sourcePath}");

        if (!Directory.Exists(destPath))
            Directory.CreateDirectory(destPath);

        var manifest = _manifestManager.Load(pair.Id);
        var currentFiles = ScanDirectory(sourcePath, pair.FilterPatterns);
        var actions = ComputePlan(currentFiles, manifest, pair.DeleteOrphans);
        var result = ExecuteActions(actions, sourcePath, destPath);

        var newManifest = new FileManifest { Entries = currentFiles };
        _manifestManager.Save(newManifest, pair.Id);

        return result;
    }

    private SyncResult SyncBidirectional(SyncPair pair)
    {
        var sourcePath = pair.SourcePath;
        var destPath = pair.DestinationPath;

        if (!Directory.Exists(sourcePath))
            throw new SyncException($"Source folder not found: {sourcePath}");

        if (!Directory.Exists(destPath))
            Directory.CreateDirectory(destPath);

        var biManifest = _manifestManager.LoadBiDir(pair.Id);
        var sourceFiles = ScanDirectory(sourcePath, pair.FilterPatterns);
        var destFiles = ScanDirectory(destPath, pair.FilterPatterns);

        var actions = ComputeBidirectionalPlan(sourceFiles, destFiles, biManifest, pair.DeleteOrphans);

        int filesCopied = 0, filesDeleted = 0;
        ulong bytesTransferred = 0;
        var errors = new List<string>();

        foreach (var action in actions)
        {
            switch (action.Action)
            {
                case SyncAction.Copy:
                    try
                    {
                        var bytes = CopyFile(sourcePath, destPath, action.RelativePath);
                        filesCopied++;
                        bytesTransferred += bytes;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Copy '{action.RelativePath}': {ex.Message}");
                    }
                    break;

                case SyncAction.CopyReverse:
                    try
                    {
                        var bytes = CopyFile(destPath, sourcePath, action.RelativePath);
                        filesCopied++;
                        bytesTransferred += bytes;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Copy reverse '{action.RelativePath}': {ex.Message}");
                    }
                    break;

                case SyncAction.Delete:
                    try
                    {
                        DeleteFile(destPath, action.RelativePath);
                        filesDeleted++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Delete '{action.RelativePath}': {ex.Message}");
                    }
                    break;

                case SyncAction.DeleteReverse:
                    try
                    {
                        DeleteFile(sourcePath, action.RelativePath);
                        filesDeleted++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Delete reverse '{action.RelativePath}': {ex.Message}");
                    }
                    break;
            }
        }

        // Re-scan after operations for accurate manifest
        var updatedSource = ScanDirectory(sourcePath, pair.FilterPatterns);
        var updatedDest = ScanDirectory(destPath, pair.FilterPatterns);
        var newBiManifest = new BiDirectionalManifest
        {
            SourceEntries = updatedSource,
            DestEntries = updatedDest
        };
        _manifestManager.SaveBiDir(newBiManifest, pair.Id);

        return new SyncResult
        {
            FilesCopied = filesCopied,
            FilesDeleted = filesDeleted,
            BytesTransferred = bytesTransferred,
            Errors = errors
        };
    }

    private Dictionary<string, FileEntry> ScanDirectory(string basePath, List<string> filters)
    {
        var entries = new Dictionary<string, FileEntry>(StringComparer.OrdinalIgnoreCase);

        if (!Directory.Exists(basePath))
            throw new SyncException($"Cannot enumerate {basePath}");

        basePath = Path.GetFullPath(basePath);
        if (!basePath.EndsWith(Path.DirectorySeparatorChar))
            basePath += Path.DirectorySeparatorChar;

        ScanRecursive(basePath, basePath, filters, entries);
        return entries;
    }

    private void ScanRecursive(string basePath, string currentPath, List<string> filters,
        Dictionary<string, FileEntry> entries)
    {
        try
        {
            foreach (var dir in Directory.GetDirectories(currentPath))
            {
                var dirName = Path.GetFileName(dir);

                // Skip hidden directories (starting with .)
                if (dirName.StartsWith('.')) continue;

                // Check if directory name matches a filter
                if (ShouldExclude(dirName, filters)) continue;

                ScanRecursive(basePath, dir, filters, entries);
            }

            foreach (var file in Directory.GetFiles(currentPath))
            {
                var fileName = Path.GetFileName(file);

                // Skip hidden files
                if (fileName.StartsWith('.')) continue;

                // Check if file matches a filter
                if (ShouldExclude(fileName, filters)) continue;

                try
                {
                    var info = new FileInfo(file);
                    var relativePath = file.Substring(basePath.Length).Replace('\\', '/');
                    entries[relativePath] = new FileEntry
                    {
                        Size = (ulong)info.Length,
                        ModificationDate = info.LastWriteTimeUtc
                    };
                }
                catch
                {
                    // Skip files we can't read
                }
            }
        }
        catch
        {
            // Skip directories we can't access
        }
    }

    private static bool ShouldExclude(string fileName, List<string> filters)
    {
        foreach (var pattern in filters)
        {
            if (MatchGlob(pattern, fileName))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Simple glob matching (supports * and ? wildcards), equivalent to fnmatch on macOS.
    /// </summary>
    private static bool MatchGlob(string pattern, string input)
    {
        // Convert glob pattern to regex
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
    }

    private List<SyncActionItem> ComputePlan(Dictionary<string, FileEntry> current,
        FileManifest manifest, bool deleteOrphans)
    {
        var actions = new List<SyncActionItem>();

        // New or modified files
        foreach (var (path, entry) in current)
        {
            if (manifest.Entries.TryGetValue(path, out var oldEntry))
            {
                if (entry.Size != oldEntry.Size || entry.ModificationDate != oldEntry.ModificationDate)
                {
                    actions.Add(new SyncActionItem { Action = SyncAction.Copy, RelativePath = path });
                }
            }
            else
            {
                actions.Add(new SyncActionItem { Action = SyncAction.Copy, RelativePath = path });
            }
        }

        // Deleted files
        if (deleteOrphans)
        {
            foreach (var path in manifest.Entries.Keys)
            {
                if (!current.ContainsKey(path))
                {
                    actions.Add(new SyncActionItem { Action = SyncAction.Delete, RelativePath = path });
                }
            }
        }

        return actions;
    }

    private List<SyncActionItem> ComputeBidirectionalPlan(
        Dictionary<string, FileEntry> sourceFiles,
        Dictionary<string, FileEntry> destFiles,
        BiDirectionalManifest manifest,
        bool deleteOrphans)
    {
        var actions = new List<SyncActionItem>();
        var allPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var k in sourceFiles.Keys) allPaths.Add(k);
        foreach (var k in destFiles.Keys) allPaths.Add(k);
        foreach (var k in manifest.SourceEntries.Keys) allPaths.Add(k);
        foreach (var k in manifest.DestEntries.Keys) allPaths.Add(k);

        foreach (var path in allPaths)
        {
            sourceFiles.TryGetValue(path, out var inSource);
            destFiles.TryGetValue(path, out var inDest);
            manifest.SourceEntries.TryGetValue(path, out var wasInSource);
            manifest.DestEntries.TryGetValue(path, out var wasInDest);

            if (inSource != null && inDest == null)
            {
                if (wasInDest != null && deleteOrphans)
                {
                    // Was in dest but deleted from dest -> delete from source
                    actions.Add(new SyncActionItem { Action = SyncAction.DeleteReverse, RelativePath = path });
                }
                else
                {
                    // New in source or dest never had it -> copy to dest
                    actions.Add(new SyncActionItem { Action = SyncAction.Copy, RelativePath = path });
                }
            }
            else if (inSource == null && inDest != null)
            {
                if (wasInSource != null && deleteOrphans)
                {
                    // Was in source but deleted from source -> delete from dest
                    actions.Add(new SyncActionItem { Action = SyncAction.Delete, RelativePath = path });
                }
                else
                {
                    // New in dest or source never had it -> copy to source
                    actions.Add(new SyncActionItem { Action = SyncAction.CopyReverse, RelativePath = path });
                }
            }
            else if (inSource != null && inDest != null)
            {
                var srcChanged = wasInSource == null
                    || inSource.Size != wasInSource.Size
                    || inSource.ModificationDate != wasInSource.ModificationDate;

                var dstChanged = wasInDest == null
                    || inDest.Size != wasInDest.Size
                    || inDest.ModificationDate != wasInDest.ModificationDate;

                if (srcChanged && !dstChanged)
                {
                    actions.Add(new SyncActionItem { Action = SyncAction.Copy, RelativePath = path });
                }
                else if (!srcChanged && dstChanged)
                {
                    actions.Add(new SyncActionItem { Action = SyncAction.CopyReverse, RelativePath = path });
                }
                else if (srcChanged && dstChanged)
                {
                    // Conflict: last-write-wins
                    if (inSource.ModificationDate >= inDest.ModificationDate)
                    {
                        actions.Add(new SyncActionItem { Action = SyncAction.Copy, RelativePath = path });
                    }
                    else
                    {
                        actions.Add(new SyncActionItem { Action = SyncAction.CopyReverse, RelativePath = path });
                    }
                }
            }
        }

        return actions;
    }

    private SyncResult ExecuteActions(List<SyncActionItem> actions, string sourceBase, string destBase)
    {
        int filesCopied = 0, filesDeleted = 0;
        ulong bytesTransferred = 0;
        var errors = new List<string>();

        foreach (var action in actions)
        {
            switch (action.Action)
            {
                case SyncAction.Copy:
                    try
                    {
                        var bytes = CopyFile(sourceBase, destBase, action.RelativePath);
                        filesCopied++;
                        bytesTransferred += bytes;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Copy '{action.RelativePath}': {ex.Message}");
                    }
                    break;

                case SyncAction.Delete:
                    try
                    {
                        DeleteFile(destBase, action.RelativePath);
                        filesDeleted++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Delete '{action.RelativePath}': {ex.Message}");
                    }
                    break;
            }
        }

        return new SyncResult
        {
            FilesCopied = filesCopied,
            FilesDeleted = filesDeleted,
            BytesTransferred = bytesTransferred,
            Errors = errors
        };
    }

    private ulong CopyFile(string sourceBase, string destBase, string relativePath)
    {
        // Convert forward slashes to platform-specific separator
        var platformPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var sourceFile = Path.Combine(sourceBase, platformPath);
        var destFile = Path.Combine(destBase, platformPath);

        // Ensure parent directory exists
        var destDir = Path.GetDirectoryName(destFile);
        if (destDir != null && !Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        // Copy file (overwrite if exists)
        File.Copy(sourceFile, destFile, overwrite: true);

        // Preserve modification date
        var sourceInfo = new FileInfo(sourceFile);
        File.SetLastWriteTimeUtc(destFile, sourceInfo.LastWriteTimeUtc);

        return (ulong)new FileInfo(destFile).Length;
    }

    private void DeleteFile(string basePath, string relativePath)
    {
        var platformPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var filePath = Path.Combine(basePath, platformPath);

        if (File.Exists(filePath))
            File.Delete(filePath);

        // Clean up empty parent directories
        var parent = Path.GetDirectoryName(filePath);
        var baseFullPath = Path.GetFullPath(basePath);
        while (parent != null && !string.Equals(Path.GetFullPath(parent), baseFullPath, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                if (Directory.Exists(parent) && !Directory.EnumerateFileSystemEntries(parent).Any())
                {
                    Directory.Delete(parent);
                    parent = Path.GetDirectoryName(parent);
                }
                else
                {
                    break;
                }
            }
            catch
            {
                break;
            }
        }
    }
}
