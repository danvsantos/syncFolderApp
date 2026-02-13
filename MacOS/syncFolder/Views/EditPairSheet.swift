import SwiftUI

struct EditPairSheet: View {
    enum Mode {
        case add
        case edit(SyncPair)
    }

    let mode: Mode
    let onSave: (SyncPair) -> Void

    @Environment(\.dismiss) private var dismiss

    @State private var name: String = ""
    @State private var sourcePath: String = ""
    @State private var destinationPath: String = ""
    @State private var intervalMinutes: Int = 5
    @State private var deleteOrphans: Bool = false
    @State private var isEnabled: Bool = true
    @State private var filterPatterns: [String] = []
    @State private var newFilterPattern: String = ""
    @State private var syncMode: SyncMode = .oneWay

    private var pairId: UUID

    init(mode: Mode, onSave: @escaping (SyncPair) -> Void) {
        self.mode = mode
        self.onSave = onSave

        switch mode {
        case .add:
            self.pairId = UUID()
        case .edit(let pair):
            self.pairId = pair.id
            self._name = State(initialValue: pair.name)
            self._sourcePath = State(initialValue: pair.sourcePath)
            self._destinationPath = State(initialValue: pair.destinationPath)
            self._intervalMinutes = State(initialValue: pair.intervalMinutes)
            self._deleteOrphans = State(initialValue: pair.deleteOrphans)
            self._isEnabled = State(initialValue: pair.isEnabled)
            self._filterPatterns = State(initialValue: pair.filterPatterns)
            self._syncMode = State(initialValue: pair.syncMode)
        }
    }

    private var isValid: Bool {
        !name.trimmingCharacters(in: .whitespaces).isEmpty
            && !sourcePath.isEmpty
            && !destinationPath.isEmpty
            && sourcePath != destinationPath
            && intervalMinutes >= 1
            && intervalMinutes <= 120
    }

    private var title: String {
        switch mode {
        case .add: return "Add Sync Pair"
        case .edit: return "Edit Sync Pair"
        }
    }

    private let presetFilters = [".DS_Store", "*.tmp", "*.log", "node_modules", ".git"]

    var body: some View {
        VStack(spacing: 0) {
            // Header
            Text(title)
                .font(.headline)
                .padding(.top, 20)
                .padding(.bottom, 16)

            Form {
                // Name
                TextField("Name:", text: $name, prompt: Text("My Documents Backup"))

                // Source
                HStack {
                    TextField("Source:", text: $sourcePath, prompt: Text("/path/to/source"))
                        .truncationMode(.middle)
                    Button("Browse...") {
                        browseFolder { sourcePath = $0 }
                    }
                }

                // Destination
                HStack {
                    TextField("Destination:", text: $destinationPath, prompt: Text("/path/to/destination"))
                        .truncationMode(.middle)
                    Button("Browse...") {
                        browseFolder { destinationPath = $0 }
                    }
                }

                // Interval
                HStack {
                    Picker("Interval:", selection: $intervalMinutes) {
                        Text("1 minute").tag(1)
                        Text("2 minutes").tag(2)
                        Text("5 minutes").tag(5)
                        Text("10 minutes").tag(10)
                        Text("15 minutes").tag(15)
                        Text("30 minutes").tag(30)
                        Text("60 minutes").tag(60)
                        Text("120 minutes").tag(120)
                    }
                }

                // Sync Mode
                Picker("Sync Mode:", selection: $syncMode) {
                    Text("One-way (Source → Destination)").tag(SyncMode.oneWay)
                    Text("Bidirectional").tag(SyncMode.bidirectional)
                }
                if syncMode == .bidirectional {
                    Text("Conflicts resolved by keeping the most recently modified file.")
                        .font(.caption)
                        .foregroundStyle(.secondary)
                }

                // Options
                Toggle("Delete orphans in destination", isOn: $deleteOrphans)
                    .help("When enabled, files deleted from the source will also be deleted from the destination.")

                Toggle("Enabled", isOn: $isEnabled)

                // Exclude Filters
                Section("Exclude Filters") {
                    if !filterPatterns.isEmpty {
                        ForEach(filterPatterns, id: \.self) { pattern in
                            HStack {
                                Image(systemName: "line.3.horizontal.decrease")
                                    .foregroundStyle(.secondary)
                                Text(pattern)
                                    .font(.body.monospaced())
                                Spacer()
                                Button {
                                    filterPatterns.removeAll { $0 == pattern }
                                } label: {
                                    Image(systemName: "minus.circle.fill")
                                        .foregroundStyle(.red)
                                }
                                .buttonStyle(.plain)
                            }
                        }
                    }

                    HStack {
                        TextField("Pattern", text: $newFilterPattern, prompt: Text("*.log"))
                            .font(.body.monospaced())
                            .onSubmit { addFilter() }
                        Button("Add") { addFilter() }
                            .disabled(newFilterPattern.trimmingCharacters(in: .whitespaces).isEmpty)
                    }

                    HStack(spacing: 6) {
                        Text("Presets:")
                            .font(.caption)
                            .foregroundStyle(.secondary)
                        ForEach(presetFilters, id: \.self) { preset in
                            Button(preset) {
                                if !filterPatterns.contains(preset) {
                                    filterPatterns.append(preset)
                                }
                            }
                            .font(.caption)
                            .buttonStyle(.bordered)
                            .controlSize(.mini)
                            .disabled(filterPatterns.contains(preset))
                        }
                    }
                }
            }
            .formStyle(.grouped)

            // Validation warnings
            if sourcePath == destinationPath && !sourcePath.isEmpty {
                Label("Source and destination must be different.", systemImage: "exclamationmark.triangle.fill")
                    .font(.caption)
                    .foregroundStyle(.red)
                    .padding(.horizontal, 20)
            }

            // Buttons
            HStack {
                Button("Cancel") {
                    dismiss()
                }
                .keyboardShortcut(.cancelAction)

                Spacer()

                Button(mode.isAdd ? "Add" : "Save") {
                    savePair()
                }
                .keyboardShortcut(.defaultAction)
                .disabled(!isValid)
            }
            .padding(20)
        }
        .frame(width: 520, height: 580)
    }

    private func addFilter() {
        let pattern = newFilterPattern.trimmingCharacters(in: .whitespaces)
        guard !pattern.isEmpty, !filterPatterns.contains(pattern) else { return }
        filterPatterns.append(pattern)
        newFilterPattern = ""
    }

    private func savePair() {
        var pair = SyncPair(
            name: name.trimmingCharacters(in: .whitespaces),
            sourcePath: sourcePath,
            destinationPath: destinationPath,
            intervalMinutes: intervalMinutes,
            deleteOrphans: deleteOrphans,
            isEnabled: isEnabled,
            filterPatterns: filterPatterns,
            syncMode: syncMode
        )
        pair.id = pairId

        if case .edit(let original) = mode {
            pair.lastSyncDate = original.lastSyncDate
        }

        onSave(pair)
        dismiss()
    }

    private func browseFolder(completion: @escaping (String) -> Void) {
        let panel = NSOpenPanel()
        panel.canChooseFiles = false
        panel.canChooseDirectories = true
        panel.allowsMultipleSelection = false
        panel.canCreateDirectories = true

        if panel.runModal() == .OK, let url = panel.url {
            completion(url.path)
        }
    }
}

extension EditPairSheet.Mode {
    var isAdd: Bool {
        if case .add = self { return true }
        return false
    }
}
