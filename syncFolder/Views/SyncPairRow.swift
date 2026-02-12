import SwiftUI

struct SyncPairRow: View {
    let pair: SyncPair
    let status: PairStatus?
    @EnvironmentObject var appState: AppState

    var body: some View {
        HStack(spacing: 12) {
            // Status indicator
            statusIcon
                .frame(width: 28, height: 28)

            // Info
            VStack(alignment: .leading, spacing: 4) {
                HStack(alignment: .firstTextBaseline) {
                    Text(pair.name)
                        .font(.body.weight(.semibold))

                    if !pair.isEnabled {
                        Text("DISABLED")
                            .font(.caption2.weight(.bold))
                            .foregroundStyle(.secondary)
                            .padding(.horizontal, 6)
                            .padding(.vertical, 1)
                            .background(.quaternary, in: Capsule())
                    }

                    Spacer()

                    if let status = status, let date = status.lastSyncDate {
                        Text(date.relativeDescription)
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }

                // Paths
                HStack(spacing: 4) {
                    Image(systemName: "folder.fill")
                        .font(.caption2)
                        .foregroundStyle(.blue)
                    Text(pair.sourcePath.abbreviatingWithTilde)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .lineLimit(1)
                        .truncationMode(.middle)

                    Image(systemName: pair.syncMode == .bidirectional ? "arrow.left.arrow.right" : "arrow.right")
                        .font(.caption2)
                        .foregroundStyle(.tertiary)

                    Image(systemName: "folder.fill")
                        .font(.caption2)
                        .foregroundStyle(.teal)
                    Text(pair.destinationPath.abbreviatingWithTilde)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .lineLimit(1)
                        .truncationMode(.middle)
                }

                // Status line
                HStack(spacing: 12) {
                    Label("Every \(pair.intervalMinutes) min", systemImage: "clock")
                        .font(.caption2)
                        .foregroundStyle(.tertiary)

                    if pair.deleteOrphans {
                        Label("Delete orphans", systemImage: "trash")
                            .font(.caption2)
                            .foregroundStyle(.tertiary)
                    }

                    if let status = status {
                        switch status.state {
                        case .syncing:
                            HStack(spacing: 4) {
                                ProgressView()
                                    .controlSize(.mini)
                                Text("Syncing...")
                                    .font(.caption2)
                                    .foregroundStyle(.blue)
                            }
                        case .error:
                            Text(status.errorMessage ?? "Error")
                                .font(.caption2)
                                .foregroundStyle(.red)
                                .lineLimit(1)
                        case .idle:
                            if let result = status.lastResult {
                                Text(result.summary)
                                    .font(.caption2)
                                    .foregroundStyle(result.hasErrors ? .orange : .green)
                            }
                        }
                    }
                }
            }

            // Sync button
            Button {
                appState.syncNow(pairId: pair.id)
            } label: {
                Image(systemName: "arrow.triangle.2.circlepath")
                    .font(.body)
            }
            .buttonStyle(.borderless)
            .help("Sync now")
            .disabled(status?.state == .syncing)
        }
        .padding(.vertical, 4)
    }

    @ViewBuilder
    private var statusIcon: some View {
        ZStack {
            Circle()
                .fill(statusColor.opacity(0.15))
            Image(systemName: statusSystemImage)
                .font(.system(size: 13, weight: .semibold))
                .foregroundStyle(statusColor)
        }
    }

    private var statusColor: Color {
        guard pair.isEnabled else { return .secondary }
        switch status?.state {
        case .syncing: return .blue
        case .error: return .red
        default:
            if let r = status?.lastResult, r.hasErrors { return .orange }
            return .green
        }
    }

    private var statusSystemImage: String {
        guard pair.isEnabled else { return "pause" }
        switch status?.state {
        case .syncing: return "arrow.triangle.2.circlepath"
        case .error: return "exclamationmark"
        default: return "checkmark"
        }
    }
}

// MARK: - Path Abbreviation

extension String {
    var abbreviatingWithTilde: String {
        if let home = ProcessInfo.processInfo.environment["HOME"], self.hasPrefix(home) {
            return "~" + self.dropFirst(home.count)
        }
        return self
    }
}
