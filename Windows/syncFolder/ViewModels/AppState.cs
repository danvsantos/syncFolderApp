using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using syncFolder.Models;
using syncFolder.Services;

namespace syncFolder.ViewModels;

public class AppState : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly ConfigManager _configManager = ConfigManager.Shared;
    private readonly SyncEngine _engine = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _scheduledTasks = new();

    // Observable collections for UI binding
    public ObservableCollection<SyncPair> Pairs { get; } = new();
    public ObservableCollection<LogEntry> Logs { get; } = new();
    public Dictionary<Guid, PairStatus> Statuses { get; } = new();

    private bool _notificationsEnabled;
    public bool NotificationsEnabled
    {
        get => _notificationsEnabled;
        set
        {
            if (_notificationsEnabled != value)
            {
                _notificationsEnabled = value;
                Properties.Settings.Default.NotificationsEnabled = value;
                Properties.Settings.Default.Save();
                OnPropertyChanged();
            }
        }
    }

    public bool IsSyncing => Statuses.Values.Any(s => s.State == PairStatus.StateType.Syncing);
    public int ActivePairCount => Pairs.Count(p => p.IsEnabled);
    public int ErrorCount => Statuses.Values.Count(s => s.State == PairStatus.StateType.Error);

    public AppState()
    {
        _notificationsEnabled = Properties.Settings.Default.NotificationsEnabled;
        var loaded = _configManager.LoadPairs();
        foreach (var pair in loaded)
            Pairs.Add(pair);

        ScheduleAll();
    }

    // MARK: - Pair Management

    public void AddPair(SyncPair pair)
    {
        Pairs.Add(pair);
        Save();
    }

    public void UpdatePair(SyncPair pair)
    {
        for (int i = 0; i < Pairs.Count; i++)
        {
            if (Pairs[i].Id == pair.Id)
            {
                Pairs[i] = pair;
                break;
            }
        }
        Save();
    }

    public void DeletePair(SyncPair pair)
    {
        if (_scheduledTasks.TryGetValue(pair.Id, out var cts))
        {
            cts.Cancel();
            _scheduledTasks.Remove(pair.Id);
        }

        Pairs.Remove(pair);
        Statuses.Remove(pair.Id);
        ManifestManager.Shared.DeleteManifest(pair.Id);
        _configManager.SavePairs(new List<SyncPair>(Pairs));
        NotifyStatusChanged();
    }

    public void Save()
    {
        _configManager.SavePairs(new List<SyncPair>(Pairs));
        ScheduleAll();
    }

    // MARK: - Sync

    public void SyncNow(Guid? pairId = null)
    {
        var pairsToSync = pairId.HasValue
            ? Pairs.Where(p => p.Id == pairId.Value).ToList()
            : Pairs.Where(p => p.IsEnabled).ToList();

        foreach (var pair in pairsToSync)
        {
            _ = PerformSyncAsync(pair);
        }
    }

    private async Task PerformSyncAsync(SyncPair pair)
    {
        // Skip if already syncing
        if (Statuses.TryGetValue(pair.Id, out var currentStatus) && currentStatus.State == PairStatus.StateType.Syncing)
            return;

        // Mark as syncing
        var prev = Statuses.GetValueOrDefault(pair.Id);
        Statuses[pair.Id] = new PairStatus
        {
            State = PairStatus.StateType.Syncing,
            LastResult = prev?.LastResult,
            LastSyncDate = prev?.LastSyncDate
        };
        NotifyStatusChanged();

        SyncResult result;
        try
        {
            result = await _engine.SyncAsync(pair);
            Statuses[pair.Id] = new PairStatus
            {
                State = PairStatus.StateType.Idle,
                LastResult = result,
                LastSyncDate = DateTime.Now
            };
        }
        catch (Exception ex)
        {
            result = new SyncResult
            {
                FilesCopied = 0,
                FilesDeleted = 0,
                BytesTransferred = 0,
                Errors = new List<string> { ex.Message }
            };
            Statuses[pair.Id] = new PairStatus
            {
                State = PairStatus.StateType.Error,
                ErrorMessage = ex.Message,
                LastResult = result,
                LastSyncDate = DateTime.Now
            };
        }

        AppendLog(new LogEntry(pair.Id, pair.Name, result, DateTime.Now));
        NotifyStatusChanged();

        // Send notifications
        if (NotificationsEnabled)
        {
            if (Statuses[pair.Id].State == PairStatus.StateType.Error)
            {
                NotificationManager.Shared.SendSyncFailed(pair.Name, result.Errors.FirstOrDefault() ?? "Unknown error");
            }
            else if (!result.IsClean)
            {
                NotificationManager.Shared.SendSyncCompleted(pair.Name, result);
            }
        }

        // Persist last sync date
        var found = Pairs.FirstOrDefault(p => p.Id == pair.Id);
        if (found != null)
        {
            found.LastSyncDate = DateTime.Now;
            _configManager.SavePairs(new List<SyncPair>(Pairs));
        }
    }

    // MARK: - Scheduler

    public void ScheduleAll()
    {
        foreach (var cts in _scheduledTasks.Values)
            cts.Cancel();
        _scheduledTasks.Clear();

        foreach (var pair in Pairs.Where(p => p.IsEnabled))
            SchedulePair(pair);
    }

    private void SchedulePair(SyncPair pair)
    {
        var cts = new CancellationTokenSource();
        _scheduledTasks[pair.Id] = cts;
        var token = cts.Token;
        var intervalMs = pair.IntervalMinutes * 60 * 1000;

        _ = Task.Run(async () =>
        {
            try
            {
                // Small delay before first sync
                await Task.Delay(3000, token);
                if (token.IsCancellationRequested) return;

                // Dispatch to UI thread for sync
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => PerformSyncAsync(pair));

                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(intervalMs, token);
                    if (token.IsCancellationRequested) break;
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => PerformSyncAsync(pair));
                }
            }
            catch (TaskCanceledException)
            {
                // Expected when cancelling
            }
        }, token);
    }

    // MARK: - Logs

    private void AppendLog(LogEntry entry)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            Logs.Insert(0, entry);
            while (Logs.Count > 200)
                Logs.RemoveAt(Logs.Count - 1);
        });
    }

    public void ClearLogs()
    {
        Logs.Clear();
        OnPropertyChanged(nameof(Logs));
    }

    // MARK: - Notifications

    public void NotifyStatusChanged()
    {
        OnPropertyChanged(nameof(IsSyncing));
        OnPropertyChanged(nameof(ActivePairCount));
        OnPropertyChanged(nameof(ErrorCount));
        OnPropertyChanged(nameof(Statuses));
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Shutdown()
    {
        foreach (var cts in _scheduledTasks.Values)
            cts.Cancel();
        _scheduledTasks.Clear();
    }
}
