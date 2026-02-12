import ServiceManagement
import SwiftUI

struct PreferencesView: View {
    @EnvironmentObject var appState: AppState
    @State private var selectedTab = 0

    var body: some View {
        TabView(selection: $selectedTab) {
            SyncPairsTab()
                .environmentObject(appState)
                .tabItem {
                    Label("Sync Pairs", systemImage: "folder.badge.gearshape")
                }
                .tag(0)

            ActivityTab()
                .environmentObject(appState)
                .tabItem {
                    Label("Activity", systemImage: "clock.arrow.circlepath")
                }
                .tag(1)

            GeneralTab()
                .environmentObject(appState)
                .tabItem {
                    Label("General", systemImage: "gearshape")
                }
                .tag(2)

            AboutTab()
                .tabItem {
                    Label("About", systemImage: "info.circle")
                }
                .tag(3)
        }
        .frame(width: 680, height: 480)
    }
}

// MARK: - Sync Pairs Tab

struct SyncPairsTab: View {
    @EnvironmentObject var appState: AppState
    @State private var showAddSheet = false
    @State private var editingPair: SyncPair?
    @State private var pairToDelete: SyncPair?

    var body: some View {
        VStack(spacing: 0) {
            if appState.pairs.isEmpty {
                emptyState
            } else {
                pairsList
            }

            Divider()
            toolbar
        }
        // Single sheet for adding — attached at the top level to avoid duplicates
        .sheet(isPresented: $showAddSheet) {
            EditPairSheet(mode: .add) { pair in
                appState.addPair(pair)
            }
        }
        // Sheet for editing
        .sheet(item: $editingPair) { pair in
            EditPairSheet(mode: .edit(pair)) { updated in
                appState.updatePair(updated)
            }
        }
        // Delete confirmation alert
        .alert("Delete Sync Pair?",
               isPresented: Binding(
                   get: { pairToDelete != nil },
                   set: { if !$0 { pairToDelete = nil } }
               )
        ) {
            Button("Cancel", role: .cancel) {
                pairToDelete = nil
            }
            Button("Delete", role: .destructive) {
                if let pair = pairToDelete {
                    appState.deletePair(pair)
                }
                pairToDelete = nil
            }
        } message: {
            if let pair = pairToDelete {
                Text("Are you sure you want to remove \"\(pair.name)\"? Files in the destination will not be deleted.")
            }
        }
    }

    private var emptyState: some View {
        VStack(spacing: 16) {
            Spacer()
            Image(systemName: "folder.badge.plus")
                .font(.system(size: 48))
                .foregroundStyle(.tertiary)
            Text("No Sync Pairs")
                .font(.title2.weight(.medium))
                .foregroundStyle(.secondary)
            Text("Add a sync pair to start synchronizing folders automatically.")
                .font(.subheadline)
                .foregroundStyle(.tertiary)
                .multilineTextAlignment(.center)
            Button("Add Sync Pair") {
                showAddSheet = true
            }
            .buttonStyle(.borderedProminent)
            Spacer()
        }
        .frame(maxWidth: .infinity)
    }

    private var pairsList: some View {
        List {
            ForEach(appState.pairs) { pair in
                SyncPairRow(pair: pair, status: appState.statuses[pair.id])
                    .environmentObject(appState)
                    .contextMenu {
                        Button("Sync Now") { appState.syncNow(pairId: pair.id) }
                        Divider()
                        Button("Edit...") { editingPair = pair }
                        Divider()
                        Button("Delete", role: .destructive) { pairToDelete = pair }
                    }
            }
        }
        .listStyle(.inset(alternatesRowBackgrounds: true))
    }

    private var toolbar: some View {
        HStack(spacing: 12) {
            Button {
                showAddSheet = true
            } label: {
                Image(systemName: "plus")
            }
            .help("Add sync pair")

            Spacer()

            Button {
                appState.syncNow()
            } label: {
                Label("Sync All", systemImage: "arrow.triangle.2.circlepath")
            }
            .disabled(appState.pairs.isEmpty)
        }
        .padding(8)
    }
}

// MARK: - Activity Tab

struct ActivityTab: View {
    @EnvironmentObject var appState: AppState

    var body: some View {
        VStack(spacing: 0) {
            if appState.logs.isEmpty {
                VStack(spacing: 12) {
                    Spacer()
                    Image(systemName: "clock.arrow.circlepath")
                        .font(.system(size: 40))
                        .foregroundStyle(.tertiary)
                    Text("No Activity Yet")
                        .font(.title3.weight(.medium))
                        .foregroundStyle(.secondary)
                    Text("Sync activity will appear here.")
                        .font(.subheadline)
                        .foregroundStyle(.tertiary)
                    Spacer()
                }
                .frame(maxWidth: .infinity)
            } else {
                List(appState.logs) { entry in
                    LogEntryRow(entry: entry)
                }
                .listStyle(.inset(alternatesRowBackgrounds: true))
            }

            Divider()

            HStack {
                Text("\(appState.logs.count) entries")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                Spacer()
                Button("Clear Log") {
                    appState.clearLogs()
                }
                .disabled(appState.logs.isEmpty)
            }
            .padding(8)
        }
    }
}

struct LogEntryRow: View {
    let entry: LogEntry

    var body: some View {
        HStack(spacing: 10) {
            Image(systemName: entry.icon)
                .foregroundStyle(iconColor)
                .frame(width: 20)

            VStack(alignment: .leading, spacing: 2) {
                HStack {
                    Text(entry.pairName)
                        .font(.subheadline.weight(.medium))
                    Spacer()
                    Text(entry.date, style: .relative)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                if let error = entry.error, entry.result == nil {
                    Text(error)
                        .font(.caption)
                        .foregroundStyle(.red)
                        .lineLimit(2)
                } else if let result = entry.result {
                    Text(result.summary)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }
            }
        }
        .padding(.vertical, 2)
    }

    private var iconColor: Color {
        if entry.isError { return .red }
        if entry.result?.hasErrors == true { return .orange }
        return .green
    }
}

// MARK: - General Tab

struct GeneralTab: View {
    @EnvironmentObject var appState: AppState
    @State private var launchAtLogin = SMAppService.mainApp.status == .enabled

    var body: some View {
        Form {
            Section {
                Toggle("Launch syncFolder at Login", isOn: $launchAtLogin)
                    .onChange(of: launchAtLogin) { newValue in
                        do {
                            if newValue {
                                try SMAppService.mainApp.register()
                            } else {
                                try SMAppService.mainApp.unregister()
                            }
                        } catch {
                            // Revert the toggle if the operation failed
                            launchAtLogin = SMAppService.mainApp.status == .enabled
                        }
                    }

                Text("When enabled, syncFolder will start automatically when you log in to your Mac.")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }

            Section {
                Toggle("Enable sync notifications", isOn: $appState.notificationsEnabled)
                    .onChange(of: appState.notificationsEnabled) { newValue in
                        if newValue {
                            NotificationManager.shared.requestPermission()
                        }
                    }

                Text("Show macOS notifications when syncs complete with changes or errors.")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .formStyle(.grouped)
        .onAppear {
            launchAtLogin = SMAppService.mainApp.status == .enabled
        }
    }
}

// MARK: - About Tab

struct AboutTab: View {
    @Environment(\.openURL) private var openURL

    var body: some View {
        VStack(spacing: 16) {
            Spacer()

            Image(systemName: "arrow.triangle.2.circlepath.circle.fill")
                .font(.system(size: 64))
                .foregroundStyle(.blue)

            Text("syncFolder")
                .font(.title.weight(.bold))

            Text("Version 1.1.0")
                .font(.subheadline)
                .foregroundStyle(.secondary)

            Text("Automatic folder synchronization for macOS.\nKeep your folders in sync with configurable polling intervals.")
                .font(.body)
                .foregroundStyle(.secondary)
                .multilineTextAlignment(.center)
                .frame(maxWidth: 400)

            Divider()
                .frame(maxWidth: 300)

            VStack(spacing: 8) {
                Text("Author: Daniel Vieira")
                    .font(.subheadline)
                    .foregroundStyle(.secondary)

                Button {
                    openURL(URL(string: "https://github.com/danvsantos/syncFolderApp")!)
                } label: {
                    HStack(spacing: 4) {
                        Image(systemName: "link")
                        Text("GitHub Repository")
                    }
                    .font(.subheadline)
                }
                .buttonStyle(.link)

                Text("Licensed under the MIT License")
                    .font(.caption)
                    .foregroundStyle(.tertiary)
            }

            Spacer()
        }
        .frame(maxWidth: .infinity)
    }
}
