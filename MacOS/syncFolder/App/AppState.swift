import Foundation
import SwiftUI

@MainActor
final class AppState: ObservableObject {
    // MARK: - Published State

    @Published var pairs: [SyncPair] = []
    @Published var statuses: [UUID: PairStatus] = [:]
    @Published var logs: [LogEntry] = []
    @Published var notificationsEnabled: Bool {
        didSet { UserDefaults.standard.set(notificationsEnabled, forKey: "notificationsEnabled") }
    }

    // MARK: - Computed

    var isSyncing: Bool {
        statuses.values.contains { $0.state == .syncing }
    }

    var activePairCount: Int {
        pairs.filter(\.isEnabled).count
    }

    // MARK: - Private

    private let configManager = ConfigManager.shared
    private let engine = SyncEngine()
    private var scheduledTasks: [UUID: Task<Void, Never>] = [:]

    // MARK: - Init

    init() {
        self.notificationsEnabled = UserDefaults.standard.bool(forKey: "notificationsEnabled")
        pairs = configManager.loadPairs()
        if notificationsEnabled {
            NotificationManager.shared.requestPermission()
        }
        scheduleAll()
    }

    // MARK: - Pair Management

    func addPair(_ pair: SyncPair) {
        pairs.append(pair)
        save()
    }

    func updatePair(_ pair: SyncPair) {
        if let i = pairs.firstIndex(where: { $0.id == pair.id }) {
            pairs[i] = pair
            save()
        }
    }

    func deletePair(_ pair: SyncPair) {
        // Cancel the task first, then clean up
        scheduledTasks[pair.id]?.cancel()
        scheduledTasks.removeValue(forKey: pair.id)
        pairs.removeAll { $0.id == pair.id }
        statuses.removeValue(forKey: pair.id)
        ManifestManager.shared.deleteManifest(pairId: pair.id)
        configManager.savePairs(pairs)
    }

    func save() {
        configManager.savePairs(pairs)
        scheduleAll()
    }

    // MARK: - Sync

    func syncNow(pairId: UUID? = nil) {
        let pairsToSync: [SyncPair]
        if let id = pairId {
            pairsToSync = pairs.filter { $0.id == id }
        } else {
            pairsToSync = pairs.filter(\.isEnabled)
        }
        for pair in pairsToSync {
            Task { await performSync(pair: pair) }
        }
    }

    private func performSync(pair: SyncPair) async {
        // Skip if already syncing this pair
        guard statuses[pair.id]?.state != .syncing else { return }

        // Mark as syncing (preserving last result for UI)
        let prev = statuses[pair.id]
        statuses[pair.id] = PairStatus(state: .syncing,
                                       lastResult: prev?.lastResult,
                                       lastSyncDate: prev?.lastSyncDate)

        let result: SyncResult
        do {
            result = try await engine.sync(pair: pair)
            statuses[pair.id] = PairStatus(state: .idle, lastResult: result, lastSyncDate: Date())
        } catch {
            result = SyncResult(filesCopied: 0, filesDeleted: 0, bytesTransferred: 0,
                                errors: [error.localizedDescription])
            statuses[pair.id] = PairStatus(state: .error,
                                           errorMessage: error.localizedDescription,
                                           lastResult: result,
                                           lastSyncDate: Date())
        }

        appendLog(LogEntry(pairId: pair.id, pairName: pair.name, result: result, date: Date()))

        // Send notifications
        if notificationsEnabled {
            if statuses[pair.id]?.state == .error {
                NotificationManager.shared.sendSyncFailed(
                    pairName: pair.name,
                    error: result.errors.first ?? "Unknown error"
                )
            } else if !result.isClean {
                NotificationManager.shared.sendSyncCompleted(pairName: pair.name, result: result)
            }
        }

        // Persist last sync date
        if let i = pairs.firstIndex(where: { $0.id == pair.id }) {
            pairs[i].lastSyncDate = Date()
            configManager.savePairs(pairs)
        }
    }

    // MARK: - Scheduler

    func scheduleAll() {
        // Cancel all existing scheduled tasks
        for (_, task) in scheduledTasks {
            task.cancel()
        }
        scheduledTasks.removeAll()

        for pair in pairs where pair.isEnabled {
            schedulePair(pair)
        }
    }

    private func schedulePair(_ pair: SyncPair) {
        let intervalSeconds = pair.intervalMinutes * 60
        let task = Task { [weak self] in
            // Small delay before first sync so UI has time to settle
            try? await Task.sleep(for: .seconds(3))
            guard !Task.isCancelled else { return }

            await self?.performSync(pair: pair)

            while !Task.isCancelled {
                try? await Task.sleep(for: .seconds(intervalSeconds))
                guard !Task.isCancelled else { break }
                await self?.performSync(pair: pair)
            }
        }
        scheduledTasks[pair.id] = task
    }

    // MARK: - Logs

    private func appendLog(_ entry: LogEntry) {
        logs.insert(entry, at: 0)
        if logs.count > 200 {
            logs = Array(logs.prefix(200))
        }
    }

    func clearLogs() {
        logs.removeAll()
    }
}
