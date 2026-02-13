using Microsoft.Toolkit.Uwp.Notifications;
using syncFolder.Models;

namespace syncFolder.Services;

public sealed class NotificationManager
{
    public static NotificationManager Shared { get; } = new();

    private NotificationManager() { }

    public void SendSyncCompleted(string pairName, SyncResult result)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(pairName)
                .AddText(result.Summary)
                .Show();
        }
        catch
        {
            // Notifications may not be available on all Windows versions
        }
    }

    public void SendSyncFailed(string pairName, string error)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(pairName)
                .AddText(error)
                .Show();
        }
        catch
        {
            // Notifications may not be available on all Windows versions
        }
    }
}
