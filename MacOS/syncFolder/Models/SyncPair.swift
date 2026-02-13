import Foundation

struct SyncPair: Identifiable, Codable, Equatable, Hashable, Sendable {
    var id: UUID
    var name: String
    var sourcePath: String
    var destinationPath: String
    /// Polling interval in minutes (1-120)
    var intervalMinutes: Int
    var deleteOrphans: Bool
    var isEnabled: Bool
    var lastSyncDate: Date?
    /// Glob-like exclude patterns (e.g. "*.log", ".DS_Store", "node_modules")
    var filterPatterns: [String]
    /// Sync direction mode
    var syncMode: SyncMode

    init(name: String = "",
         sourcePath: String = "",
         destinationPath: String = "",
         intervalMinutes: Int = 5,
         deleteOrphans: Bool = false,
         isEnabled: Bool = true,
         filterPatterns: [String] = [],
         syncMode: SyncMode = .oneWay) {
        self.id = UUID()
        self.name = name
        self.sourcePath = sourcePath
        self.destinationPath = destinationPath
        self.intervalMinutes = intervalMinutes
        self.deleteOrphans = deleteOrphans
        self.isEnabled = isEnabled
        self.filterPatterns = filterPatterns
        self.syncMode = syncMode
    }

    var isValid: Bool {
        !name.trimmingCharacters(in: .whitespaces).isEmpty
            && !sourcePath.isEmpty
            && !destinationPath.isEmpty
            && sourcePath != destinationPath
            && intervalMinutes >= 1
            && intervalMinutes <= 120
    }

    var sourceURL: URL { URL(fileURLWithPath: sourcePath) }
    var destinationURL: URL { URL(fileURLWithPath: destinationPath) }
}

// MARK: - Sync Mode

enum SyncMode: String, Codable, Sendable, CaseIterable {
    case oneWay = "oneWay"
    case bidirectional = "bidirectional"
}

// MARK: - Sync Result

struct SyncResult: Codable, Equatable, Sendable {
    let filesCopied: Int
    let filesDeleted: Int
    let bytesTransferred: UInt64
    let errors: [String]

    var hasErrors: Bool { !errors.isEmpty }
    var isClean: Bool { filesCopied == 0 && filesDeleted == 0 && errors.isEmpty }

    var summary: String {
        var parts: [String] = []
        if filesCopied > 0 { parts.append(String(localized: "\(filesCopied) copied")) }
        if filesDeleted > 0 { parts.append(String(localized: "\(filesDeleted) deleted")) }
        if bytesTransferred > 0 { parts.append(ByteCountFormatter.string(fromByteCount: Int64(bytesTransferred), countStyle: .file)) }
        if parts.isEmpty { return String(localized: "No changes") }
        return parts.joined(separator: ", ")
    }
}

// MARK: - Pair Status

struct PairStatus: Equatable, Sendable {
    enum State: Equatable, Sendable {
        case idle, syncing, error
    }

    var state: State = .idle
    var errorMessage: String?
    var lastResult: SyncResult?
    var lastSyncDate: Date?
}

// MARK: - Log Entry

struct LogEntry: Identifiable, Sendable {
    let id = UUID()
    let pairId: UUID
    let pairName: String
    let result: SyncResult?
    let error: String?
    let date: Date

    init(pairId: UUID, pairName: String, result: SyncResult, date: Date) {
        self.pairId = pairId
        self.pairName = pairName
        self.result = result
        self.error = result.hasErrors ? result.errors.first : nil
        self.date = date
    }

    init(pairId: UUID, pairName: String, error: String, date: Date) {
        self.pairId = pairId
        self.pairName = pairName
        self.result = nil
        self.error = error
        self.date = date
    }

    var isError: Bool { error != nil && result == nil }

    var icon: String {
        if isError { return "exclamationmark.triangle.fill" }
        if let r = result, r.hasErrors { return "exclamationmark.circle.fill" }
        return "checkmark.circle.fill"
    }
}
