# syncFolder

Automatic folder synchronization utility for **macOS** and **Windows**. Keep your folders in sync with configurable polling intervals, all from a lightweight system tray / menu bar app.

## Features

- **Multiple Sync Pairs** - Configure multiple source/destination folder pairs
- **One-way & Bidirectional Sync** - Choose between source-to-destination or two-way sync with last-write-wins conflict resolution
- **Configurable Intervals** - Set sync frequency from 1 to 120 minutes per pair
- **Selective Deletion** - Optionally remove orphan files when deleted from source
- **Exclude Filters** - Glob-based patterns to skip files (e.g. `*.tmp`, `node_modules`, `.git`)
- **System Tray / Menu Bar Integration** - Quick access to sync status and manual triggers
- **Activity Log** - Track sync history with detailed results (last 200 entries)
- **Notifications** - Native OS notifications on sync completion or failure
- **Launch at Login** - Start automatically when you log in
- **Multi-language** - English and Portuguese (Brazil)
- **Lightweight** - Runs quietly in the background with minimal resource usage

## Platforms

| | macOS | Windows |
|---|---|---|
| **Language** | Swift / SwiftUI | C# / WPF |
| **Minimum OS** | macOS 13.0 (Ventura) | Windows 10 (x64) |
| **Tray Integration** | Menu Bar (MenuBarExtra) | System Tray (NotifyIcon) |
| **Notifications** | UNUserNotificationCenter | Windows Toast Notifications |
| **Auto-start** | SMAppService | Windows Registry |
| **Config Location** | `~/Library/Application Support/com.syncfolder.syncFolder/` | `%LOCALAPPDATA%\syncFolder\` |
| **Build Tool** | Xcode 16+ / XcodeGen | .NET 8 SDK |

---

## macOS

### Requirements

- macOS 13.0 (Ventura) or later
- Xcode 16.0+ (for building from source)

### Installation

#### From DMG

1. Download the latest `syncFolder.dmg` from [`dist/last_version/`](dist/last_version/) or from [Releases](https://github.com/danvsantos/syncFolderApp/releases)
2. Open the DMG and drag **syncFolder** to your **Applications** folder
3. Launch syncFolder from Applications

> The `dist/last_version/` folder always contains the most recent build of the app (`.app` and `.dmg`).

#### Building from Source

```bash
cd MacOS

# Generate the Xcode project (requires XcodeGen)
xcodegen generate

# Build with Xcode
xcodebuild -project syncFolder.xcodeproj -scheme syncFolder -configuration Release -derivedDataPath build

# Or open in Xcode
open syncFolder.xcodeproj
```

#### Creating the DMG

```bash
mkdir -p dist/last_version
cp -R build/Build/Products/Release/syncFolder.app dist/last_version/

STAGING_DIR=$(mktemp -d)
cp -R dist/last_version/syncFolder.app "$STAGING_DIR/"
ln -s /Applications "$STAGING_DIR/Applications"
hdiutil create -volname "syncFolder" -srcfolder "$STAGING_DIR" -ov -format UDZO dist/last_version/syncFolder.dmg
rm -rf "$STAGING_DIR"
```

---

## Windows

### Requirements

- Windows 10 or later (x64)
- .NET 8 SDK (for building from source)

### Installation

#### From Executable

1. Download `syncFolder.exe` from [`Windows/dist/`](Windows/dist/) or from [Releases](https://github.com/danvsantos/syncFolderApp/releases)
2. Run `syncFolder.exe` - no installation required (self-contained, includes .NET runtime)

#### Building from Source

```bash
cd Windows

# Restore dependencies
dotnet restore syncFolder.sln

# Build
dotnet build syncFolder.sln -c Release

# Run
dotnet run --project syncFolder
```

#### Publishing a Self-Contained Executable

```bash
cd Windows

# Single-file self-contained exe (no .NET install needed on target machine)
dotnet publish syncFolder/syncFolder.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

The resulting `dist/syncFolder.exe` (~179 MB) can be distributed as a standalone file.

#### Generating the App Icon (Optional)

```powershell
# Run on Windows with PowerShell
powershell -ExecutionPolicy Bypass -File GenerateIcon.ps1
```

---

## Usage

1. Launch the app - it will appear as an icon in your **menu bar** (macOS) or **system tray** (Windows)
2. **Left-click** the icon to see sync status, pair list, and quick actions
3. **Right-click** (Windows) for context menu: Sync All, Preferences, Quit
4. Open **Preferences** to manage sync pairs
5. Click **+** to add a new sync pair, selecting source and destination folders
6. Configure the sync interval, sync mode, filters, and options for each pair
7. The app will automatically sync your folders at the configured intervals

### Sync Modes

- **One-way (Source -> Destination):** Copies new/modified files from source to destination. Optionally deletes orphans in destination.
- **Bidirectional:** Syncs changes in both directions. Conflicts are resolved by keeping the most recently modified file (last-write-wins).

### Exclude Filters

Use glob patterns to exclude files or directories from sync:

| Pattern | Effect |
|---|---|
| `*.tmp` | Skip all `.tmp` files |
| `*.log` | Skip all `.log` files |
| `.git` | Skip `.git` directories |
| `node_modules` | Skip `node_modules` directories |
| `.DS_Store` | Skip macOS metadata files |
| `Thumbs.db` | Skip Windows thumbnail cache |
| `desktop.ini` | Skip Windows folder settings |

---

## How to Contribute

Contributions are welcome! Here's how you can help:

1. **Fork** the repository on [GitHub](https://github.com/danvsantos/syncFolderApp)
2. **Create a branch** for your feature or bugfix:
   ```bash
   git checkout -b feature/my-new-feature
   ```
3. **Make your changes** and test them
4. **Commit** with a clear message describing what you changed
5. **Push** to your fork and open a **Pull Request**

### Ideas for Contribution

- Localization / internationalization support (ES, CH, etc.)
- Linux version
- iCloud / network folder support improvements
- Real-time file watching (FSEvents / FileSystemWatcher) instead of polling

### Reporting Issues

Found a bug or have a suggestion? [Open an issue](https://github.com/danvsantos/syncFolderApp/issues) on GitHub.

---

## Project Structure

```
syncFolderApp/
├── MacOS/                          # macOS version (Swift / SwiftUI)
│   ├── syncFolder/
│   │   ├── App/                    # App entry point and state management
│   │   ├── Models/                 # Data models (SyncPair, FileManifest)
│   │   ├── Services/               # Sync engine, config, notifications
│   │   ├── Views/                  # SwiftUI views (MenuBar, Preferences, etc.)
│   │   └── Resources/              # Assets, entitlements, localization
│   ├── project.yml                 # XcodeGen configuration
│   └── LICENSE
│
├── Windows/                        # Windows version (C# / WPF)
│   ├── syncFolder/
│   │   ├── Models/                 # Data models (SyncPair, FileManifest, LogEntry)
│   │   ├── Services/               # Sync engine, config, manifest, notifications
│   │   ├── ViewModels/             # AppState (MVVM)
│   │   ├── Views/                  # WPF views (TrayPopup, Preferences, EditPairDialog)
│   │   ├── Strings/                # Localization (EN, PT-BR)
│   │   └── Properties/             # App settings
│   ├── syncFolder.sln              # Visual Studio solution
│   ├── dist/                       # Published executable
│   ├── GenerateIcon.ps1            # Icon generator script
│   └── LICENSE
│
├── dist/                           # macOS builds
│   └── last_version/               # Latest .app and .dmg
│
└── README.md
```

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

## Author

**Daniel Vieira** - [GitHub](https://github.com/danvsantos)
