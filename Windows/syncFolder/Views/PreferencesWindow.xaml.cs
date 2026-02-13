using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using syncFolder.Models;
using syncFolder.ViewModels;

namespace syncFolder.Views;

public partial class PreferencesWindow : Window
{
    private readonly AppState _appState;

    public PreferencesWindow(AppState appState)
    {
        InitializeComponent();
        _appState = appState;

        LocalizeUI();
        LoadGeneralSettings();
        RefreshPairsList();
        RefreshActivityLog();

        _appState.Pairs.CollectionChanged += (_, _) => Dispatcher.Invoke(RefreshPairsList);
        _appState.Logs.CollectionChanged += (_, _) => Dispatcher.Invoke(RefreshActivityLog);
        _appState.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppState.Statuses))
                Dispatcher.Invoke(RefreshPairsList);
        };
    }

    private void LocalizeUI()
    {
        Title = Strings.Resources.SyncFolderPreferences;
        SyncPairsTabHeader.Text = Strings.Resources.SyncPairs;
        ActivityTabHeader.Text = Strings.Resources.Activity;
        GeneralTabHeader.Text = Strings.Resources.General;
        AboutTabHeader.Text = Strings.Resources.About;

        // Sync Pairs tab
        NoPairsTitle.Text = Strings.Resources.NoSyncPairs;
        NoPairsDescription.Text = Strings.Resources.AddSyncPairDescription;
        AddFirstPairButton.Content = Strings.Resources.AddSyncPair;
        SyncAllPairsText.Text = Strings.Resources.SyncAll;
        AddPairButton.ToolTip = Strings.Resources.Get("Add sync pair");

        // Context menu
        CtxSyncNow.Header = Strings.Resources.SyncNow;
        CtxEdit.Header = Strings.Resources.EditDots;
        CtxDelete.Header = Strings.Resources.DeleteLabel;

        // Activity tab
        NoActivityTitle.Text = Strings.Resources.NoActivityYet;
        NoActivityDescription.Text = Strings.Resources.SyncActivityWillAppear;
        ClearLogButton.Content = Strings.Resources.ClearLog;

        // General tab
        LaunchAtLoginCheckbox.Content = Strings.Resources.LaunchAtLogin;
        LaunchAtLoginDescription.Text = Strings.Resources.LaunchAtLoginDescription;
        NotificationsCheckbox.Content = Strings.Resources.EnableNotifications;
        NotificationsDescription.Text = Strings.Resources.NotificationsDescription;

        // About tab
        VersionText.Text = Strings.Resources.Version;
        AboutDescription.Text = Strings.Resources.AppDescription;
        AuthorText.Text = Strings.Resources.AuthorDaniel;
        GitHubButton.Content = Strings.Resources.GitHubRepository;
        LicenseText.Text = Strings.Resources.MITLicense;
    }

    // ==========================================
    // Sync Pairs Tab
    // ==========================================

    private void RefreshPairsList()
    {
        PairsListView.Items.Clear();

        if (_appState.Pairs.Count == 0)
        {
            PairsEmptyState.Visibility = Visibility.Visible;
            PairsListView.Visibility = Visibility.Collapsed;
            SyncAllPairsButton.IsEnabled = false;
        }
        else
        {
            PairsEmptyState.Visibility = Visibility.Collapsed;
            PairsListView.Visibility = Visibility.Visible;
            SyncAllPairsButton.IsEnabled = true;

            foreach (var pair in _appState.Pairs)
            {
                PairsListView.Items.Add(pair);
            }
        }

        // Force visual update for custom rendering
        PairsListView.UpdateLayout();
        UpdatePairVisuals();
    }

    private void UpdatePairVisuals()
    {
        for (int i = 0; i < PairsListView.Items.Count; i++)
        {
            var container = PairsListView.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;
            if (container == null) continue;

            var pair = (SyncPair)PairsListView.Items[i];
            _appState.Statuses.TryGetValue(pair.Id, out var status);

            // Find elements in the template
            var statusCircle = FindChild<Border>(container, "StatusCircle");
            var statusIcon = FindChild<TextBlock>(container, "StatusIcon");
            var disabledBadge = FindChild<Border>(container, "DisabledBadge");
            var lastSyncText = FindChild<TextBlock>(container, "LastSyncText");
            var sourceText = FindChild<TextBlock>(container, "SourceText");
            var destText = FindChild<TextBlock>(container, "DestText");
            var arrowText = FindChild<TextBlock>(container, "ArrowText");
            var intervalText = FindChild<TextBlock>(container, "IntervalText");
            var orphansText = FindChild<TextBlock>(container, "OrphansText");
            var syncStatusText = FindChild<TextBlock>(container, "SyncStatusText");

            if (statusCircle != null)
            {
                var (color, icon) = GetStatusVisual(pair, status);
                statusCircle.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, color.R, color.G, color.B));
                if (statusIcon != null)
                {
                    statusIcon.Text = icon;
                    statusIcon.Foreground = new SolidColorBrush(color);
                }
            }

            if (disabledBadge != null)
                disabledBadge.Visibility = pair.IsEnabled ? Visibility.Collapsed : Visibility.Visible;

            if (lastSyncText != null && status?.LastSyncDate != null)
                lastSyncText.Text = FormatRelativeDate(status.LastSyncDate.Value);

            if (sourceText != null)
                sourceText.Text = AbbreviatePath(pair.SourcePath);

            if (destText != null)
                destText.Text = AbbreviatePath(pair.DestinationPath);

            if (arrowText != null)
                arrowText.Text = pair.SyncMode == SyncMode.Bidirectional ? "\u2194" : "\u2192";

            if (intervalText != null)
                intervalText.Text = Strings.Resources.Get("Every {0} min", pair.IntervalMinutes);

            if (orphansText != null)
            {
                orphansText.Visibility = pair.DeleteOrphans ? Visibility.Visible : Visibility.Collapsed;
                orphansText.Text = Strings.Resources.Get("Delete orphans");
            }

            if (syncStatusText != null && status != null)
            {
                switch (status.State)
                {
                    case PairStatus.StateType.Syncing:
                        syncStatusText.Text = Strings.Resources.SyncingDots;
                        syncStatusText.Foreground = (SolidColorBrush)FindResource("BlueBrush");
                        break;
                    case PairStatus.StateType.Error:
                        syncStatusText.Text = status.ErrorMessage ?? Strings.Resources.Error;
                        syncStatusText.Foreground = (SolidColorBrush)FindResource("RedBrush");
                        break;
                    case PairStatus.StateType.Idle when status.LastResult != null:
                        syncStatusText.Text = status.LastResult.Summary;
                        syncStatusText.Foreground = status.LastResult.HasErrors
                            ? (SolidColorBrush)FindResource("OrangeBrush")
                            : (SolidColorBrush)FindResource("GreenBrush");
                        break;
                }
            }
        }
    }

    private static (System.Windows.Media.Color color, string icon) GetStatusVisual(SyncPair pair, PairStatus? status)
    {
        if (!pair.IsEnabled)
            return (System.Windows.Media.Color.FromRgb(156, 163, 175), "\u23F8"); // Gray, Pause

        return status?.State switch
        {
            PairStatus.StateType.Syncing => (System.Windows.Media.Color.FromRgb(59, 130, 246), "\u1F504"),   // Blue, Sync
            PairStatus.StateType.Error => (System.Windows.Media.Color.FromRgb(239, 68, 68), "!"),              // Red, !
            _ => status?.LastResult?.HasErrors == true
                ? (System.Windows.Media.Color.FromRgb(245, 158, 11), "!")                                       // Orange, !
                : (System.Windows.Media.Color.FromRgb(34, 197, 94), "\u2713")                                  // Green, Check
        };
    }

    private void AddPair_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new EditPairDialog(null);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true && dialog.ResultPair != null)
        {
            _appState.AddPair(dialog.ResultPair);
        }
    }

    private void SyncAllPairs_Click(object sender, RoutedEventArgs e)
    {
        _appState.SyncNow();
    }

    private void SyncPairButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Guid pairId)
        {
            _appState.SyncNow(pairId);
        }
    }

    private void PairsList_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        EditSelectedPair();
    }

    private void CtxSyncNow_Click(object sender, RoutedEventArgs e)
    {
        if (PairsListView.SelectedItem is SyncPair pair)
            _appState.SyncNow(pair.Id);
    }

    private void CtxEdit_Click(object sender, RoutedEventArgs e)
    {
        EditSelectedPair();
    }

    private void CtxDelete_Click(object sender, RoutedEventArgs e)
    {
        if (PairsListView.SelectedItem is SyncPair pair)
        {
            var message = Strings.Resources.Get(
                "Are you sure you want to remove \"{0}\"? Files in the destination will not be deleted.",
                pair.Name);

            var result = MessageBox.Show(
                message,
                Strings.Resources.DeleteSyncPairQuestion,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _appState.DeletePair(pair);
            }
        }
    }

    private void EditSelectedPair()
    {
        if (PairsListView.SelectedItem is SyncPair pair)
        {
            var dialog = new EditPairDialog(pair);
            dialog.Owner = this;
            if (dialog.ShowDialog() == true && dialog.ResultPair != null)
            {
                _appState.UpdatePair(dialog.ResultPair);
            }
        }
    }

    // ==========================================
    // Activity Tab
    // ==========================================

    private void RefreshActivityLog()
    {
        LogListView.Items.Clear();

        if (_appState.Logs.Count == 0)
        {
            ActivityEmptyState.Visibility = Visibility.Visible;
            LogListView.Visibility = Visibility.Collapsed;
            ClearLogButton.IsEnabled = false;
        }
        else
        {
            ActivityEmptyState.Visibility = Visibility.Collapsed;
            LogListView.Visibility = Visibility.Visible;
            ClearLogButton.IsEnabled = true;

            foreach (var entry in _appState.Logs)
            {
                LogListView.Items.Add(entry);
            }
        }

        LogCountText.Text = Strings.Resources.Get("{0} entries", _appState.Logs.Count);

        LogListView.UpdateLayout();
        UpdateLogVisuals();
    }

    private void UpdateLogVisuals()
    {
        for (int i = 0; i < LogListView.Items.Count; i++)
        {
            var container = LogListView.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;
            if (container == null) continue;

            var entry = (LogEntry)LogListView.Items[i];

            var icon = FindChild<TextBlock>(container, "LogIcon");
            var pairName = FindChild<TextBlock>(container, "LogPairName");
            var date = FindChild<TextBlock>(container, "LogDate");
            var detail = FindChild<TextBlock>(container, "LogDetail");

            if (icon != null)
            {
                if (entry.IsError)
                {
                    icon.Text = "\u26A0";
                    icon.Foreground = (SolidColorBrush)FindResource("RedBrush");
                }
                else if (entry.Result?.HasErrors == true)
                {
                    icon.Text = "\u26A0";
                    icon.Foreground = (SolidColorBrush)FindResource("OrangeBrush");
                }
                else
                {
                    icon.Text = "\u2705";
                    icon.Foreground = (SolidColorBrush)FindResource("GreenBrush");
                }
            }

            if (pairName != null) pairName.Text = entry.PairName;
            if (date != null) date.Text = FormatRelativeDate(entry.Date);

            if (detail != null)
            {
                if (entry.Error != null && entry.Result == null)
                {
                    detail.Text = entry.Error;
                    detail.Foreground = (SolidColorBrush)FindResource("RedBrush");
                }
                else if (entry.Result != null)
                {
                    detail.Text = entry.Result.Summary;
                    detail.Foreground = new SolidColorBrush(System.Windows.Media.Colors.Gray);
                }
            }
        }
    }

    private void ClearLog_Click(object sender, RoutedEventArgs e)
    {
        _appState.ClearLogs();
        RefreshActivityLog();
    }

    // ==========================================
    // General Tab
    // ==========================================

    private void LoadGeneralSettings()
    {
        LaunchAtLoginCheckbox.IsChecked = IsLaunchAtLoginEnabled();
        NotificationsCheckbox.IsChecked = _appState.NotificationsEnabled;
    }

    private void LaunchAtLogin_Changed(object sender, RoutedEventArgs e)
    {
        var enabled = LaunchAtLoginCheckbox.IsChecked == true;
        SetLaunchAtLogin(enabled);
    }

    private void Notifications_Changed(object sender, RoutedEventArgs e)
    {
        _appState.NotificationsEnabled = NotificationsCheckbox.IsChecked == true;
    }

    private static bool IsLaunchAtLoginEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return key?.GetValue("syncFolder") != null;
        }
        catch
        {
            return false;
        }
    }

    private static void SetLaunchAtLogin(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key == null) return;

            if (enabled)
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (exePath != null)
                    key.SetValue("syncFolder", $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue("syncFolder", false);
            }
        }
        catch
        {
            // Registry access may fail in restricted environments
        }
    }

    // ==========================================
    // About Tab
    // ==========================================

    private void GitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/danvsantos/syncFolderApp",
                UseShellExecute = true
            });
        }
        catch { }
    }

    // ==========================================
    // Helpers
    // ==========================================

    private static string AbbreviatePath(string path)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (path.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
            return "~" + path.Substring(userProfile.Length);
        return path;
    }

    private static string FormatRelativeDate(DateTime date)
    {
        var diff = DateTime.Now - date;
        if (diff.TotalSeconds < 60) return "just now";
        if (diff.TotalMinutes < 2) return "1 min ago";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} min ago";
        if (diff.TotalHours < 2) return "1 hr ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hr ago";
        if (diff.TotalDays < 2) return "yesterday";
        return $"{(int)diff.TotalDays} days ago";
    }

    /// <summary>
    /// Recursively finds a child element by name in a visual tree.
    /// </summary>
    private static T? FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
    {
        int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T typedChild)
            {
                if (child is FrameworkElement fe && fe.Name == childName)
                    return typedChild;
            }

            var found = FindChild<T>(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}
