import Foundation

struct FileManifest: Codable, Sendable {
    var entries: [String: FileEntry]

    init(entries: [String: FileEntry] = [:]) {
        self.entries = entries
    }
}

struct FileEntry: Codable, Equatable, Sendable {
    let size: UInt64
    let modificationDate: Date
}

// MARK: - Sync Plan

enum SyncAction: Sendable {
    case copy(relativePath: String)
    case delete(relativePath: String)
    case copyReverse(relativePath: String)
    case deleteReverse(relativePath: String)
}

// MARK: - Bidirectional Manifest

struct BiDirectionalManifest: Codable, Sendable {
    var sourceEntries: [String: FileEntry]
    var destEntries: [String: FileEntry]

    init(sourceEntries: [String: FileEntry] = [:], destEntries: [String: FileEntry] = [:]) {
        self.sourceEntries = sourceEntries
        self.destEntries = destEntries
    }
}

enum SyncError: LocalizedError, Sendable {
    case sourceNotFound(String)
    case destinationNotWritable(String)
    case scanFailed(String)

    var errorDescription: String? {
        switch self {
        case .sourceNotFound(let path):
            return String(localized: "Source folder not found: \(path)")
        case .destinationNotWritable(let path):
            return String(localized: "Cannot write to destination: \(path)")
        case .scanFailed(let reason):
            return String(localized: "Scan failed: \(reason)")
        }
    }
}

// MARK: - Manifest Manager

final class ManifestManager: @unchecked Sendable {
    static let shared = ManifestManager()

    private let baseURL: URL
    private let queue = DispatchQueue(label: "com.syncfolder.manifestManager")

    init() {
        let caches = FileManager.default.urls(for: .cachesDirectory, in: .userDomainMask).first
            ?? FileManager.default.temporaryDirectory
        baseURL = caches.appendingPathComponent("com.syncfolder.syncFolder/manifests", isDirectory: true)
        try? FileManager.default.createDirectory(at: baseURL, withIntermediateDirectories: true)
    }

    private func url(for pairId: UUID) -> URL {
        baseURL.appendingPathComponent("\(pairId.uuidString).json")
    }

    func load(pairId: UUID) -> FileManifest {
        queue.sync {
            let fileURL = url(for: pairId)
            guard let data = try? Data(contentsOf: fileURL),
                  let manifest = try? JSONDecoder().decode(FileManifest.self, from: data) else {
                return FileManifest()
            }
            return manifest
        }
    }

    func save(_ manifest: FileManifest, pairId: UUID) {
        queue.sync {
            let fileURL = url(for: pairId)
            guard let data = try? JSONEncoder().encode(manifest) else { return }
            try? data.write(to: fileURL, options: .atomic)
        }
    }

    func deleteManifest(pairId: UUID) {
        queue.sync {
            try? FileManager.default.removeItem(at: url(for: pairId))
            try? FileManager.default.removeItem(at: biDirURL(for: pairId))
        }
    }

    // MARK: - Bidirectional Manifest

    private func biDirURL(for pairId: UUID) -> URL {
        baseURL.appendingPathComponent("\(pairId.uuidString)_bidir.json")
    }

    func loadBiDir(pairId: UUID) -> BiDirectionalManifest {
        queue.sync {
            let fileURL = biDirURL(for: pairId)
            guard let data = try? Data(contentsOf: fileURL),
                  let manifest = try? JSONDecoder().decode(BiDirectionalManifest.self, from: data) else {
                return BiDirectionalManifest()
            }
            return manifest
        }
    }

    func saveBiDir(_ manifest: BiDirectionalManifest, pairId: UUID) {
        queue.sync {
            let fileURL = biDirURL(for: pairId)
            guard let data = try? JSONEncoder().encode(manifest) else { return }
            try? data.write(to: fileURL, options: .atomic)
        }
    }
}
