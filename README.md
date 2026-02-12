# syncFolder

Automatic folder synchronization utility for macOS. Keep your folders in sync with configurable polling intervals, all from a lightweight menu bar app.

## Features

- **Multiple Sync Pairs** - Configure multiple source/destination folder pairs
- **Configurable Intervals** - Set sync frequency from 1 to 120 minutes per pair
- **Selective Deletion** - Optionally remove files from destination when deleted from source
- **Menu Bar Integration** - Quick access to sync status and manual triggers
- **Activity Log** - Track sync history with detailed results
- **Lightweight** - Runs quietly in the background with minimal resource usage

## Requirements

- macOS 13.0 (Ventura) or later
- Xcode 16.0+ (for building from source)

## Installation

### From DMG

1. Download the latest `.dmg` from the `dist/` folder or from [Releases](https://github.com/danvsantos/syncFolderApp/releases)
2. Open the DMG and drag **syncFolder** to your Applications folder
3. Launch syncFolder from Applications

### Building from Source

```bash
# Clone the repository
git clone https://github.com/danvsantos/syncFolderApp.git
cd syncFolderApp

# Generate the Xcode project (requires XcodeGen)
xcodegen generate

# Build with Xcode
xcodebuild -project syncFolder.xcodeproj -scheme syncFolder -configuration Release build

# Or open in Xcode
open syncFolder.xcodeproj
```

## Usage

1. Launch the app - it will appear as an icon in your menu bar
2. Click the menu bar icon to see sync status and quick actions
3. Open **Preferences** to manage sync pairs
4. Click **+** to add a new sync pair, selecting source and destination folders
5. Configure the sync interval and options for each pair
6. The app will automatically sync your folders at the configured intervals

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

- Localization / internationalization support (ES e CH)
- Notifications for sync events
- iCloud / network folder support improvements

### Reporting Issues

Found a bug or have a suggestion? [Open an issue](https://github.com/danvsantos/syncFolderApp/issues) on GitHub.

## Project Structure

```
syncFolder/
├── App/            # App entry point and state management
├── Models/         # Data models (SyncPair, FileManifest)
├── Services/       # Sync engine and configuration persistence
├── Views/          # SwiftUI views (MenuBar, Preferences, etc.)
└── Resources/      # Assets and entitlements
```

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

## Author

**Daniel Vieira** - [GitHub](https://github.com/danvsantos)
