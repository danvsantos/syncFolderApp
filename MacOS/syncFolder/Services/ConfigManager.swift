import Foundation

final class ConfigManager: @unchecked Sendable {
    static let shared = ConfigManager()

    private let configURL: URL
    private let queue = DispatchQueue(label: "com.syncfolder.configManager")

    init() {
        let appSupport = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first
            ?? FileManager.default.temporaryDirectory
        let appDir = appSupport.appendingPathComponent("com.syncfolder.syncFolder", isDirectory: true)
        try? FileManager.default.createDirectory(at: appDir, withIntermediateDirectories: true)
        configURL = appDir.appendingPathComponent("config.json")
    }

    func loadPairs() -> [SyncPair] {
        queue.sync {
            guard let data = try? Data(contentsOf: configURL),
                  let pairs = try? JSONDecoder().decode([SyncPair].self, from: data) else {
                return []
            }
            return pairs
        }
    }

    func savePairs(_ pairs: [SyncPair]) {
        queue.sync {
            let encoder = JSONEncoder()
            encoder.outputFormatting = [.prettyPrinted, .sortedKeys]
            guard let data = try? encoder.encode(pairs) else { return }
            try? data.write(to: configURL, options: .atomic)
        }
    }
}
