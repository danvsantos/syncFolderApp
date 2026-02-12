import SwiftUI

struct MenuBarView: View {
    @EnvironmentObject var appState: AppState
    @Environment(\.openWindow) private var openWindow

    var body: some View {
        VStack(spacing: 0) {
            // Header
            HStack {
                Image(systemName: "arrow.triangle.2.circlepath.circle.fill")
                    .font(.title2)
                    .foregroundStyle(.blue)
                Text("syncFolder")
                    .font(.headline)
                Spacer()
                statusBadge
            }
            .padding(.horizontal, 16)
            .padding(.vertical, 12)

            Divider()

            if appState.pairs.isEmpty {
                emptyState
            } else {
                pairsList
            }

            Divider()

            // Actions
            VStack(spacing: 2) {
                Button {
                    appState.syncNow()
                } label: {
                    Label("Sync All Now", systemImage: "arrow.triangle.2.circlepath")
                        .frame(maxWidth: .infinity, alignment: .leading)
                }
                .buttonStyle(.plain)
                .padding(.horizontal, 16)
                .padding(.vertical, 6)
                .contentShape(Rectangle())
                .hoverHighlight()
                .disabled(appState.pairs.isEmpty)

                Button {
                    openPreferences()
                } label: {
                    Label("Preferences...", systemImage: "gearshape")
                        .frame(maxWidth: .infinity, alignment: .leading)
                }
                .buttonStyle(.plain)
                .padding(.horizontal, 16)
                .padding(.vertical, 6)
                .contentShape(Rectangle())
                .hoverHighlight()
            }
            .padding(.vertical, 4)

            Divider()

            Button {
                NSApplication.shared.terminate(nil)
            } label: {
                Label("Quit syncFolder", systemImage: "power")
                    .frame(maxWidth: .infinity, alignment: .leading)
            }
            .buttonStyle(.plain)
            .padding(.horizontal, 16)
            .padding(.vertical, 8)
            .contentShape(Rectangle())
            .hoverHighlight()
        }
        .frame(width: 320)
    }

    // MARK: - Actions

    private func openPreferences() {
        NSApp.activate(ignoringOtherApps: true)
        openWindow(id: "preferences")
    }

    // MARK: - Subviews

    private var statusBadge: some View {
        Group {
            if appState.isSyncing {
                HStack(spacing: 4) {
                    ProgressView()
                        .controlSize(.small)
                    Text("Syncing")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            } else {
                let errorCount = appState.statuses.values.filter { $0.state == .error }.count
                if errorCount > 0 {
                    Label("\(errorCount) error(s)",
                          systemImage: "exclamationmark.triangle.fill")
                        .font(.caption)
                        .foregroundStyle(.orange)
                } else if appState.activePairCount > 0 {
                    Label("\(appState.activePairCount) active",
                          systemImage: "checkmark.circle.fill")
                        .font(.caption)
                        .foregroundStyle(.green)
                } else {
                    Text("No pairs")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
        }
    }

    private var emptyState: some View {
        VStack(spacing: 8) {
            Image(systemName: "folder.badge.plus")
                .font(.largeTitle)
                .foregroundStyle(.secondary)
            Text("No sync pairs configured")
                .font(.subheadline)
                .foregroundStyle(.secondary)
            Button("Open Preferences") {
                openPreferences()
            }
            .buttonStyle(.borderedProminent)
            .controlSize(.small)
        }
        .frame(maxWidth: .infinity)
        .padding(.vertical, 20)
    }

    private var pairsList: some View {
        ScrollView {
            VStack(spacing: 1) {
                ForEach(appState.pairs) { pair in
                    MenuBarPairRow(pair: pair, status: appState.statuses[pair.id])
                        .environmentObject(appState)
                }
            }
        }
        .frame(maxHeight: 240)
    }
}

// MARK: - Pair Row for Menu Bar

struct MenuBarPairRow: View {
    let pair: SyncPair
    let status: PairStatus?
    @EnvironmentObject var appState: AppState

    var body: some View {
        Button {
            appState.syncNow(pairId: pair.id)
        } label: {
            HStack(spacing: 10) {
                statusIcon
                    .frame(width: 20)

                VStack(alignment: .leading, spacing: 2) {
                    Text(pair.name)
                        .font(.subheadline.weight(.medium))
                        .lineLimit(1)
                    statusText
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .lineLimit(1)
                }

                Spacer()

                if !pair.isEnabled {
                    Text("OFF")
                        .font(.caption2.weight(.semibold))
                        .foregroundStyle(.secondary)
                        .padding(.horizontal, 6)
                        .padding(.vertical, 2)
                        .background(.quaternary, in: Capsule())
                }
            }
            .padding(.horizontal, 16)
            .padding(.vertical, 8)
            .contentShape(Rectangle())
        }
        .buttonStyle(.plain)
        .hoverHighlight()
    }

    @ViewBuilder
    private var statusIcon: some View {
        switch status?.state {
        case .syncing:
            ProgressView()
                .controlSize(.small)
        case .error:
            Image(systemName: "exclamationmark.circle.fill")
                .foregroundStyle(.orange)
        default:
            if pair.isEnabled {
                Image(systemName: "checkmark.circle.fill")
                    .foregroundStyle(.green)
            } else {
                Image(systemName: "pause.circle.fill")
                    .foregroundStyle(.secondary)
            }
        }
    }

    private var statusText: some View {
        Group {
            if let status = status {
                switch status.state {
                case .syncing:
                    Text("Syncing...")
                case .error:
                    Text(status.errorMessage ?? "Error")
                        .foregroundStyle(.orange)
                case .idle:
                    if let date = status.lastSyncDate {
                        Text("Synced \(date.relativeDescription)")
                    } else {
                        Text("Waiting for first sync")
                    }
                }
            } else if pair.isEnabled {
                Text("Waiting for first sync")
            } else {
                Text("Disabled")
            }
        }
    }
}

// MARK: - Hover Highlight Modifier

struct HoverHighlightModifier: ViewModifier {
    @State private var isHovered = false

    func body(content: Content) -> some View {
        content
            .background(isHovered ? Color.primary.opacity(0.08) : Color.clear)
            .cornerRadius(4)
            .onHover { isHovered = $0 }
    }
}

extension View {
    func hoverHighlight() -> some View {
        modifier(HoverHighlightModifier())
    }
}

// MARK: - Date Extension

extension Date {
    var relativeDescription: String {
        let formatter = RelativeDateTimeFormatter()
        formatter.unitsStyle = .abbreviated
        return formatter.localizedString(for: self, relativeTo: Date())
    }
}
