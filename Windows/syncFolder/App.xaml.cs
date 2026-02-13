using System.Drawing;
using System.IO;
using System.Windows;
using syncFolder.ViewModels;
using syncFolder.Views;
using WinForms = System.Windows.Forms;

namespace syncFolder;

public partial class App : Application
{
    private WinForms.NotifyIcon? _trayIcon;
    private AppState? _appState;
    private TrayPopupWindow? _trayPopup;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _appState = new AppState();

        // Create system tray icon
        _trayIcon = new WinForms.NotifyIcon
        {
            Text = "syncFolder",
            Icon = LoadAppIcon(),
            Visible = true
        };

        _trayIcon.MouseClick += TrayIcon_MouseClick;

        // Right-click context menu
        var contextMenu = new WinForms.ContextMenuStrip();

        var syncAllItem = new WinForms.ToolStripMenuItem(Strings.Resources.SyncAllNow);
        syncAllItem.Click += (_, _) => Dispatcher.Invoke(() => _appState.SyncNow());

        var prefsItem = new WinForms.ToolStripMenuItem(Strings.Resources.Preferences);
        prefsItem.Click += (_, _) => Dispatcher.Invoke(OpenPreferences);

        var quitItem = new WinForms.ToolStripMenuItem(Strings.Resources.QuitSyncFolder);
        quitItem.Click += (_, _) => Dispatcher.Invoke(QuitApplication);

        contextMenu.Items.Add(syncAllItem);
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add(prefsItem);
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add(quitItem);

        _trayIcon.ContextMenuStrip = contextMenu;
    }

    private static Icon LoadAppIcon()
    {
        // Try to load icon from file
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
            if (File.Exists(iconPath))
                return new Icon(iconPath);
        }
        catch { }

        // Generate a simple sync icon programmatically
        return GenerateDefaultIcon();
    }

    private static Icon GenerateDefaultIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);

        // Blue circle background
        using var brush = new SolidBrush(System.Drawing.Color.FromArgb(59, 130, 246));
        g.FillEllipse(brush, 2, 2, 28, 28);

        // White sync arrows
        using var pen = new Pen(System.Drawing.Color.White, 2.5f)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };
        g.DrawArc(pen, 8, 8, 16, 16, 0, 270);
        g.DrawLine(pen, 16, 8, 20, 8);
        g.DrawLine(pen, 16, 8, 16, 12);

        var handle = bmp.GetHicon();
        return Icon.FromHandle(handle);
    }

    private void TrayIcon_MouseClick(object? sender, WinForms.MouseEventArgs e)
    {
        if (e.Button != WinForms.MouseButtons.Left) return;

        Dispatcher.Invoke(() =>
        {
            if (_trayPopup != null && _trayPopup.IsVisible)
            {
                _trayPopup.Close();
                _trayPopup = null;
                return;
            }

            _trayPopup = new TrayPopupWindow(_appState!);
            _trayPopup.OpenPreferencesRequested += (_, _) => OpenPreferences();
            _trayPopup.QuitRequested += (_, _) => QuitApplication();

            // Position near system tray (bottom-right area)
            var cursorPos = WinForms.Cursor.Position;
            var screen = WinForms.Screen.FromPoint(cursorPos);
            var workArea = screen.WorkingArea;

            // Get DPI scaling factor
            double scaleFactor;
            using (var gfx = Graphics.FromHwnd(IntPtr.Zero))
            {
                scaleFactor = gfx.DpiX / 96.0;
            }

            var popupWidth = 320;
            var popupHeight = 400;

            _trayPopup.Left = Math.Min(
                (cursorPos.X / scaleFactor) - (popupWidth / 2),
                (workArea.Right / scaleFactor) - popupWidth - 10);
            _trayPopup.Top = (workArea.Bottom / scaleFactor) - popupHeight - 10;

            if (_trayPopup.Left < workArea.Left / scaleFactor)
                _trayPopup.Left = (workArea.Left / scaleFactor) + 10;

            _trayPopup.Show();
            _trayPopup.Activate();
        });
    }

    private void OpenPreferences()
    {
        foreach (Window window in Windows)
        {
            if (window is PreferencesWindow existing)
            {
                existing.Activate();
                return;
            }
        }

        var prefsWindow = new PreferencesWindow(_appState!);
        prefsWindow.Show();
        prefsWindow.Activate();
    }

    private void QuitApplication()
    {
        _appState?.Shutdown();

        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        _trayPopup?.Close();

        try
        {
            Microsoft.Toolkit.Uwp.Notifications.ToastNotificationManagerCompat.Uninstall();
        }
        catch { }

        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_trayIcon != null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        }
        base.OnExit(e);
    }
}
