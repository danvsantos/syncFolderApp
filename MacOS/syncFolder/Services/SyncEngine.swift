import Foundation

actor SyncEngine {
    private let fileManager = FileManager()
    private let manifestManager = ManifestManager.shared
    private let maxConcurrent = 4

    func sync(pair: SyncPair) throws -> SyncResult {
        switch pair.syncMode {
        case .oneWay:
            return try syncOneWay(pair: pair)
        case .bidirectional:
            return try syncBidirectional(pair: pair)
        }
    }

    // MARK: - One-Way Sync

    private func syncOneWay(pair: SyncPair) throws -> SyncResult {
        let sourceURL = pair.sourceURL
        let destURL = pair.destinationURL

        // Validate source exists
        var isDir: ObjCBool = false
        guard fileManager.fileExists(atPath: sourceURL.path, isDirectory: &isDir), isDir.boolValue else {
            throw SyncError.sourceNotFound(pair.sourcePath)
        }

        // Create destination if needed
        if !fileManager.fileExists(atPath: destURL.path) {
            try fileManager.createDirectory(at: destURL, withIntermediateDirectories: true)
        }

        // Load previous manifest
        let manifest = manifestManager.load(pairId: pair.id)

        // Scan source directory
        let currentFiles = try scanDirectory(at: sourceURL, filters: pair.filterPatterns)

        // Compute plan
        let actions = computePlan(current: currentFiles, manifest: manifest, deleteOrphans: pair.deleteOrphans)

        // Execute actions
        let result = try executeActions(actions, sourceBase: sourceURL, destBase: destURL)

        // Save updated manifest
        let newManifest = FileManifest(entries: currentFiles)
        manifestManager.save(newManifest, pairId: pair.id)

        return result
    }

    // MARK: - Bidirectional Sync

    private func syncBidirectional(pair: SyncPair) throws -> SyncResult {
        let sourceURL = pair.sourceURL
        let destURL = pair.destinationURL

        // Validate source exists
        var isDir: ObjCBool = false
        guard fileManager.fileExists(atPath: sourceURL.path, isDirectory: &isDir), isDir.boolValue else {
            throw SyncError.sourceNotFound(pair.sourcePath)
        }

        // Create destination if needed
        if !fileManager.fileExists(atPath: destURL.path) {
            try fileManager.createDirectory(at: destURL, withIntermediateDirectories: true)
        }

        // Load previous bidirectional manifest
        let biManifest = manifestManager.loadBiDir(pairId: pair.id)

        // Scan both directories
        let sourceFiles = try scanDirectory(at: sourceURL, filters: pair.filterPatterns)
        let destFiles = try scanDirectory(at: destURL, filters: pair.filterPatterns)

        // Compute bidirectional plan
        let actions = computeBidirectionalPlan(
            sourceFiles: sourceFiles,
            destFiles: destFiles,
            manifest: biManifest,
            deleteOrphans: pair.deleteOrphans
        )

        // Execute actions
        var filesCopied = 0
        var filesDeleted = 0
        var bytesTransferred: UInt64 = 0
        var errors: [String] = []

        for action in actions {
            switch action {
            case .copy(let relativePath):
                do {
                    let bytes = try copyFile(from: sourceURL, to: destURL, relativePath: relativePath)
                    filesCopied += 1
                    bytesTransferred += bytes
                } catch {
                    errors.append("Copy '\(relativePath)': \(error.localizedDescription)")
                }
            case .copyReverse(let relativePath):
                do {
                    let bytes = try copyFile(from: destURL, to: sourceURL, relativePath: relativePath)
                    filesCopied += 1
                    bytesTransferred += bytes
                } catch {
                    errors.append("Copy reverse '\(relativePath)': \(error.localizedDescription)")
                }
            case .delete(let relativePath):
                do {
                    try deleteFile(at: destURL, relativePath: relativePath)
                    filesDeleted += 1
                } catch {
                    errors.append("Delete '\(relativePath)': \(error.localizedDescription)")
                }
            case .deleteReverse(let relativePath):
                do {
                    try deleteFile(at: sourceURL, relativePath: relativePath)
                    filesDeleted += 1
                } catch {
                    errors.append("Delete reverse '\(relativePath)': \(error.localizedDescription)")
                }
            }
        }

        // Save updated bidirectional manifest
        // Re-scan to get accurate state after operations
        let updatedSource = try scanDirectory(at: sourceURL, filters: pair.filterPatterns)
        let updatedDest = try scanDirectory(at: destURL, filters: pair.filterPatterns)
        let newBiManifest = BiDirectionalManifest(
            sourceEntries: updatedSource,
            destEntries: updatedDest
        )
        manifestManager.saveBiDir(newBiManifest, pairId: pair.id)

        return SyncResult(
            filesCopied: filesCopied,
            filesDeleted: filesDeleted,
            bytesTransferred: bytesTransferred,
            errors: errors
        )
    }

    private func computeBidirectionalPlan(
        sourceFiles: [String: FileEntry],
        destFiles: [String: FileEntry],
        manifest: BiDirectionalManifest,
        deleteOrphans: Bool
    ) -> [SyncAction] {
        var actions: [SyncAction] = []
        let allPaths = Set(sourceFiles.keys).union(destFiles.keys).union(manifest.sourceEntries.keys).union(manifest.destEntries.keys)

        for path in allPaths {
            let inSource = sourceFiles[path]
            let inDest = destFiles[path]
            let wasInSource = manifest.sourceEntries[path]
            let wasInDest = manifest.destEntries[path]

            switch (inSource, inDest) {
            case (.some(_), .none):
                if wasInDest != nil && deleteOrphans {
                    // Was in dest but deleted from dest → delete from source
                    actions.append(.deleteReverse(relativePath: path))
                } else {
                    // New in source or dest never had it → copy to dest
                    actions.append(.copy(relativePath: path))
                }

            case (.none, .some(_)):
                if wasInSource != nil && deleteOrphans {
                    // Was in source but deleted from source → delete from dest
                    actions.append(.delete(relativePath: path))
                } else {
                    // New in dest or source never had it → copy to source
                    actions.append(.copyReverse(relativePath: path))
                }

            case (.some(let srcEntry), .some(let dstEntry)):
                let srcChanged = wasInSource.map { srcEntry.size != $0.size || srcEntry.modificationDate != $0.modificationDate } ?? true
                let dstChanged = wasInDest.map { dstEntry.size != $0.size || dstEntry.modificationDate != $0.modificationDate } ?? true

                if srcChanged && !dstChanged {
                    actions.append(.copy(relativePath: path))
                } else if !srcChanged && dstChanged {
                    actions.append(.copyReverse(relativePath: path))
                } else if srcChanged && dstChanged {
                    // Conflict: last-write-wins
                    if srcEntry.modificationDate >= dstEntry.modificationDate {
                        actions.append(.copy(relativePath: path))
                    } else {
                        actions.append(.copyReverse(relativePath: path))
                    }
                }
                // else neither changed → no action

            case (.none, .none):
                break
            }
        }

        return actions
    }

    // MARK: - Scan

    private func scanDirectory(at url: URL, filters: [String]) throws -> [String: FileEntry] {
        var entries: [String: FileEntry] = [:]
        // Use realpath to resolve symlinks (e.g. /tmp → /private/tmp on macOS)
        // so basePath matches paths returned by the enumerator
        let resolvedPath: String
        if let rp = realpath(url.path, nil) {
            resolvedPath = String(cString: rp)
            free(rp)
        } else {
            resolvedPath = url.path
        }
        let basePath = resolvedPath.hasSuffix("/") ? resolvedPath : resolvedPath + "/"
        let resolvedURL = URL(fileURLWithPath: resolvedPath, isDirectory: true)

        guard let enumerator = fileManager.enumerator(
            at: resolvedURL,
            includingPropertiesForKeys: [.fileSizeKey, .contentModificationDateKey, .isRegularFileKey],
            options: [.skipsHiddenFiles, .skipsPackageDescendants]
        ) else {
            throw SyncError.scanFailed("Cannot enumerate \(url.path)")
        }

        for case let fileURL as URL in enumerator {
            do {
                let resourceValues = try fileURL.resourceValues(forKeys: [
                    .isRegularFileKey, .fileSizeKey, .contentModificationDateKey
                ])

                guard resourceValues.isRegularFile == true else {
                    // Check if directory name matches a filter pattern (e.g. "node_modules")
                    let dirName = fileURL.lastPathComponent
                    if shouldExclude(fileName: dirName, filters: filters) {
                        enumerator.skipDescendants()
                    }
                    continue
                }

                let relativePath = String(fileURL.path.dropFirst(basePath.count))

                // Check if file should be excluded
                if shouldExclude(fileName: fileURL.lastPathComponent, filters: filters) {
                    continue
                }

                let size = UInt64(resourceValues.fileSize ?? 0)
                let modDate = resourceValues.contentModificationDate ?? Date.distantPast

                entries[relativePath] = FileEntry(size: size, modificationDate: modDate)
            } catch {
                continue
            }
        }

        return entries
    }

    /// Check if a filename matches any of the exclude filter patterns using fnmatch
    private func shouldExclude(fileName: String, filters: [String]) -> Bool {
        for pattern in filters {
            if fnmatch(pattern, fileName, 0) == 0 {
                return true
            }
        }
        return false
    }

    // MARK: - Plan

    private func computePlan(
        current: [String: FileEntry],
        manifest: FileManifest,
        deleteOrphans: Bool
    ) -> [SyncAction] {
        var actions: [SyncAction] = []

        // New or modified files
        for (path, entry) in current {
            if let oldEntry = manifest.entries[path] {
                if entry.size != oldEntry.size || entry.modificationDate != oldEntry.modificationDate {
                    actions.append(.copy(relativePath: path))
                }
            } else {
                actions.append(.copy(relativePath: path))
            }
        }

        // Deleted files
        if deleteOrphans {
            for path in manifest.entries.keys {
                if current[path] == nil {
                    actions.append(.delete(relativePath: path))
                }
            }
        }

        return actions
    }

    // MARK: - Execute Actions

    private func executeActions(_ actions: [SyncAction], sourceBase: URL, destBase: URL) throws -> SyncResult {
        var filesCopied = 0
        var filesDeleted = 0
        var bytesTransferred: UInt64 = 0
        var errors: [String] = []

        for action in actions {
            switch action {
            case .copy(let relativePath):
                do {
                    let bytes = try copyFile(from: sourceBase, to: destBase, relativePath: relativePath)
                    filesCopied += 1
                    bytesTransferred += bytes
                } catch {
                    errors.append("Copy '\(relativePath)': \(error.localizedDescription)")
                }
            case .delete(let relativePath):
                do {
                    try deleteFile(at: destBase, relativePath: relativePath)
                    filesDeleted += 1
                } catch {
                    errors.append("Delete '\(relativePath)': \(error.localizedDescription)")
                }
            case .copyReverse, .deleteReverse:
                break // Not used in one-way sync
            }
        }

        return SyncResult(
            filesCopied: filesCopied,
            filesDeleted: filesDeleted,
            bytesTransferred: bytesTransferred,
            errors: errors
        )
    }

    // MARK: - File Operations

    private func copyFile(from sourceBase: URL, to destBase: URL, relativePath: String) throws -> UInt64 {
        let sourceFile = sourceBase.appendingPathComponent(relativePath)
        let destFile = destBase.appendingPathComponent(relativePath)

        // Ensure parent directory exists
        let destDir = destFile.deletingLastPathComponent()
        if !fileManager.fileExists(atPath: destDir.path) {
            try fileManager.createDirectory(at: destDir, withIntermediateDirectories: true)
        }

        // Remove existing file if present
        if fileManager.fileExists(atPath: destFile.path) {
            try fileManager.removeItem(at: destFile)
        }

        // Copy
        try fileManager.copyItem(at: sourceFile, to: destFile)

        // Preserve modification date
        let attrs = try fileManager.attributesOfItem(atPath: sourceFile.path)
        if let modDate = attrs[.modificationDate] as? Date {
            try fileManager.setAttributes([.modificationDate: modDate], ofItemAtPath: destFile.path)
        }

        let size = (try? fileManager.attributesOfItem(atPath: destFile.path)[.size] as? UInt64) ?? 0
        return size
    }

    private func deleteFile(at destBase: URL, relativePath: String) throws {
        let destFile = destBase.appendingPathComponent(relativePath)

        if fileManager.fileExists(atPath: destFile.path) {
            try fileManager.removeItem(at: destFile)
        }

        // Clean up empty parent directories
        var parent = destFile.deletingLastPathComponent()
        while parent.path != destBase.path {
            let contents = try? fileManager.contentsOfDirectory(atPath: parent.path)
            if contents?.isEmpty == true {
                try? fileManager.removeItem(at: parent)
                parent = parent.deletingLastPathComponent()
            } else {
                break
            }
        }
    }
}
