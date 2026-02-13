import SwiftUI
import UserNotifications

@main
struct syncFolderApp: App {
    @StateObject private var appState = AppState()
    @NSApplicationDelegateAdaptor(AppDelegate.self) var appDelegate

    var body: some Scene {
        MenuBarExtra {
            MenuBarView()
                .environmentObject(appState)
        } label: {
            Image(systemName: appState.isSyncing
                ? "arrow.triangle.2.circlepath.circle.fill"
                : "arrow.triangle.2.circlepath.circle")
        }
        .menuBarExtraStyle(.window)

        Window("syncFolder Preferences", id: "preferences") {
            PreferencesView()
                .environmentObject(appState)
                .frame(minWidth: 680, minHeight: 480)
                .onAppear {
                    NSApp.activate(ignoringOtherApps: true)
                }
        }
        .defaultSize(width: 700, height: 500)
        .windowResizability(.contentSize)
    }
}

// MARK: - AppDelegate to close stale windows on launch

class AppDelegate: NSObject, NSApplicationDelegate {
    func applicationDidFinishLaunching(_ notification: Notification) {
        // Set notification delegate
        UNUserNotificationCenter.current().delegate = NotificationManager.shared

        // Close any auto-opened windows on launch — we only want the menu bar icon
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.3) {
            for window in NSApp.windows {
                if window.title.contains("Preferences") {
                    window.close()
                }
            }
        }
    }
}
