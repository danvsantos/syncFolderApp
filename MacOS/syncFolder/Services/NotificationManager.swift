import Foundation
import UserNotifications

final class NotificationManager: NSObject, UNUserNotificationCenterDelegate, @unchecked Sendable {
    static let shared = NotificationManager()

    private override init() {
        super.init()
    }

    func requestPermission() {
        UNUserNotificationCenter.current().requestAuthorization(options: [.alert, .sound]) { _, _ in }
    }

    func sendSyncCompleted(pairName: String, result: SyncResult) {
        let content = UNMutableNotificationContent()
        content.title = pairName
        content.body = result.summary
        content.categoryIdentifier = "syncEvent"
        if result.hasErrors {
            content.sound = .default
        }

        let request = UNNotificationRequest(
            identifier: UUID().uuidString,
            content: content,
            trigger: nil
        )
        UNUserNotificationCenter.current().add(request)
    }

    func sendSyncFailed(pairName: String, error: String) {
        let content = UNMutableNotificationContent()
        content.title = pairName
        content.body = error
        content.sound = .default
        content.categoryIdentifier = "syncEvent"

        let request = UNNotificationRequest(
            identifier: UUID().uuidString,
            content: content,
            trigger: nil
        )
        UNUserNotificationCenter.current().add(request)
    }

    // MARK: - UNUserNotificationCenterDelegate

    func userNotificationCenter(
        _ center: UNUserNotificationCenter,
        willPresent notification: UNNotification,
        withCompletionHandler completionHandler: @escaping (UNNotificationPresentationOptions) -> Void
    ) {
        completionHandler([.banner, .sound])
    }
}
