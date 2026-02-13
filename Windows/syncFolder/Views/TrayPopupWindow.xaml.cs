using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using syncFolder.Models;
using syncFolder.ViewModels;

namespace syncFolder.Views;

public partial class TrayPopupWindow : Window
{
    private readonly AppState _appState;

    public event EventHandler? OpenPreferencesRequested;
    public event EventHandler? QuitRequested;

    public TrayPopupWindow(AppState appState)
    {
        InitializeComponent();
        _appState = appState;

        // Set localized texts
        SyncAllText.Text = Strings.Resources.SyncAllNow;
        PrefsText.Text = Strings.Resources.Preferences;
        QuitText.Text = Strings.Resources.QuitSyncFolder;
        EmptyText.Text = Strings.Resources.NoSyncPairsConfigured;
        EmptyPrefsButton.Content = Strings.Resources.OpenPreferences;

        RefreshUI();
    }

    private void RefreshUI()
    {
        // Status badge
        StatusBadge.Children.Clear();
        if (_appState.IsSyncing)
        {
            var text = new TextBlock
            {
                Text = Strings.Resources.Syncing,
                FontSize = 11,
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center
            };
            StatusBadge.Children.Add(text);
        }
        else if (_appState.ErrorCount > 0)
        {
            var text = new TextBlock
            {
                Text = Strings.Resources.Get("{0} error(s)", _appState.ErrorCount),
                FontSize = 11,
                Foreground = (SolidColorBrush)FindResource("OrangeBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            StatusBadge.Children.Add(text);
        }
        else if (_appState.ActivePairCount > 0)
        {
            var text = new TextBlock
            {
                Text = Strings.Resources.Get("{0} active", _appState.ActivePairCount),
                FontSize = 11,
                Foreground = (SolidColorBrush)FindResource("GreenBrush"),
                VerticalAlignment = VerticalAlignment.Center
            };
            StatusBadge.Children.Add(text);
        }
        else
        {
            var text = new TextBlock
            {
                Text = Strings.Resources.NoPairs,
                FontSize = 11,
                Foreground = Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Center
            };
            StatusBadge.Children.Add(text);
        }

        // Pairs list
        PairsList.Children.Clear();
        if (_appState.Pairs.Count == 0)
        {
            EmptyState.Visibility = Visibility.Visible;
            PairsScroller.Visibility = Visibility.Collapsed;
            SyncAllButton.IsEnabled = false;
        }
        else
        {
            EmptyState.Visibility = Visibility.Collapsed;
            PairsScroller.Visibility = Visibility.Visible;
            SyncAllButton.IsEnabled = true;

            foreach (var pair in _appState.Pairs)
            {
                _appState.Statuses.TryGetValue(pair.Id, out var status);
                var row = CreatePairRow(pair, status);
                PairsList.Children.Add(row);
            }
        }
    }

    private Button CreatePairRow(SyncPair pair, PairStatus? status)
    {
        var button = new Button
        {
            Style = (Style)FindResource("HoverButtonStyle"),
            Padding = new Thickness(16, 8, 16, 8),
            Tag = pair.Id
        };

        var panel = new DockPanel();

        // Status icon
        var iconText = GetStatusIcon(pair, status);
        var icon = new TextBlock
        {
            Text = iconText,
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 24,
            Margin = new Thickness(0, 0, 10, 0)
        };
        DockPanel.SetDock(icon, Dock.Left);
        panel.Children.Add(icon);

        // OFF badge
        if (!pair.IsEnabled)
        {
            var badge = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, 128, 128, 128)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 2, 6, 2),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            badge.Child = new TextBlock
            {
                Text = Strings.Resources.Off,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = Brushes.Gray
            };
            DockPanel.SetDock(badge, Dock.Right);
            panel.Children.Add(badge);
        }

        // Name and status
        var infoPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

        var name = new TextBlock
        {
            Text = pair.Name,
            FontWeight = FontWeights.Medium,
            FontSize = 13,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        infoPanel.Children.Add(name);

        var statusText = GetStatusText(pair, status);
        var statusLabel = new TextBlock
        {
            Text = statusText,
            FontSize = 11,
            Foreground = GetStatusForeground(status),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        infoPanel.Children.Add(statusLabel);

        panel.Children.Add(infoPanel);
        button.Content = panel;

        button.Click += (_, _) =>
        {
            _appState.SyncNow(pair.Id);
            RefreshUI();
        };

        return button;
    }

    private static string GetStatusIcon(SyncPair pair, PairStatus? status)
    {
        return status?.State switch
        {
            PairStatus.StateType.Syncing => "\u23F3",    // Hourglass
            PairStatus.StateType.Error => "\u26A0",       // Warning
            _ => pair.IsEnabled ? "\u2705" : "\u23F8"     // Check or Pause
        };
    }

    private static string GetStatusText(SyncPair pair, PairStatus? status)
    {
        if (status != null)
        {
            return status.State switch
            {
                PairStatus.StateType.Syncing => Strings.Resources.SyncingDots,
                PairStatus.StateType.Error => status.ErrorMessage ?? Strings.Resources.Error,
                PairStatus.StateType.Idle when status.LastSyncDate.HasValue =>
                    FormatRelativeDate(status.LastSyncDate.Value),
                _ => Strings.Resources.WaitingForFirstSync
            };
        }

        return pair.IsEnabled ? Strings.Resources.WaitingForFirstSync : Strings.Resources.Disabled;
    }

    private static SolidColorBrush GetStatusForeground(PairStatus? status)
    {
        return status?.State switch
        {
            PairStatus.StateType.Error => new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)),
            _ => new SolidColorBrush(System.Windows.Media.Colors.Gray)
        };
    }

    private static string FormatRelativeDate(DateTime date)
    {
        var diff = DateTime.Now - date;
        if (diff.TotalSeconds < 60) return "Synced just now";
        if (diff.TotalMinutes < 2) return "Synced 1 min ago";
        if (diff.TotalMinutes < 60) return $"Synced {(int)diff.TotalMinutes} min ago";
        if (diff.TotalHours < 2) return "Synced 1 hr ago";
        if (diff.TotalHours < 24) return $"Synced {(int)diff.TotalHours} hr ago";
        return $"Synced {(int)diff.TotalDays} days ago";
    }

    private void SyncAll_Click(object sender, RoutedEventArgs e)
    {
        _appState.SyncNow();
        RefreshUI();
    }

    private void OpenPreferences_Click(object sender, RoutedEventArgs e)
    {
        Close();
        OpenPreferencesRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        Close();
        QuitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        Close();
    }
}
